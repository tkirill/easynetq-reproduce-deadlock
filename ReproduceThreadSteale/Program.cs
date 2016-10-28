using System;
using System.Diagnostics;
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
        private static Stopwatch stopwatch;

        static void Main(string[] args)
        {
            output = File.CreateText("log.txt");
            bus = RabbitHutch.CreateBus("host=localhost");
            random = new Random();
            stopwatch = new Stopwatch();
            using (output)
            using (bus)
            {
                bus.SubscribeAsync<TestMessage>("consumer", Consume);

                stopwatch.Start();
                for (int i = 0; i < NumberOfInitialMessages; i++)
                    bus.Publish(new TestMessage {Number = random.Next(1000)});

                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }

        static async Task Consume(TestMessage msg)
        {
            Console.WriteLine($"[{stopwatch.Elapsed}] Consume {msg.Number}");
            for (int i = 0; i < 10; i++)
            {
                await bus.PublishAsync(new TestMessage {Number = random.Next(1000)});
                await CheckThread();
                await bus.PublishAsync(new TestMessage {Number = random.Next(1000)});
                await CheckThread();
                await bus.PublishAsync(new TestMessage {Number = random.Next(1000)});
                await CheckThread();
            }
        }

        private static async Task CheckThread()
        {
            if (!string.IsNullOrWhiteSpace(Thread.CurrentThread.Name))
            {
                Console.WriteLine($"[{stopwatch.Elapsed}] {Thread.CurrentThread.Name}");
                await output.WriteLineAsync(Thread.CurrentThread.Name);
            }
        }
    }

    class TestMessage
    {
        public int Number { get; set; }
    }
}
