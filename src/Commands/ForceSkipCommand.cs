using BetterAmongUs.Attributes;
using BetterAmongUs.Modules;

namespace BetterAmongUs.Commands;

[RegisterCommand]
internal sealed class ForceSkipCommand : BaseCommand
{
    internal override string Name => "forceskip";
    internal override string Description => "Force skips a meeting in progress";
    internal override bool ShowCommand() => GameState.IsHost && GameState.IsMeeting;
    internal override void Run()
    {
        if (GameState.IsHost)
        {
            foreach (var client in AmongUsClient.Instance.allClients)
            {
                MeetingHud.Instance.RpcClearVote(client.Id);
            }
            MeetingHud.Instance.Close();
        }
    }
}
