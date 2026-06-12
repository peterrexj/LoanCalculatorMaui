# 21 — UX Usability: Input Friction, Accessibility, Gestures

**Priority:** Medium  
**Status:** ❌ Not started  
**Goal:** Reduce friction at every input point, make the app usable for people with accessibility needs, and add gesture shortcuts that feel natural on mobile.

---

## Issue A — Keyboard type not optimal for numeric fields

Several numeric inputs in `ExpenseView.xaml` and `IncomeView.xaml` use the default keyboard instead of `Keyboard.Numeric`. This means users see a full QWERTY keyboard when entering a dollar amount — an extra tap to switch.

**Fix:** Audit all `Entry` elements where the value is numeric and set:

```xml
<Entry Keyboard="Numeric" ... />
```

For currency fields that allow decimals, use `Keyboard.Decimal`.

---

## Issue B — No "Done" / "Next" keyboard navigation

When a user fills in multiple fields (income amount, then frequency, etc.), tapping "Done" on the keyboard dismisses it entirely instead of moving to the next field. This is a common iOS friction point.

**Fix:** Use `ReturnType` to chain fields:

```xml
<Entry x:Name="entryAmount" ReturnType="Next" ReturnCommand="{Binding FocusNextCommand}" />
<Entry x:Name="entryDescription" ReturnType="Done" />
```

Or in code-behind, hook `Completed` to focus the next `Entry`:

```csharp
private void entryAmount_Completed(object sender, EventArgs e)
    => entryDescription.Focus();
```

---

## Issue C — Small tap targets on income/expense list rows

The edit and delete buttons on list rows (`btnEditEntry`, `btnDeleteEntry`) are icon-only buttons. On small phones their effective tap area is ~30x30px — below Apple HIG and Material Design minimums of 44x44pt / 48x48dp.

**Fix:** Add `MinimumHeightRequest="44"` and `MinimumWidthRequest="44"` to these buttons. Or wrap in a `Grid` with explicit `HeightRequest="48"`.

---

## Issue D — No `SemanticProperties` for screen readers

None of the custom controls (charts, segment controls, icon buttons) have `SemanticProperties.Description` or `AutomationProperties.Name`. VoiceOver / TalkBack users cannot use the app.

**Fix (high-impact subset):**

```xml
<!-- Delete button -->
<SfButton AutomationId="{Binding Id}"
          SemanticProperties.Description="Delete expense entry"
          ... />

<!-- Loan amount display -->
<Label SemanticProperties.HeadingLevel="Level2"
       SemanticProperties.Description="{Binding LoanAmountStrFormatted, StringFormat='Loan amount: {0}'}"
       ... />
```

Prioritise: action buttons (add, delete, edit), the repayment summary labels, and the tab bar.

---

## Issue E — Swipe-to-delete on expense/income list rows

Currently delete requires tapping the trash icon button. On mobile, swipe-to-delete is the expected gesture.

**Fix:** In `ExpenseView.xaml` and `IncomeView.xaml`, wrap list items in `SwipeView`:

```xml
<SwipeView>
    <SwipeView.RightItems>
        <SwipeItems>
            <SwipeItem Text="Delete"
                       BackgroundColor="{DynamicResource LoanAppErrorColor}"
                       Command="{Binding Source={x:Reference Page}, Path=BindingContext.DeleteEntryCommand}"
                       CommandParameter="{Binding Id}" />
        </SwipeItems>
    </SwipeView.RightItems>
    <!-- existing row content -->
</SwipeView>
```

Keep the explicit delete button as well for discoverability.

---

## Issue F — Interest rate step too fine for slider (0.01 increments)

The interest rate slider currently allows 0.01% increments. In practice, rates move in 0.05% or 0.25% steps. The slider is hard to land on a round number.

**Fix already partially in place** — `LoanView.xaml.cs` rounds to 2dp on `CommitInterestRateEntry`. Apply the same rounding logic to the slider: in the slider's `ValueChanged` handler, snap to 0.05 increments:

```csharp
_viewModel.InterestRate = Math.Round(e.NewValue / 0.05) * 0.05;
```

---

## Issue G — No confirmation before delete

Tapping delete on an income/expense entry is instant and irreversible (no undo). Users occasionally fat-finger the delete button.

**Fix:** Show a simple inline confirmation (`AlertService.ShowConfirmationAsync`) before executing delete. Already used elsewhere in the codebase.

---

## Files to Touch

| File | Change |
|------|--------|
| `src/LoanCalculator/View/ExpenseView.xaml` | Keyboard type, SwipeView, SemanticProperties, tap targets |
| `src/LoanCalculator/View/IncomeView.xaml` | Same |
| `src/LoanCalculator/View/LoanView.xaml` | SemanticProperties on key labels; interest rate slider step |
| `src/LoanCalculator/View/LoanView.xaml.cs` | Interest rate snap, delete confirmation |
| `src/LoanCalculator/View/ExpenseView.xaml.cs` | Delete confirmation, keyboard navigation |
| `src/LoanCalculator/View/IncomeView.xaml.cs` | Same |

---

## Verification

1. Open Income tab → amount field → numeric keyboard appears immediately.
2. Tab through fields with "Next" key — keyboard stays open, focus moves.
3. Swipe left on an expense entry → "Delete" swipe action appears.
4. Delete confirmation dialog appears before entry is removed.
5. VoiceOver reads "Delete expense entry" when focusing the delete button.
6. Interest rate slider snaps to 0.05 increments.
