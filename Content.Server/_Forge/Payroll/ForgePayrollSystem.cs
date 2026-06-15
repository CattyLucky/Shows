using Content.Server._Forge.Payroll.Components;
using Content.Server._NF.Bank;
using Content.Shared._Forge.Payroll;
using Content.Shared._Forge.Payroll.Prototypes;
using Content.Shared._NF.Bank.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Forge.Payroll;

public sealed partial class ForgePayrollSystem : EntitySystem
{
    private const string DefaultConfigId = "Default";
    private const int FallbackSalary = 1000;
    private static readonly TimeSpan FallbackPayInterval = TimeSpan.FromMinutes(20);

    [Dependency] private BankSystem _bank = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ISharedPlayerManager _players = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<ForgePayrollRecordComponent, EntityRenamedEvent>(OnEmployeeRenamed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var interval = GetPayInterval();
        var query = EntityQueryEnumerator<ForgePayrollRecordComponent, BankAccountComponent>();

        while (query.MoveNext(out var uid, out var payroll, out _))
        {
            if (payroll.Status != ForgePayrollEmploymentStatus.Working ||
                payroll.TotalSalary <= 0 ||
                payroll.NextPayAt > now)
                continue;

            if (!_players.TryGetSessionByEntity(uid, out _))
            {
                payroll.NextPayAt = now + interval;
                Dirty(uid, payroll);
                continue;
            }

            if (_mobState.IsDead(uid))
            {
                payroll.NextPayAt = now + interval;
                Dirty(uid, payroll);
                continue;
            }

            if (!TryPay((uid, payroll), now, interval))
            {
                payroll.NextPayAt = now + interval;
                Dirty(uid, payroll);
            }
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (string.IsNullOrWhiteSpace(args.JobId) ||
            !_prototypes.TryIndex<JobPrototype>(args.JobId, out var job))
            return;

        var payroll = EnsureComp<ForgePayrollRecordComponent>(args.Mob);
        payroll.EmployeeName = args.Profile.Name;
        payroll.JobPrototype = job.ID;
        payroll.JobTitle = job.LocalizedName;
        payroll.Department = GetDepartmentName(job.ID);
        payroll.BaseSalary = GetDefaultSalary(job.ID);
        payroll.Adjustment = 0;
        payroll.Status = ForgePayrollEmploymentStatus.Working;
        payroll.LastPaidAt = TimeSpan.Zero;
        payroll.LastPaidAmount = 0;
        payroll.LastFineAmount = 0;
        payroll.LastFineReason = string.Empty;
        payroll.NextPayAt = _timing.CurTime + GetPayInterval();

        Dirty(args.Mob, payroll);
    }

    private void OnEmployeeRenamed(Entity<ForgePayrollRecordComponent> ent, ref EntityRenamedEvent args)
    {
        ent.Comp.EmployeeName = args.NewName;
        Dirty(ent);
    }

    public bool TryPayNow(Entity<ForgePayrollRecordComponent> employee)
    {
        if (employee.Comp.Status != ForgePayrollEmploymentStatus.Working ||
            employee.Comp.TotalSalary <= 0 ||
            _mobState.IsDead(employee.Owner))
            return false;

        return TryPay(employee, _timing.CurTime, GetPayInterval());
    }

    public bool TryFine(Entity<ForgePayrollRecordComponent> employee, int amount, string reason)
    {
        if (amount <= 0)
            return false;

        if (!_bank.TryBankWithdraw(employee.Owner, amount))
            return false;

        employee.Comp.LastFineAmount = amount;
        employee.Comp.LastFineReason = reason;
        Dirty(employee);
        return true;
    }

    private bool TryPay(Entity<ForgePayrollRecordComponent> employee, TimeSpan now, TimeSpan interval)
    {
        var (uid, payroll) = employee;
        var amount = payroll.TotalSalary;

        if (!_bank.TryBankDeposit(uid, amount, false))
            return false;

        payroll.LastPaidAmount = amount;
        payroll.LastPaidAt = now;
        payroll.NextPayAt = now + interval;
        Dirty(uid, payroll);
        return true;
    }

    public int GetDefaultSalary(string jobId)
    {
        if (_prototypes.TryIndex<ForgePayrollJobSalaryPrototype>(jobId, out var salary))
            return salary.Salary;

        return TryGetConfig(out var config)
            ? config.DefaultSalary
            : FallbackSalary;
    }

    public TimeSpan GetPayInterval()
    {
        return TryGetConfig(out var config)
            ? config.PayInterval
            : FallbackPayInterval;
    }

    private bool TryGetConfig(out ForgePayrollConfigPrototype config)
    {
        if (_prototypes.TryIndex<ForgePayrollConfigPrototype>(DefaultConfigId, out var indexed))
        {
            config = indexed;
            return true;
        }

        config = default!;
        return false;
    }

    private string GetDepartmentName(string jobId)
    {
        if (_jobs.TryGetPrimaryDepartment(jobId, out var primary))
            return Loc.GetString(primary.Name);

        return _jobs.TryGetDepartment(jobId, out var department)
            ? Loc.GetString(department.Name)
            : Loc.GetString("forge-payroll-department-unknown");
    }
}
