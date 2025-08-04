using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using TodoApi.Application.Common;
using TodoApi.Application.Interfaces;
using TodoApi.Application.Services;
using TodoApi.GRPC.Client;
using TodoApi.GRPC.Services;
using TodoApi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory { Uri = new Uri(builder.Configuration.GetConnectionString("Rabbit")) };
    return factory.CreateConnection();
});

builder.Services.AddScoped<ITasksService, TasksService>();

builder.Services.AddScoped<ITodoAnalyticsClient, TodoAnalyticsClient>();

builder.Services.AddScoped<TodoAnalyticsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseMiddleware<ValidationExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<TodoAnalyticsService>();

PrepDb.PrepPopulation(app);

app.Run();