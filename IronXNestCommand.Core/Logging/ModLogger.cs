using System;

namespace IronXNestCommand.Core.Logging
{
    public static class ModLogger
    {
        public static Action<string> OnLog { get; set; } = msg => Console.WriteLine($"[IronXNestCommand] {msg}");
        public static Action<string> OnWarning { get; set; } = msg => Console.WriteLine($"[IronXNestCommand][WARN] {msg}");
        public static Action<string> OnError { get; set; } = msg => Console.Error.WriteLine($"[IronXNestCommand][ERROR] {msg}");

        public static void Info(string message) => OnLog?.Invoke(message);
        public static void Warn(string message) => OnWarning?.Invoke(message);
        public static void Error(string message) => OnError?.Invoke(message);
    }
}
