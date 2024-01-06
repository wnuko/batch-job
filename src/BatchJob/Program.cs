foreach (var i in Enumerable.Range(1, 10))
{
    if (i > 1)
        Thread.Sleep(TimeSpan.FromMilliseconds(750));

    Console.WriteLine($"{i}. Hello, World!");
}
