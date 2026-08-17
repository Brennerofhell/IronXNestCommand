using System;
using System.Collections.Generic;

namespace IronXNestCommand.Host.BepInEx.Notes
{
    public class TeleprinterMessage
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } = "HIGH COMMAND";
        public string Text { get; set; } = "";
    }

    public static class TeleprinterLog
    {
        private static readonly List<TeleprinterMessage> Messages = new();
        private const int MaxHistory = 15;

        public static IReadOnlyList<TeleprinterMessage> History => Messages;

        public static void AddDispatch(string sourceId, IEnumerable<string> lines)
        {
            if (lines == null) return;

            string fullText = string.Join("\n", lines);
            if (string.IsNullOrWhiteSpace(fullText)) return;

            string sourceName = string.IsNullOrEmpty(sourceId) ? "HIGH COMMAND" : sourceId.ToUpper();
            if (sourceName.Contains("FIELD")) sourceName = "FIELD REPORT";
            if (sourceName.Contains("HQ") || sourceName.Contains("COMMAND")) sourceName = "HIGH COMMAND";

            Messages.Insert(0, new TeleprinterMessage
            {
                Timestamp = DateTime.Now,
                Source = sourceName,
                Text = fullText
            });

            if (Messages.Count > MaxHistory)
                Messages.RemoveAt(Messages.Count - 1);
        }

        public static void Clear() => Messages.Clear();
    }
}
