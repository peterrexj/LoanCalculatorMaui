# Prototype ↔ implementation reconciliation

Written for: whoever picks this branch up next (owner or another engineer).

A surface-by-surface check of the ported app against `design/prototype/`. **Structural coverage is
complete** — every prototype surface exists in the app, the What If sub-tab set matches exactly
(Answer · Scenarios · Projection · Insights), and Insights carries all nine panels on both sides.

Four disagreements were found and all four are now resolved. One item remains open, and it is a
pre-existing release blocker rather than a redesign gap.

---

## Resolved

### Budget sub-tab order — the prototype was wrong, not the app

Decision: **keep Income · Expenses · Summary · Asset costs**, and the five Budget prototype
surfaces (four phone + `tablet-budget.html`) were reordered to match.

The prototype led with Summary, which is defensible on "lead with the answer" grounds, but Summary
is premium-gated through `TabView_OnSelectionChanging` (`case 2`) and **that does not fire for the
tab a page lands on** — `BudgetView.OnAppearing` does the landing tab's work itself rather than
relying on `case 0`. Summary at index 0 would therefore have handed trial users a paid panel, and
`e.Cancel = true` is meaningless on a landing tab anyway: there is nothing to fall back to.

Worth knowing if this is ever revisited: **no test could have caught that regression.** See the
open item at the bottom.

### Scenario index deltas — all nine present, coloured by meaning, signed

Three separate problems, now all fixed:

1. **One row's delta was broken.** It bound a path spelled the same as its own `AutomationId`, and
   no such property exists, so it rendered empty — which iOS reports as a *missing AutomationId*,
   pointing at the one attribute that was correct. Recorded in `CLAUDE.md`.
