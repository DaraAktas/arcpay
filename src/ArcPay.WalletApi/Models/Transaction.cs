using ArcPay.Shared;

namespace ArcPay.WalletApi.Models;

public class Transaction : BaseEntity
{
    public int? SenderWalletId { get; set; }
    public int ReceiverWalletId { get; set; }
    public decimal Amount { get; set; }
    public Wallet? SenderWallet { get; set; }
    public Wallet ReceiverWallet { get; set; } = null!;
}
