using EventConsumer.Queue;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace EventConsumer.Queues
{
    public class QueueWorker(IOptions<RabbitMqServerSettings> consumerSettingsOptions, QueuePair queuePair, HttpClient httpClient, CancellationToken cancellationToken)
    {
        public async Task StartAsync()
        {
            var consumerSettings = consumerSettingsOptions.Value;

            Console.WriteLine($"Starting QueueWorker for queue: {queuePair.QueueName}");

            var factory = new ConnectionFactory()
            {
                HostName = consumerSettings.Server,
                Port = consumerSettings.ServerPort,
                UserName = consumerSettings.Username,
                Password = consumerSettings.Password
            };
            Console.WriteLine($"Connecting to RabbitMQ server at {consumerSettings.Server}:{consumerSettings.ServerPort}");

            var connection = await factory.CreateConnectionAsync();
            Console.WriteLine("RabbitMQ connection established.");

            using var channel = await connection.CreateChannelAsync();
            Console.WriteLine($"Channel created for queue: {queuePair.QueueName}");

            await channel.QueueDeclareAsync(
                queue: queuePair.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            Console.WriteLine($"Queue declared: {queuePair.QueueName}");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"[{queuePair.QueueName}] Received message: {message}");

                var response = await httpClient.PostAsync(
                    queuePair.ApiEndpoint,
                    new StringContent(message, Encoding.UTF8, "application/json"),
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{queuePair.QueueName}] Message processed successfully: {message}");
                }
                else
                {
                    Console.WriteLine($"[{queuePair.QueueName}] Failed to process message: {message}. Response: {response.StatusCode}");
                }
            };

            await channel.BasicConsumeAsync(
                queue: queuePair.QueueName,
                autoAck: true,
                consumer: consumer);
            Console.WriteLine($"Started consuming messages from queue: {queuePair.QueueName}");

            // Keep the worker alive until cancellation is requested  
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"QueueWorker for queue {queuePair.QueueName} is shutting down gracefully.");
            }
        }
    }
}
