using Content.Shared._NF.Bank;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Bank.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BankAccountComponent : Component
{
    [DataField, Access(typeof(SharedBankSystem))]
    [AutoNetworkedField]
    public int Balance;
}
