namespace EventConsumer.Queue
{
    public class QueuePair
    {
        public required string QueueName { get; set; }
        public required string ApiEndpoint { get; set; }
    }
}
