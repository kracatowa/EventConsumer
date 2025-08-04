namespace EventConsumer.Queues
{
    public class QueuePair
    {
        public required string QueueName { get; set; }
        public required string ApiEndpoint { get; set; }
    }
}
