using Content.Client._Forge.Payroll.UI;
using Content.Shared._Forge.Payroll;
using Robust.Client.UserInterface;

namespace Content.Client._Forge.Payroll;

public sealed class ForgePayrollConsoleBoundUserInterface : BoundUserInterface
{
    private ForgePayrollConsoleWindow? _window;

    public ForgePayrollConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ForgePayrollConsoleWindow>();
        _window.OnRecordSelected += employee => SendMessage(new ForgePayrollSelectRecordMessage(employee));
        _window.OnSavePressed += (employee, jobPrototype, baseSalary, adjustment, bankBalance, status) =>
            SendMessage(new ForgePayrollUpdateRecordMessage(employee, jobPrototype, baseSalary, adjustment, bankBalance, status));
        _window.OnPayNowPressed += employee => SendMessage(new ForgePayrollPayNowMessage(employee));
        _window.OnFinePressed += (employee, amount, reason) => SendMessage(new ForgePayrollFineMessage(employee, amount, reason));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ForgePayrollConsoleState payrollState)
            _window?.UpdateState(payrollState);
    }
}
