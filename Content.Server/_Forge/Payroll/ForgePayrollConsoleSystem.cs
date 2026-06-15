using System.Linq;
using Content.Server._Forge.Payroll.Components;
using Content.Server.Popups;
using Content.Shared._Forge.Payroll;
using Content.Shared.Access.Systems;
using Content.Shared._NF.Bank.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Forge.Payroll;

public sealed partial class ForgePayrollConsoleSystem : EntitySystem
{
    private static readonly TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(1);

    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private ForgePayrollSystem _payroll = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private TimeSpan _nextUiUpdate;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ForgePayrollConsoleComponent>(ForgePayrollConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<ForgePayrollSelectRecordMessage>(OnSelect);
            subs.Event<ForgePayrollUpdateRecordMessage>(OnUpdate);
            subs.Event<ForgePayrollPayNowMessage>(OnPayNow);
            subs.Event<ForgePayrollFineMessage>(OnFine);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUiUpdate)
            return;

        _nextUiUpdate = now + UiUpdateInterval;

        var query = EntityQueryEnumerator<ForgePayrollConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var console, out _))
        {
            if (!_ui.IsUiOpen(uid, ForgePayrollConsoleUiKey.Key))
                continue;

            foreach (var actor in _ui.GetActors(uid, ForgePayrollConsoleUiKey.Key))
            {
                UpdateUi((uid, console), actor);
                break;
            }
        }
    }

    private void OnOpened(Entity<ForgePayrollConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent, args.Actor);
    }

    private void OnSelect(Entity<ForgePayrollConsoleComponent> ent, ref ForgePayrollSelectRecordMessage args)
    {
        var employee = GetEntity(args.Employee);

        if (!IsVisiblePayrollEmployee(employee))
            return;

        ent.Comp.SelectedEmployee = employee;
        UpdateUi(ent, args.Actor);
    }

    private void OnUpdate(Entity<ForgePayrollConsoleComponent> ent, ref ForgePayrollUpdateRecordMessage args)
    {
        if (!CanEdit(args.Actor, ent.Owner))
            return;

        var employee = GetEntity(args.Employee);

        if (!IsVisiblePayrollEmployee(employee) ||
            !TryComp<ForgePayrollRecordComponent>(employee, out var payroll))
            return;

        if (!_prototypes.TryIndex<JobPrototype>(Sanitize(args.JobPrototype, 64), out var job) ||
            !job.OverrideConsoleVisibility.GetValueOrDefault(job.SetPreference))
            return;

        payroll.JobPrototype = job.ID;
        payroll.JobTitle = Sanitize(job.LocalizedName, 64);
        payroll.Department = Sanitize(_payroll.GetDepartmentName(job.ID), 48);
        payroll.BaseSalary = Math.Clamp(args.BaseSalary, 0, 250000);
        payroll.Adjustment = Math.Clamp(args.Adjustment, -250000, 250000);
        var status = NormalizeStatus(args.Status);
        if (payroll.Status != status)
            payroll.NextPayAt = _timing.CurTime + _payroll.GetPayInterval();

        payroll.Status = status;

        ent.Comp.SelectedEmployee = employee;
        UpdateUi(ent, args.Actor);
    }

    private void OnPayNow(Entity<ForgePayrollConsoleComponent> ent, ref ForgePayrollPayNowMessage args)
    {
        if (!CanEdit(args.Actor, ent.Owner))
            return;

        var employee = GetEntity(args.Employee);

        if (!IsVisiblePayrollEmployee(employee) ||
            !TryComp<ForgePayrollRecordComponent>(employee, out var payroll))
            return;

        _payroll.TryPayNow((employee, payroll));
        ent.Comp.SelectedEmployee = employee;
        UpdateUi(ent, args.Actor);
    }

    private void OnFine(Entity<ForgePayrollConsoleComponent> ent, ref ForgePayrollFineMessage args)
    {
        if (!CanEdit(args.Actor, ent.Owner))
            return;

        var employee = GetEntity(args.Employee);

        if (!IsVisiblePayrollEmployee(employee) ||
            !TryComp<ForgePayrollRecordComponent>(employee, out var payroll))
            return;

        if (args.Amount <= 0)
            return;

        var amount = Math.Clamp(args.Amount, 1, 250000);
        var reason = Sanitize(args.Reason, 96);

        if (_payroll.TryFine((employee, payroll), amount, reason))
        {
            var popupReason = string.IsNullOrWhiteSpace(reason)
                ? Loc.GetString("forge-payroll-console-fine-no-reason")
                : reason;

            _popup.PopupEntity(
                Loc.GetString("forge-payroll-console-fine-success", ("amount", amount), ("employee", payroll.EmployeeName)),
                ent.Owner,
                args.Actor,
                PopupType.Medium);
            _popup.PopupEntity(
                Loc.GetString("forge-payroll-console-fine-target", ("amount", amount), ("reason", popupReason)),
                employee,
                employee,
                PopupType.MediumCaution);
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("forge-payroll-console-fine-failed", ("employee", payroll.EmployeeName)),
                ent.Owner,
                args.Actor,
                PopupType.MediumCaution);
        }

        ent.Comp.SelectedEmployee = employee;
        UpdateUi(ent, args.Actor);
    }

    private void UpdateUi(Entity<ForgePayrollConsoleComponent> ent, EntityUid actor)
    {
        var canEdit = CanEdit(actor, ent.Owner);
        var records = BuildRecords();
        var jobOptions = BuildJobOptions();
        var selectedNet = ent.Comp.SelectedEmployee is { } selectedEmployee
            ? GetNetEntity(selectedEmployee)
            : (NetEntity?) null;

        if (selectedNet is not { } selected ||
            !ContainsRecord(records, selected))
        {
            ent.Comp.SelectedEmployee = records.Count > 0
                ? GetEntity(records[0].Entity)
                : null;
            selectedNet = records.Count > 0
                ? records[0].Entity
                : null;
        }

        _ui.SetUiState(ent.Owner,
            ForgePayrollConsoleUiKey.Key,
            new ForgePayrollConsoleState(records, jobOptions, selectedNet, canEdit, (int) _payroll.GetPayInterval().TotalSeconds));
    }

    private List<ForgePayrollRecordState> BuildRecords()
    {
        var now = _timing.CurTime;
        var records = new List<ForgePayrollRecordState>();
        var query = EntityQueryEnumerator<ForgePayrollRecordComponent, BankAccountComponent>();

        while (query.MoveNext(out var uid, out var payroll, out _))
        {
            if (!_players.TryGetSessionByEntity(uid, out _))
                continue;

            var secondsToNextPay = Math.Max(0, (int) Math.Ceiling((payroll.NextPayAt - now).TotalSeconds));

            records.Add(new ForgePayrollRecordState(
                GetNetEntity(uid),
                payroll.EmployeeName,
                payroll.JobTitle,
                payroll.JobPrototype,
                payroll.Department,
                payroll.BaseSalary,
                payroll.Adjustment,
                payroll.TotalSalary,
                payroll.Status,
                secondsToNextPay,
                payroll.LastPaidAmount,
                payroll.LastFineAmount,
                payroll.LastFineReason,
                payroll.OutstandingFineAmount));
        }

        records.Sort((a, b) => string.Compare(a.EmployeeName, b.EmployeeName, StringComparison.CurrentCulture));
        return records;
    }

    private List<ForgePayrollJobOptionState> BuildJobOptions()
    {
        var jobs = _prototypes.EnumeratePrototypes<JobPrototype>()
            .Where(job => job.OverrideConsoleVisibility.GetValueOrDefault(job.SetPreference))
            .ToList();

        jobs.Sort((a, b) => string.Compare(a.LocalizedName, b.LocalizedName, StringComparison.CurrentCulture));

        var options = new List<ForgePayrollJobOptionState>(jobs.Count);
        foreach (var job in jobs)
        {
            options.Add(new ForgePayrollJobOptionState(
                job.ID,
                job.LocalizedName,
                _payroll.GetDepartmentName(job.ID),
                _payroll.GetDefaultSalary(job.ID)));
        }

        return options;
    }

    private bool IsVisiblePayrollEmployee(EntityUid employee)
    {
        return HasComp<ForgePayrollRecordComponent>(employee) &&
               HasComp<BankAccountComponent>(employee) &&
               _players.TryGetSessionByEntity(employee, out _);
    }

    private static bool ContainsRecord(List<ForgePayrollRecordState> records, NetEntity selected)
    {
        foreach (var record in records)
        {
            if (record.Entity == selected)
                return true;
        }

        return false;
    }

    private bool CanEdit(EntityUid actor, EntityUid console)
    {
        return _access.IsAllowed(actor, console);
    }

    private static string Sanitize(string? value, int maxLength)
    {
        value = value?.Trim() ?? string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static ForgePayrollEmploymentStatus NormalizeStatus(ForgePayrollEmploymentStatus status)
    {
        return Enum.IsDefined(status)
            ? status
            : ForgePayrollEmploymentStatus.Working;
    }
}
