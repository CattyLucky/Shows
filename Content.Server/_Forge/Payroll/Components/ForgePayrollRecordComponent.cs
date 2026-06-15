using Content.Shared._Forge.Payroll;

namespace Content.Server._Forge.Payroll.Components;

[RegisterComponent]
public sealed partial class ForgePayrollRecordComponent : Component
{
    [DataField]
    public string EmployeeName = string.Empty;

    [DataField]
    public string JobTitle = string.Empty;

    [DataField]
    public string JobPrototype = string.Empty;

    [DataField]
    public string Department = string.Empty;

    [DataField]
    public int BaseSalary = 1000;

    [DataField]
    public int Adjustment;

    [DataField]
    public ForgePayrollEmploymentStatus Status = ForgePayrollEmploymentStatus.Working;

    [DataField]
    public TimeSpan NextPayAt;

    [DataField]
    public TimeSpan LastPaidAt;

    [DataField]
    public int LastPaidAmount;

    [DataField]
    public int LastFineAmount;

    [DataField]
    public string LastFineReason = string.Empty;

    public int TotalSalary => Math.Max(0, BaseSalary + Adjustment);
}
