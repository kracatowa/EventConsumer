namespace EventConsumer.Queues
{
    public class ConsumerConfig
    {
        public required List<QueuePair> Queues { get; set; }
    }
}
