using System;
using System.Text.Json.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace FoodSplitApp.Model.Balance
{
    public class PairBalance
    {
        public DateTimeOffset LastChange { get; set; }

        /// <summary>
        /// First (alphabetically, by unique Id) side of the balance.
        /// </summary>
        public FoodUser PartyA { get; set; }

        /// <summary>
        /// Second (alphabetically, by unique Id) side of the balance.
        /// </summary>
        public FoodUser PartyB { get; set; }

        /// <summary>
        /// Negative means that A owes B. Positive means that B owes A.
        /// </summary>
        public decimal Balance { get; set; }

        public PairBalance(FoodUser a, FoodUser b)
        {
            var sortedPair = CreateSortedPair(a, b);
            PartyA = sortedPair.A;
            PartyB = sortedPair.B;
            Balance = 0;
            LastChange = DateTimeOffset.UtcNow;
        }

        public PairBalance()
        {
        }

        [IgnoreProperty]
        [JsonIgnore]
        public string Key => CreateKey(PartyA, PartyB);

        /// <summary>
        /// Sorts any two users alphabetically.
        /// </summary>
        public static (FoodUser A, FoodUser B) CreateSortedPair(FoodUser a, FoodUser b)
        {
            if (string.CompareOrdinal(a.UniqueId, b.UniqueId) < 0)
            {
                return (a, b);
            }

            if (string.CompareOrdinal(a.UniqueId, b.UniqueId) > 0)
            {
                return (b, a);
            }

            throw new ArgumentException("You can't create a pair from the same user.");
        }

        /// <summary>
        /// Creates a string key that uniquely identifies a pair (in any argument order).
        /// </summary>

        public static string CreateKey(FoodUser a, FoodUser b)
        {
            var sortedPair = CreateSortedPair(a, b);
            return sortedPair.A.UniqueId + "@@@" + sortedPair.B.UniqueId;
        }

        /// <summary>
        /// Increases user's debt by given amount.
        /// </summary>
        /// <param name="userLosingMoney">User losing money.</param>
        /// <param name="amtLost">Positive number, amount of money lost.</param>
        public void AddDebt(FoodUser userLosingMoney, decimal amtLost)
        {
            if (amtLost <= 0)
            {
                throw new ArgumentException("Amount needs to be a positive number.");
            }

            if (userLosingMoney.UniqueId == PartyA.UniqueId)
            {
                Balance -= amtLost;
            }
            else if (userLosingMoney.UniqueId == PartyB.UniqueId)
            {
                Balance += amtLost;
            }
            else
            {
                throw new ArgumentException("User doesn't belong in this pair.");
            }
        }

        /// <summary>
        /// Returns true if given <see cref="user"/> is owed money by the other one.
        /// </summary>
        public bool HasPositiveBalance(FoodUser user)
        {
            if (user.UniqueId == PartyA.UniqueId)
            {
                return Balance > 0;
            }

            if (user.UniqueId == PartyB.UniqueId)
            {
                return Balance < 0;
            }

            throw new InvalidOperationException("Invalid pair.");
        }

        /// <summary>
        /// Gets the person with negative balance and the debt owed (as positive number).
        /// </summary>
        public (FoodUser debtor, decimal debtValue) GetDebt()
        {
            return Balance > 0 ? (PartyB, Balance) : (PartyA, -Balance);
        }
    }
}