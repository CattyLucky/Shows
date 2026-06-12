using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Server._Forge.Trade;

public sealed class ForgeArtifactStudyPresetSystem : EntitySystem
{
    public bool ApplyPreset(EntityUid uid, ForgeArtifactStudyPresetComponent component)
    {
        if (component.Applied)
            return true;

        if (!HasComp<XenoArtifactComponent>(uid))
            return false;

        component.Applied = true;
        return true;
    }
}
