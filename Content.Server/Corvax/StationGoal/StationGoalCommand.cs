using System.Linq;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Corvax.StationGoal;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Corvax.StationGoal;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class StationGoalCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public string Command => "sendstationgoal";
    public string Description => Loc.GetString("send-station-goal-command-description");
    public string Help => Loc.GetString("send-station-goal-command-help-text", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var stationNet) ||
            !_entity.TryGetEntity(stationNet, out var station))
        {
            shell.WriteError($"Failed to parse euid '{args[0]}'.");
            return;
        }

        var goalId = args[1];
        if (!_prototype.HasIndex<StationGoalPrototype>(goalId))
        {
            shell.WriteError($"No station goal found with ID {goalId}!");
            return;
        }

        var stationGoals = _entity.System<StationGoalPaperSystem>();
        if (!stationGoals.SendStationGoal(station.Value, goalId))
            shell.WriteError("Station goal was not sent");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                var stations = ContentCompletionHelper.StationIds(_entity);
                return CompletionResult.FromHintOptions(stations, Loc.GetString("send-station-goal-command-arg-station"));
            case 2:
                var options = _prototype
                    .EnumeratePrototypes<StationGoalPrototype>()
                    .Select(p => new CompletionOption(p.ID));

                return CompletionResult.FromHintOptions(options, Loc.GetString("send-station-goal-command-arg-id"));
            default:
                return CompletionResult.Empty;
        }
    }
}
