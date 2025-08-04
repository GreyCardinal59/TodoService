using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Data;

public class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var logger = serviceScope.ServiceProvider.GetService<ILogger<PrepDb>>();
        var context = serviceScope.ServiceProvider.GetService<AppDbContext>();
        
        // ApplyMigrations(context, logger);
        
        SeedData(context, logger);
    }

    // private static void ApplyMigrations(AppDbContext context, ILogger<PrepDb> logger)
    // {
    //     try
    //     {
    //         var pendingMigrations = context.Database.GetPendingMigrations();
    //
    //         if (pendingMigrations.Any())
    //         {
    //             logger.LogInformation("Applying migrations...");
    //
    //             context.Database.Migrate();
    //
    //             logger.LogInformation("--> Migrations applied");
    //         }
    //         else
    //         {
    //             logger.LogInformation("--> No migrations to apply.");
    //
    //             if (!context.Database.CanConnect())
    //             {
    //                 logger.LogInformation("--> Database doesn't exist - creating...");
    //
    //                 context.Database.EnsureCreated();
    //
    //                 logger.LogInformation("--> Database created.");
    //             }
    //         }
    //     }
    //     catch(Exception ex)
    //     {
    //         logger.LogError(ex, "An error occurred while applying migrations.");
    //         throw;
    //     }
    // }

    private static void SeedData(AppDbContext context, ILogger<PrepDb> logger)
    {
        if (!context.Todos.Any())
        {
            logger.LogInformation("--> Seeding Data...");
            
            context.Todos.AddRange(
                new TaskEntity { Title = "Изучить C#", Description = "Async/await", Status = "active", CreatedAt = DateTime.UtcNow },
                new TaskEntity { Title = "Прочитать книгу", Description = "ASP.NET Core в действии", Status = "active", CreatedAt = DateTime.UtcNow + TimeSpan.FromSeconds(1) },
                new TaskEntity { Title = "Купить пиво", Description = "Лидское аксамитное", Status = "completed", CreatedAt = DateTime.UtcNow + TimeSpan.FromSeconds(2) },
                new TaskEntity { Title = "Выполнить тестовое", Description = "Разработать микросервис для управления задачами", Status = "active", CreatedAt = DateTime.UtcNow + TimeSpan.FromSeconds(3)},
                new TaskEntity { Title = "Скатать в доту", Description = "На шамане", Status = "completed", CreatedAt = DateTime.UtcNow + TimeSpan.FromSeconds(4)}
            );

            context.SaveChanges();
        }
        else
        {
            logger.LogInformation("--> We already have data");
        }
    }
}