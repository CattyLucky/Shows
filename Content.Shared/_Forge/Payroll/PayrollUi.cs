using Robust.Shared.Serialization;

namespace Content.Shared._Forge.Payroll;

[Serializable, NetSerializable]
public enum ForgePayrollConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum ForgePayrollEmploymentStatus : byte
{
    Working,
    Suspended,
    Fired
}

[Serializable, NetSerializable]
public sealed class ForgePayrollConsoleState : BoundUserInterfaceState
{
    public readonly List<ForgePayrollRecordState> Records;
    public readonly NetEntity? Selected;
    public readonly bool CanEdit;
    public readonly int PayIntervalSeconds;

    public ForgePayrollConsoleState(
        List<ForgePayrollRecordState> records,
        NetEntity? selected,
        bool canEdit,
        int payIntervalSeconds)
    {
        Records = records;
        Selected = selected;
        CanEdit = canEdit;
        PayIntervalSeconds = payIntervalSeconds;
    }
}

[Serializable, NetSerializable]
public sealed class ForgePayrollRecordState
{
    public readonly NetEntity Entity;
    public readonly string EmployeeName;
    public readonly string JobTitle;
    public readonly string JobPrototype;
    public readonly string Department;
    public readonly int BaseSalary;
    public readonly int Adjustment;
    public readonly int TotalSalary;
    public readonly ForgePayrollEmploymentStatus Status;
    public readonly int SecondsToNextPay;
    public readonly int LastPaidAmount;
    public readonly int LastFineAmount;
    public readonly string LastFineReason;

    public ForgePayrollRecordState(
        NetEntity entity,
        string employeeName,
        string jobTitle,
        string jobPrototype,
        string department,
        int baseSalary,
        int adjustment,
        int totalSalary,
        ForgePayrollEmploymentStatus status,
        int secondsToNextPay,
        int lastPaidAmount,
        int lastFineAmount,
        string lastFineReason)
    {
        Entity = entity;
        EmployeeName = employeeName;
        JobTitle = jobTitle;
        JobPrototype = jobPrototype;
        Department = department;
        BaseSalary = baseSalary;
        Adjustment = adjustment;
        TotalSalary = totalSalary;
        Status = status;
        SecondsToNextPay = secondsToNextPay;
        LastPaidAmount = lastPaidAmount;
        LastFineAmount = lastFineAmount;
        LastFineReason = lastFineReason;
    }
}

[Serializable, NetSerializable]
public sealed class ForgePayrollSelectRecordMessage(NetEntity employee) : BoundUserInterfaceMessage
{
    public readonly NetEntity Employee = employee;
}

[Serializable, NetSerializable]
public sealed class ForgePayrollUpdateRecordMessage(
    NetEntity employee,
    string jobTitle,
    string department,
    int baseSalary,
    int adjustment,
    ForgePayrollEmploymentStatus status) : BoundUserInterfaceMessage
{
    public readonly NetEntity Employee = employee;
    public readonly string JobTitle = jobTitle;
    public readonly string Department = department;
    public readonly int BaseSalary = baseSalary;
    public readonly int Adjustment = adjustment;
    public readonly ForgePayrollEmploymentStatus Status = status;
}

[Serializable, NetSerializable]
public sealed class ForgePayrollPayNowMessage(NetEntity employee) : BoundUserInterfaceMessage
{
    public readonly NetEntity Employee = employee;
}

[Serializable, NetSerializable]
public sealed class ForgePayrollFineMessage(NetEntity employee, int amount, string reason) : BoundUserInterfaceMessage
{
    public readonly NetEntity Employee = employee;
    public readonly int Amount = amount;
    public readonly string Reason = reason;
}
