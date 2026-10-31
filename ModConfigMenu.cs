using ContentPatcher;
using StardewModdingAPI;
using System.IO;
using System.Linq;

namespace UnlimitedEventExpansion
{
    public partial class ModEntry
    {
        public static void ConfigMenu(IContentPatcherAPI api, IManifest modManifest, IModHelper helper)
        {
            var configMenu = helper.ModRegistry.GetApi<UnlimitedEventExpansion.Data.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: modManifest,
                reset: () => Config = new ModConfig(),
                save: () => helper.WriteConfig(Config)
            );

            configMenu.AddSectionTitle(
                mod: modManifest,
                text: () => GetTranslation("config.section.language")
            );

            configMenu.AddTextOption(
                mod: modManifest,
                getValue: () => string.IsNullOrWhiteSpace(Config.Language) ? ModConfig.LanguageEnglish : Config.Language,
                setValue: value => Config.Language = string.IsNullOrWhiteSpace(value) ? ModConfig.LanguageEnglish : value.Trim(),
                name: () => GetTranslation("config.language.name"),
                tooltip: () => GetTranslation("config.language.tooltip")
            );

            string npcProfilePath = Path.Combine(helper.DirectoryPath, "npc_profile");
            string[] themeOptions = Directory.Exists(npcProfilePath)
                ? Directory.GetDirectories(npcProfilePath).Select(Path.GetFileName).Where(name => !string.IsNullOrEmpty(name)).ToArray()!
                : new[] { "vanilla" };
            if (themeOptions.Length == 0)
                themeOptions = new[] { "vanilla" };

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => GetTranslation("config.theme.name"),
                tooltip: () => GetTranslation("config.theme.tooltip"),
                getValue: () => string.IsNullOrWhiteSpace(Config.NpcProfileTheme) ? "vanilla" : Config.NpcProfileTheme,
                setValue: value => Config.NpcProfileTheme = string.IsNullOrWhiteSpace(value) ? "vanilla" : value.Trim(),
                allowedValues: themeOptions
            );

            configMenu.AddSectionTitle(
                mod: modManifest,
                text: () => GetTranslation("config.section.ai-config")
            );

            configMenu.AddParagraph(
                mod: modManifest,
                text: () => GetTranslation("config.ai-config.desc")
            );

            configMenu.SetTitleScreenOnlyForNextOptions(mod: modManifest, titleScreenOnly: true);

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => GetTranslation("config.provider.name"),
                tooltip: () => GetTranslation("config.provider.tooltip"),
                getValue: () => Config.ApiProvider,
                setValue: value => Config.ApiProvider = string.IsNullOrWhiteSpace(value) ? ModConfig.ProviderAuto : value.Trim(),
                allowedValues: ModConfig.ApiProviders
            );

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => GetTranslation("config.base-url.name"),
                tooltip: () => GetTranslation("config.base-url.tooltip"),
                getValue: () => Config.CustomApiBaseUrl ?? "",
                setValue: value => Config.CustomApiBaseUrl = value?.Trim() ?? ""
            );

            configMenu.AddTextOption(
                mod: modManifest,
                getValue: () => Config?.Key ?? "",
                setValue: value => Config.Key = value?.Trim() ?? "",
                name: () => GetTranslation("config.key.name"),
                tooltip: () => GetTranslation("config.key.tooltip")
            );

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => GetTranslation("config.model.name"),
                tooltip: () => GetTranslation("config.model.tooltip"),
                getValue: () => Config.Model,
                setValue: value => Config.Model = value?.Trim() ?? "",
                allowedValues: string.Equals(Config.ApiProvider, ModConfig.ProviderCustom, System.StringComparison.OrdinalIgnoreCase)
                    ? null
                    : ModConfig.AllSupportedModels,
                formatAllowedValue: FormatModel
            );

            configMenu.AddParagraph(
                mod: modManifest,
                text: () => GetTranslation("config.section.pacing")
            );

            configMenu.AddBoolOption(
                mod: modManifest,
                name: () => GetTranslation("config.allow-early-event.name"),
                tooltip: () => GetTranslation("config.allow-early-event.tooltip"),
                getValue: () => Config.AllowEarlyEvent,
                setValue: value => Config.AllowEarlyEvent = value
            );

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => GetTranslation("config.event-length.name"),
                tooltip: () => GetTranslation("config.event-length.tooltip"),
                getValue: () => Config.EventLength,
                setValue: value => Config.EventLength = value,
                allowedValues: new string[]
                {
                    ModConfig.EventLengthShort,
                    ModConfig.EventLengthMedium,
                    ModConfig.EventLengthLong,
                    ModConfig.EventLengthExtraLong
                },
                formatAllowedValue: FormatEventLength
            );

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => GetTranslation("config.characteristic-mode.name"),
                tooltip: () => GetTranslation("config.characteristic-mode.tooltip"),
                getValue: () => Config.CharacteristicMode,
                setValue: value => Config.CharacteristicMode = value,
                allowedValues: new string[]
                {
                    ModConfig.CharacteristicModeMinimal,
                    ModConfig.CharacteristicModeShort,
                    ModConfig.CharacteristicModeLong
                },
                formatAllowedValue: FormatCharacteristicMode
            );
        }

        private static string FormatModel(string value)
        {
            return value switch
            {
                ModConfig.ModelGpt51 => GetTranslation("config.model.gpt51"),
                ModConfig.ModelGpt5Mini => GetTranslation("config.model.gpt5mini"),
                ModConfig.ModelGpt5Nano => GetTranslation("config.model.gpt5nano"),
                ModConfig.ModelGpt54Mini => GetTranslation("config.model.gpt54mini"),
                ModConfig.ModelGpt54Nano => GetTranslation("config.model.gpt54nano"),
                ModConfig.ModelGemini35Flash => GetTranslation("config.model.gemini35flash"),
                ModConfig.ModelGemini31FlashLite => GetTranslation("config.model.gemini31flashlite"),
                ModConfig.ModelGemini3FlashPreview => GetTranslation("config.model.gemini3flashpreview"),
                _ => value
            };
        }

        private static string FormatEventLength(string value)
        {
            return value switch
            {
                ModConfig.EventLengthShort => GetTranslation("config.event-length.short"),
                ModConfig.EventLengthMedium => GetTranslation("config.event-length.medium"),
                ModConfig.EventLengthLong => GetTranslation("config.event-length.long"),
                ModConfig.EventLengthExtraLong => GetTranslation("config.event-length.extra-long"),
                _ => value
            };
        }

        private static string FormatCharacteristicMode(string value)
        {
            return value switch
            {
                ModConfig.CharacteristicModeMinimal => GetTranslation("config.characteristic-mode.minimal"),
                ModConfig.CharacteristicModeShort => GetTranslation("config.characteristic-mode.short"),
                ModConfig.CharacteristicModeLong => GetTranslation("config.characteristic-mode.long"),
                _ => value
            };
        }
    }
}