2. **Three rows had no delta at all** — the three-column comparisons. Each now has a computed
   property on `WhatIfViewModel`, with the baseline chosen to match the row's own subtitle rather
   than picked arbitrarily:
   - `TermComparisonMonthlyDiff` — what the column one step *shorter* than the user's term costs
     per month ("Shorter term, higher payment"). Placeholder when they are already on the shortest
     column, since there is then nothing to cost.
   - `DepositScenariosMonthlyDiff` — the monthly saving from the next deposit bracket up ("What a
     bigger deposit changes"). Gated on `DepositColCAvailable`, so it is a placeholder at the
     ceiling.
   - Repayment frequency reuses the existing `FreqWeeklyIntSaved`; no new computation was needed.

   Four unit tests pin both the value's direction and the placeholder cases.
3. **Colour carried identity, not meaning.** Now semantic, matching the prototype *and* the app's
   own scenario pages, which already did this — the index was the odd one out. `LoanAppChartColor2`
   (amber) for a cost increase, `LoanAppChartColor1` (teal) for a saving, with a `DataTrigger` on
   the two rows whose direction can flip: `RateChangeDiffIsPositive`, and a new
   `StressTestHasBuffer`. Accent **stripes** still carry scenario identity, deliberately, so a
   scenario keeps the same colour in the list as on its own page.

   `StressTestHasBuffer` is just the sign of the rate buffer. That matters: it avoids inventing a
   "how much buffer counts as safe" threshold, which would have been a product decision smuggled
   into a port.

Time deltas are now signed (`-8yr 5mo`) via `StringFormat` in the index only, so the scenario
pages keep reading `TIME SAVED: 8yr 5mo` without a doubled-up minus.

### Loan's third hero box is now a link

Per the prototype's own note on `loan.html`, box 3 carries the verdict and links to What If. The
verdict was already displayed; only the tap was missing.

`ThreeColumnBorderedContentView` gained two bindable properties — `Box3Command` and
`Box3AutomationId`. Both default to null, and a `TapGestureRecognizer` with a null Command does
nothing, so **every other use of this shared control is unaffected** and boxes 1 and 2 stay inert.
The gesture is scoped to box 3's `Border`, not the control, or a tap anywhere on the row would
navigate.

Two details that are easy to get wrong:

- **The caption is underlined** (`TextDecorations="Underline"`), matching the prototype's
  `.boxes a .lbl` rule. Without an affordance the tap target is invisible and nobody finds it.
- **The link sets `WhatIfView.PendingTabIndex = 0` before navigating.** Shell keeps the What If
  page alive, so arriving lands on whichever sub-tab was last selected — a link promising "the
  answer" could legitimately drop you on Projection. That is not hypothetical: the screenshot
  sweep captured exactly that. The index is consumed once and cleared so it cannot stick.

The command is assigned in `LoanView.xaml.cs`, not bound in XAML, because switching Shell tabs is a
MAUI concern and the page's `BindingContext` is `LoanViewModel`, which lives in Core and must stay
ignorant of Shell.

### FABs

All four now match the prototype's `.fab` rule — `LoanAppInputTextFgColor` background,
`LoanAppAppShellBgColor` glyph, opacity **0.85**. Every one was at 0.5, which read as a *disabled*
control against the Dark background, and Loan's was additionally the only one using
`LoanAppNavigationButtonBackgroundColor` (`#526D82`). Budget's three were already on the
prototype's light background, so Loan was the outlier rather than the rule.

### Supporting fixes

- **`NAVIGATION-PROPOSAL.md` carried two false claims** used to justify cutting four Insights
  panels (PDF coupling, and panels 6/7 being duplicates). Both corrected in place rather than
  deleted, since the reasoning is what misled the decision.
- **The sweep now captures the scenario index and a scenario page** (`09b`, `09c`) — the two
  surfaces the redesign actually introduced, both with prototypes, neither previously captured.
  And `09-whatif` was a silent duplicate of `08-whatif-projection`, because a bare
  `GoTo(AppTab.WhatIf)` keeps the last-selected sub-tab; it now opens Answer explicitly.

---

## Still open: the premium override makes every paywall untestable

`SharedServiceCore.TESTING_PREMIUM_OVERRIDE` is `true`, so `IsPremiumUser()` always returns true
and `IsTrialUser` is always false. **Every premium gate in the app is dead code at runtime**, which
means a fully green UI suite says nothing about gating, and the port plan's intended check ("visit
Asset costs and Insights as a trial user") cannot be performed at all.

This is already a release blocker in `TECH-DEBT.md`; it is repeated here only because it is what
made the Budget tab-order question unanswerable by testing. If that ordering is ever revisited,
flip the override first.

---

## Checked and found NOT to be defects

Recorded so they are not "fixed" later:

- **The light hero chips on the dark page are deliberate.** The three Loan chips and the two asset
  boxes render `#9DB2BF`→`#DDE6ED` with `#27374D` text, which looks exactly like the
  light-card-on-dark-ground bug that *was* genuine for charts in `Theme.Dark.xaml`. It is not:
  `tokens.css` carries the identical values, so the prototype shows the same thing.
- **`BudgetSummaryCaption` is meant to be at opacity 0.5.** It is a muted caption, not a FAB, and a
  find-and-replace of `Opacity="0.5"` across `BudgetView.xaml` hits it along with the three FABs
  (4 matches, not 3) — the same shape of trap `CLAUDE.md` records for the separator sweep.
- **Figures differing from the prototype are expected.** The prototype used illustrative inputs
  ($200/mo extra, a $20,000 lump sum, a $30,000 offset); the app computes from the real model's
  defaults. Where the inputs coincide the figures agree exactly — Rate change is `+$161/mo` in
  both.
- **The stress-test row is a near-match by design.** The prototype shows a verdict ("still
  affordable"); the app shows `StressTestRateBuffer`, the figure that verdict derives from. Adding
  a verdict string means choosing the threshold at which "affordable" flips, which is a product
  decision rather than a port.

---

## Not carried over from the proposal

**Budget has no "See the projection ›" link.** `NAVIGATION-PROPOSAL.md` promises contextual links
at the figure each explains: *"See the full schedule ›"* under the repayment on Loan (present, as
`LoanScheduleCard`) and *"See the projection ›"* under the surplus on Budget (absent).

It matters more than Loan's did, because Projection **left** Budget for What If — so without it
nothing on Budget points at where it went. Not raised as a question because it was never one of
the four items under discussion; it is simply still outstanding.
