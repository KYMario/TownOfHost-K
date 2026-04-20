using TownOfHost.Modules;

namespace TownOfHost
{
    public class PresetOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;
        public static OptionItem Preset;

        // コンストラクタ
        public PresetOptionItem(int defaultValue, TabGroup tab)
        : base(0, "Preset", defaultValue, tab, true)
        {
            Rule = (0, NumPresets - 1, 1);
        }
        public static PresetOptionItem Create(int defaultValue, TabGroup tab)
        {
            return new PresetOptionItem(defaultValue, tab);
        }

        // Getter
        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()//プリセット名決める適菜奴作りたいなぁ。
        {
            var ch = "";
            if (GameModeManager.IsStandardClass())
            {
                var text = UtilsShowOption.GetRoleTypesCount();
                if (text != "")
                    ch += $"<size=70%>{text}</size>";
            }
            return CurrentValue switch
            {
                0 => Main.Preset1.Value == (string)Main.Preset1.DefaultValue ? Translator.GetString("Preset_1") : Main.Preset1.Value,
                1 => Main.Preset2.Value == (string)Main.Preset2.DefaultValue ? Translator.GetString("Preset_2") : Main.Preset2.Value,
                2 => Main.Preset3.Value == (string)Main.Preset3.DefaultValue ? Translator.GetString("Preset_3") : Main.Preset3.Value,
                3 => Main.Preset4.Value == (string)Main.Preset4.DefaultValue ? Translator.GetString("Preset_4") : Main.Preset4.Value,
                4 => Main.Preset5.Value == (string)Main.Preset5.DefaultValue ? Translator.GetString("Preset_5") : Main.Preset5.Value,
                5 => Main.Preset6.Value == (string)Main.Preset6.DefaultValue ? Translator.GetString("Preset_6") : Main.Preset6.Value,
                6 => Main.Preset7.Value == (string)Main.Preset7.DefaultValue ? Translator.GetString("Preset_7") : Main.Preset7.Value,
                7 => Main.Preset8.Value == (string)Main.Preset8.DefaultValue ? Translator.GetString("Preset_8") : Main.Preset8.Value,
                8 => Main.Preset9.Value == (string)Main.Preset9.DefaultValue ? Translator.GetString("Preset_9") : Main.Preset9.Value,
                9 => Main.Preset10.Value == (string)Main.Preset10.DefaultValue ? Translator.GetString("Preset_10") : Main.Preset10.Value,
                10 => Main.Preset11.Value == (string)Main.Preset11.DefaultValue ? Translator.GetString("Preset_11") : Main.Preset11.Value,
                11 => Main.Preset12.Value == (string)Main.Preset12.DefaultValue ? Translator.GetString("Preset_12") : Main.Preset12.Value,
                12 => Main.Preset13.Value == (string)Main.Preset13.DefaultValue ? Translator.GetString("Preset_13") : Main.Preset13.Value,
                13 => Main.Preset14.Value == (string)Main.Preset14.DefaultValue ? Translator.GetString("Preset_14") : Main.Preset14.Value,
                14 => Main.Preset15.Value == (string)Main.Preset15.DefaultValue ? Translator.GetString("Preset_15") : Main.Preset15.Value,
                15 => Main.Preset16.Value == (string)Main.Preset16.DefaultValue ? Translator.GetString("Preset_16") : Main.Preset16.Value,
                _ => null,
            } + "<size=50%>\n" + (ch == "" ? Translator.GetString($"{Options.CurrentGameMode}") : ch) + "</size>";
        }
        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        // Setter
        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(value), doSync);
            SwitchPreset(Rule.RepeatIndex(value));
        }
        public override void SetValue(int afterValue, bool doSave, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(afterValue), doSave, doSync);
            SwitchPreset(Rule.RepeatIndex(afterValue));
            VanillaOptionHolder.SetVanillaValue();
        }
    }
}