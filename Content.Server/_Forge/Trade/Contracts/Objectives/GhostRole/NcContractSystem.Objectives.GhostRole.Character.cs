using Content.Shared._Forge.Trade;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Server._Forge.Trade;

public sealed partial class NcContractSystem : EntitySystem
{
    private void ApplyContractGhostRoleCharacter(EntityUid mob, ContractObjectiveConfigData config)
    {
        if (!string.IsNullOrWhiteSpace(config.GhostRoleCharacterName))
            _contractMeta.SetEntityName(mob, config.GhostRoleCharacterName);

        if (!TryComp(mob, out HumanoidProfileComponent? humanoid))
            return;

        var sex = config.GhostRoleCharacterSex ?? humanoid.Sex;
        var gender = config.GhostRoleCharacterGender ?? humanoid.Gender;
        var age = config.GhostRoleCharacterAge is { } configuredAge
            ? Math.Max(0, configuredAge)
            : humanoid.Age;

        var profile = HumanoidCharacterProfile.DefaultWithSpecies(humanoid.Species, sex)
            .WithGender(gender)
            .WithAge(age);
        _contractGhostRoleHumanoid.ApplyProfileTo((mob, humanoid), profile);
    }

    private void ApplyContractGhostRolePerks(EntityUid mob, ContractObjectiveConfigData config)
    {
        if (config.GhostRolePerks.Count == 0)
            return;

        var perks = EnsureComp<NcContractGhostRolePerksComponent>(mob);
        perks.PerkIds.Clear();
        perks.WalkSpeedMultiplier = 1f;
        perks.SprintSpeedMultiplier = 1f;
        perks.IncomingDamageMultiplier = 1f;
        perks.MeleeDamageMultiplier = 1f;
        perks.ProjectileDamageMultiplier = 1f;
        perks.WeaponPrototypes.Clear();
        perks.ArmorItemPrototypes.Clear();
        perks.ArmorIncomingDamageMultiplier = 1f;
        perks.IncomingFlatReductions.Clear();

        foreach (var perkId in config.GhostRolePerks)
        {
            if (!_prototypes.TryIndex<NcGhostRolePerkPrototype>(perkId, out var perk))
                continue;

            perks.PerkIds.Add(perk.ID);
            perks.WalkSpeedMultiplier *= perk.WalkSpeedMultiplier;
            perks.SprintSpeedMultiplier *= perk.SprintSpeedMultiplier;
            perks.IncomingDamageMultiplier *= perk.IncomingDamageMultiplier;
            perks.MeleeDamageMultiplier *= perk.MeleeDamageMultiplier;
            perks.ProjectileDamageMultiplier *= perk.ProjectileDamageMultiplier;
            perks.ArmorIncomingDamageMultiplier *= perk.ArmorIncomingDamageMultiplier;
            AddUnique(perks.WeaponPrototypes, perk.WeaponPrototypes);
            AddUnique(perks.ArmorItemPrototypes, perk.ArmorItemPrototypes);
            AddFlatReductions(perks.IncomingFlatReductions, perk.IncomingFlatReductions);
        }

        Dirty(mob, perks);
    }

    private static void AddUnique(List<string> target, IEnumerable<string> source)
    {
        foreach (var value in source)
        {
            if (!string.IsNullOrWhiteSpace(value) && !target.Contains(value))
                target.Add(value);
        }
    }

    private static void AddFlatReductions(
        Dictionary<string, float> target,
        IReadOnlyDictionary<string, float> source
    )
    {
        foreach (var (damageType, reduction) in source)
        {
            if (string.IsNullOrWhiteSpace(damageType) || reduction <= 0f)
                continue;

            target[damageType] = target.TryGetValue(damageType, out var existing)
                ? existing + reduction
                : reduction;
        }
    }

    private void TryAttachGhostRoleCharacterInfo(EntityUid mob)
    {
        if (!_objectiveRuntime.ByTarget.TryGetValue(mob, out var key) ||
            !_objectiveRuntime.ByContract.TryGetValue(key, out var state) ||
            !TryGetObjectiveContract(key, out _, out var contract) ||
            !_contractMind.TryGetMind(mob, out var mindId, out var mind))
            return;

        AddGhostRoleBriefing(mindId, contract);
        TryAddGhostRoleSurvivalObjective(key, state, contract, mindId, mind);
    }
}
