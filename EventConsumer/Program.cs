using EventConsumer.Queues;
using RabbitMQ.Client;
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
            builder.Services.AddHostedService<QueueWorkersHostedService>();
            builder.Services.AddLogging();  

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            await TestRabbitMQConnection(app.Configuration, logger);

            await app.RunAsync();
        }

        private static async Task TestRabbitMQConnection(IConfiguration configuration, ILogger logger)
        {
            var rabbitMqServerSettings = configuration.GetSection("RabbitMqServerSettings").Get<RabbitMqServerSettings>();

            if (rabbitMqServerSettings == null)
            {
                logger.LogError("RabbitMqServerSettings are not configured properly.");
                return;
            }

            var factory = new ConnectionFactory()
            {
                HostName = rabbitMqServerSettings.Server,
                Port = rabbitMqServerSettings.ServerPort,
                UserName = rabbitMqServerSettings.Username,
                Password = rabbitMqServerSettings.Password
            };

            try
            {
                using var connection = await factory.CreateConnectionAsync();
                logger.LogInformation("Successfully connected to RabbitMQ server at {rabbitMqServerSettings.Server}.", rabbitMqServerSettings.Server);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to RabbitMQ server at {rabbitMqServerSettings.Server}.", rabbitMqServerSettings.Server);
            }
        }
    }
}
