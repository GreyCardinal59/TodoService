namespace TodoApi.GRPC.Client;

public interface ITodoAnalyticsClient
{
    Task<(int ActiveTasks, int CompletedTasks)> ReturnStats();
}