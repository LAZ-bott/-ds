#!/usr/bin/env python3
from pathlib import Path
import json, re, shutil, sys

root = Path(sys.argv[1]).resolve()
mod = root / "framework/GenericModConfigMenu"
def replace_once(path, old, new, label):
    text = path.read_text(encoding="utf-8-sig")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: expected one match in {path}, got {count}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")

# Copy the translator implementation and editable dictionary.
shutil.copy2(Path(__file__).parent / "AutoChineseTranslator.cs", mod / "Framework/AutoChineseTranslator.cs")
shutil.copy2(Path(__file__).parent / "auto-zh.json", mod / "auto-zh.json")

# Configuration toggle.
p = mod / "Framework/OwnModConfig.cs"
replace_once(p,
'''        public int ScrollSpeed { get; set; } = 120;''',
'''        public int ScrollSpeed { get; set; } = 120;

        /// <summary>Translate common English GMCM labels into Simplified Chinese using the local dictionary.</summary>
        public bool EnableAutoChineseTranslation { get; set; } = true;''',
"add translator setting")

# Initialize dictionary and expose config toggle.
p = mod / "Mod.cs"
text = p.read_text(encoding="utf-8-sig")
if "AutoChineseTranslator.Load" not in text:
    text = text.replace(
        "            this.Config = helper.ReadConfig<OwnModConfig>();",
        "            this.Config = helper.ReadConfig<OwnModConfig>();\n            AutoChineseTranslator.Load(helper.DirectoryPath);",
        1
    )
if "AutoChineseTranslationEnabled" not in text:
    text = text.replace(
        "        private OwnModConfig Config = null!;",
        "        private OwnModConfig Config = null!;\n\n        internal static bool AutoChineseTranslationEnabled => instance?.Config?.EnableAutoChineseTranslation ?? true;",
        1
    )
needle = '''            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new OwnModConfig(),
                save: () => this.Helper.WriteConfig(this.Config),
                titleScreenOnly: false
            );'''
if needle not in text:
    raise RuntimeError("GMCM self-registration block not found")
text = text.replace(needle, needle + '''

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: I18n.Options_AutoChineseTranslation_Name,
                tooltip: I18n.Options_AutoChineseTranslation_Desc,
                getValue: () => this.Config.EnableAutoChineseTranslation,
                setValue: value => this.Config.EnableAutoChineseTranslation = value
            );''', 1)
p.write_text(text, encoding="utf-8")

# Translate labels and descriptions at display construction time, never config values/callbacks.
p = mod / "Framework/SpecificModConfigMenu.cs"
text = p.read_text(encoding="utf-8-sig")
text = text.replace("string? name = opt.Name?.Invoke();", "string? name = AutoChineseTranslator.Translate(opt.Name?.Invoke(), config.ModManifest);", 1)
text = text.replace("string? tooltip = opt.Tooltip?.Invoke();", "string? tooltip = AutoChineseTranslator.Translate(opt.Tooltip?.Invoke(), config.ModManifest);", 1)
text = text.replace("string? name = opt.Name?.Invoke();", "string? name = AutoChineseTranslator.Translate(opt.Name?.Invoke(), this.ModConfig?.ModManifest);", 1)
text = text.replace("string? tooltip = opt.Tooltip?.Invoke();", "string? tooltip = AutoChineseTranslator.Translate(opt.Tooltip?.Invoke(), this.ModConfig?.ModManifest);", 1)
text = text.replace("String = config.ModName,\n                        UserData = config.ModManifest.Description,", "String = AutoChineseTranslator.Translate(config.ModName, config.ModManifest),\n                        UserData = AutoChineseTranslator.Translate(config.ModManifest.Description, config.ModManifest),", 1)
text = text.replace("ScreenReaderDescription = config.ModManifest.Description,", "ScreenReaderDescription = AutoChineseTranslator.Translate(config.ModManifest.Description, config.ModManifest),", 1)
text = text.replace(
    "Labels = option.Choices.Select(value => option.FormatChoice?.Invoke(value) ?? value).ToArray(),",
    "Labels = option.Choices.Select(value => AutoChineseTranslator.Translate(option.FormatChoice?.Invoke(value) ?? value, this.ModConfig?.ModManifest) ?? value).ToArray(),",
    1
)
text = text.replace("name: option.Name?.Invoke(),", "name: AutoChineseTranslator.Translate(option.Name?.Invoke(), this.Manifest),")
text = text.replace(
    "string pageTitle = modManifest == null || this.CurrPage == null ? \"\" : this.ModConfig?.Pages[this.CurrPage].PageTitle() ?? \"\";",
    "string pageTitle = modManifest == null || this.CurrPage == null ? \"\" : AutoChineseTranslator.Translate(this.ModConfig?.Pages[this.CurrPage].PageTitle() ?? \"\", modManifest) ?? \"\";",
    1
)
text = text.replace(
    "String = modManifest == null ? I18n.List_Keybinds() : (modManifest.Name + (pageTitle == \"\" ? \"\" : \" > \" + pageTitle)),",
    "String = modManifest == null ? I18n.List_Keybinds() : ((AutoChineseTranslator.Translate(modManifest.Name, modManifest) ?? modManifest.Name) + (pageTitle == \"\" ? \"\" : \" > \" + pageTitle)),",
    1
)
p.write_text(text, encoding="utf-8")

