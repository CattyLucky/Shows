using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[Serializable, NetSerializable]
public sealed class SetPlayerBankBalanceEvent(NetUserId userId, int balance) : EntityEventArgs
{
    public readonly NetUserId UserId = userId;
    public readonly int Balance = balance;
}
