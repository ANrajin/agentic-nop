namespace NopCommerceAgents.Services;

public static class ConsoleOutput
{
    public static void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ {message}");
        Console.ResetColor();
    }

    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠️  {message}");
        Console.ResetColor();
    }

    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {message}");
        Console.ResetColor();
    }

    public static void Info(string message)
    {
        Console.WriteLine($"ℹ️  {message}");
    }
    
    public static void Step(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{message}");
        Console.ResetColor();
    }

    public static void Header(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(new string('=', message.Length + 4));
        Console.WriteLine($"= {message} =");
        Console.WriteLine(new string('=', message.Length + 4));
        Console.ResetColor();
        Console.WriteLine();
    }
}
