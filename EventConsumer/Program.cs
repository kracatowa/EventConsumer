using EventConsumer.Queue;
using EventConsumer.Queues;
using System.Text.Json;

namespace EventConsumer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register HttpClient and configuration  
            builder.Services.AddSingleton(provider =>
            {
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(5)
                };
                return httpClient;
            });

            builder.Services.AddSingleton(provider =>
            {
                var config = JsonSerializer.Deserialize<ConsumerConfig>(File.ReadAllText("config.json"));
                return config ?? throw new InvalidOperationException("Configuration could not be loaded.");
            });

            builder.Services.Configure<RabbitMqServerSettings>(builder.Configuration.GetSection("RabbitMqServerSettings"));

            // Register a QueueWorker for each queue as a hosted service  
            builder.Services.AddHostedService<QueueWorkersHostedService>();

            var app = builder.Build();

            await app.RunAsync();
        }
    }
}
