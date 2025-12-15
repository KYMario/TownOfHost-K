using System;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices.RTC;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost
{
    public abstract class OptionItem
    {
        #region static
        public static IReadOnlyList<OptionItem> AllOptions => _allOptions;
        private static List<OptionItem> _allOptions = new(2048);
        public static IReadOnlyDictionary<int, OptionItem> FastOptions => _fastOptions;
        private static Dictionary<int, OptionItem> _fastOptions = new(2048);
        public static IReadOnlyList<OptionItem> KillCoolOption => _killcooloption;
        private static List<OptionItem> _killcooloption = new(1024);
        public static int CurrentPreset { get; set; }
#if DEBUG
        public static bool IdDuplicated { get; private set; } = false;
#endif
        #endregion

        // 必須情報 (コンストラクタで必ず設定させる必要がある値)
        public int Id { get; }
        public string Name { get; }
        public int DefaultValue { get; }
        public TabGroup Tab { get; }
        public bool IsSingleValue { get; }

        // 任意情報 (空・nullを許容する または、ほとんど初期値で問題ない値)
        public Color NameColor { get; protected set; }
        public string NameColorCode { get; protected set; }
        public string Fromtext { get; protected set; }
        public OptionZeroNotation ZeroNotation { get; protected set; }
        public OptionFormat ValueFormat { get; protected set; }
        public CustomGameMode GameMode { get; protected set; }
        public bool IsHeader { get; protected set; }
        public bool IsHidden { get; protected set; }
        public Func<bool> IsEnabled { get; protected set; }
        public bool HideValue { get; protected set; }
        public CustomRoles CustomRole { get; protected set; }
        public CustomRoles ParentRole { get; protected set; }
        public Func<string> funcOptionName { get; protected set; }
        public string Tooltip { get; protected set; }
        public Dictionary<string, string> ReplacementDictionary
        {
            get => _replacementDictionary;
            set
            {
                if (value == null) _replacementDictionary?.Clear();
                else _replacementDictionary = value;
            }
        }
        private Dictionary<string, string> _replacementDictionary;

        // 設定値情報 (オプションの値に関わる情報)
        public int[] AllValues { get; private set; } = new int[NumPresets];
        public int CurrentValue
        {
            get => GetValue();
            set => SetValue(value);
        }
        public int SingleValue { get; private set; }

        // 親子情報
        public bool parented = false;
        public OptionItem Parent { get; private set; }
        public List<OptionItem> Children;

        public OptionBehaviour OptionBehaviour;

        // イベント
        // eventキーワードにより、クラス外からのこのフィールドに対する以下の操作は禁止されます。
        // - 代入 (+=, -=を除く)
        // - 直接的な呼び出し
        public event EventHandler<UpdateValueEventArgs> UpdateValueEvent;

        public OptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, string From = "", bool hidevalue = false)
        {
            // 必須情報の設定
            Id = id;
            Name = name;
            DefaultValue = defaultValue;
            Tab = tab;
            IsSingleValue = isSingleValue;

            // 任意情報の初期値設定
            HideValue = hidevalue;
            Fromtext = From;
            NameColor = Color.white;
            NameColorCode = "#ffffff";
            ValueFormat = OptionFormat.None;
            GameMode = CustomGameMode.All;
            IsHeader = false;
            IsHidden = false;
            IsEnabled = () => true;
            funcOptionName = () => "";
            ZeroNotation = OptionZeroNotation.None;
            parented = false;
            Tooltip = "";
            CustomRole = CustomRoles.NotAssigned;
            ParentRole = CustomRoles.NotAssigned;

            // オブジェクト初期化
            Children = new();

            // デフォルト値に設定
            if (Id == 0)
            {
                SingleValue = DefaultValue;
                CurrentPreset = SingleValue;
            }
            else if (IsSingleValue)
            {
                SingleValue = DefaultValue;
            }
            else
            {
                for (int i = 0; i < NumPresets; i++)
                {
                    AllValues[i] = DefaultValue;
                }
            }
            if (_fastOptions.TryAdd(id, this))
            {
                _allOptions.Add(this);
            }
            else
            {
#if DEBUG
                IdDuplicated = true;
#endif
                Logger.Error($"ID:{id}が重複しています name:{name} : {_fastOptions[id].Name}", "OptionItem");
            }
            if (name == "KillCooldown")
            {
                _killcooloption.Add(this);
            }
        }

        // Setter
        public OptionItem Do(Action<OptionItem> action)
        {
            action(this);
            return this;
        }

        public OptionItem SetColor(Color value) => Do(i => i.NameColor = value);
        public OptionItem SetColorcode(string value) => Do(i => i.NameColorCode = value);
        public OptionItem SetValueFormat(OptionFormat value) => Do(i => i.ValueFormat = value);
        public OptionItem SetGameMode(CustomGameMode value) => Do(i => i.GameMode = value);
        public OptionItem SetHeader(bool value) => Do(i => i.IsHeader = value);
        public OptionItem SetCustomRole(CustomRoles role) => Do(i => i.CustomRole = role);
        public OptionItem SetHidden(bool value) => Do(i => i.IsHidden = value);
        public OptionItem SetEnabled(Func<bool> value) => Do(i => i.IsEnabled = value);
        public OptionItem SetInfo(string value) => Do(i => i.Fromtext = "<line-height=25%><size=25%>\n</size><size=60%></color> <b>" + value + "</b></size>");
        public OptionItem SetZeroNotation(OptionZeroNotation value) => Do(i => i.ZeroNotation = value);
        public OptionItem SetOptionName(Func<string> value) => Do(i => i.funcOptionName = value);
        public OptionItem SetTooltip(string value) => Do(i => i.Tooltip = value);

        public OptionItem SetParent(OptionItem parent) => Do(i =>
        {
            if (parent != null && parented)
            {
                Logger.Warn($"{Name} : 既にSetParentがされてます", "SetParent");
                return;
            }
            if (parent != null) parented = true;
            i.Parent = parent;
            parent.SetChild(i);
        });
        public OptionItem SetParentRole(CustomRoles parentrole)
            => Do(i => i.ParentRole = parentrole);
        public OptionItem SetChild(OptionItem child) => Do(i => i.Children.Add(child));
        public OptionItem RegisterUpdateValueEvent(EventHandler<UpdateValueEventArgs> handler)
            => Do(i => UpdateValueEvent += handler);

        // 置き換え辞書
        public OptionItem AddReplacement((string key, string value) kvp)
            => Do(i =>
            {
                ReplacementDictionary ??= new();
                ReplacementDictionary.Add(kvp.key, kvp.value);
            });
        public OptionItem RemoveReplacement(string key)
            => Do(i => ReplacementDictionary?.Remove(key));

        // Getter
        public virtual string GetName(bool disableColor = false, bool isoption = false)
        {
            if (funcOptionName.Invoke() is not "")
            {
                var name = funcOptionName.Invoke();
                if (disableColor) return name;

                if (isoption) return name;
                return NameColorCode != "#ffffff" ? $"<{NameColorCode}>" + name + "</color>" : Utils.ColorString(NameColor, name);
            }
            if (disableColor) return Translator.GetString(Name, ReplacementDictionary);

            if (isoption)
            {
                var str = Translator.GetString(Name, ReplacementDictionary);
                if (str != str.RemoveColorTags() && Name.StartsWith("Give"))
                {
                    str = str.RemoveGiveAddon();
                    str += "<size=70%> :" + Translator.GetString($"{Name}Info", ReplacementDictionary);
                    return NameColorCode != "#ffffff" ? $"<{NameColorCode}>" + str + "</color>" : Utils.ColorString(NameColor, str);
                }
            }
            return NameColorCode != "#ffffff" ? $"<{NameColorCode}>" + Translator.GetString(Name, ReplacementDictionary) + "</color>" : Utils.ColorString(NameColor, Translator.GetString(Name, ReplacementDictionary));
        }
        public virtual bool GetBool() => CurrentValue != 0 && (Parent == null || Parent.GetBool() || CheckRoleOption(Parent))
                                        && (GameMode == CustomGameMode.All || GameMode == Options.CurrentGameMode);
        public bool InfoGetBool() => CurrentValue != 0 && (Parent == null || Parent.InfoGetBool());
        public bool CheckRoleOption(OptionItem option) => option.CustomRole is not CustomRoles.NotAssigned;

        /* オプションのgetboolの表示のやつ */
        public virtual bool OptionMeGetBool() => CurrentValue != 0;
        public virtual int GetInt() => CurrentValue;
        public virtual float GetFloat() => CurrentValue;
        public virtual string GetString()
        {
            return ApplyFormat(CurrentValue.ToString());
        }
        public virtual string GetValueString(bool coloroff)
        {
            return ApplyFormat(CurrentValue.ToString(), coloroff);
        }
        public virtual string GetTextString()
        {
            if (this is StringOptionItem stringOptionItem)
            {
                return stringOptionItem.GetString();
            }
            return GetValueString(false);
        }
        public virtual int GetValue() => IsSingleValue ? SingleValue : AllValues[CurrentPreset];

        // 旧IsHidden関数
        public virtual bool IsHiddenOn(CustomGameMode mode)
        {
            if (IsEnabled == null) return IsHidden || (GameMode != CustomGameMode.All && GameMode != mode);

            return IsHidden || (GameMode != CustomGameMode.All && GameMode != mode) || !IsEnabled();
        }

        public string ApplyFormat(string value, bool coloroff = true)
        {
            if (value == "-0") value = "0";
            if (value == "0")
            {
                switch (ZeroNotation)
                {
                    case OptionZeroNotation.Infinity: return "∞";
                    case OptionZeroNotation.Hyphen: return "―";
                    case OptionZeroNotation.Off: return coloroff ? Translator.GetString("ColoredOff") : "×";
                    default: break;
                }
            }
            if (ValueFormat == OptionFormat.None) return value;
            if (CustomRole is not CustomRoles.NotAssigned)
            {
                var format = string.Format(Translator.GetString("Format." + ValueFormat), value);
                if (Options.GetRoleCount(CustomRole) > 0)
                {
                    switch (value)
                    {
                        case "10": format = $"<#fc7979>{format}</color>"; break;
                        case "20": format = $"<#f7b199>{format}</color>"; break;
                        case "30": format = $"<#fcf479>{format}</color>"; break;
                        case "40": format = $"<#dcfc79>{format}</color>"; break;
                        case "50": format = $"<#b5f77c>{format}</color>"; break;
                        case "60": format = $"<#99f79b>{format}</color>"; break;
                        case "70": format = $"<#87ff9c>{format}</color>"; break;
                        case "80": format = $"<#63ffc6>{format}</color>"; break;
                        case "90": format = $"<#40ffc6>{format}</color>"; break;
                        case "100": format = $"<#79e2fc>{format}</color>"; break;
                    }
                }
                return format;
            }
            return string.Format(Translator.GetString("Format." + ValueFormat), value);
        }

        // 外部からの操作
        public virtual void Refresh()
        {
            if (OptionBehaviour is not null and StringOption opt)
            {
                var role = CustomRoles.NotAssigned;
                var size = "<size=105%>";
                string mark = "";
                if (Enum.TryParse(typeof(CustomRoles), Name, false, out var id))
                {
                    role = (CustomRoles)id;
                    size = "<size=125%>";
                    if (role.IsAddOn())
                    {
                        List<CustomRoles> list = new(1) { role };
                        mark = $" {UtilsRoleText.GetSubRoleMarks(list, CustomRoles.NotAssigned)}";
                    }
                }
                opt.TitleText.text = size + "<b>" + GetName(isoption: true) + mark + Fromtext + "</b></size>";
                opt.ValueText.text = GetString();
                opt.oldValue = opt.Value = CurrentValue;
            }
            OptionShower.Update = true;
        }
        public virtual void SetValue(int afterValue, bool doSave, bool doSync = true)
        {
            int beforeValue = CurrentValue;
            if (IsSingleValue)
            {
                SingleValue = afterValue;
            }
            else
            {
                AllValues[CurrentPreset] = afterValue;
            }

            CallUpdateValueEvent(beforeValue, afterValue);
            Refresh();
            if (doSync)
            {
                SyncOptions();
            }
            if (doSave)
            {
                OptionSaver.Save();
            }
        }
        public virtual void SetValue(int afterValue, bool doSync = true)
        {
            SetValue(afterValue, true, doSync);
        }
        public void SetAllValues(int[] values)  // プリセット読み込み専用
        {
            AllValues = values;
        }

        // 演算子オーバーロード
        public static OptionItem operator ++(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue + 1));
        public static OptionItem operator --(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue - 1));

        // 全体操作用
        public static void SwitchPreset(int newPreset)
        {
            CurrentPreset = Math.Clamp(newPreset, 0, NumPresets - 1);

            foreach (var op in AllOptions)
                op.Refresh();

            SyncAllOptions();
        }
        public static void SyncAllOptions()
        {
            if (
                PlayerCatch.AllPlayerControls.Count() <= 1 ||
                AmongUsClient.Instance == null ||
                AmongUsClient.Instance.AmHost == false ||
                PlayerControl.LocalPlayer == null
            ) return;

            RPC.SyncCustomSettingsRPC();
        }

        public void SyncOptions()
        {
            if (
                PlayerCatch.AllPlayerControls.Count() <= 1 ||
                AmongUsClient.Instance == null ||
                AmongUsClient.Instance.AmHost == false ||
                PlayerControl.LocalPlayer == null
            ) return;

            RPC.SyncCustomSettingsRPC(this);
        }

        // EventArgs
        private void CallUpdateValueEvent(int beforeValue, int currentValue)
        {
            if (UpdateValueEvent == null) return;
            try
            {
                UpdateValueEvent(this, new UpdateValueEventArgs(beforeValue, currentValue));
            }
            catch (Exception ex)
            {
                Logger.Error($"[{Name}] UpdateValueEventの呼び出し時に例外が発生しました", "OptionItem.UpdateValueEvent");
                Logger.Exception(ex, "OptionItem.UpdateValueEvent");
            }
        }

        public class UpdateValueEventArgs : EventArgs
        {
            public int CurrentValue { get; set; }
            public int BeforeValue { get; set; }
            public UpdateValueEventArgs(int beforeValue, int currentValue)
            {
                CurrentValue = currentValue;
                BeforeValue = beforeValue;
            }
        }

        public const int NumPresets = 7;
        public const int PresetId = 0;
    }

    public enum TabGroup
    {
        MainSettings,
        ImpostorRoles,
        MadmateRoles,
        CrewmateRoles,
        NeutralRoles,
        Combinations,
        Addons,
        GhostRoles
    }
    public enum OptionFormat
    {
        None,
        Players,
        Seconds,
        Percent,
        Times,
        /// <summary>x</summary>
        Multiplier,
        Votes,
        Pieces,
        day,
        Set
    }
    public enum OptionZeroNotation
    {
        None,
        Infinity,
        Hyphen,
        Off
    }
}