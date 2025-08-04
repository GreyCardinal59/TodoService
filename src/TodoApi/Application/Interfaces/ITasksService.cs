using TodoApi.Application.Common;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Interfaces;

public interface ITasksService
{
    Task<IReadOnlyList<TaskDto>> GetAllAsync(TodoQueryParameters query, CancellationToken cancellationToken);
    Task<TaskEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> GetByStatusAsync(string status);
    Task CreateAsync(CreateTaskDto task, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, TaskUpdateDto updateDto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
