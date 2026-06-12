# 18 — User Retention: Smart Empty States with CTAs

**Priority:** High  
**Status:** ❌ Not started  
**Goal:** Replace dead-end empty states (plain label, no action) with actionable cards that guide users to the next step — highest-leverage retention change for the least code.

---

## Problem

Three places currently show an empty/placeholder state with no guidance:

1. **Insights tab — no income/expense data:** Shows `AffordabilityTextDescription` = " record your income & expenses". This is a string label with no button. The user has no obvious path forward.
2. **Expense tab — no entries:** Shows an empty list with no explanation of what to add or why.
3. **Income tab — no entries:** Same problem.

Users who hit these dead ends have a high probability of closing the app.

---

## Fix A — Insights tab empty state CTA

### Current state
```
[ your affordability status ]
  ↳ "record your income & expenses"   (just a label)
```

### Proposed state
```
┌──────────────────────────────────────────────────┐
│  📊  See your affordability                      │
│  Record your income and expenses to find out     │
│  if this loan fits your budget.                  │
│                                                  │
│  [ Add Income & Expenses →  ]   (button)         │
└──────────────────────────────────────────────────┘
```

The button navigates to the Income tab: `Shell.Current.GoToAsync("///income")`.

Replace the existing `Label` for `AffordabilityTextDescription` (when `IsAffordabilityAvailable == false && !IsTrialUser`) with an `EmptyStateCard` content view.

### Implementation

New reusable control `src/LoanCalculator/Controls/EmptyStateCard.xaml`:

```xml
<ContentView>
    <Frame CornerRadius="12" Padding="16"
           BackgroundColor="{DynamicResource LoanAppCardBackgroundColor}">
        <StackLayout Spacing="8">
            <Label Text="{Binding Headline}" Style="{DynamicResource LabelSubtitleStyle}" />
            <Label Text="{Binding Body}" Style="{DynamicResource LabelBodyStyle}" />
            <SfButton Text="{Binding ActionLabel}"
                      Command="{Binding ActionCommand}"
                      IsVisible="{Binding HasAction}" />
        </StackLayout>
    </Frame>
</ContentView>
```

Properties: `Headline`, `Body`, `ActionLabel`, `ActionCommand` (all bindable).

---

## Fix B — Expense tab empty state

When `TransactionRecords.IncomeExpenseEntries.Count == 0`:

```
┌──────────────────────────────────────────────────┐
│  💡  Track your monthly expenses                 │
│  Add your regular outgoings to see how they      │
│  affect your loan affordability.                 │
│                                                  │
│  [ Add your first expense →  ]   (button)        │
└──────────────────────────────────────────────────┘
```

The button opens the add-expense form (same as the FAB).

### ViewModel

Add to `ExpenseViewModel`:

```csharp
[JsonIgnore] public bool HasNoExpenses =>
    TransactionRecords?.IncomeExpenseEntries?.Count == 0;
```

In `ExpenseView.xaml`, bind `EmptyStateCard` visibility to `HasNoExpenses`.

---

## Fix C — Income tab empty state

Same pattern as Expense. `HasNoIncomeEntries` flag, card with "Add your income sources" headline and CTA to open the add-income form.

---

## Files to Touch

| File | Change |
|------|--------|
| `src/LoanCalculator/Controls/EmptyStateCard.xaml` | New reusable control |
| `src/LoanCalculator/Controls/EmptyStateCard.xaml.cs` | Code-behind |
| `src/LoanCalculator/View/LoanView.xaml` | Replace Insights empty label with EmptyStateCard |
| `src/LoanCalculator/View/ExpenseView.xaml` | Add EmptyStateCard when list is empty |
| `src/LoanCalculator/View/IncomeView.xaml` | Add EmptyStateCard when list is empty |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/ExpenseViewModel.cs` | Add `HasNoExpenses` |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/IncomeViewModel.cs` | Add `HasNoIncomeEntries` |

---

## Verification

1. Fresh install → Expense tab shows empty state card; tap CTA → add form opens.
2. Add one expense → empty state card disappears.
3. Insights tab with no income/expense → shows the affordability CTA card; tap → navigates to Income tab.
4. After adding income and expenses → affordability card shows real data; CTA card gone.
