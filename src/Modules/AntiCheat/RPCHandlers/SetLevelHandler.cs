using BetterAmongUs.Attributes;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using Hazel;

namespace BetterAmongUs.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetLevelHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetLevel;

    internal override void Handle(PlayerControl? sender, MessageReader reader)
    {
        uint level = reader.ReadPackedUInt32() + 1;

        if (level < BetterGameSettings.KickLevelBelow.GetInt())
        {
            sender.Kick(setReasonInfo: $" is level {level}, level must be equal or above {BetterGameSettings.KickLevelBelow.GetInt()} to join");
        }
    }

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (sender.DataIsCollected() == true && sender.BetterData().AntiCheatInfo.HasSetLevel && !GameState.IsLocalGame && GameState.IsVanillaServer)
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatSetText()))
            {
                LogRpcInfo($"Player attempted to set level multiple times");
            }

            return false;
        }

        sender.BetterData().AntiCheatInfo.HasSetLevel = true;

        return true;
    }

    internal override void HandleAntiCheat(PlayerControl? sender, MessageReader reader)
    {
        uint level = reader.ReadPackedUInt32() + 1;

        if (level > BetterGameSettings.DetectedLevelAbove.GetInt())
        {
            if (BetterNotificationManager.NotifyCheat(sender, string.Format(Translator.GetString("AntiCheat.InvalidLevelRPC"), level)))
            {
                LogRpcInfo($"Suspicious level set: {level} (max allowed: {BetterGameSettings.DetectedLevelAbove.GetInt()})");
            }
        }
    }
}
