using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Fclp;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;

namespace Lykke.Tools.RabbitMqReader
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var appArguments = TryGetAppArguments(args);

            if (appArguments == null)
            {
                return;
            }

            var filter = TryCreateFilter(appArguments);
            var outputWriter = TryGetOutputWriter(appArguments);
            var subscriber = TrySubscribe(appArguments, filter, outputWriter);

            if (subscriber == null)
            {
                return;
            }

            Console.WriteLine("Press any key to exit");
            
            Console.ReadKey(true);

            subscriber.Stop();
            subscriber.Dispose();

            if (outputWriter != null)
            {
                await outputWriter.FlushAsync();
                outputWriter.Close();
                outputWriter.Dispose();
            }
        }

        private static Regex TryCreateFilter(AppArguments appArguments)
        {
            if (appArguments.Filter == null)
            {
                return null;
            }

            Console.WriteLine($"Filter: \"{appArguments.Filter}\"");

            return new Regex(appArguments.Filter, RegexOptions.Compiled);
        }

        private static IStopable TrySubscribe(AppArguments appArguments, Regex filter, StreamWriter outputWriter)
        {
            Console.WriteLine("Subscribing...");

            try
            {
                var settings = new RabbitMqSubscriptionSettings
                {
                    ConnectionString = appArguments.ConnectionString,
                    ExchangeName = appArguments.ExchangeName,
                    QueueName = $"{appArguments.ExchangeName}.RabbitMqReader-{Guid.NewGuid()}",
                    IsDurable = false,
                    ReconnectionsCountToAlarm = -1,
                    ReconnectionDelay = TimeSpan.FromSeconds(5)
                };
                var subscriber = new RabbitMqSubscriber<object>(
                        settings,
                        new DefaultErrorHandlingStrategy(new LogToConsole(), settings))
                    .CreateDefaultBinding()
                    .SetLogger(new LogToConsole())
                    .SetMessageDeserializer(GetDeserializer(appArguments.MessageFormat))
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(
                        message =>
                        {
                            var userMessage = JsonConvert.SerializeObject(message, Formatting.Indented);

                            if (filter != null)
                            {
                                if (!filter.IsMatch(userMessage))
                                {
                                    return Task.CompletedTask;
                                }
                            }

                            Console.WriteLine(userMessage);
                            
                            outputWriter?.WriteLine(userMessage);

                            if (appArguments.MessageSeparator != null)
                            {
                                Console.WriteLine(appArguments.MessageSeparator);
                                outputWriter?.WriteLine(appArguments.MessageSeparator);
                            }

                            return Task.CompletedTask;
                        })
                    .Start();

                Console.WriteLine("Subscriber is started");
                
                return subscriber;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to subscribe: {e.Message}");

                return null;
            }
        }

        private static IMessageDeserializer<object> GetDeserializer(MessageFormat format)
        {
            switch (format)
            {
                case MessageFormat.Json:
                    return new JsonMessageDeserializer<object>();
                case MessageFormat.MessagePack:
                    return new MessagePackMessageDeserializer<object>();
                default:
                    throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(MessageFormat));

            }
        }

        private static StreamWriter TryGetOutputWriter(AppArguments appArguments)
        {
            if (string.IsNullOrWhiteSpace(appArguments.OutputFilePath))
            {
                return null;
            }

            var fileStream = File.Open(
                appArguments.OutputFilePath,
                appArguments.AppendOutput ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.Read);

            return new StreamWriter(fileStream, Encoding.UTF8, bufferSize: 16, leaveOpen: false);
        }

        private static AppArguments TryGetAppArguments(string[] args)
        {
            var parser = new FluentCommandLineParser<AppArguments>();

            parser.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));

            parser.Setup(x => x.ConnectionString)
                .As('c')
                .Required()
                .WithDescription("-c <connection-string>: Amqp connections string. Required");

            parser.Setup(x => x.ExchangeName)
                .As('e')
                .Required()
                .WithDescription("-e <exchange>: Exchange name. Required");

            parser.Setup(x => x.MessageFormat)
                .As('f')
                .SetDefault(MessageFormat.Json)
                .WithDescription("-f <format:{json, messagepack}>: Message format. Optional, default is json");

            parser.Setup(x => x.OutputFilePath)
                .As('o')
                .SetDefault(null)
                .WithDescription("-o <file>: Output file path. Optional, default is empty");

            parser.Setup(x => x.AppendOutput)
                .As('a')
                .SetDefault(false)
                .WithDescription("-a: Append output file. Optional, default is false");

            parser.Setup(x => x.MessageSeparator)
                .As('s')
                .SetDefault(null)
                .WithDescription("-s <separator>: Message separation text. Optional, default is empty");

            parser.Setup(x => x.Filter)
                .As("filter")
                .SetDefault(null)
                .WithDescription("--filter <regexp>: Message filter regexp. Example: \"EURUSD\"");

            var parsingResult = parser.Parse(args);

            if (!parsingResult.HasErrors)
            {
                return parser.Object;
            }

            Console.WriteLine("Lykke Wamp Reader (c) 2017");
            Console.WriteLine("Usage:");

            parser.HelpOption.ShowHelp(parser.Options);

            return null;
        }
    }
}
