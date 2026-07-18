namespace Content.Server._Forge.Payroll.Components;

[RegisterComponent]
public sealed partial class ForgePayrollConsoleComponent : Component
{
    [DataField]
    public EntityUid? SelectedEmployee;
}
