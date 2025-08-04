using TodoApi.Domain.Enums;

namespace TodoApi.Application.DTOs;

public class TaskUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoStatus Status { get; set; }
}
