using FluentValidation;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Validators;

public class TaskUpdateDtoValidator : AbstractValidator<TaskUpdateDto>
{
    public TaskUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок задачи не может быть пустым")
            .MaximumLength(100).WithMessage("Заголовок задачи не может быть длиннее 100 символов")
            .MinimumLength(3).WithMessage("Заголовок задачи должен содержать минимум 3 символа");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Описание задачи не может быть длиннее 500 символов");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Статус должен быть одним из: Active, Completed, Pending");
    }
} 