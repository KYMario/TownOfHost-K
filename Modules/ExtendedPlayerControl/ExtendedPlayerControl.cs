using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;

using static TownOfHost.Translator;
using UnityEngine.ProBuilder;

namespace TownOfHost
{
    static class ExtendedPlayerControl
    {
        public static InnerNet.ClientData GetClient(this PlayerControl player)
        {
            if (player?.isDummy ?? false) return null;
            if (!player) return null;
            var client = AmongUsClient.Instance?.allClients?.ToArray()?.Where(cd => cd?.Character?.PlayerId == player?.PlayerId)?.FirstOrDefault() ?? null;
            return client;
        }
        public static int GetClientId(this PlayerControl player)
        {
            if (player?.isDummy ?? false) return -1;
            var client = player?.GetClient();
            if (client == null) Logger.Error($"{player?.Data?.GetLogPlayerName() ?? "null"}のclientがnull", "GetClientId");
            return client == null ? -1 : client.Id;
        }
        public static CustomRoles GetCustomRole(this NetworkedPlayerInfo player)
        {
            return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
        }
        /// <summary>
        /// ※サブロールは取得できません。
        /// </summary>
        public static CustomRoles GetCustomRole(this PlayerControl player)
        {
            if (player == null)
            {
                var caller = new System.Diagnostics.StackFrame(1, false);
                var callerMethod = caller.GetMethod();
                string callerMethodName = callerMethod.Name;
                string callerClassName = callerMethod.DeclaringType.FullName;
                Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
                return CustomRoles.Crewmate;
            }
            var state = PlayerState.GetByPlayerId(player.PlayerId);

            return state?.MainRole ?? CustomRoles.Crewmate;
        }

        public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
        {
            if (player == null)
            {
                Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
                return new() { CustomRoles.NotAssigned };
            }
            else
                return PlayerState.GetByPlayerId(player.PlayerId)?.SubRoles ?? new() { CustomRoles.NotAssigned };
        }
        public static CountTypes GetCountTypes(this PlayerControl player)
        {
            if (player == null)
            {
                var caller = new System.Diagnostics.StackFrame(1, false);
                var callerMethod = caller.GetMethod();
                string callerMethodName = callerMethod.Name;
                string callerClassName = callerMethod.DeclaringType.FullName;
                Logger.Warn(callerClassName + "." + callerMethodName + "がCountTypesを取得しようとしましたが、対象がnullでした。", "GetCountTypes");
                return CountTypes.None;
            }

            return PlayerState.GetByPlayerId(player.PlayerId)?.CountType ?? CountTypes.None;
        }

        public static void SetKillCooldown(this PlayerControl player, float time = -1f, PlayerControl target = null, bool force = false, bool delay = false)
        {
            if (player == null) return;
            if (target == null) target = player;
            CustomRoles role = player.GetCustomRole();
            if (!player.CanUseKillButton() && !force) return;

            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) CustomButtonHud.BottonHud();

            if (player.CanUseKillButton())
            {
                if (IUsePhantomButton.IPPlayerKillCooldown.ContainsKey(player.PlayerId))
                {
                    IUsePhantomButton.IPPlayerKillCooldown[player.PlayerId] = 0;
                }
                if (!Main.AllPlayerKillCooldown.ContainsKey(player.PlayerId))
                {
                    player.ResetKillCooldown();
                }
                if (0f <= time)
                {
                    Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
                    if (force is false) player.GetPlayerState().Is10secKillButton = false;
                }
                else
                {
                    Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
                }
                player.SyncSettings();
            }
            else if (force) player.SyncSettings();

            if (delay)
            {
                _ = new LateTask(() =>
                {
                    player.RpcProtectedMurderPlayer(target);
                    if (player != target) player.RpcProtectedMurderPlayer();
                }, Main.LagTime, "Setkillcooldown delay", true);
            }
            else
            {
                player.RpcProtectedMurderPlayer(target);
                if (player != target) player.RpcProtectedMurderPlayer();
            }
            if (player.CanUseKillButton())
            {
                _ = new LateTask(() =>
                {
                    player.ResetKillCooldown();
                    player.SyncSettings();
                }, delay ? Main.LagTime + 1f : 1f, "", true);
            }
        }

