namespace Lykke.Tools.RabbitMqReader
{
    internal class AppArguments
    {
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        public MessageFormat MessageFormat { get; set; }
        public string MessageSeparator { get; set; }
        public string OutputFilePath { get; set; }
        public bool AppendOutput { get; set; }
    }
}
