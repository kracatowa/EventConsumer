using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace EventConsumer.Queues
{
    public class QueueWorker(IOptions<RabbitMqServerSettings> consumerSettingsOptions, QueuePair queuePair, HttpClient httpClient, ILogger<QueueWorker> logger, CancellationToken cancellationToken)
    {
        public async Task StartAsync()
        {
            var consumerSettings = consumerSettingsOptions.Value;

            logger.LogInformation("Starting QueueWorker for queue: {QueueName}", queuePair.QueueName);

            var factory = new ConnectionFactory()
            {
                HostName = consumerSettings.Server,
                Port = consumerSettings.ServerPort,
                UserName = consumerSettings.Username,
                Password = consumerSettings.Password
            };
            logger.LogInformation("Connecting to RabbitMQ server at {Server}:{Port}", consumerSettings.Server, consumerSettings.ServerPort);

            var connection = await factory.CreateConnectionAsync();
            logger.LogInformation("RabbitMQ connection established.");

            using var channel = await connection.CreateChannelAsync();
            logger.LogInformation("Channel created for queue: {QueueName}", queuePair.QueueName);

            await channel.QueueDeclareAsync(
                queue: queuePair.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            logger.LogInformation("Queue declared: {QueueName}", queuePair.QueueName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                logger.LogInformation("[{QueueName}] Received message: {Message}", queuePair.QueueName, message);

                var response = await httpClient.PostAsync(
                    queuePair.ApiEndpoint,
                    new StringContent(message, Encoding.UTF8, "application/json"),
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("[{QueueName}] Message processed successfully: {Message}", queuePair.QueueName, message);
                }
                else
                {
                    logger.LogWarning("[{QueueName}] Failed to process message: {Message}. Response: {StatusCode}", queuePair.QueueName, message, response.StatusCode);
                }
            };

            await channel.BasicConsumeAsync(
                queue: queuePair.QueueName,
                autoAck: true,
                consumer: consumer);
            logger.LogInformation("Started consuming messages from queue: {QueueName}", queuePair.QueueName);

            // Keep the worker alive until cancellation is requested  
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("QueueWorker for queue {QueueName} is shutting down gracefully.", queuePair.QueueName);
            }
        }
    }
}
