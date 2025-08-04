using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using TodoApi.Application.Common;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Application.Services;

public class TasksService(
    AppDbContext context,
    IDistributedCache redis,
    IConnection rabbitConnection,
    ILogger<TasksService> logger)
    : ITasksService
{
    private const string CacheKey = "todos:all";

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(TodoQueryParameters query, CancellationToken cancellationToken)
    {
        var cached = await redis.GetStringAsync(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<TaskDto>>(cached)!;
        }

        var todos = context.Todos.AsNoTracking();

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
            .ToListAsync(cancellationToken);

        var serialized = JsonSerializer.Serialize(result);
        await redis.SetStringAsync(CacheKey, serialized,
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10) }, cancellationToken);

        return result;
    }

    public async Task<TaskEntity?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Todos.FindAsync(id, cancellationToken);
    }
    
    public async Task<int> GetByStatusAsync(string status)
    {
        var result = await context.Todos.Where(t => t.Status.ToLower() == status.ToLower()).ToListAsync();
        return result.Count;
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

        context.Todos.Add(taskEntity);
        await context.SaveChangesAsync(cancellationToken);
        // Инвалидация кэша после создания задачи
        await redis.RemoveAsync(CacheKey, cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, TaskUpdateDto updated, CancellationToken cancellationToken)
    {
        var oldTodo = await context.Todos
            .Where(t => t.Id == id)
            .Select(t => new { t.Id, t.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (oldTodo is null)
            return false;

        var updatedRows = await context.Todos
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Title, updated.Title)
                .SetProperty(t => t.Description, updated.Description)
                .SetProperty(t => t.Status, updated.Status), cancellationToken);

        if (updatedRows == 0)
            return false;

        await context.SaveChangesAsync(cancellationToken);
        await redis.RemoveAsync(CacheKey, cancellationToken);

        if (oldTodo.Status != updated.Status)
            await PublishStatusChange(oldTodo.Id, updated.Status);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var todo = await context.Todos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (todo is null)
            return false;

        context.Todos.Remove(todo);
        await context.SaveChangesAsync(cancellationToken);

        await redis.RemoveAsync(CacheKey, cancellationToken);
        return true;
    }

    private async Task PublishStatusChange(int taskId, string newStatus)
    {
        try
        {
            using var channel = rabbitConnection.CreateModel();
            channel.ExchangeDeclare("task.status.changed", ExchangeType.Fanout, durable: true);

            var payload = JsonSerializer.Serialize(new { TaskId = taskId, NewStatus = newStatus });
            var body = Encoding.UTF8.GetBytes(payload);

            channel.BasicPublish(exchange: "task.status.changed", routingKey: "", body: body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish to RabbitMQ");
        }
    }
}
