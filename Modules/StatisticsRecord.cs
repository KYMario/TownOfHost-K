/*using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using static TownOfHost.PlayerCatch;
using static TownOfHost.UtilsRoleText;
using Rewired.Utils.Classes.Data;

namespace TownOfHost
{
    public class StatisticsRecordSave
    {
        private static readonly string PATH = new($"{Application.persistentDataPath}/TownOfHost_K/StatisticsRecord.txt");
        public static void SetLogFolder()
        {
            try
            {
                if (!Directory.Exists($"{Application.persistentDataPath}/TownOfHost_K"))
                    Directory.CreateDirectory($"{Application.persistentDataPath}/TownOfHost_K");
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                SetLogFolder();
                if (File.Exists($"{Application.persistentDataPath}/TownOfHost_K/Statistics.txt"))
                {
                    File.Move($"{Application.persistentDataPath}/TownOfHost_K/Statistics.txt", PATH);
                }
                var text = "";
                foreach (var record in StatisticsRecord.AllRecord)
                {
                    text += $"^{record.id}^{(record.completed ? "1" : "0")}^";
                }
            }
            catch
            {
                Logger.Error("Saveでエラー！", "Statistics");
            }
        }
        public static Statistics Load()
        {
        }
        public class StatisticsRecord
        {
            public static List<StatisticsRecord> AllRecord = new();
            public string Name;
            public bool completed;
            public int id;
            public int Difficult;
            public StatisticsRecord(string name, int id, int difficult)
            {
                Name = name;
                completed = false;
                this.id = id;
                Difficult = difficult;
                AllRecord.Add(this);
            }
            public void Complete()
            {
                completed = true;
                Logger.Info($"{Name}が<kv.12>を<少し>達<くみて>成<りたつ。>".RemoveHtmlTags(), "S<>ta<>tis<>ti<>cs<>Re<>c<>ord<>");
            }
            public void Load(bool completed)
            {
                this.completed = completed;
            }
        }
    }
}
*/