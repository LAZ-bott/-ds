using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using StardewModdingAPI;

namespace GenericModConfigMenu.Framework
{
    /// <summary>
    /// Offline, dictionary-based translation for GMCM display strings.
    /// It only changes labels shown by GMCM; config values and callbacks remain untouched.
    /// </summary>
    internal static class AutoChineseTranslator
    {
        private static readonly Dictionary<string, string> Phrases = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> Words = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Dictionary<string, string>> ModOverrides = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<(Regex Pattern, string Value)> PhrasePatterns = new();

        public static void Load(string directoryPath)
        {
            Phrases.Clear();
            Words.Clear();
            ModOverrides.Clear();
            PhrasePatterns.Clear();

            string path = Path.Combine(directoryPath, "auto-zh.json");
            if (!File.Exists(path))
                return;

            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement root = document.RootElement;
                ReadStringMap(root, "phrases", Phrases);
                ReadStringMap(root, "words", Words);

                if (root.TryGetProperty("mods", out JsonElement mods) && mods.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty mod in mods.EnumerateObject())
                    {
                        if (mod.Value.ValueKind != JsonValueKind.Object)
                            continue;

                        Dictionary<string, string> entries = new(StringComparer.OrdinalIgnoreCase);
                        foreach (JsonProperty entry in mod.Value.EnumerateObject())
                        {
                            if (entry.Value.ValueKind == JsonValueKind.String)
                                entries[entry.Name] = entry.Value.GetString() ?? entry.Name;
                        }

                        ModOverrides[mod.Name] = entries;
                    }
                }

                foreach (var phrase in Phrases.OrderByDescending(p => p.Key.Length))
                {
                    string pattern = @"(?<![\p{L}\p{N}])" + Regex.Escape(phrase.Key) + @"(?![\p{L}\p{N}])";
                    PhrasePatterns.Add((new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), phrase.Value));
                }
            }
            catch (Exception ex)
            {
                Mod.instance.Monitor.Log($"Couldn't load auto-zh.json: {ex.Message}", LogLevel.Warn);
            }
        }

        public static string? Translate(string? text, IManifest? manifest = null)
        {
            if (string.IsNullOrWhiteSpace(text) || !Mod.AutoChineseTranslationEnabled)
                return text;

            if (manifest != null && ModOverrides.TryGetValue(manifest.UniqueID, out var overrides)
                && overrides.TryGetValue(text, out string? modTranslation))
                return modTranslation;

            if (Phrases.TryGetValue(text.Trim(), out string? exactPhrase))
                return exactPhrase;
            if (Words.TryGetValue(text.Trim(), out string? exactWord))
                return exactWord;

            string result = text;
            foreach (var item in PhrasePatterns)
                result = item.Pattern.Replace(result, item.Value);

            result = Regex.Replace(result, @"[A-Za-z][A-Za-z0-9_'-]*", match =>
                Words.TryGetValue(match.Value, out string? translated) ? translated : match.Value);

            return result;
        }

        private static void ReadStringMap(JsonElement root, string propertyName, Dictionary<string, string> destination)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement map) || map.ValueKind != JsonValueKind.Object)
                return;

            foreach (JsonProperty item in map.EnumerateObject())
            {
                if (item.Value.ValueKind == JsonValueKind.String)
                    destination[item.Name] = item.Value.GetString() ?? item.Name;
            }
        }
    }
}
