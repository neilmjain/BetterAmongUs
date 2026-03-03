using AmongUs.GameOptions;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules;

using Il2CppInterop.Runtime.Attributes;
using InnerNet;
using UnityEngine;

namespace BetterAmongUs.Mono;

/// <summary>
/// Extended player information with additional data and anti-cheat features.
/// </summary>
internal sealed class ExtendedPlayerInfo : MonoBehaviour, IMonoExtension<NetworkedPlayerInfo>
{
    internal ExtendedPlayerInfo()
    {
    }

    /// <summary>
    /// Gets or sets the base NetworkedPlayerInfo instance.
    /// </summary>
    public NetworkedPlayerInfo? BaseMono { get; set; }

    /// <summary>
    /// Gets the NetworkedPlayerInfo instance.
    /// </summary>
    internal NetworkedPlayerInfo? _Data => BaseMono;

    private bool hasSet = false;

    /// <summary>
    /// Initializes the extended player info.
    /// </summary>
    /// <param name="data">The NetworkedPlayerInfo to extend.</param>
    [HideFromIl2Cpp]
    internal void SetInfo(NetworkedPlayerInfo data)
    {
        if (hasSet) return;
        _PlayerId = data.PlayerId;
        hasSet = true;
    }

    private float timeAccumulator = 0f;

    private void Awake()
    {
        if (!this.RegisterExtension()) return;
    }

    private void OnDestroy()
    {
        this.UnregisterExtension();
    }




    /// <summary>
    /// Gets the player ID.
    /// </summary>
    [HideFromIl2Cpp]
    internal byte _PlayerId { get; private set; }

    /// <summary>
    /// Gets the player's real name.
    /// </summary>
    internal string RealName => _Data?.PlayerName ?? "???";

    /// <summary>
    /// Gets or sets the last name set for this player.
    /// </summary>
    internal string NameSetAsLast { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this player is a BetterAmongUs user.
    /// </summary>
    internal bool IsBetterUser { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this player is a verified BetterAmongUs user.
    /// </summary>
    internal bool IsVerifiedBetterUser { get; set; } = false;

    /// <summary>
    /// Gets or sets whether disconnect message has been shown.
    /// </summary>
    internal bool HasShowDcMsg { get; set; } = false;

    /// <summary>
    /// Gets or sets the disconnect reason.
    /// </summary>
    internal DisconnectReasons DisconnectReason { get; set; } = DisconnectReasons.Unknown;

    /// <summary>
    /// Gets the extended role information.
    /// </summary>
    [HideFromIl2Cpp]
    internal ExtendedRoleInfo? RoleInfo { get; } = new();

/// <summary>
/// Contains extended role information for a player.
/// </summary>
internal class ExtendedRoleInfo
{
    /// <summary>
    /// Gets or sets the number of kills.
    /// </summary>
    internal int Kills { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether noisemaker notification is enabled.
    /// </summary>
    internal bool HasNoisemakerNotify { get; set; } = false;

    /// <summary>
    /// Gets or sets the role to display when dead.
    /// </summary>
    internal RoleTypes DeadDisplayRole { get; set; }
}
}

/// <summary>
/// Extension methods for accessing extended player data.
/// </summary>
internal static class PlayerControlDataExtension
{
    /// <summary>
    /// Gets extended player data from a PlayerControl.
    /// </summary>
    /// <param name="player">The PlayerControl instance.</param>
    /// <returns>The ExtendedPlayerInfo, or null if not found.</returns>
    internal static ExtendedPlayerInfo? BetterData(this PlayerControl player)
    {
        return MonoExtensionManager.Get<ExtendedPlayerInfo>(player.Data);
    }

    /// <summary>
    /// Waits for extended player data to be available, then calls a callback.
    /// </summary>
    /// <param name="player">The PlayerControl instance.</param>
    /// <param name="callback">The callback to execute with the extended data.</param>
    internal static void BetterDataWait(this PlayerControl player, Action<ExtendedPlayerInfo> callback)
    {
        MonoExtensionManager.RunWhenNotNull<ExtendedPlayerInfo>(player, () => player?.BetterData(), callback);
    }

    /// <summary>
    /// Gets extended player data from a NetworkedPlayerInfo.
    /// </summary>
    /// <param name="data">The NetworkedPlayerInfo instance.</param>
    /// <returns>The ExtendedPlayerInfo, or null if not found.</returns>
    internal static ExtendedPlayerInfo? BetterData(this NetworkedPlayerInfo data)
    {
        return MonoExtensionManager.Get<ExtendedPlayerInfo>(data);
    }

    /// <summary>
    /// Gets extended player data from a ClientData.
    /// </summary>
    /// <param name="data">The ClientData instance.</param>
    /// <returns>The ExtendedPlayerInfo, or null if not found.</returns>
    internal static ExtendedPlayerInfo? BetterData(this ClientData data)
    {
        var player = Utils.PlayerFromClientId(data.Id);
        return MonoExtensionManager.Get<ExtendedPlayerInfo>(player.Data);
    }
}