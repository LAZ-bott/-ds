using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GenericModConfigMenu
{
    /// <summary>
    /// Applies a deterministic Simplified Chinese fallback to text supplied through GMCM's public API.
    /// Config values and callbacks are never translated or modified.
    /// </summary>
    internal static class AutoChineseTranslator
    {
        private static Dictionary<string, string> Entries = new(StringComparer.OrdinalIgnoreCase);

        public static bool Enabled { get; set; } = true;

        public static void Load(Dictionary<string, string>? entries)
        {
            Entries = entries is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(entries, StringComparer.OrdinalIgnoreCase);
        }

        public static Func<string>? Wrap(Func<string>? source)
        {
            return source is null ? null : () => Translate(source());
        }

        public static string Translate(string? source)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(source))
                return source ?? string.Empty;

            string input = source;
            if (Entries.TryGetValue(input.Trim(), out string? exact))
                return exact;

            string result = input;
            // Longer phrases are replaced before individual words. Word boundaries avoid
            // changing identifiers such as "EnableNPCFeature" or URLs.
            foreach (var entry in Entries.OrderByDescending(p => p.Key.Length))
            {
                if (entry.Key.Length < 2)
                    continue;

                string pattern = $@"(?<![\\p{{L}}\\p{{N}}]){Regex.Escape(entry.Key)}(?![\\p{{L}}\\p{{N}}])";
                result = Regex.Replace(result, pattern, _ => entry.Value, RegexOptions.IgnoreCase);
            }

            return result;
        }
    }
}
