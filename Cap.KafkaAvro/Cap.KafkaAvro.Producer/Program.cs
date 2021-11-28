namespace Cap.KafkaAvro.Producer
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using cap.kafkaavro.payment.events;

    using Confluent.Kafka;
    using Confluent.SchemaRegistry;
    using Confluent.SchemaRegistry.Serdes;

    internal static class Program
    {
        const string Topic = "cap.kafkaavro.payment.paymentcreated";
        const string BootstrapServers = "localhost:9092";
        const string SchemaRegistryUrl = "http://localhost:8085";

        static async Task Main(string[] args)
        {
            await ProduceGenericAsync(BootstrapServers, SchemaRegistryUrl);
        }

        /*
            schema.registry.url = "url to the Schema Registry"
            auto.register.schemas = false
            key.serializer = org.apache.kafka.common.serialization.StringSerializer
            key.serializer.encoding = UTF8
            value.serializer = io.confluent.kafka.serializers.KafkaAvroSerializer
            value.subject.name.strategy = io.confluent.kafka.serializers.subject.RecordNameStrategy
         */
        async static Task ProduceGenericAsync(string bootstrapServers, string schemaRegistryUrl)
        {
            var color = 2;

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = schemaRegistryUrl
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            var avroSerializerConfig = new AvroSerializerConfig
            {
                AutoRegisterSchemas = false,
                SubjectNameStrategy = SubjectNameStrategy.Record
            };

            using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);
            using var producer = new ProducerBuilder<Null, PaymentCreated>(producerConfig)
                .SetValueSerializer(new AvroSerializer<PaymentCreated>(schemaRegistry, avroSerializerConfig))
                .Build();

            while (true)
            {
                try
                {
                    SetConsoleForegroundColor(color);

                    var paymentCreated = new PaymentCreated
                    {
                        id = GetRandomStringId(10000, 99999),
                        amount = GetRandomDubleNumber(10000, int.MaxValue, 4),
                        // externalNumber = GetRandomNumber(10000, int.MaxValue),
                        //tags = new List<string>() { $"tag{GetRandomNumber(10, 99)}", $"tag{GetRandomNumber(10, 99)}"}
                    };

                    await producer
                        .ProduceAsync(Topic, new Message<Null, PaymentCreated> { Value = paymentCreated })
                        .ContinueWith(task =>
                            Console.WriteLine(task.IsFaulted ?
                                $"Error producing message: {task.Exception.Message}" :
                                $"Message Produced - Partition {task.Result.Partition} | Offset [{task.Result.Offset}] | " +
                                $"PaymentCreated [ id: {paymentCreated.id} amount: {paymentCreated.amount} ]"));

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    color++;
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            static string GetRandomStringId(int min, int max) =>
                $"{ Guid.NewGuid().ToString().Substring(0, 5).ToUpper() }{ GetRandomNumber(min, max) }";

            static int GetRandomNumber(int min, int max) =>
                new Random().Next(min, max);

            static double GetRandomDubleNumber(int min, int max, int decimalPlaces) =>
                Convert.ToDouble((new Random().NextDouble() * (max - min) + min).ToString("f" + decimalPlaces));

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