        public static void MarkDirtySettings(this PlayerControl player)
        {
            PlayerGameOptionsSender.SetDirty(player.PlayerId);
        }
        public static void SyncSettings(this PlayerControl player)
        {
            PlayerGameOptionsSender.SetDirty(player.PlayerId);
            GameOptionsSender.SendAllGameOptions();
        }
        public static string GetSubRoleName(this PlayerControl player)
        {
            var SubRoles = PlayerState.GetByPlayerId(player.PlayerId).SubRoles;
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role == CustomRoles.NotAssigned) continue;
                sb.Append($"{Utils.ColorString(Color.white, " + ")}{UtilsRoleText.GetRoleName(role)}");
            }

            return sb.ToString();
        }
        public static string GetAllRoleName(this PlayerControl player)
        {
            if (!player) return null;
            var text = UtilsRoleText.GetRoleName(player.GetCustomRole());
            text += player.GetSubRoleName();
            return text;
        }
        public static string GetRoleColorCode(this PlayerControl player)
        {
            return UtilsRoleText.GetRoleColorCode(player.GetCustomRole());
        }
        public static Color GetRoleColor(this PlayerControl player)
        {
            var roleClass = player.GetRoleClass();
            var role = player.GetCustomRole();
            if (role is CustomRoles.Amnesiac && Amnesiac.IsWolf) return UtilsRoleText.GetRoleColor(CustomRoles.WolfBoy);

            if (player.Is(CustomRoles.Amnesia))
            {
                switch (role.GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor: return Palette.ImpostorRed;
                    case CustomRoleTypes.Neutral:
                    case CustomRoleTypes.Madmate: return Palette.DisabledGrey;
                    case CustomRoleTypes.Crewmate: return Palette.CrewmateBlue;
                }
            }
            if (player.GetMisidentify(out var missrole))
            {
                return UtilsRoleText.GetRoleColor(missrole);
            }

            return UtilsRoleText.GetRoleColor(role);
        }
        public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner || GameStates.IsLobby) return;

            var systemtypes = Utils.GetCriticalSabotageSystemType();
            _ = new LateTask(() =>
            {
                pc.RpcDesyncUpdateSystem(systemtypes, 128);
            }, 0f + delay, "Reactor Desync");

            _ = new LateTask(() =>
            {
                pc.RpcSpecificMurderPlayer();
            }, 0.3f + delay, "Murder To Reset Cam");

            _ = new LateTask(() =>
            {
                pc.RpcDesyncUpdateSystem(systemtypes, 16);
                if (Main.NormalOptions.MapId == 4) //Airship用
                    pc.RpcDesyncUpdateSystem(systemtypes, 17);
            }, 0.4f + delay, "Fix Desync Reactor", true);
        }
        public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null || GameStates.IsLobby) return;
            int clientId = pc.GetClientId();
            // Logger.Info($"{pc}", "ReactorFlash");
            var systemtypes = Utils.GetCriticalSabotageSystemType();
            float FlashDuration = Options.KillFlashDuration.GetFloat();

            Utils.NowKillFlash = true;
            pc.RpcDesyncUpdateSystem(systemtypes, 128);

            _ = new LateTask(() =>
            {
                pc.RpcDesyncUpdateSystem(systemtypes, 16);

                if (Main.NormalOptions.MapId == 4) //Airship用
                    pc.RpcDesyncUpdateSystem(systemtypes, 17);
            }, FlashDuration + delay, "Fix Desync Reactor", true);
            _ = new LateTask(() => Utils.NowKillFlash = false, (FlashDuration + delay) * 2, "", true);

        }

        public static bool CanUseKillButton(this PlayerControl pc)
        {
            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && !Main.showkillbutton && Main.CustomSprite.Value) return false;
            if (!pc.IsAlive()) return false;
            if (pc?.Data?.Role?.Role == RoleTypes.GuardianAngel) return false;

            if (pc.Is(CustomRoles.Amnesia) && !pc.Is(CustomRoleTypes.Impostor)) return false;

            var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseKillButton();

            if (pc.Is(CustomRoles.SlowStarter) && !pc.Is(CustomRoles.Mafia))
            {
                roleCanUse = SlowStarter.CanUseKill();
            }

            return roleCanUse ?? pc.Is(CustomRoleTypes.Impostor);
        }
        public static bool CanUseImpostorVentButton(this PlayerControl pc)
        {
            if (!pc.IsAlive()) return false;

            if (pc.Is(CustomRoles.Amnesia) && !pc.Is(CustomRoleTypes.Impostor)) return false;

            var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseImpostorVentButton();

            return roleCanUse ?? false;
        }
        public static bool CanUseSabotageButton(this PlayerControl pc)
        {
            if (SuddenDeathMode.NowSuddenDeathMode) return false;
            if (pc.Is(CustomRoles.Amnesia) && !pc.Is(CustomRoleTypes.Impostor)) return false;

            var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseSabotageButton();

            return roleCanUse ?? false;
        }
        /// <summary>
        /// 現在自身が誤識している役職を取得します。<br/>
        /// アムネシア処理は行われません<br/>
        /// </summary>
        /// <param name="role">誤認している役職</param>
        /// <returns>現在誤認しているか</returns>
        public static bool GetMisidentify(this PlayerControl pc, out CustomRoles role)
        {
            var roleclass = pc.GetRoleClass();

            if (roleclass is not null)
            {
                role = roleclass.Misidentify();
                return role != pc.GetCustomRole() && role is not CustomRoles.NotAssigned;
            }
            role = CustomRoles.NotAssigned;
            return false;
        }
        /// <summary>
        /// 能力行使相手の役職を取得する<br/>
        /// </summary>
        /// <returns></returns>
        public static CustomRoles GetTellResults(this PlayerControl target, PlayerControl pc)
        {
            var roleclass = target.GetRoleClass();
            var role = CustomRoles.NotAssigned;
            if (roleclass is not null)
            {
                role = roleclass.TellResults(pc);
            }
            if (role is CustomRoles.NotAssigned)
            {
                role = target.GetCustomRole();
            }
            if (role is CustomRoles.SKMadmate)
            {
                role = Options.MadTellOpt() is CustomRoles.NotAssigned ? role : Options.MadTellOpt();
            }
            return role;
        }
        public static void ResetKillCooldown(this PlayerControl player)
        {
            if (!Main.AllPlayerKillCooldown.ContainsKey(player.PlayerId)) Main.AllPlayerKillCooldown.Add(player.PlayerId, Options.DefaultKillCooldown);
            Main.AllPlayerKillCooldown[player.PlayerId] = (player.GetRoleClass() as IKiller)?.CalculateKillCooldown() ?? Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
            if (player.Is(CustomRoles.Serial))
                Main.AllPlayerKillCooldown[player.PlayerId] = Serial.KillCooldown.GetFloat();
            if (player.PlayerId == LastImpostor.currentId)
                LastImpostor.SetKillCooldown(player);
            if (player.PlayerId == LastNeutral.currentId)
                LastNeutral.SetKillCooldown(player);

            if (player.Is(CustomRoles.Amnesia) && Amnesia.OptionDefaultKillCool.GetBool()) Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown;
        }
        public static bool CanMakeMadmate(this PlayerControl player)
        {
            if (Amnesia.CheckAbilityreturn(player)) return false;
            var role = player.GetCustomRole();

            if (
            Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount ||
            player == null ||
            (player.Data.Role.Role != RoleTypes.Shapeshifter) || role.GetRoleInfo()?.BaseRoleType.Invoke() != RoleTypes.Shapeshifter)
            {
                return false;
            }

            var isSidekickableCustomRole = player.GetRoleClass() is ISidekickable sidekickable && sidekickable.CanMakeSidekick();

            return isSidekickableCustomRole ||
               role.CanMakeMadmate(); // ISideKickable対応前の役職はこちら
        }
        public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);

        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
        ///</summary>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);

        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
        ///</summary>
        ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
        {
            var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
            List<PlayerControl> rangePlayers = new();
            player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
            foreach (var pc in rangePlayersIL)
            {
                if (predicate(pc)) rangePlayers.Add(pc);
            }
            return rangePlayers;
        }
        public static PlayerControl TryGetKilltarget(this PlayerControl pc, bool IsOneclick = false)//SNR参考!(((
        {
            float killdis = NormalGameOptionsV09.KillDistances[Mathf.Clamp(GameManager.Instance.LogicOptions.currentGameOptions.GetInt(Int32OptionNames.KillDistance), 0, 2)];

            if (pc.Data.IsDead || pc.inVent) return null;

            var roletype = pc.GetCustomRole().GetCustomRoleTypes();
            Vector2 psi = pc.GetTruePosition();
            var ta = pc;
            foreach (var playerInfo in GameData.Instance.AllPlayers)
            {
                if (playerInfo.Disconnected || playerInfo.PlayerId == pc.PlayerId || playerInfo.IsDead) continue;

                var tage = playerInfo.Object;

                if (tage == null || tage.inVent) continue;
                if (!SuddenDeathMode.NowSuddenDeathTemeMode && IsOneclick && tage.GetCustomRole().GetCustomRoleTypes() == roletype && roletype is CustomRoleTypes.Impostor
                && !tage.Is(CustomRoles.OneWolf) && !pc.Is(CustomRoles.OneWolf)) continue;
                if (SuddenDeathMode.NowSuddenDeathTemeMode)
                {
                    if (SuddenDeathMode.IsSameteam(pc.PlayerId, tage.PlayerId)) continue;
                }

                var vector = tage.GetTruePosition() - psi;
                float dis = vector.magnitude;

                if (IsOneclick)//ワンクリ取得のラグが洒落にならん位えぐいから補正
                {
                    dis = Mathf.Clamp(dis - 2f, 0.01f, 99);
                }
                if (dis > killdis || PhysicsHelpers.AnyNonTriggersBetween(psi, vector.normalized, dis, Constants.ShipAndObjectsMask)) continue;
                killdis = dis;
                ta = tage;
            }
            if (ta == pc) return null;
            return ta;
        }
        public static bool IsNeutralKiller(this CustomRoles role)
        {
            return
                role is
                CustomRoles.Egoist or
                CustomRoles.Banker or
                CustomRoles.DoppelGanger or
                CustomRoles.Jackal or
                CustomRoles.JackalMafia or
                CustomRoles.JackalAlien or
                CustomRoles.Remotekiller or
                CustomRoles.CountKiller or
                CustomRoles.Altair;
        }
        public static bool IsNeutralKiller(this PlayerControl player)
        {
            if (player.GetRoleClass() is BakeCat bakeCat) return bakeCat.Team is not ISchrodingerCatOwner.TeamType.None;

            return player.GetCustomRole().IsNeutralKiller();
        }
        public static bool KnowDeathReason(this PlayerControl seer, PlayerControl seen)
        {
            // targetが生きてたらfalse
            if (seen.IsAlive())
            {
                return false;
            }
            // seerが死亡済で，霊界から死因が見える設定がON
            if (!seer.IsAlive() && (Options.GhostCanSeeDeathReason.GetBool() || !Options.GhostOptions.GetBool()) && !seer.Is(CustomRoles.AsistingAngel) && (!seer.IsGhostRole() || Options.GhostRoleCanSeeDeathReason.GetBool()))
            {
                return true;
            }

            var check = false;

            // 役職による仕分け
            if (seer.GetRoleClass() is IDeathReasonSeeable deathReasonSeeable)
            {
                if (Amnesia.CheckAbility(seer))
                {
                    var role = deathReasonSeeable.CheckSeeDeathReason(seen);
                    if (role is null) return false;
                    check |= role is true;
                }
            }

            if (seer.Is(CustomRoles.LastImpostor) && LastImpostor.GiveAutopsy.GetBool()) check |= !Utils.IsActive(SystemTypes.Comms) || LastImpostor.AutopsyCanSeeComms.GetBool();
            if (seer.Is(CustomRoles.LastNeutral) && LastNeutral.GiveAutopsy.GetBool()) check |= !Utils.IsActive(SystemTypes.Comms) || LastNeutral.AutopsyCanSeeComms.GetBool();

            if (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer, subrole: CustomRoles.Autopsy))
                if (data.GiveAutopsy.GetBool()) check |= !Utils.IsActive(SystemTypes.Comms) || data.AutopsyCanSeeComms.GetBool();

            // IDeathReasonSeeable未対応役職はこちら
            return check ||
            (seer.Is(CustomRoleTypes.Madmate) && Options.MadmateCanSeeDeathReason.GetBool())
            || (seer.Is(CustomRoles.Autopsy) && (!Utils.IsActive(SystemTypes.Comms) || Autopsy.CanUseActiveComms.GetBool()));
        }
        public static string GetRoleDesc(this PlayerControl player, bool InfoLong = false)
        {
            var roleClass = player.GetRoleClass();
            var role = player.GetCustomRole();
            if (player.Is(CustomRoles.Amnesia))
                role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
            if (player.GetMisidentify(out var missrole))
            {
                role = missrole;
            }
            if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
                InfoLong = false;

            var text = role.ToString();

            var Prefix = "";
            if (!InfoLong)
                switch (role)
                {
                    case CustomRoles.Mafia:
                        if (roleClass is not Mafia mafia) break;

                        Prefix = mafia.CanUseKillButton() ? "After" : "Before";
                        break;
                    case CustomRoles.MadSnitch:
                    case CustomRoles.MadGuardian:
                        text = CustomRoles.Madmate.ToString();
                        Prefix = player.GetPlayerTaskState().IsTaskFinished ? "" : "Before";
                        break;
                }

            if (role is CustomRoles.Amnesiac)
            {
                if (roleClass is Amnesiac amnesiac && !amnesiac.Realized)
                {
                    text = Amnesiac.IsWolf ? CustomRoles.WolfBoy.ToString() : CustomRoles.Sheriff.ToString();
                }
            }

            var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
            if (player.IsGhostRole())
            {
                var state = PlayerState.GetByPlayerId(player.PlayerId);
                if (state != null)
                {
                    return Utils.ColorString(UtilsRoleText.GetRoleColor(state.GhostRole), GetString($"{state.GhostRole}Info"));
                }
            }
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                var suddeninfo = "<size=60%>" + GetString($"{Prefix}{text}{Info}") + "\n</size>";
                suddeninfo += "<size=80%>" + GetString("SuddenDeathModeInfo") + "</size>";
                return suddeninfo;
            }
            return GetString($"{Prefix}{text}{Info}");
        }
        public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
        {
            if (target == null)
            {
                Logger.Info("target=null", "SetRealKiller");
                return;
            }
            var State = PlayerState.GetByPlayerId(target.PlayerId);
            if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
            byte killerId = killer == null ? byte.MaxValue : killer.PlayerId;
            RPC.SetRealKiller(target.PlayerId, killerId);
            if (killer?.PlayerId is 0)
            {
                Main.HostKill.TryAdd(target.PlayerId, State.DeathReason);
            }
            if (!(AntiBlackout.IsCached || GameStates.CalledMeeting || GameStates.ExiledAnimate))
                Twins.TwinsSuicide();
        }
        public static PlayerControl GetRealKiller(this PlayerControl target)
        {
            var killerId = PlayerState.GetByPlayerId(target.PlayerId).GetRealKiller();
            return killerId == byte.MaxValue ? null : PlayerCatch.GetPlayerById(killerId);
        }
        public static PlainShipRoom GetPlainShipRoom(this PlayerControl pc, bool IsDead = false)
        {
            var Rooms = ShipStatus.Instance.AllRooms;
            if (Rooms == null) return null;

            if (!pc.IsAlive())
            {
                PlainShipRoom room = null;
                if (IsDead)
                {
                    foreach (var psr in Rooms)
                    {
                        if (psr.roomArea.OverlapPoint((Vector2)pc.transform.position))
                        {
                            room = psr;
                        }
                    }
                }
                return room;
            }
            foreach (var room in Rooms)
            {
                if (!room.roomArea) continue;
                if (pc.Collider.IsTouching(room.roomArea))
                    return room;
            }
            return null;
        }
        public static string GetShipRoomName(this PlayerControl pc)
        {
            if (ShipStatus.Instance is null || pc is null) return "";
            var RoomName = "";
            var Room = pc.GetPlainShipRoom(true);
            RoomName = Room is null ? "" : GetString($"{Room.RoomId}");

            if (Room?.RoomId is SystemTypes.Hallway or null)
            {
                var AllRooms = ShipStatus.Instance.AllRooms;
                Dictionary<byte, float> Distance = new();

                if (AllRooms != null)
                {
                    if (Main.NormalOptions.MapId is (byte)MapNames.Fungle)
                    {
                        Distance.Add(200, Vector2.Distance(pc.transform.position, new Vector2(-7.95f, -14.10f))); //西ジャングル
                        Distance.Add(201, Vector2.Distance(pc.transform.position, new Vector2(1.74f, -9.76f)));//中央ジャングル
                        Distance.Add(202, Vector2.Distance(pc.transform.position, new Vector2(15.81f, -8.3f)));//東ジャングル
                        Distance.Add(203, Vector2.Distance(pc.transform.position, new Vector2(-8.95f, 1.79f)));//焚火
                    }
                    foreach (var room in AllRooms)
                    {
                        if (room.RoomId == SystemTypes.Hallway) continue;
                        Distance.Add((byte)room.RoomId, Vector2.Distance(pc.transform.position, room.transform.position));
                    }
                }
                var Nearestroomid = Distance.OrderByDescending(x => x.Value).Last().Key;
                if (Room is not null)
                {
                    if (Room?.RoomId is SystemTypes.Hallway && 200 > Nearestroomid && (SystemTypes)Nearestroomid is SystemTypes.VaultRoom)
                        Nearestroomid = (byte)SystemTypes.Comms;
                }
                var Nearestroom = 200 <= Nearestroomid ? GetString($"ModMapName.{Nearestroomid}") : GetString($"{(SystemTypes)Nearestroomid}");
                RoomName = Room is null ? string.Format(GetString("Nearroom"), Nearestroom)
                : Nearestroom + RoomName;
            }
            return RoomName;
        }
        public static bool IsProtected(this PlayerControl self) => self.protectedByGuardianId > -1;

        //汎用
        public static bool Is(this PlayerControl target, CustomRoles role) =>
            role > CustomRoles.NotAssigned ? (role.IsGhostRole() ? PlayerState.GetByPlayerId(target.PlayerId).GhostRole == role : target.GetCustomSubRoles().Contains(role)) : target.GetCustomRole() == role;
        public static bool Is(this PlayerControl target, CustomRoleTypes type) { return target.GetCustomRole().GetCustomRoleTypes() == type; }
        public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
        public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
        public static bool IsAlive(this PlayerControl target)
        {
            //ロビーなら生きている
            if (GameStates.IsLobby)
            {
                return true;
            }
            //targetがnullならば切断者なので生きていない
            if (target == null)
            {
                return false;
            }
            //targetがnullでなく取得できない場合は登録前なので生きているとする
            if (PlayerState.GetByPlayerId(target.PlayerId) is not PlayerState state)
            {
                return true;
            }
            return !state.IsDead;
        }

        public static PlayerControl GetKillTarget(this PlayerControl player, bool IsOneclick)
        {
            var playerrole = player.GetCustomRole();

            if (IsOneclick && !player.AmOwner) return player.TryGetKilltarget(true);

            if (player.AmOwner && GameStates.IsInTask && !GameStates.Intro && !(playerrole.IsImpostor() || playerrole is CustomRoles.Egoist) && (playerrole.GetRoleInfo()?.IsDesyncImpostor ?? false) && !player.Data.IsDead)
            {
                return player.TryGetKilltarget();
            }
            var players = player.GetPlayersInAbilityRangeSorted(false);
            return players.Count <= 0 ? null : players[0];
        }

        //アプデ対応の参考
        //https://github.com/Hyz-sui/TownOfHost-H
        public const MurderResultFlags SucceededFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;
        public const MurderResultFlags SuccessFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;
    }
}
