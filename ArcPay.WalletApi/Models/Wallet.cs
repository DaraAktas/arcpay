// Models/Wallet.cs
namespace ArcPay.WalletApi.Models;

public class Wallet : BaseEntity
{
    public int CustomerId { get; set; } 
    public decimal Balance { get; set; } = 0.0m;
    public string Currency { get; set; } = "TRY";
    
    public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
}