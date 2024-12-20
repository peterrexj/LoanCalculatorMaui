using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income.Summary;
using System.Collections.ObjectModel;

namespace LoanCalculator.Models.Income
{
    public class IncomeExpenseBase
    {
        public IncomeExpenseBase()
        {
            IncomeExpenseSummary = new IncomeExpenseSummary();
        }

        public IncomeExpenseSummary IncomeExpenseSummary { get; set; }

        private ObservableCollection<IncomeExpense> _incomeExpenseEntries;
        public ObservableCollection<IncomeExpense> IncomeExpenseEntries
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

        public void SumUpData(double monthlyValue = 0, double yearlyValue = 0)
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
            IncomeExpenseSummary.TotalMonthly = IncomeExpenseSummary.TotalMonthly - monthlyValue;
            IncomeExpenseSummary.TotalYearly = IncomeExpenseSummary.TotalYearly - yearlyValue;
        }
    }
}
