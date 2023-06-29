// ReSharper disable AccessToDisposedClosure
namespace Demo;

using System;
using Nurseries;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello! Keep pressing enter to spawn child tasks. Press any other key when you're done");
        using var nursery = new Nursery();
        var sleepUntil = DateTime.Now;
        var random = new Random();
        while (Console.ReadKey(true).Key == ConsoleKey.Enter)
        {
            if (sleepUntil <= DateTime.Now)
            {
                sleepUntil = DateTime.Now;
            }
            sleepUntil += TimeSpan.FromSeconds(5);
            var delay = sleepUntil - DateTime.Now;

            nursery.StartWithRetry(
                () =>
                {
                    if (random.Next() % 10 == 0)
                    {
                        Console.WriteLine("Uh oh! Something bad is going to happen!");
                        throw new Exception("Uh oh! Something bad happened");
                    }
                    Console.WriteLine($"Sleeping for {delay}...");
                    nursery.Sleep(delay);
                    Console.WriteLine("Done sleeping");
                },
                exception =>
                {
                    Console.WriteLine($"An exception was thrown: {exception.Message}");
                    if (exception is OperationCanceledException)
                        return false;
                    if (random.Next() % 2 != 0)
                        return false;
                    Console.WriteLine("But don't worry! We recovered from it");
                    return true;
                }
            );
        }
        Console.WriteLine("Waiting for all child tasks to complete...");
    }
}