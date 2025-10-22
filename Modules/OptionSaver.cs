using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HarmonyLib;
using TownOfHost.Roles.Core;

namespace TownOfHost.Modules;

public static class OptionSaver
{
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new(Main.BaseDirectory + "/SaveData/");
    private static readonly FileInfo OptionSaverFileInfo = new($"{SaveDataDirectoryInfo.FullName}/Options_TOHkv{Version}.json");
    private static readonly LogHandler logger = Logger.Handler(nameof(OptionSaver));

    public static void Initialize()
    {
        if (!SaveDataDirectoryInfo.Exists)
        {
            SaveDataDirectoryInfo.Create();
            SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
        }
        if (!OptionSaverFileInfo.Exists)
        {
            OptionSaverFileInfo.Create().Dispose();
        }
        /*バグり散らかしてv30に戻したいってのがなくなるまで削除処理を入れないでおく。
        FileInfo oldinfo = new($"{SaveDataDirectoryInfo.FullName}/Options_TOHk.json");
        if (oldinfo.Exists)
        {
            oldinfo.Delete();
        }*/
    }
    /// <summary>現在のオプションからjsonシリアライズ用のオブジェクトを生成</summary>
    private static SerializableOptionsData GenerateOptionsData()
    {
        Dictionary<int, int> singleOptions = new();
        Dictionary<int, int[]> presetOptions = new();
        Dictionary<int, Dictionary<int, int[]>> assignOptions = new();
        foreach (var option in OptionItem.AllOptions)
        {
            if (option.IsSingleValue)
            {
                if (!singleOptions.TryAdd(option.Id, option.SingleValue))
                {
                    logger.Warn($"SingleOptionのID {option.Id} が重複");
                }
            }
            else if (option is AssignOptionItem assignOptionitem)
            {
                Dictionary<int, int[]> assign = new();

                var i = 0;
                foreach (var _ in option.AllValues)
                {
                    List<int> ids = new();
                    if (assignOptionitem.RoleValues.Count <= 0)
                    {
                        assign.Add(i, []);

                        i++;
                        continue;
                    }
                    assignOptionitem.RoleValues[i].Do(role =>
                    {
                        ids.Add(role.GetRoleInfo()?.ConfigId ?? -300);
                    });

                    assign.Add(i, ids.ToArray());
                    i++;
                }
                if (!assignOptions.TryAdd(option.Id, assign))
                {
                    logger.Warn($"アサインオプションの{option.Id}が重複");
                }
            }
            else if (!presetOptions.TryAdd(option.Id, option.AllValues))
            {
                logger.Warn($"プリセットオプションのID {option.Id} が重複");
            }
        }
        return new SerializableOptionsData
        {
            Version = Version,
            SingleOptions = singleOptions,
            PresetOptions = presetOptions,
            AssignOptions = assignOptions,
        };
    }
    /// <summary>デシリアライズされたオブジェクトを読み込み，オプション値を設定</summary>
    private static void LoadOptionsData(SerializableOptionsData serializableOptionsData)
    {
        if (serializableOptionsData.Version != Version)
        {
            // 今後バージョン間の移行方法を用意する場合，ここでバージョンごとの変換メソッドに振り分ける
            logger.Info($"読み込まれたオプションのバージョン {serializableOptionsData.Version} が現在のバージョン {Version} と一致しないためデフォルト値で上書きします");
            Save();
            Main.Preset1.Value = Translator.GetString("Preset_1");
            Main.Preset2.Value = Translator.GetString("Preset_2");
            Main.Preset3.Value = Translator.GetString("Preset_3");
            Main.Preset4.Value = Translator.GetString("Preset_4");
            Main.Preset5.Value = Translator.GetString("Preset_5");
            Main.Preset6.Value = Translator.GetString("Preset_6");
            Main.Preset7.Value = Translator.GetString("Preset_7");
            return;
        }
        Dictionary<int, int> singleOptions = serializableOptionsData.SingleOptions;
        Dictionary<int, int[]> presetOptions = serializableOptionsData.PresetOptions;
        Dictionary<int, Dictionary<int, int[]>> assignOptions = serializableOptionsData.AssignOptions;
        foreach (var singleOption in singleOptions)
        {
            var id = singleOption.Key;
            var value = singleOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetValue(value, doSave: false);
            }
        }
        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var values = presetOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetAllValues(values);
            }
        }
        foreach (var assignoption in assignOptions)
        {
            var id = assignoption.Key;
            var values = assignoption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                if (optionItem is AssignOptionItem assignOptionItem)
                {
                    Dictionary<int, List<CustomRoles>> role = new();
                    foreach (var item in values)
                    {
                        List<CustomRoles> rolelist = new();
                        if (item.Value.Count() <= 0)
                        {
                            role.Add(item.Key, rolelist);
                            continue;
                        }
                        foreach (var roleid in item.Value)
                        {
                            var roleopt = OptionItem.AllOptions.Where(opt => opt.Id == roleid).FirstOrDefault();

                            if (roleopt is not null)
                            {
                                rolelist.Add(roleopt.CustomRole);
                            }
                            else if (roleid is -1) rolelist.Add(CustomRoles.Crewmate);
                            else if (roleid is -3) rolelist.Add(CustomRoles.Impostor);
                        }
                        role.Add(item.Key, rolelist);
                    }
                    assignOptionItem.RoleValues = role;
                }
            }
        }
    }
    /// <summary>現在のオプションをjsonファイルに保存</summary>
    public static void Save()
    {
        // 接続済みで，ホストじゃなければ保存しない
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        var jsonString = JsonSerializer.Serialize(GenerateOptionsData(), new JsonSerializerOptions { WriteIndented = true, });
        File.WriteAllText(OptionSaverFileInfo.FullName, jsonString);
    }
    /// <summary>jsonファイルからオプションを読み込み</summary>
    public static void Load()
    {
        var jsonString = File.ReadAllText(OptionSaverFileInfo.FullName);
        // 空なら読み込まず，デフォルト値をセーブする
        if (jsonString.Length <= 0)
        {
            logger.Info("オプションデータが空のためデフォルト値を保存");
            Save();
            return;
        }
        LoadOptionsData(JsonSerializer.Deserialize<SerializableOptionsData>(jsonString));
    }

    /// <summary>json保存に適したオプションデータ</summary>
    public class SerializableOptionsData
    {
        public int Version { get; init; }
        /// <summary>プリセット外のオプション</summary>
        public Dictionary<int, int> SingleOptions { get; init; }
        /// <summary>プリセット内のオプション</summary>
        public Dictionary<int, int[]> PresetOptions { get; init; }
        /// <summary>アサインオプション</summary>
        public Dictionary<int, Dictionary<int, int[]>> AssignOptions { get; init; }
    }

    /// <summary>オプションの形式に互換性のない変更(プリセット数変更など)を加えるときはここの数字を上げる</summary>
    public const int Version = 4;
}
