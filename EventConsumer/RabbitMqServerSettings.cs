namespace EventConsumer
{
    public class RabbitMqServerSettings
    {
        public required string Server { get; set; }
        public required int ServerPort { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
