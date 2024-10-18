using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Polly;
using Polly.Retry;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScopedFeatureManagement(builder.Configuration)
    .AddFeatureFilter<PercentageFilter>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<DelegatingMessageHandlerTransientFeatureFlag>();
builder.Services.AddTransient<DelegatingMessageHandlerScopedFeatureFlagGoneWrong>();
builder.Services.AddTransient<DelegatingMessageHandlerScopedFeatureFlagCorrect>();

builder.Services.AddScoped<ScopedClass>();
builder.Services.AddHttpClient<ITodoClient, TodoClient>()
    .SetHandlerLifetime(TimeSpan.FromSeconds(30))
    .AddHttpMessageHandler<DelegatingMessageHandlerTransientFeatureFlag>()
    // .AddHttpMessageHandler<DelegatingMessageHandlerScopedFeatureFlagGoneWrong>()
    // .AddHttpMessageHandler<DelegatingMessageHandlerScopedFeatureFlagCorrect>()
    ;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/todoitem", async (ITodoClient todoClient, ScopedClass scopedClass) =>
    {
        Console.WriteLine($"Scoped Guid from controller {scopedClass.GuidString.ToString()}");
        var todoItemTasks = Enumerable.Range(0, 6).Select(x => todoClient.GetTodoItem()).ToArray();
        return await Task.WhenAll(todoItemTasks);
    })
.WithName("GetTodoitem")
.WithOpenApi();

app.Run();
