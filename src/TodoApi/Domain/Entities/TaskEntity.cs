using TodoApi.Domain.Enums;

namespace TodoApi.Domain.Entities;

public class TaskEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoStatus Status { get; set; } = TodoStatus.Active;
    public DateTime CreatedAt { get; set; }
}
