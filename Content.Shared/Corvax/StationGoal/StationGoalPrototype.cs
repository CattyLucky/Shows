using Robust.Shared.Prototypes;

namespace Content.Shared.Corvax.StationGoal;

[Prototype]
public sealed partial class StationGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Text { get; set; } = string.Empty;

    [DataField]
    public int MinPlayers;

    [DataField]
    public int MaxPlayers = int.MaxValue;

    [DataField]
    public List<EntProtoId> Spawns = new();
}
