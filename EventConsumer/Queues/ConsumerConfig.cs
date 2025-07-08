namespace EventConsumer.Queue
{
    public class ConsumerConfig
    {
        public required List<QueuePair> Queues { get; set; }
    }
}
