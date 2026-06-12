using Content.Server.Atmos.Rotting;
using Content.Server.Cuffs;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;

namespace Content.Server._Forge.Trade;

public sealed partial class NcContractSystem : EntitySystem
{
    [Dependency] private readonly CuffableSystem _contractGhostRoleCuffs = default!;
    [Dependency] private readonly DamageableSystem _contractGhostRoleDamage = default!;
    [Dependency] private readonly HumanoidProfileSystem _contractGhostRoleHumanoid = default!;
    [Dependency] private readonly RottingSystem _contractGhostRoleRotting = default!;
}
