# Lykke.Tools.RabbitMqReader
Tool to read messages from the RabbitMq exchange in the testing purpose. Using this tool, you can specify a RabbitMq host uri, exchange and topic to read from. Received data will be displayed in the console and optionally can be saved to the file.

## Download

You cand download the latest binaries by this [link](https://github.com/LykkeCity/Lykke.Tools.RabbitMqReader/releases/download/v1.0.0/Lykke.Tools.RabbitMqReader-v1.0.0.zip).
You can found all versions in the [Releases](https://github.com/LykkeCity/Lykke.Tools.RabbitMqReader/releases). 

## Run

To run this tool, you should have [.NetCore runtime](https://www.microsoft.com/net/download/windows) installed on your machine.

To run the tool you need to type ```dotnet Lykke.Tools.RabbitMqReader.dll <options>``` in the console.

Awailable options:

```
 -a: Append output file. Optional, default is false
 -c <connection-string>: Amqp connections string. Required
 -e <exchange>: Exchange name. Required
 -f <format:{json, messagepack}>: Message format. Optional, default is json
 -o <file>: Output file path. Optional, default is empty
 -s <separator>: Message separation text. Optional, default is empty
```

Run example:

```
-c amqp://user:password@rabbit-host:5672 -e lykke.candles -f json -s *** -o log.txt -a
```
