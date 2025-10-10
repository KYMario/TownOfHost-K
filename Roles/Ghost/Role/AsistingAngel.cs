using UnityEngine;
using System.Linq;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using Hazel;

namespace TownOfHost.Roles.Ghost
{
    public class AsistingAngel
    {
        private static readonly int Id = 22000;
        public static byte AsistingAngelId = byte.MaxValue;
        public static OptionItem CoolDown;
        public static OptionItem AddClowDown;
        public static OptionItem Guardtime;
        public static OptionItem LimitDay;
        public static PlayerControl Asist;
        public static float Count;
        public static byte Track;
        public static bool Guard;
        public static int Limit;
        public static Vector3 pos;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.AsistingAngel, new(1, 1, 1));
            GhostRoleAssingData.Create(Id + 1, CustomRoles.AsistingAngel, CustomRoleTypes.Crewmate, CustomRoleTypes.Neutral);
            CoolDown = FloatOptionItem.Create(Id + 2, "AsistingAngelCoolDown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]).SetParentRole(CustomRoles.AsistingAngel);
            AddClowDown = FloatOptionItem.Create(Id + 3, "AsistingAngelAddClowDown", new(0f, 30f, 0.5f), 5f, TabGroup.GhostRoles, false)
            .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]).SetParentRole(CustomRoles.AsistingAngel);
            Guardtime = FloatOptionItem.Create(Id + 4, "AsistingAngelGuardtime", new(1f, 30f, 1f), 5f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]).SetParentRole(CustomRoles.AsistingAngel);
            LimitDay = IntegerOptionItem.Create(Id + 5, "AsistingAngelLimitDay", new(0, 5, 1), 3, TabGroup.GhostRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]).SetParentRole(CustomRoles.AsistingAngel);
        }
        public static void Init()
        {
            AsistingAngelId = byte.MaxValue;
            Asist = null;
            Track = byte.MaxValue;
            Count = 0;
            Limit = 0;
            Guard = false;
            pos = new Vector3(999f, 999f);
            CustomRoleManager.MarkOthers.Add(OtherMark);
            SubRoleRPCSender.AddHandler(CustomRoles.AsistingAngel, ReceiveRPC);
        }
        public static void Add(byte playerId)
        {
            AsistingAngelId = playerId;
        }

        public static bool CanSetAsistTarget()
        {
            //アシスト先が決まってるなら～
            if (Asist != null) return false;
            foreach (var pc in PlayerCatch.AllPlayerControls.Where(x => x.IsGhostRole()))
            {
                if (pc.Is(CustomRoles.AsistingAngel)) return true;
            }
            return false;
        }

        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.AsistingAngel))
            {
                //アシスト先が決まってない場合
                if (Limit > LimitDay.GetFloat() && Asist == null) return;//経過日数が設定を超えたらもう負け。

                if (Asist == null)
                {
                    Asist = target;
                    SendRPC(AsistingAngelId);
                    pc.RpcResetAbilityCooldown();
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: [target, pc]);
                    Logger.Info($"Set:{pc.Data.GetLogPlayerName()} => {Asist.Data.GetLogPlayerName()}", "AsistingAngel");
                }
                else
                {
                    //どっちかは知らんが回数とリセットは入れるで～
                    Count++;
                    pc.RpcResetAbilityCooldown(Sync: true);

                    if (!Asist.IsAlive()) return;//アシスト対象が死んでるならでしゃばるな。

                    if (target == Asist)//アシスト対象なら一定時間ばりあー
                    {
                        Guard = true;
                        _ = new LateTask(() =>
                        {
                            Guard = false;
                            pc.RpcResetAbilityCooldown(Sync: true);//成功の有無にかかわらずリセットさせる。
                        }, Guardtime.GetFloat(), "AsistingAngelSetGuard", true);
                    }
                    else//違うなら対象の位置を矢印で教えないとねっ
                    {
                        //リセット処理
                        GetArrow.Remove(Asist.PlayerId, pos);
                        Track = target.PlayerId;
                        pos = target.transform.position;
                        GetArrow.Add(Asist.PlayerId, target.transform.position);
                        SendRPC(AsistingAngelId);
                        UtilsNotifyRoles.NotifyRoles(SpecifySeer: [target, pc]);
                        pc.RpcResetAbilityCooldown(Sync: true);
                    }
                }
            }
        }
        public static string OtherMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;

            //タゲ未設定時
            if (Limit > LimitDay.GetFloat() && Asist == null) return "";//過ぎたなら後は後悔しなっ。
            if (Asist == null && seer == seen && seer.Is(CustomRoles.AsistingAngel)) return $"<color=#8da0d6> ({Limit}/{LimitDay.GetFloat()})</color>";
            if (Asist == null) return "";

            //タゲがいる時
            var MarkAndArrow = "";
            if (seer == seen)
                if (seer == Asist || seer.Is(CustomRoles.AsistingAngel))//対象 or アシストの場合
                {
                    MarkAndArrow = "<color=#8da0b6>＠</color>";
                    if (Track is not byte.MaxValue && !isForMeeting)
                    {
                        return MarkAndArrow + $"  <color=#8da0b6>{GetArrow.GetArrows(seer, pos)}</color>";
                    }
                    else
                        return MarkAndArrow;
                }

            if (!seer.IsAlive())//霊界の場合
            {
                if (seen == Asist || seen.Is(CustomRoles.AsistingAngel))
                    return "<color=#8da0b6>＠</color>";
            }

            return "";
        }
        public static bool CheckAddWin()
        {
            if (AsistingAngelId is byte.MaxValue) return false;
            if (AsistingAngelId.GetPlayerState()?.GhostRole is CustomRoles.AsistingAngel)
            {
                if (Asist != null)
                {
                    if (CustomWinnerHolder.WinnerIds.Contains(Asist.PlayerId) || CustomWinnerHolder.WinnerRoles.Contains(Asist.GetCustomRole()))
                    {
                        CustomWinnerHolder.CantWinPlayerIds.Remove(AsistingAngelId);
                        CustomWinnerHolder.WinnerIds.Add(AsistingAngelId);
                        CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.AsistingAngel);
                        Logger.Info($"{AsistingAngelId} => Asist対象が勝利したので自身も勝利", "AsistingAngel");
                        return true;
                    }
                    Logger.Info($"{AsistingAngelId} => Asist対象が負けてるので負け。", "AsistingAngel");
                }
                else
                {
                    Logger.Info($"{AsistingAngelId} => Asist対象がいないので負け", "AsistingAngel");
                }
                CustomWinnerHolder.CantWinPlayerIds.Add(AsistingAngelId);
            }

            return false;
        }

        public static float GetNowCoolDown()
        {
            if (Asist == null) return CoolDown.GetFloat();

            return CoolDown.GetFloat() + (AddClowDown.GetFloat() * Count);
        }

        public static void SendRPC(byte playerId)
        {
            var isPos999 = pos == new Vector3(999f, 999f);
            using var sender = new SubRoleRPCSender(CustomRoles.AsistingAngel, playerId);
            sender.Writer.Write(Limit);
            sender.Writer.Write(Track);
            sender.Writer.Write(Asist?.PlayerId ?? byte.MaxValue);
            sender.Writer.Write(isPos999);
            if (!isPos999) NetHelpers.WriteVector2(pos, sender.Writer);
        }

        public static void ReceiveRPC(MessageReader reader, byte playerId)
        {
            Limit = reader.ReadInt32();
            Track = reader.ReadByte();
            var AsistId = reader.ReadByte();
            Asist = AsistId == byte.MaxValue ? null : PlayerCatch.GetPlayerById(AsistId);

            var newPos = reader.ReadBoolean() ? new Vector2(999f, 999f) : NetHelpers.ReadVector2(reader);

            if (newPos != (Vector2)pos)
            {
                GetArrow.Remove(playerId, pos);
                pos = newPos;
                GetArrow.Add(playerId, pos);
            }
        }
    }
}