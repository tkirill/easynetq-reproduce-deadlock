using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;

namespace ReproduceThreadSteale
{
    class Program
    {
        private const int NumberOfInitialMessages = 10000;

        private static IBus bus;
        private static StreamWriter output;
        private static Random random;

        static void Main(string[] args)
        {
            output = File.CreateText("log.txt");
            bus = RabbitHutch.CreateBus("host=localhost");
            random = new Random();
            using (output)
            using (bus)
            {
                bus.SubscribeAsync<TestMessage>("consumer", Consume);

                for (int i = 0; i < NumberOfInitialMessages; i++)
                    bus.Publish(new TestMessage {Number = random.Next(1000)});

                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }

        static async Task Consume(TestMessage msg)
        {
            Console.WriteLine($"Consume {msg.Number}");
            for (int i = 0; i < 10; i++)
            {
                await bus.PublishAsync(new TestMessage {Number = random.Next(1000)});
                await CheckThread();
                await bus.PublishAsync(new TestMessage { Number = random.Next(1000) });
                await CheckThread();
                await bus.PublishAsync(new TestMessage { Number = random.Next(1000) });
                await CheckThread();
            }
        }

        private static async Task CheckThread()
        {
            if (!string.IsNullOrWhiteSpace(Thread.CurrentThread.Name))
            {
                Console.WriteLine(Thread.CurrentThread.Name);
                await output.WriteLineAsync(Thread.CurrentThread.Name);
            }
        }
    }

    class TestMessage
    {
        public int Number { get; set; }
    }
}
