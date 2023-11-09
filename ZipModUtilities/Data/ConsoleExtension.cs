using System;

namespace ZipModUtilities.Data;

public static class ConsoleExtension
{
    public static void WriteLine(this ConsoleColor color,string value)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(value);
        Console.ResetColor();
    }
}