using Grpc.Core;
using Todo.Api;
using TodoApi.Application.Interfaces;

namespace TodoApi.GRPC.Services;

public class TodoAnalyticsService(
    ITasksService tasksService,
    ILogger<TodoAnalyticsService> logger) : TodoAnalytics.TodoAnalyticsBase
{
    public override async Task<StatsResponse> GetStats(
        StatsRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("GetStats called");
        
        var activeTasks = await tasksService.GetByStatusAsync("active");
        var completedTasks = await tasksService.GetByStatusAsync("completed");

        logger.LogInformation("Retrieved stats: ActiveTasks={ActiveTasks}, CompletedTasks={CompletedTasks}", 
            activeTasks, completedTasks);

        return new StatsResponse
        {
            ActiveTasks = activeTasks,
            CompletedTasks = completedTasks
        };
    }
}