# Translate mod names/descriptions in the main mod list too.
p = mod / "Framework/ModConfigMenu.cs"
text = p.read_text(encoding="utf-8-sig")
text = text.replace("String = entry.ModName,\n                            UserData = entry.ModManifest.Description,", "String = AutoChineseTranslator.Translate(entry.ModName, entry.ModManifest),\n                            UserData = AutoChineseTranslator.Translate(entry.ModManifest.Description, entry.ModManifest),")
p.write_text(text, encoding="utf-8")

# Translate the name/tooltip shown by keybind overlays and any page label handled above.
# Add translation keys to built-in locale files.
for locale, additions in {
    "default.json": {
        "options.auto-chinese-translation.name": "Auto-translate common English config text",
        "options.auto-chinese-translation.desc": "Translate common English option names, descriptions, and choice labels using the local dictionary in auto-zh.json. Unknown text stays unchanged."
    },
    "zh.json": {
        "options.auto-chinese-translation.name": "自动汉化配置文字",
        "options.auto-chinese-translation.desc": "使用 auto-zh.json 本地词库翻译常见英文配置名称、说明和选项。未收录的文字会保留原文。"
    }
}.items():
    path = mod / "i18n" / locale
    data_text = path.read_text(encoding="utf-8-sig")
    # These files contain JSON comments, so insert before the final closing brace.
    pos = data_text.rfind("}")
    if pos < 0:
        raise RuntimeError(f"Invalid locale file: {path}")
    for key, value in additions.items():
        if f'"{key}"' not in data_text:
            data_text = data_text[:pos].rstrip() + ',\n    ' + json.dumps(key, ensure_ascii=False) + ': ' + json.dumps(value, ensure_ascii=False) + '\n' + data_text[pos:]
            pos = data_text.rfind("}")
    path.write_text(data_text, encoding="utf-8")

# Set a clearly identifiable personal build version.
p = mod / "GenericModConfigMenu.csproj"
proj = p.read_text(encoding="utf-8-sig")
proj = re.sub(r'<Version>.*?</Version>', '<Version>1.17.0-zh.1</Version>', proj, count=1)
if 'Content Include="auto-zh.json"' not in proj:
    proj = proj.replace(
        "</Project>",
        '  <ItemGroup>\\n    <Content Include="auto-zh.json" CopyToOutputDirectory="PreserveNewest" />\\n  </ItemGroup>\\n</Project>'
    )
p.write_text(proj, encoding="utf-8")

print("Applied GMCM universal Chinese dictionary patch.")
