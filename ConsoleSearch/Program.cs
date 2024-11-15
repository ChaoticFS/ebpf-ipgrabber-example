namespace ConsoleSearch;
class Program
{
    static void Main(string[] args)
    {
        // new App().Run();
        var app = new App();
        app.RunAsync().GetAwaiter().GetResult();
    }
}
