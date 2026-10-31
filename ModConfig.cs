using System;
using System.Collections.Generic;
using System.Linq;

namespace UnlimitedEventExpansion
{
    public class ModConfig
    {
        public const string ModelGpt51 = "gpt-5.1";
        public const string ModelGpt5Mini = "gpt-5-mini";
        public const string ModelGpt5Nano = "gpt-5-nano";
        public const string ModelGpt54Mini = "gpt-5.4-mini";
        public const string ModelGpt54Nano = "gpt-5.4-nano";

        public const string ModelGemini35Flash = "gemini-3.5-flash";
        public const string ModelGemini31FlashLite = "gemini-3.1-flash-lite";
        public const string ModelGemini3FlashPreview = "gemini-3-flash-preview";

        public const string ProviderAuto = "Auto (original behavior)";
        public const string ProviderOpenAI = "OpenAI";
        public const string ProviderGemini = "Gemini";
        public const string ProviderCustom = "Custom (OpenAI-compatible)";
        public static readonly string[] ApiProviders =
        {
            ProviderAuto,
            ProviderOpenAI,
            ProviderGemini,
            ProviderCustom
        };

        public const string EventLengthShort = "short";
        public const string EventLengthMedium = "medium";
        public const string EventLengthLong = "long";
        public const string EventLengthExtraLong = "extra_long";

        public const string CharacteristicModeMinimal = "minimal";
        public const string CharacteristicModeShort = "short";
        public const string CharacteristicModeLong = "long";

        public const string LanguageEnglish = "English";

        private static readonly string[] OpenAiModels = new[]
        {
            ModelGpt51,
            ModelGpt54Mini,
            ModelGpt54Nano,
            ModelGpt5Mini,
            ModelGpt5Nano
        };

        private static readonly string[] GeminiModels = new[]
        {
            ModelGemini35Flash,
            ModelGemini31FlashLite,
            ModelGemini3FlashPreview
        };

        public static readonly string[] AllSupportedModels = new[]
        {
            ModelGpt51,
            ModelGpt54Mini,
            ModelGpt54Nano,
            ModelGpt5Mini,
            ModelGpt5Nano,
            ModelGemini35Flash,
            ModelGemini31FlashLite,
            ModelGemini3FlashPreview
        };

        public static bool IsOpenAiModel(string? model)
        {
            return !string.IsNullOrWhiteSpace(model)
                && OpenAiModels.Contains(model, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsGeminiModel(string? model)
        {
            return !string.IsNullOrWhiteSpace(model)
                && GeminiModels.Contains(model, StringComparer.OrdinalIgnoreCase);
        }

        public string ApiProvider { get; set; } = ProviderAuto;
        public string CustomApiBaseUrl { get; set; } = "https://api.deepseek.com";
        public string Key { get; set; } = "";
        public string Model { get; set; } = ModelGpt51;
        public bool AllowEarlyEvent { get; set; } = false;

        public string EventLength { get; set; } = EventLengthShort;
        public string CharacteristicMode { get; set; } = CharacteristicModeShort;
        public string Language { get; set; } = LanguageEnglish;
        public string NpcProfileTheme { get; set; } = "vanilla";
    }
}
