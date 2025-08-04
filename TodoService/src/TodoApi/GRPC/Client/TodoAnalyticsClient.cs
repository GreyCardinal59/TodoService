using TodoApi.Application.Interfaces;

namespace TodoApi.GRPC.Client;

public class TodoAnalyticsClient(
    ITasksService tasksService,
    ILogger<TodoAnalyticsClient> logger) : ITodoAnalyticsClient
{
    public async Task<(int ActiveTasks, int CompletedTasks)> ReturnStats()
    {
        try
        {
            logger.LogInformation("Запрос статистики");
            
            var activeTasks = await tasksService.GetByStatusAsync("active");
            var completedTasks = await tasksService.GetByStatusAsync("completed");
                
            return (activeTasks, completedTasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении статистики");
            return (0, 0);
        }
    }
}

/*
 *
 * Как примерно выглядел бы клиент, если бы запускали не в одном приложении
 * 
 */

// public class TodoAnalyticsClient(
//     IConfiguration configuration,
//     ILogger<TodoAnalyticsClient> logger) : ITodoAnalyticsClient
// {
//     public async Task<(int ActiveTasks, int CompletedTasks)> ReturnStats()
//     {
//         var channel = GrpcChannel.ForAddress(configuration["TodoAnalytics"]);
//         var client = new TodoAnalytics.TodoAnalyticsClient(channel);
//         var request = new StatsRequest();
//         
//         try
//         {
//             var response = await client.GetStats(request);
//             return (response.ActiveTasks, response.CompletedTasks);
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Ошибка при получении статистики");
//             return (0, 0);
//         }
//     }
// }