using System.Collections.Generic;
using Hazel;
using TownOfHost.Attributes;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public static class CustomWinnerHolder
    {
        // 勝者のチームが格納されます。
        // リザルトの背景色の決定などに使用されます。
        // 注: この変数を変更する時、WinnerRoles・WinnerIdsを同時に変更しないと予期せぬ勝者が現れる可能性があります。
        public static CustomWinner WinnerTeam;
        // 追加勝利するプレイヤーの役職が格納されます。
        // リザルトの表示に使用されます。
        public static HashSet<CustomRoles> AdditionalWinnerRoles;
        // 勝者の役職が格納され、この変数に格納されている役職のプレイヤーは全員勝利となります。
        // チームとなるニュートラルの処理に最適です。
        public static HashSet<CustomRoles> WinnerRoles;
        // 勝者のPlayerIDが格納され、このIDを持つプレイヤーは全員勝利します。
        // 単独勝利するニュートラルの処理に最適です。
        public static HashSet<byte> WinnerIds;

        // 元役職に関わらず敗北するPlayerIdが格納され、
        // このID持つプレイヤーは問答無用で負けます
        public static HashSet<byte> IdRemoveLovers;

        // 勝利優先順位の最高値です。
        // この数値より大きい(同値除く)と勝利を上書きします
        public static int WinPriority;

        [GameModuleInitializer, PluginModuleInitializer]
        public static void Reset()
        {
            WinnerTeam = CustomWinner.Default;
            AdditionalWinnerRoles = new();
            WinnerRoles = new();
            WinnerIds = new();
            IdRemoveLovers = new();
            WinPriority = -1;
            GameStates.Meeting = false;
        }
        public static void ClearWinners()
        {
            IdRemoveLovers.Clear();
            WinnerRoles.Clear();
            WinnerIds.Clear();
            WinPriority = -1;
        }
        /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、AdditionalWinnerRolesに追加します。</para></summary>
        public static void SetWinnerOrAdditonalWinner(CustomWinner winner)
        {
            GameStates.Meeting = false;
            if (WinnerTeam == CustomWinner.Default) WinnerTeam = winner;
            else AdditionalWinnerRoles.Add((CustomRoles)winner);
        }
        /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、既存の値をAdditionalWinnerRolesに追加してから代入します。</para></summary>
        public static void ShiftWinnerAndSetWinner(CustomWinner winner)
        {
            if (WinnerTeam != CustomWinner.Default)
                AdditionalWinnerRoles.Add((CustomRoles)WinnerTeam);
            WinnerTeam = winner;
        }
        /// <summary><para>既存の値をすべて削除してから、WinnerTeamに値を代入します。</para></summary>
        public static void ResetAndSetWinner(CustomWinner winner)
        {
            GameStates.Meeting = false;
            Logger.Info($"{WinnerTeam} => {winner}", "CustomWinner");
            Reset();
            if (SoloWinOption.AllData.TryGetValue((CustomRoles)winner, out var data))
            {
                WinPriority = data.OptionWin.GetInt();
            }
            WinnerTeam = winner;
        }

        /// <summary>
        /// 設定された勝利優先順位に基づいて勝利判定をする
        /// </summary>
        /// <param name="winner">勝利者</param>
        /// <param name="playerId">勝利者のid</param>
        /// <param name="AddWin">同値だった場合、追加勝利するか</param>
        public static bool ResetAndSetAndChWinner(CustomWinner winner, byte playerId, bool AddWin = true, CustomRoles hantrole = CustomRoles.NotAssigned)
        {
            GameStates.Meeting = false;
            Logger.Info($"RASACW {WinnerTeam} => {winner}", "CustomWinner");

            if (SoloWinOption.AllData.TryGetValue(hantrole is CustomRoles.NotAssigned ? (CustomRoles)winner : hantrole, out var data))
            {
                //現在値より設定値が大きい
                if (WinPriority < data.OptionWin.GetInt())
                {
                    //単独勝利
                    Reset();
                    WinPriority = data.OptionWin.GetInt();
                    WinnerTeam = winner;
                    if (playerId is byte.MaxValue) return true;
                    WinnerIds.Add(playerId);
                    return true;
                }
                else if (WinPriority == data.OptionWin.GetInt() && AddWin)
                {
                    //追加勝利
                    AdditionalWinnerRoles.Add((CustomRoles)winner);
                    if (playerId is byte.MaxValue) return true;
                    WinnerIds.Add(playerId);
                    IdRemoveLovers.Remove(playerId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Logger.Error($"{winner} is no Data", "CustomWinner");
                return false;
            }
        }

        public static MessageWriter WriteTo(MessageWriter writer)
        {
            writer.WritePacked((int)WinnerTeam);

            writer.WritePacked(AdditionalWinnerRoles.Count);
            foreach (var wr in AdditionalWinnerRoles)
                writer.WritePacked((int)wr);

            writer.WritePacked(WinnerRoles.Count);
            foreach (var wr in WinnerRoles)
                writer.WritePacked((int)wr);

            writer.WritePacked(WinnerIds.Count);
            foreach (var id in WinnerIds)
                writer.Write(id);

            writer.WritePacked(IdRemoveLovers.Count);
            foreach (var lid in IdRemoveLovers)
                writer.Write(lid);

            return writer;
        }
        public static void ReadFrom(MessageReader reader)
        {
            WinnerTeam = (CustomWinner)reader.ReadPackedInt32();

            AdditionalWinnerRoles = new();
            int AdditionalWinnerRolesCount = reader.ReadPackedInt32();
            for (int i = 0; i < AdditionalWinnerRolesCount; i++)
                AdditionalWinnerRoles.Add((CustomRoles)reader.ReadPackedInt32());

            WinnerRoles = new();
            int WinnerRolesCount = reader.ReadPackedInt32();
            for (int i = 0; i < WinnerRolesCount; i++)
                WinnerRoles.Add((CustomRoles)reader.ReadPackedInt32());

            WinnerIds = new();
            int WinnerIdsCount = reader.ReadPackedInt32();
            for (int i = 0; i < WinnerIdsCount; i++)
                WinnerIds.Add(reader.ReadByte());

            IdRemoveLovers = new();
            int IdRemoveLoversCount = reader.ReadPackedInt32();
            for (int i = 0; i < IdRemoveLoversCount; i++)
                IdRemoveLovers.Add(reader.ReadByte());
        }
    }
}