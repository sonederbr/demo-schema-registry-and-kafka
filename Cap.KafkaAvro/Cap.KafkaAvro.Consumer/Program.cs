namespace Cap.KafkaAvro.Consumer
{
    using System;

    using cap.kafkaavro.payment.events;

    using Confluent.Kafka;
    using Confluent.Kafka.SyncOverAsync;
    using Confluent.SchemaRegistry;
    using Confluent.SchemaRegistry.Serdes;

    // Run to generate c# class  avrogen -s PaymentCreated.avsc .
    internal static class Program
    {
        const string Topic = "cap.kafkaavro.payment.paymentcreated";
        const string BootstrapServers = "localhost:9092";
        const string SchemaRegistryUrl = "http://localhost:8085";

        static void Main(string[] args)
        {
            ConsumeSpecific(BootstrapServers, SchemaRegistryUrl);
        }

        /*
            schema.registry.url = "url to the Schema Registry"
            auto.register.schemas = false
            key.deserializer = org.apache.kafka.common.serialization.StringDeserializer
            key.deserializer.encoding = UTF8
            value.deserializer = io.confluent.kafka.serializers.KafkaAvroDeserializer
            value.subject.name.strategy = io.confluent.kafka.serializers.subject.RecordNameStrategy
        */
        static void ConsumeSpecific(string bootstrapServers, string schemaRegistryUrl)
        {
            var color = 2;

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = schemaRegistryUrl
            };

            var consumerConfig = new ConsumerConfig
            {
                GroupId = "cap.kafkaavro.payment.paymentcreated",
                BootstrapServers = bootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);
            using var consumer = new ConsumerBuilder<string, PaymentCreated>(consumerConfig)
                .SetValueDeserializer(new AvroDeserializer<PaymentCreated>(schemaRegistry).AsSyncOverAsync())
                .Build();

            consumer.Subscribe(Topic);

            while (true)
            {
                try
                {
                    SetConsoleForegroundColor(color);

                    var consumeResult = consumer.Consume();

                    Console.WriteLine(
                        $"Schema: {consumeResult.Message.Value.Schema.Name} " +
                        $"Id: {consumeResult.Message.Value.id} | " +
                        $"Amount: {consumeResult.Message.Value.amount} " );
                       // $"ExernalNumber: {consumeResult.Message.Value.externalNumber} ");

                    // { Newtonsoft.Json.JsonConvert.SerializeObject(consumeResult.Message.Value.) }

                    try
                    {
                        consumer.Commit(consumeResult);
                    }
                    catch (KafkaException e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Commit error: {e.Error.Reason}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                catch (ConsumeException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occured: {e.Error.Reason}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                finally
                {
                    color++;
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            static void SetConsoleForegroundColor(int color)
            {
                if (color % 2 == 0)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
            }
        }
    }
}
