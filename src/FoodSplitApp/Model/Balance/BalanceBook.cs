using System;
using System.Collections.Generic;
using System.Linq;
using FoodSplitApp.Errors;

namespace FoodSplitApp.Model.Balance
{
    public class BalanceBook
    {
        public DateTimeOffset LastChange { get; set; }

        public Dictionary<string, PairBalance> Balances { get; set; }

        public BalanceBook()
        {
            Balances = new Dictionary<string, PairBalance>();
        }

        /// <summary>
        /// Increases debtor's debt to creditor by specific amount.
        /// </summary>
        /// <param name="debtor">User losing credit.</param>
        /// <param name="creditor">User gaining credit.</param>
        /// <param name="amtLost">Positive number, amount of money lost.</param>
        /// <returns>New balance state.</returns>
        public PairBalance AddDebt(FoodUser debtor, FoodUser creditor, decimal amtLost)
        {
            if (debtor.UniqueId == creditor.UniqueId)
            {
                throw new BadRequestException("You can't add debt to yourself.");
            }
            var pair = GetBalance(debtor, creditor);
            pair.AddDebt(debtor, amtLost);
            LastChange = DateTimeOffset.UtcNow;

            return pair;
        }

        /// <summary>
        /// Returns balance between the given user pair.
        /// </summary>
        public PairBalance GetBalance(FoodUser a, FoodUser b)
        {
            var pairKey = PairBalance.CreateKey(a, b);
            if (!Balances.ContainsKey(pairKey))
            {
                Balances.Add(pairKey, new PairBalance(a, b));
            }

            return Balances[pairKey];
        }

        /// <summary>
        /// Returns person that owes the most (in total to all others) or null if balance book is empty or everyone is even.
        /// </summary>
        public (FoodUser debtor, decimal totalDebt) FindBiggestDebtor()
        {
            var totalDebt = new Dictionary<string, decimal>();
            var debtorLookup = new Dictionary<string, FoodUser>();

            if (Balances.Count == 0)
            {
                return (null, 0);
            }

            foreach (var pair in Balances.Values)
            {
                var (debtor, debtValue) = pair.GetDebt();

                if (!totalDebt.ContainsKey(debtor.UniqueId))
                {
                    totalDebt.Add(debtor.UniqueId, 0);
                }

                totalDebt[debtor.UniqueId] += debtValue;

                // Store for future reference
                debtorLookup[debtor.UniqueId] = debtor;
            }

            var biggestDebt = totalDebt.OrderByDescending(pair => pair.Value).First();

            return biggestDebt.Value == 0 ? (null, 0) : (debtorLookup[biggestDebt.Key], biggestDebt.Value);
        }
    }
}