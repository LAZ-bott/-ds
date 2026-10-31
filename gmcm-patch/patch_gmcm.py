import json
from pathlib import Path

root = Path("source")
base = root / "framework/GenericModConfigMenu"
def edit(rel, pairs):
    p = base / rel
    s = p.read_text(encoding="utf-8-sig")
    for old, new in pairs:
        n = s.count(old)
        if n != 1:
            raise RuntimeError(f"{rel}: expected one occurrence, got {n}: {old[:90]!r}")
        s = s.replace(old, new, 1)
    p.write_text(s, encoding="utf-8")

# Config toggle.
edit("Framework/OwnModConfig.cs", [
    ("public int ScrollSpeed { get; set; } = 120;",
     "public int ScrollSpeed { get; set; } = 120;\n\n        /// <summary>Whether to apply the built-in Simplified Chinese fallback dictionary to mod config UI text.</summary>\n        public bool AutoChineseTranslation { get; set; } = true;")
])

# Load dictionary and initialize toggle.
edit("Mod.cs", [
    ("this.Config = helper.ReadConfig<OwnModConfig>();",
     "this.Config = helper.ReadConfig<OwnModConfig>();\n            AutoChineseTranslator.Load(helper.Data.ReadJsonFile<Dictionary<string, string>>(\"assets/auto-zh.json\"));\n            AutoChineseTranslator.Enabled = this.Config.AutoChineseTranslation;"),
    ("reset: () => this.Config = new OwnModConfig(),",
     "reset: () => { this.Config = new OwnModConfig(); AutoChineseTranslator.Enabled = this.Config.AutoChineseTranslation; },"),
    ("            configMenu.AddKeybindList(\n                mod: this.ModManifest,",
     "            configMenu.AddBoolOption(\n                mod: this.ModManifest,\n                name: I18n.Options_AutoChinese_Name,\n                tooltip: I18n.Options_AutoChinese_Desc,\n                getValue: () => this.Config.AutoChineseTranslation,\n                setValue: value => { this.Config.AutoChineseTranslation = value; AutoChineseTranslator.Enabled = value; }\n            );\n\n            configMenu.AddKeybindList(\n                mod: this.ModManifest,")
])

# Wrap all API-provided display strings at the central registration layer.
edit("Framework/Api.cs", [
    ("new SectionTitleModOption(text, tooltip, modConfig)", "new SectionTitleModOption(AutoChineseTranslator.Wrap(text), AutoChineseTranslator.Wrap(tooltip), modConfig)"),
    ("new SectionSubHeaderModOption(text, modConfig)", "new SectionSubHeaderModOption(AutoChineseTranslator.Wrap(text), modConfig)"),
    ("new ParagraphModOption(text, modConfig)", "new ParagraphModOption(AutoChineseTranslator.Wrap(text), modConfig)"),
    (".SetActiveRegisteringPage(pageId, pageTitle);", ".SetActiveRegisteringPage(pageId, AutoChineseTranslator.Wrap(pageTitle));"),
    ("new PageLinkModOption(pageId, text, tooltip, modConfig)", "new PageLinkModOption(pageId, AutoChineseTranslator.Wrap(text), AutoChineseTranslator.Wrap(tooltip), modConfig)"),
    ("new ComplexModOption(fieldId: fieldId, name: name, tooltip: tooltip,", "new ComplexModOption(fieldId: fieldId, name: AutoChineseTranslator.Wrap(name), tooltip: AutoChineseTranslator.Wrap(tooltip),"),
    ("new SimpleModOption<T>(fieldId, name, tooltip, modConfig, getValue, setValue)", "new SimpleModOption<T>(fieldId, AutoChineseTranslator.Wrap(name), AutoChineseTranslator.Wrap(tooltip), modConfig, getValue, setValue)"),
    ("new NumericModOption<T>(fieldId: fieldId, name: name, tooltip: tooltip, mod: modConfig, getValue: getValue, setValue: setValue, min: min, max: max, interval: interval, formatValue: formatValue)",
     "new NumericModOption<T>(fieldId: fieldId, name: AutoChineseTranslator.Wrap(name), tooltip: AutoChineseTranslator.Wrap(tooltip), mod: modConfig, getValue: getValue, setValue: setValue, min: min, max: max, interval: interval, formatValue: formatValue is null ? null : value => AutoChineseTranslator.Translate(formatValue(value)))"),
    ("new ChoiceModOption<string>(fieldId, name, tooltip, modConfig, getValue, setValue, allowedValues, formatAllowedValues)",
     "new ChoiceModOption<string>(fieldId, AutoChineseTranslator.Wrap(name), AutoChineseTranslator.Wrap(tooltip), modConfig, getValue, setValue, allowedValues, formatAllowedValues is null ? AutoChineseTranslator.Translate : value => AutoChineseTranslator.Translate(formatAllowedValues(value)))")
])

# Update GMCM's own option and zh/default labels.
edit("Mod.cs", [
    ("using System.Collections.Generic;", "using System.Collections.Generic;")
])
for lang, additions in [
    ("zh.json", '\n    "options.auto-chinese.name": "自动汉化其他 Mod 的配置项",\n    "options.auto-chinese.desc": "使用内置简体中文词典翻译其他 Mod 提供的配置标题、说明、分组和选项。无法识别的文字保留原文。",\n'),
    ("default.json", '\n    "options.auto-chinese.name": "Auto-translate mod config text to Simplified Chinese",\n    "options.auto-chinese.desc": "Use the built-in Simplified Chinese dictionary for config labels, descriptions, section headings, pages, and choice labels. Unknown text stays unchanged.",\n')
]:
    p = base / "i18n" / lang
    s = p.read_text(encoding="utf-8-sig")
    if "options.auto-chinese.name" not in s:
        s = s.rstrip()
        s = s[:-1].rstrip() + "," + additions + "}\n"
    if lang == "zh.json":
        s = s.replace('"list.keybinds": "Keybinds", // TODO', '"list.keybinds": "快捷键"')
        s = s.replace('"config.rebind-key.keybind-list": "Keybinds:", // TODO', '"config.rebind-key.keybind-list": "快捷键："')
    p.write_text(s, encoding="utf-8")

# Use a distinct personal-build version.
p = base / "GenericModConfigMenu.csproj"
s = p.read_text(encoding="utf-8-sig").replace("<Version>1.17.0-alpha</Version>", "<Version>1.17.0-zh.1</Version>")
p.write_text(s, encoding="utf-8")

print("Patched GMCM API, config toggle, translations, and version.")
