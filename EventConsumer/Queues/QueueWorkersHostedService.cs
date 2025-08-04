using Microsoft.Extensions.Options;

namespace EventConsumer.Queues
{
    public class QueueWorkersHostedService(IOptions<RabbitMqServerSettings> consumerSettings, ConsumerConfig config, HttpClient httpClient, ILogger<QueueWorker> logger) : BackgroundService
    {
        private readonly List<Task> _workerTasks = [];

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var queuePair in config.Queues)
            {
                var worker = new QueueWorker(consumerSettings, queuePair, httpClient, logger, stoppingToken);
                _workerTasks.Add(worker.StartAsync());
            }
            return Task.WhenAll(_workerTasks);
        }
    }
}
