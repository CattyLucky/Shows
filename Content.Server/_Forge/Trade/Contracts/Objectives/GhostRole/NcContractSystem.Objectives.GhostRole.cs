using Content.Server.Atmos.Rotting;
using Content.Server.Cuffs;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;

namespace Content.Server._Forge.Trade;

public sealed partial class NcContractSystem : EntitySystem
{
    [Dependency] private CuffableSystem _contractGhostRoleCuffs = default!;
    [Dependency] private DamageableSystem _contractGhostRoleDamage = default!;
    [Dependency] private HumanoidProfileSystem _contractGhostRoleHumanoid = default!;
    [Dependency] private RottingSystem _contractGhostRoleRotting = default!;
}
