using Robust.Shared.Prototypes;

namespace Content.Shared.Corvax.StationGoal;

[RegisterComponent]
public sealed partial class StationGoalComponent : Component
{
    [DataField]
    public List<ProtoId<StationGoalPrototype>> Goals = new();
}
