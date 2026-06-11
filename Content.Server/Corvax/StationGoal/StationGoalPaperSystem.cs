using Content.Server.Fax;
using Content.Server.GameTicking;
using Content.Server.MassMedia.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Corvax.StationGoal;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Corvax.StationGoal;

/// <summary>
/// Sends a station goal to authorized fax machines when a round starts.
/// </summary>
public sealed partial class StationGoalPaperSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FaxSystem _fax = default!;
    [Dependency] private readonly NewsSystem _news = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New != GameRunLevel.InRound)
            return;

        if (!_cfg.GetCVar(CCVars.StationGoal))
            return;

        var playerCount = _player.PlayerCount;
        var query = EntityQueryEnumerator<StationGoalComponent>();

        while (query.MoveNext(out var uid, out var stationGoal))
        {
            if (!TryPickGoal(stationGoal, playerCount, out var goal))
                continue;

            if (SendStationGoal(uid, goal))
                Log.Info($"Goal {goal.ID} has been sent to station {MetaData(uid).EntityName}");
        }
    }

    private bool TryPickGoal(StationGoalComponent stationGoal, int playerCount, out StationGoalPrototype goal)
    {
        var goals = new List<ProtoId<StationGoalPrototype>>(stationGoal.Goals);

        while (goals.Count > 0)
        {
            var goalId = _random.Pick(goals);
            var goalPrototype = _prototype.Index(goalId);

            if (playerCount < goalPrototype.MinPlayers || playerCount > goalPrototype.MaxPlayers)
            {
                goals.Remove(goalId);
                continue;
            }

            goal = goalPrototype;
            return true;
        }

        goal = default!;
        return false;
    }

    public bool SendStationGoal(EntityUid station, ProtoId<StationGoalPrototype> goal)
    {
        return SendStationGoal(station, _prototype.Index(goal));
    }

    /// <summary>
    /// Sends a station goal to all fax machines authorized to receive it.
    /// </summary>
    /// <returns>True if at least one station fax received the goal.</returns>
    public bool SendStationGoal(EntityUid station, StationGoalPrototype goal)
    {
        var stationName = MetaData(station).EntityName;
        var printout = new FaxPrintout(
            Loc.GetString(goal.Text, ("station", stationName)),
            Loc.GetString("station-goal-fax-paper-name"),
            null,
            null,
            "paper_stamp-centcom",
            new List<StampDisplayInfo>
            {
                new()
                {
                    StampedName = Loc.GetString("stamp-component-stamped-name-centcom"),
                    StampedColor = Color.FromHex("#006600"),
                },
            });

        var wasSentToStation = false;
        var query = EntityQueryEnumerator<FaxMachineComponent>();

        while (query.MoveNext(out var faxUid, out var fax))
        {
            var isStationFax = fax.ReceiveStationGoal && _station.GetOwningStation(faxUid) == station;

            if (!fax.ReceiveAllStationGoals && !isStationFax)
                continue;

            _fax.Receive(faxUid, printout, null, fax);

            foreach (var spawnEnt in goal.Spawns)
                SpawnAtPosition(spawnEnt, Transform(faxUid).Coordinates);

            wasSentToStation |= isStationFax;
        }

        if (wasSentToStation)
            PublishStationGoalNews(station, goal);

        return wasSentToStation;
    }

    private void PublishStationGoalNews(EntityUid station, StationGoalPrototype goal)
    {
        var stationName = MetaData(station).EntityName;
        var title = Loc.GetString("station-goal-news-title", ("station", stationName));
        var content = Loc.GetString(goal.Text, ("station", stationName));
        var endPattern = Loc.GetString("station-goal-end");

        if (content.EndsWith(endPattern))
        {
            content = content[..^endPattern.Length];
            content = content.TrimEnd();
        }

        _news.TryAddNews(station, title, content, out _, Loc.GetString("station-goal-news-author"));
    }
}
