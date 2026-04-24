using System;
using System.Collections.Generic;
using Assets.InnerNet;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using InnerNet;
using UnityEngine;
#nullable enable

namespace frinedintive;

[BepInPlugin("jp.tobyp.invitepatch", "Friend Invite Patch", "1.0.0")]
public sealed class Plugin : BasePlugin
{
    private const string InviteButtonObjectName = "InviteAllFriendsButton";
    private static readonly Vector3 InviteButtonOffset = new(-0.68f, 0f, 0f);

    private static ManualLogSource _logger = null!;
    private Harmony? _harmony;

    public override void Load()
    {
        _logger = Log;
        _harmony = new Harmony("jp.tobyp.invitepatch");

        PatchHooks();

        _logger.LogInfo("Friend Invite Patch loaded. Press F2 in lobby to invite all friends.");
    }

    private void PatchHooks()
    {
        try
        {
            var updateMethod = AccessTools.Method(typeof(GameStartManager), nameof(GameStartManager.Update));
            var updatePostfix = AccessTools.Method(typeof(Plugin), nameof(GameStartManagerUpdatePostfix));
            var receiveClickUpMethod = AccessTools.Method(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp));
            var receiveClickUpPrefix = AccessTools.Method(typeof(Plugin), nameof(PassiveButtonReceiveClickUpPrefix));

            if (updateMethod == null || updatePostfix == null || receiveClickUpMethod == null || receiveClickUpPrefix == null)
            {
                _logger.LogError("Failed to find required methods for harmony patching.");
                return;
            }

            _harmony!.Patch(updateMethod, postfix: new HarmonyMethod(updatePostfix));
            _harmony.Patch(receiveClickUpMethod, prefix: new HarmonyMethod(receiveClickUpPrefix));

            _logger.LogInfo("Harmony patches applied.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to apply harmony patches: {ex}");
        }
    }

    private static void GameStartManagerUpdatePostfix()
    {
        var gameStartManager = GameStartManager.Instance;
        if (gameStartManager != null)
        {
            TryCreateInviteButton(gameStartManager.LobbyInfoPane);
        }

        if (!Input.GetKeyDown(KeyCode.F2))
        {
            return;
        }

        TryInviteAllFriends("F2");
    }

    private static bool PassiveButtonReceiveClickUpPrefix(PassiveButton __instance)
    {
        if (__instance == null || __instance.gameObject == null)
        {
            return true;
        }

        if (!string.Equals(__instance.gameObject.name, InviteButtonObjectName, StringComparison.Ordinal))
        {
            return true;
        }

        TryInviteAllFriends("Button");
        return false;
    }

    private static void TryInviteAllFriends(string trigger)
    {
        if (!TryGetLobbyContext(out _, out var friendsListManager, out var roomCode))
        {
            return;
        }

        if (friendsListManager.Friends != null && friendsListManager.Friends.Count > 0)
        {
            SendInvitesFromCachedFriends(friendsListManager, friendsListManager.Friends, roomCode, trigger);
            return;
        }

        _logger.LogInfo("Friends cache is empty. Refreshing friends list before inviting...");
        try
        {
            var callback = new System.Action(() =>
            {
                SendInvitesFromCachedFriends(friendsListManager, friendsListManager.Friends, roomCode, trigger);
            });

            friendsListManager.StartCoroutine(friendsListManager.RefreshFriendsList(callback, false));
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to refresh friends list. Trying current cache. Error: {ex.Message}");
            SendInvitesFromCachedFriends(friendsListManager, friendsListManager.Friends, roomCode, trigger);
        }
    }

    private static bool TryGetLobbyContext(
        out AmongUsClient client,
        out FriendsListManager friendsListManager,
        out string roomCode)
    {
        client = AmongUsClient.Instance;
        friendsListManager = FriendsListManager.Instance;
        roomCode = string.Empty;

        if (client == null || !client.AmConnected)
        {
            _logger.LogWarning("Not connected to a lobby.");
            return false;
        }

        if (!client.AmHost)
        {
            _logger.LogWarning("Only the host can send lobby invites.");
            return false;
        }

        if (GameStartManager.Instance == null)
        {
            _logger.LogWarning("Invite can only be sent while in lobby.");
            return false;
        }

        if (client.IsGameStarted || client.IsGameOver)
        {
            _logger.LogWarning("Invite can only be sent before the game starts.");
            return false;
        }

        if (friendsListManager == null)
        {
            _logger.LogWarning("Friends list manager is not ready yet.");
            return false;
        }

        roomCode = GameCode.IntToGameName(client.GameId);
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            _logger.LogWarning("Could not resolve current room code.");
            return false;
        }

        return true;
    }

    private static void SendInvitesFromCachedFriends(
        FriendsListManager friendsListManager,
        Il2CppSystem.Collections.Generic.List<ResponseFriends>? friends,
        string roomCode,
        string trigger)
    {
        if (friends == null || friends.Count == 0)
        {
            _logger.LogWarning("No friends found to invite.");
            return;
        }

        var uniquePuid = new HashSet<string>(StringComparer.Ordinal);
        var invitedCount = 0;

        foreach (var friend in friends)
        {
            if (friend == null || string.IsNullOrWhiteSpace(friend.FriendPuid))
            {
                continue;
            }

            if (!uniquePuid.Add(friend.FriendPuid))
            {
                continue;
            }

            try
            {
                friendsListManager.SendGameInvite(friend.FriendPuid, roomCode, null);
                invitedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Invite failed for {friend.FriendPuid}: {ex.Message}");
            }
        }

        if (invitedCount == 0)
        {
            _logger.LogWarning("Invite was not sent to anyone.");
            return;
        }

        _logger.LogInfo($"[{trigger}] Invite queued for {invitedCount} friends. Room={roomCode}");
    }

    private static void TryCreateInviteButton(LobbyInfoPane pane)
    {
        if (pane == null || pane.CopyCodeButton == null || pane.CopyCodeButton.gameObject == null)
        {
            return;
        }

        var copyButtonObject = pane.CopyCodeButton.gameObject;
        var parent = copyButtonObject.transform.parent;
        if (parent == null || parent.Find(InviteButtonObjectName) != null)
        {
            return;
        }

        var inviteButtonObject = UnityEngine.Object.Instantiate(copyButtonObject, parent);
        inviteButtonObject.name = InviteButtonObjectName;
        inviteButtonObject.transform.localPosition = copyButtonObject.transform.localPosition + InviteButtonOffset;
        inviteButtonObject.transform.localRotation = copyButtonObject.transform.localRotation;
        inviteButtonObject.transform.localScale = copyButtonObject.transform.localScale;

        var passiveButton = inviteButtonObject.GetComponent<PassiveButton>() ??
                            inviteButtonObject.GetComponentInChildren<PassiveButton>(true);

        if (passiveButton == null)
        {
            _logger.LogWarning("Invite button was created but PassiveButton component was not found.");
            return;
        }

        TintInviteButton(inviteButtonObject);
        _logger.LogInfo("Invite button created next to the room code copy button.");
    }

    private static void TintInviteButton(GameObject buttonObject)
    {
        foreach (var renderer in buttonObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            var color = renderer.color;
            renderer.color = new Color(
                Mathf.Clamp01(color.r * 1.25f),
                Mathf.Clamp01(color.g * 0.95f),
                Mathf.Clamp01(color.b * 0.45f),
                color.a);
        }
    }
}