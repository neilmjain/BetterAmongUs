using BetterAmongUs.Attributes;
using BetterAmongUs.Enums;
using BetterAmongUs.Helpers;
using BetterAmongUs.Mono;
using Hazel;
using InnerNet;
using UnityEngine;

namespace BetterAmongUs.Modules.AntiCheat;

internal abstract class RPCHandler
{
    internal static readonly RPCHandler?[] allHandlers = [.. RegisterRPCHandlerAttribute.Instances];
    internal InnerNetClient innerNetClient => AmongUsClient.Instance;
    internal virtual byte CallId => byte.MaxValue;
    internal virtual byte GameDataTag => byte.MaxValue;
    internal virtual bool LocalHandling { get; set; }
    protected static bool CancelAsHost => !GameState.IsHost;

    internal static bool CheckRange(Vector2 pos1, Vector2 pos2, float range) => Vector2.Distance(pos1, pos2) <= range;
    internal virtual void Handle(PlayerControl? sender, MessageReader reader) { }
    internal virtual void HandleGameData(MessageReader reader) { }
    internal virtual void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader) { }
    internal virtual bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader) => true;
    internal virtual void HandleAntiCheat(PlayerControl? sender, MessageReader reader) { }
    internal virtual bool BetterHandle(PlayerControl? sender, MessageReader reader) => true;

    protected static PlayerControl? catchedSender;
    protected static HandlerFlag catchedHandlerFlag = HandlerFlag.Handle;
    internal static bool HandleRPC(byte calledId, PlayerControl? sender, MessageReader reader, HandlerFlag handlerFlag)
    {
        catchedSender = sender;
        catchedHandlerFlag = handlerFlag;
        bool cancel = false;

        foreach (var handler in allHandlers)
        {
            if (handlerFlag != HandlerFlag.HandleGameDataTag && calledId == handler.CallId)
            {
                try
                {
                    switch (handlerFlag)
                    {
                        case HandlerFlag.Handle:
                            handler.Handle(sender, reader);
                            break;
                        case HandlerFlag.AntiCheatCancel:
                            cancel = !handler.HandleAntiCheatCancel(sender, reader);
                            break;
                        case HandlerFlag.AntiCheat:
                            handler.HandleAntiCheat(sender, reader);
                            break;
                        case HandlerFlag.CheatRpcCheck:
                            handler.HandleCheatRpcCheck(sender, reader);
                            break;
                        case HandlerFlag.BetterHost:
                            cancel = !handler.BetterHandle(sender, reader);
                            break;
                    }

                    if (!cancel) break;
                }
                catch
                {
                }
            }
            else if (handlerFlag == HandlerFlag.HandleGameDataTag && calledId == handler.GameDataTag)
            {
                try
                {
                    handler.HandleGameData(reader);
                }
                catch (Exception ex)
                {
                    Logger_.Error(ex);
                }
            }
        }

        return !cancel;
    }

    internal void LogRpcInfo(string info, PlayerControl? player = null)
    {
        string Name = Enum.GetName((RpcCalls)CallId) ?? Enum.GetName((CustomRPC)CallId) ?? $"Unregistered({CallId})";
        Name = $"[{Enum.GetName(catchedHandlerFlag)}] > " + Name;
        Logger_.LogCheat($"{catchedSender?.BetterData()?.RealName ?? player.BetterData()?.RealName ?? string.Empty} {Name}: {info}");
    }

    internal string GetFormatActionText()
    {
        string Name = Enum.GetName((RpcCalls)CallId) ?? Enum.GetName((CustomRPC)CallId) ?? $"Unregistered({CallId})";
        return string.Format(Translator.GetString("AntiCheat.InvalidActionRPC"), Name);
    }

    internal string GetFormatSetText()
    {
        string Name = Enum.GetName((RpcCalls)CallId) ?? Enum.GetName((CustomRPC)CallId) ?? $"Unregistered({CallId})";
        return string.Format(Translator.GetString("AntiCheat.InvalidSetRPC"), Name);
    }
}