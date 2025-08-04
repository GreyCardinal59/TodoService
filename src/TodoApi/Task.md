# Тестовое задание для Junior C# Backend Developer

## Задача
Разработать микросервис для управления задачами (Todo API) с использованием .NET Core, PostgreSQL и Docker.

## Технологии
- .NET Core 6+
- PostgreSQL
- Entity Framework Core (Code-First)
- Docker
- Опционально: Redis, RabbitMQ, gRPC

## Базовые требования

### Функционал API
- **CRUD операции для задач:**
  ```json
  {
    "id": 1,
    "title": "Изучить C#",
    "description": "Async/await",
    "status": "active", // или "completed"
    "created_at": "2024-06-10T12:00:00Z"
  }
  ```
- Поиск задач по названию (регистронезависимый)
- Фильтрация по статусу (active, completed)
- Пагинация для GET-методов
- Асинхронные методы контроллеров

### Инфраструктура
- Docker-контейнеризация приложения и БД
- Автоматическое применение миграций EF Core при запуске
- Docker Compose для запуска стека

## Дополнительные задания (выполнение повышает оценку)
### Вариант A: Redis (кеширование)
- Закешировать ответы GET-методов:
    - GET /api/tasks - кеш на 5 минут
    - Инвалидация кеша при изменении данных (POST/PUT/DELETE)

### Вариант B: RabbitMQ (асинхронная обработка)
- Отправка события в RabbitMQ при изменении статуса задачи:

```csharp
Publish("task.status.changed", new { TaskId = 1, NewStatus = "completed" });
```
### Вариант C: gRPC (интеграция)
- gRPC-сервис для статистики:

```proto
service TodoAnalytics {
  rpc GetStats (StatsRequest) returns (StatsResponse);
}
message StatsResponse {
  int32 active_tasks = 1;
  int32 completed_tasks = 2;
}
```
### Вариант D: Unit-тесты
- Покрытие тестами (xUnit/NUnit):
    - Контроллеры (минимум 70% coverage)
    - Сервисы (логику изменения статуса)

## Требуемая инфраструктура
###Docker Compose
Пример ```docker-compose.yml```:

```yaml
services:
  api:
    build: .
    ports: ["8080:80"]
    depends_on: 
      - postgres
    environment:
      ConnectionStrings__Default: "Host=postgres;Database=todo;Username=postgres;Password=password"

  postgres:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: "password"
      POSTGRES_DB: "todo"
```
## Что предоставить
### 1. GitHub-репозиторий с:
- Исходным кодом приложения
- ```Dockerfile```
- ```docker-compose.yml```
### 2. README.md с:
- Инструкцией по запуску
- Описанием реализованных дополнительных заданий
- Примеры запросов (cURL/Postman)
- Объяснением архитектурных решений





using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;

namespace Todo.Api;

public class AppDbContext : DbContext
{
public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskEntity> Todos { get; set; }
}

public class TodoQueryParameters
{
public string? Title { get; set; }
public string? Status { get; set; }
public int Page { get; set; } = 1;
public int PageSize { get; set; } = 10;
}


public interface ITasksService
{
Task<IReadOnlyList<TaskDto>> GetAllAsync(TodoQueryParameters query);
Task<TaskEntity?> GetByIdAsync(int id);
Task CreateAsync(CreateTaskDto task, CancellationToken cancellationToken);
Task<bool> UpdateAsync(int id, TaskUpdateDto updateDto, CancellationToken cancellationToken);
Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}

public class TasksService : ITasksService
{
private readonly AppDbContext _context;
private readonly IConnection _rabbitConnection;
private readonly ILogger<TasksService> _logger;
private readonly IDistributedCache _redis;

    private const string CacheKey = "todos:all";

    public TasksService(AppDbContext context, IDistributedCache redis, IConnection rabbitConnection, ILogger<TasksService> logger)
    {
        _context = context;
        _redis = redis;
        _rabbitConnection = rabbitConnection;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(TodoQueryParameters query)
    {
        var cached = await _redis.GetStringAsync(CacheKey);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<TaskDto>>(cached)!;
        }

        var todos = _context.Todos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Title))
            todos = todos.Where(t => t.Title.ToLower().Contains(query.Title.ToLower()));

        if (!string.IsNullOrWhiteSpace(query.Status))
            todos = todos.Where(t => t.Status.ToLower() == query.Status.ToLower());

        var result = await todos
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var serialized = JsonSerializer.Serialize(result);
        await _redis.SetStringAsync(CacheKey, serialized, 
            new DistributedCacheEntryOptions { AbsoluteExpiration =  DateTimeOffset.Now.AddMinutes(10) });

        return result;
    }

    public async Task<TaskEntity?> GetByIdAsync(int id)
    {
        return await _context.Todos.FindAsync(id);
    }

    public async Task CreateAsync(CreateTaskDto task, CancellationToken cancellationToken)
    {
        var taskEntity = new TaskEntity
        {
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Todos.Add(taskEntity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, TaskUpdateDto updated, CancellationToken cancellationToken)
    {
        var oldTodo = await _context.Todos
            .Where(t => t.Id == id)
            .Select(t => new { t.Id, t.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (oldTodo is null)
            return false; 
        
        var updatedRows = await _context.Todos
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Title, updated.Title)
                .SetProperty(t => t.Description, updated.Description)
                .SetProperty(t => t.Status, updated.Status), cancellationToken);
        
        if (updatedRows == 0)
            return false;

        await _context.SaveChangesAsync(cancellationToken);
        await _redis.RemoveAsync(CacheKey, cancellationToken);
        
        if (oldTodo.Status != updated.Status)
            await PublishStatusChange(oldTodo.Id, updated.Status);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var todo = await _context.Todos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        
        if (todo is null)
            return false;

        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync(cancellationToken);
        
        await _redis.RemoveAsync(CacheKey, cancellationToken);
        return true;
    }

    private async Task PublishStatusChange(int taskId, string newStatus)
    {
        try
        {
            await using var channel = await _rabbitConnection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync("task.status.changed", ExchangeType.Fanout, durable: true);

            var payload = JsonSerializer.Serialize(new { TaskId = taskId, NewStatus = newStatus });
            var body = Encoding.UTF8.GetBytes(payload);

            await channel.BasicPublishAsync(exchange: "task.status.changed", routingKey: "", body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish to RabbitMQ");
        }
    }
}


[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
private readonly ITasksService _tasksService;

    public TasksController(ITasksService tasksService)
    {
        _tasksService = tasksService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] TodoQueryParameters query)
    {
        var todos = await _tasksService.GetAllAsync(query);
        return Ok(todos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var todo = await _tasksService.GetByIdAsync(id);
        return Ok(todo);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTodo(CreateTaskDto taskDto, CancellationToken cancellationToken)
    {
        await _tasksService.CreateAsync(taskDto, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, TaskUpdateDto updateDto, CancellationToken cancellationToken)
    {
        await _tasksService.UpdateAsync(id, updateDto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id, CancellationToken cancellationToken)
    {
        await _tasksService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

public class TaskEntity
{
public int Id { get; set; }
public string Title { get; set; }
public string Description { get; set; }
public string Status  { get; set; }
public DateTime CreatedAt { get; set; }
}

public class TaskDto
{
public int Id { get; set; }
public string Title { get; set; }
public string Description { get; set; }
public string Status { get; set; }
public DateTime CreatedAt { get; set; }
}

public class CreateTaskDto
{
public string Title { get; set; }
public string Description { get; set; }
public string Status { get; set; }
}

public class TaskUpdateDto
{
public string Title { get; set; }
public string Description { get; set; }
public string Status { get; set; }
}