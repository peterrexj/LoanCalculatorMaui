# UI test impact of the redesign

Written so the port updates the suite deliberately, rather than discovering it red halfway through.
Derived from the AutomationIds the suite actually depends on (extracted from `Pages/*.cs` and
`Tests/*.cs`), cross-referenced against what the approved structure moves.

The unit tests are not affected — all 977 target `LoanCalculator.Core` logic, and this is a UI
change. The Appium suite is the only safety net here, which is exactly why it should be updated in
step with each move rather than at the end.

---

## Unaffected — no change needed

**`ShellTabs.cs` survives intact**, which is the single biggest saving from keeping the existing tab
names. It hardcodes `"Loan"`, `"Budget"`, `"What If"`, `"Settings"` with a comment that they must
stay in sync with `AppShell.xaml`; none of them change.

Its arrival anchors also still resolve, and one gets *better*:

| anchor | today | after |
|---|---|---|
| `FabWizard` (Loan) | only exists on the Asset sub-tab | exists on the whole Loan page |
| `BudgetTabIncome` (Budget) | unchanged | unchanged |
| `WhatIfPageTitle` | unchanged | unchanged |
| `SettingsPageTitle` | unchanged | unchanged |

That `FabWizard` change fixes a real latent trap: because the FAB only lives inside the Asset
sub-tab today, walking away from Loan with Insights selected makes every later `GoTo(AppTab.Loan)`
unable to confirm arrival. It stranded two captures during the screenshot sweep. Losing the Loan
sub-tabs removes the trap.

Also unaffected: everything in `AppLaunch.cs`, the Budget income/expense entry ids
(`BudgetIncome*`/`BudgetExpense*` — the add/edit flow does not move), and all of `SettingsPage.cs`
except the delete section, below.

---

## Revised after owner review — this got cheaper

The structure changed after review: **Schedule stays under Loan** instead of moving to What If, and
What If gains a sub-tab strip (Answer · Scenarios · Projection · Breakdown) so nothing sits at a
third level. Two consequences for this document:

- **`LoanAmortisationGrid` no longer moves at all.** It stays on the Loan page, just behind a
  `LoanTabSchedule` sub-tab instead of `LoanTabAmortisation`. That is a rename, not a re-anchor,
  and it is one of only two raw ids referenced directly from test files.
- **Loan keeps a sub-tab strip** (Asset · Schedule) rather than collapsing to a single page. So
  `LoanTabAsset` survives in spirit — `OpenAssetTab()` keeps working against the renamed id rather
  than being deleted.

Net effect: the Loan section below overstates the work. Treat `LoanTabAmortisation` →
`LoanTabSchedule` as a rename, and `LoanTabRunningCost` / `LoanTabInsights` as the only two ids
that genuinely disappear.

## Breaks: Loan loses its sub-tab strip

Asset / Amortisation / Running Cost / Insights collapse — Asset becomes the whole page, the other
three move elsewhere. So these four AutomationIds **cease to exist**:

`LoanTabAsset` · `LoanTabAmortisation` · `LoanTabRunningCost` · `LoanTabInsights`

| `LoanPage.cs` member | action |
|---|---|
| `OpenAssetTab()` | delete — the Loan page *is* this now; callers just `GoTo(AppTab.Loan)` |
| `OpenAmortisationTab()` | replace with navigation to What If ▸ Schedule |
| `OpenRunningCostTab()` | replace with navigation to Budget ▸ Asset costs |
| `OpenInsightsTab()` | replace with navigation to What If (answer) / What If ▸ Breakdown |

Two ids move to a different destination and need re-anchoring, not renaming:
- `LoanAmortisationGrid` → What If ▸ Schedule. Referenced by `LoanPage.cs` **and** directly by
  `Tests/*.cs`.
- `LoanInsightsExportButton` → What If ▸ Export. Same: referenced in both places.
- `LoanRunningCostFilter` → Budget ▸ Asset costs.

Unchanged because they stay on the Loan page: `AssetEntry`, `DepositEntry`, `FabWizard`,
`LoanSummaryBox`, `LoanInterestRate*`, `LoanFrequencyControl`, `LoanUpfrontCostsCard`,
`QuickInputApplyButton`, `WizardCancelButton`.

---

## Breaks: Projection moves out of Budget

`BudgetTabProjection` ceases to exist; `BudgetProjectionYearsSlider` moves to What If ▸ Projection.

| `BudgetPage.cs` member | action |
|---|---|
| `OpenProjectionTab()` | move to a What If page object, re-anchored |

Budget *gains* an Asset costs sub-tab, so a new `BudgetTabAssetCosts` id and an
`OpenAssetCostsTab()` helper are needed — plus a case in the sub-tab strip.

Unchanged: `BudgetTabIncome`, `BudgetTabExpenses`, `BudgetTabSummary` and every total/verdict id.

---

## Breaks worst: What If becomes an index, not one scroll

This is the structural change with the largest test cost, and it is worth being clear-eyed about
it. Today every What If control sits on one long scrolling page, so `WhatIfPage.cs` can reach them
with a `ScrollTo`. After the change each scenario is **behind a tap** on the scenario index.

Affected: `WhatIfRateDeltaMinus` · `WhatIfRateDeltaValue` · `WhatIfRateDeltaPlus` ·
`WhatIfRateNewRate` · `WhatIfRateNewMonthly` · `WhatIfStressResultsPanel` ·
`WhatIfStressUnavailablePanel`

The ids themselves can survive unchanged — what changes is **reachability**. Every `WhatIfPage`
method that touches them needs an "open this scenario first" step, and the scenario rows need their
own ids (e.g. `WhatIfScenarioRate`, `WhatIfScenarioStress`) so the index is addressable.

`WhatIfNoDataPanel` keeps its meaning but gains a working route to Loan, which is new behaviour
worth a test: the empty state is currently a dead end.

Note this state cannot be reached in-session at all — deleting data clears the loan JSON but
deliberately leaves the live form populated, so the panel only appears on a fresh install
(`UITEST_FRESH_INSTALL=true`). Any test for it needs its own run.

---

## Watch: Settings loses its double labelling

`SettingsPage.OpenDeleteDataSection()` scrolls to `SettingsDeleteDataHeader` and taps it to expand a
collapsed `SfExpander`. If removing the duplicated section-header/row naming changes that group from
an expander to a plain row list, the tap becomes wrong — it would either do nothing or navigate.

`SettingsDeleteDataHeader` is the id to watch. The four delete buttons, the three combos and
`SettingsPageTitle` are unaffected.

---

## Suggested order

Each step leaves the suite green before the next starts, so a regression is attributable:

1. **Pure styling, no IA change** — chart colours (one file, `Theme.Dark.xaml`) and the two
   separator styles. No AutomationIds move, so the suite should stay green untouched. Good first
   commit: it proves the visual layer independently of the restructure.
2. **Budget gains Asset costs** — additive. New id and helper; nothing moves away yet.
3. **Loan collapses to one page** — the four `LoanTab*` ids go; re-anchor the three that moved.
4. **Projection and Schedule land under What If** — re-anchor `BudgetProjectionYearsSlider` and
   `LoanAmortisationGrid`.
5. **What If becomes an index** — the largest test change; do it last and on its own.

Run the Budget and Loan smoke tests after each step:
`./run-uitests.sh --ios --no-build --filter "TestCategory=Smoke"`

And heed the stale-build trap in `CLAUDE.md`: `--no-build` tests whatever was built last, so a fix
that is not in that build fails *as the very bug it fixes*. Compare mtimes before re-diagnosing.
