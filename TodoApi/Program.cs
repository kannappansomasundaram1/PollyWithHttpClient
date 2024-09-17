using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Polly;
using Polly.Retry;
using PollyWithHttpClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScopedFeatureManagement(builder.Configuration)
    .AddFeatureFilter<PercentageFilter>();

builder.Services.AddStackExchangeRedisCache(setupAction: options => { options.Configuration = "127.0.0.1:6379"; });
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<DistributedCachingHandler>();
builder.Services.AddHttpClient<ITodoClient, TodoClient>()
    .ConfigureHttpClient((sp, c) => { c.Timeout = TimeSpan.FromSeconds(50); })
    .AddHttpMessageHandler<DistributedCachingHandler>()
    .AddResilienceHandler("TodoClientResilience", (builder, c) =>
    {
        builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(1),
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<OperationCanceledException>(),
            OnRetry = static args =>
            {
                Console.WriteLine("Retry, Attempt: {0}", args.AttemptNumber);
                return default;
            }
        });
        /*builder.AddChaosOutcome(0.5, () =>
        {
            Console.WriteLine("Chaos Interception");
            throw new OperationCanceledException();
        });*/
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/todoitem", async (ITodoClient todoClient) =>
    {
        var todoItemTasks = Enumerable.Range(0, 10).Select(x => todoClient.GetTodoItem()).ToArray();
        return await Task.WhenAll(todoItemTasks);
    })
.WithName("GetTodoitem")
.WithOpenApi();

app.Run();

public interface ITodoClient
{
    Task<TodoItem?> GetTodoItem();
}

public record TodoItem(
    int Id,
    string Todo,
    bool Completed,
    int UserId
);

public class TodoClient(HttpClient httpClient) : ITodoClient
{
    public async Task<TodoItem?> GetTodoItem()
    {
        return await httpClient.GetFromJsonAsync<TodoItem>("https://dummyjson.com/todos");
    }
}
