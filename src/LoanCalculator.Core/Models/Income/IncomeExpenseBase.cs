using System.Collections.ObjectModel;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income.Summary;

namespace LoanCalculator.Core.Models.Income
{
    public class IncomeExpenseBase
    {
        public IncomeExpenseBase()
        {
            IncomeExpenseSummary = new IncomeExpenseSummary();
        }

        public IncomeExpenseSummary IncomeExpenseSummary { get; set; }

        private ObservableCollection<IncomeExpense>? _incomeExpenseEntries;
        public ObservableCollection<IncomeExpense>? IncomeExpenseEntries
        {
            get
            {
                if (_incomeExpenseEntries == null)
                {
                    _incomeExpenseEntries = new ObservableCollection<IncomeExpense>();
                }
                return _incomeExpenseEntries;
            }
            set => _incomeExpenseEntries = value;
        }
        
        public IncomeExpense GetEntry(string name)
        {
            if (IncomeExpenseEntries == null)
            {
                IncomeExpenseEntries = new ObservableCollection<IncomeExpense>();
            }

            if (Exists(name) == false)
            {
                Add(name, 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            }

            return Get(name);
        }

        public void AddPropertySetter(IncomeExpense incomeExpense)
        {
            Add(incomeExpense.Name, incomeExpense.Amount, incomeExpense.Frequency, isCheckForExistingRequired: false);
        }
        public void Add(string name, double income, TimeFrequencyEnum frequency, bool isCheckForExistingRequired)
        {
            if (isCheckForExistingRequired)
            {
                if (!Exists(name))
                {
                    IncomeExpenseEntries.Add(new IncomeExpense { Id = Guid.NewGuid(), Name = name, Amount = income, Frequency = frequency });
                }
                else
                {
                    Update(Get(name).Id, name, income, frequency);
                }
            }
            else
            {
                IncomeExpenseEntries.Add(new IncomeExpense { Id = Guid.NewGuid(), Name = name, Amount = income, Frequency = frequency });
            }
        }
        public void Update(Guid id, string name, double amount, TimeFrequencyEnum frequency)
        {
            if (Exists(id))
            {
                IncomeExpenseEntries[GetIndex(id)].Name = name;
                IncomeExpenseEntries[GetIndex(id)].Amount = amount;
                IncomeExpenseEntries[GetIndex(id)].Frequency = frequency;
            }
            else
            {
                Add(name, amount, frequency, isCheckForExistingRequired: false);
            }
        }
        public bool Exists(Guid id)
        {
            return IncomeExpenseEntries.Any(f => f.Id == id);
        }
        public bool Exists(string name)
        {
            return IncomeExpenseEntries.Any(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
        public void Delete(Guid id)
        {
            if (Exists(id))
            {
                IncomeExpenseEntries.Remove(Get(id));
            }
        }
        public void DeleteAll()
        {
            IncomeExpenseEntries.Select(f => f.Id).ToList().ForEach(Delete);
        }
        public IncomeExpense Get(Guid id)
        {
            return IncomeExpenseEntries.FirstOrDefault(f => f.Id == id);
        }
        public int GetIndex(Guid id)
        {
            return IncomeExpenseEntries.ToList().FindIndex(f => f.Id == id);
        }
        public IncomeExpense Get(string name)
        {
            return IncomeExpenseEntries.FirstOrDefault(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Recomputes the summary totals from the entries. Idempotent, and the only writer of
        /// <c>TotalMonthly</c>/<c>TotalYearly</c>.
        /// </summary>
        /// <remarks>
        /// This used to take a deduction and subtract it from the totals it had just computed
        /// (TECH-DEBT D1). Because one <c>IncomeExpenseSummary</c> is shared by the Income tab,
        /// Expense tab, Budget page, Loan/affordability box, Wizard and the PDF, those fields then
        /// meant net-or-gross depending on which screen refreshed last — one screen's display
        /// preference silently changed another screen's arithmetic. An "after expenses" figure is
        /// now computed where it is displayed, by subtracting from these gross totals.
        /// <para>Do not reintroduce a deduction parameter here.</para>
        /// </remarks>
        public void SumUpData()
        {
            IncomeExpenseSummary.TotalMonthly = 0;
            IncomeExpenseSummary.TotalYearly = 0;
            if (IncomeExpenseEntries != null)
            {
                foreach (var item in IncomeExpenseEntries)
                {
                    IncomeExpenseSummary.TotalMonthly += item.AmountMonthly;
                    IncomeExpenseSummary.TotalYearly += item.AmountYearly;
                }
            }
        }

        public void CalculatePercentages()
        {
            if (IncomeExpenseEntries == null || !IncomeExpenseEntries.Any())
                return;

            double totalAmount = IncomeExpenseEntries.Sum(entry => entry.Amount);

            foreach (var entry in IncomeExpenseEntries)
            {
                //entry.Percentage = totalAmount > 0 ? (entry.Amount / totalAmount) * 100 : 0;
                entry.Percentage = totalAmount != 0 ? (entry.Amount / totalAmount) * 100 : 0;
            }
        }
    }
}
