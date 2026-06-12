using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared._NF.Bank;
using Content.Shared._NF.Bank.Components;
using Content.Shared._NF.Bank.Events;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._NF.Bank;

public sealed partial class BankSystem : SharedBankSystem
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private IServerPreferencesManager _prefsManager = default!;

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("bank");

        SubscribeLocalEvent<BankAccountComponent, ComponentInit>(OnBankAccountInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    public bool TryBankWithdraw(EntityUid mobUid, int amount)
    {
        if (amount <= 0)
            return false;

        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
            return false;

        if (!_playerManager.TryGetSessionByEntity(mobUid, out var session))
            return false;

        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
            return false;

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
            return false;

        if (!TryBankWithdraw(session, prefs, profile, amount, out var newBalance))
            return false;

        bank.Balance = newBalance.Value;
        Dirty(mobUid, bank);
        return true;
    }

    public bool TryBankDeposit(EntityUid mobUid, int amount, bool tax = true)
    {
        if (amount <= 0)
            return false;

        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
            return false;

        if (!_playerManager.TryGetSessionByEntity(mobUid, out var session))
            return false;

        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
            return false;

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
            return false;

        if (!TryBankDeposit(session, prefs, profile, amount, out var newBalance))
            return false;

        bank.Balance = newBalance.Value;
        Dirty(mobUid, bank);
        return true;
    }

    public bool TryBankWithdraw(
        ICommonSession session,
        PlayerPreferences prefs,
        HumanoidCharacterProfile profile,
        int amount,
        [NotNullWhen(true)] out int? newBalance,
        bool spendLongTerm = false)
    {
        newBalance = null;

        if (amount <= 0)
            return false;

        var balance = profile.BankBalance;
        if (balance < amount)
            return false;

        balance -= amount;

        if (!TrySetProfileBalance(session, prefs, profile, balance))
            return false;

        newBalance = balance;
        RaiseLocalEvent(new BalanceChangedEvent(session, balance));
        return true;
    }

    public bool TryBankDeposit(
        ICommonSession session,
        PlayerPreferences prefs,
        HumanoidCharacterProfile profile,
        int amount,
        [NotNullWhen(true)] out int? newBalance)
    {
        newBalance = null;

        if (amount <= 0)
            return false;

        var total = (long) profile.BankBalance + amount;
        if (total > int.MaxValue)
            return false;

        var balance = (int) total;
        if (!TrySetProfileBalance(session, prefs, profile, balance))
            return false;

        newBalance = balance;
        RaiseLocalEvent(new BalanceChangedEvent(session, balance));
        return true;
    }

    public async Task<bool> TryBankWithdrawOffline(
        NetUserId userId,
        PlayerPreferences prefs,
        HumanoidCharacterProfile profile,
        int amount)
    {
        if (amount <= 0 || profile.BankBalance < amount)
            return false;

        return await TrySetOfflineProfileBalance(userId, prefs, profile, profile.BankBalance - amount);
    }

    public async Task<bool> TryBankDepositOffline(
        NetUserId userId,
        PlayerPreferences prefs,
        HumanoidCharacterProfile profile,
        int amount)
    {
        if (amount <= 0)
            return false;

        var total = (long) profile.BankBalance + amount;
        if (total > int.MaxValue)
            return false;

        return await TrySetOfflineProfileBalance(userId, prefs, profile, (int) total);
    }

    public bool TryGetBalance(EntityUid ent, out int balance)
    {
        balance = 0;

        if (!_playerManager.TryGetSessionByEntity(ent, out var session))
            return false;

        return TryGetBalance(session, out balance);
    }

    public bool TryGetBalance(ICommonSession session, out int balance)
    {
        balance = 0;

        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
            return false;

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
            return false;

        balance = profile.BankBalance;
        return true;
    }

    private bool TrySetProfileBalance(
        ICommonSession session,
        PlayerPreferences prefs,
        HumanoidCharacterProfile profile,
        int balance)
    {
        var index = prefs.IndexOfCharacter(profile);
        if (index == -1)
        {
            _log.Warning($"Tried to adjust the bank balance for {session.UserId}, but the profile was not in their character set.");
            return false;
        }

        _ = _prefsManager.SetProfile(session.UserId, index, profile.WithBankBalance(balance));
        return true;
    }

    private async Task<bool> TrySetOfflineProfileBalance(
        NetUserId userId,
        PlayerPreferences prefs,
        HumanoidCharacterProfile profile,
        int balance)
    {
        var index = prefs.IndexOfCharacter(profile);
        if (index == -1)
            return false;

        var newProfile = profile.WithBankBalance(balance);
        if (_prefsManager.TryGetCachedPreferences(userId, out _))
            _ = _prefsManager.SetProfile(userId, index, newProfile);
        else
            await _db.SaveCharacterSlotAsync(userId, newProfile, index);

        return true;
    }

    private void OnBankAccountInit(EntityUid uid, BankAccountComponent component, ComponentInit args)
    {
        UpdateBankBalance(uid, component);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var bank = EnsureComp<BankAccountComponent>(args.Mob);
        bank.Balance = args.Profile.BankBalance;
        Dirty(args.Mob, bank);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (!_prefsManager.TryGetCachedPreferences(args.Player.UserId, out var prefs) ||
            prefs.SelectedCharacter is not HumanoidCharacterProfile)
            return;

        var bank = EnsureComp<BankAccountComponent>(args.Entity);
        UpdateBankBalance(args.Entity, bank);
    }

    private void UpdateBankBalance(EntityUid mobUid, BankAccountComponent component)
    {
        component.Balance = TryGetBalance(mobUid, out var balance) ? balance : 0;
        Dirty(mobUid, component);
    }
}
