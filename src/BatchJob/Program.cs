foreach (var i in Enumerable.Range(1, 5))
{
    if (i > 1)
        Thread.Sleep(TimeSpan.FromMilliseconds(750));

    Console.WriteLine($"{i}. Hello, World!");
}

var message = Environment.GetEnvironmentVariable("MESSAGE");
if (!string.IsNullOrWhiteSpace(message))
    Console.WriteLine($"Message -> {message}");
