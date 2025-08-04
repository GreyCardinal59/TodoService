using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Common;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;

namespace TodoApi.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(ITasksService tasksService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] TodoQueryParameters query, CancellationToken cancellationToken)
    {
        var todos = await tasksService.GetAllAsync(query, cancellationToken);
        return Ok(todos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id, CancellationToken cancellationToken)
    {
        var todo = await tasksService.GetByIdAsync(id, cancellationToken);
        return Ok(todo);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTaskDto taskDto, CancellationToken cancellationToken)
    {
        await tasksService.CreateAsync(taskDto, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, [FromBody] TaskUpdateDto updateDto, CancellationToken cancellationToken)
    {
        await tasksService.UpdateAsync(id, updateDto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id, CancellationToken cancellationToken)
    {
        await tasksService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
