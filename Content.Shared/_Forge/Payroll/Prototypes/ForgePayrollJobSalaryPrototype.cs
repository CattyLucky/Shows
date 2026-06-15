using Robust.Shared.Prototypes;

namespace Content.Shared._Forge.Payroll.Prototypes;

[Prototype]
public sealed partial class ForgePayrollJobSalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("salary")]
    public int Salary { get; private set; } = 1000;
}

[Prototype]
public sealed partial class ForgePayrollConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("defaultSalary")]
    public int DefaultSalary { get; private set; } = 1000;

    [DataField("payInterval")]
    public TimeSpan PayInterval { get; private set; } = TimeSpan.FromMinutes(20);
}
