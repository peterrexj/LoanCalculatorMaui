# Full prototype ↔ app audit (all 27 surfaces)

> **STATUS — fix pass applied.** Everything below marked ✅ is done and builds clean on iOS and
> Android with 981 unit tests green. Items marked ⏸ are deliberately NOT done, each with its
> reason stated inline. Nothing here has been through UI automation yet — awaiting manual test.
>
> ✅ Empty-state `Go to Loan ›` · destructive colour on "All Data" · Settings version label ·
> Answer tab scenarios + schedule links · Asset-costs rows unified to chevron + delete-in-page ·
> Expenses third box now net · three Budget section headers · two Loan micro-notes ·
> schedule **Repayment** column · Projection intro copy
>
> ⏸ Export section picker · schedule decade-disclosures · Answer summary boxes & chart ·
> Budget three-box row & Insights rows · Settings double-labelling · tablet layouts ·
> Expenses switches card (**must not** be removed — see below)

Written for: the repo owner, deciding what else to close before this branch lands.

A complete read-across of `design/prototype/` against the app. Companion to
[`PROTOTYPE-GAPS.md`](PROTOTYPE-GAPS.md), which logs what has already been fixed; this file is the
exhaustive list of what still differs.

**On trust:** every item marked **[verified]** was confirmed by reading the file and is quoted with
a line reference. Items marked **[reported]** come from the read-across but were not individually
re-checked. Three items initially reported turned out to be false and are listed at the bottom so
nobody chases them.

**Two things the prototype itself gets wrong**, which matter when deciding whether the app or the
prototype is the source of truth:

- `budget-expenses.html:12-15` and `budget-income.html:18` both say the row lists carry
  "edit/delete" actions, while their rendered markup draws a chevron row instead. The owner has
  since decided in favour of the chevron, so the **app is now correct and those notes are stale**.
- `schedule.html:19-21` claims the page uses a `page-head` + `subtabs` strip "instead of a modal
  back-link", but its own markup at line 53 still renders `<div class="modal-head">` with a `‹`
  back arrow. The stated intent and the drawn markup disagree.

---

## Blocking-ish: things that leave the user stuck or misinformed

### 1. The What If empty state has no way out  **[verified]**

`whatif-empty.html:8-17` exists for exactly one reason: to give the "no loan data" message a
working **`Go to Loan ›`** button, explicitly contrasted with the old app's "static text … with no
way to act on it".

`WhatIfView.xaml:148-169` is still static text only — "No loan data yet" plus "Enter your asset
value and loan details on the Loan tab first." No button, no tap target.

**This was previously reported as done and was not.** An earlier note in this work claimed the
empty state "gains the working link to Loan"; that was wrong. The placeholder/index swap happened,
the CTA never did.

### 2. Export has no section picker  **[reported, structurally confirmed]**

`export.html:43-73` is a six-checkbox picker (Affordability verdict, Loan & borrowing, Income &
expenses, Repayment schedule, Projection, Scenarios), a live "N sections selected · about N pages"
count, and an Export button gated on the selection.

The app has no such screen. Both entry points — `WhatIfExportCard` and
`LoanInsightsExportButton` — fire `ExportInsightsReportCommand` directly and produce one fixed PDF
with no choice. `export.html:9-19` describes the picker as the change being *tested*, so this was
always a proposal; worth noting it is **not** listed under index.html's own "Known gaps", so it was
never self-disclosed as unbuilt.

### 3. Destructive delete buttons are not marked destructive  **[verified]**

`settings.html:115` renders the delete row's title in `--input-error-text-fg-color` (`#E57373`,
mapping to `LoanAppInputErrorTextFgColor`, which **exists in all four themes**).

`SettingsView.xaml:1071-1103` renders four equal `MauiButtonStyle` buttons — "All Data" at
line 1098-1103 among them — with no error colour on any. Irreversible data deletion currently
looks identical to every other button on the page.

---

## Substantive content gaps

### 4. What If ▸ Answer is far thinner than designed  **[verified]**

The app's Answer tab (`WhatIfView.xaml:52-132`) contains exactly two things: the affordability card
and an "Export a PDF report" row. `whatif.html` additionally specifies:

| Prototype element | Line |
|---|---|
| Three summary boxes — repayment / net income / % of income | 58-62 |
| "See all 9 scenarios" teaser row into the Scenarios sub-tab | 64-71 |
| "Where the money goes" chart (principal/interest + legend) | 73-94 |
| "See the full schedule" row | 96-101 |
| Closing disclaimer paragraph | bottom |

Since Answer is the landing sub-tab *and* the destination of the new Loan verdict link, this is the
first thing a user sees after asking "can I afford this?".

### 5. Budget ▸ Summary still differs in shape  **[verified]**

Resolved already: the verdict card now appears, the projection link and the "loan repayment not
counted" banner exist. Still outstanding:

- The **three-box row** (`in/mo`, `out/mo`, `spare after loan` → What If) from `budget.html:49-53`.
  The app's `SummaryNetCard` (`BudgetView.xaml:708-801`) is a 2×2 grid instead, with no link.
- The **"Insights" breakdown rows** (`budget.html:69-87`) — Income / Living expenses / Asset
  running costs / Net, each a tappable chevron row. No equivalent exists.
- **Ordering**: the prototype leads with the affordability figure, before the chart. The app's
  `AffordabilityCard` (`BudgetView.xaml:963-995`) is the *last* element on the page.

### 6. Repayment schedule is still the pre-redesign screen  **[verified]**

- **4 columns, not 5.** `AmortisationPage.xaml` maps Period / Principal / Interest / Balance. The
  prototype restores **Repayment** as a fifth column, and `schedule.html:44-46` explicitly names
  its absence as the thing being fixed.
- **Still paginated.** `AmortisationPage.xaml:232` is `PageSize="5"`; the prototype replaces the
  pager with decade-grouped `<details>` disclosures (`schedule.html:68-123`) specifically to avoid
  "six pages of five rows".
- The app also carries a chart the prototype does not have, and the prototype's intro/trailing
  micro-copy has no counterpart.

### 7. Asset costs rows are now inconsistent with the rest of Budget  **[verified]**

Income and Expenses rows were just converted to tappable chevron rows with delete moved onto
`TransactionEntryPage`. **Asset costs was not** — `BudgetView.xaml:1168-1190` still has
`btnEditEntry` / `btnDeleteEntry` icon buttons.

So Budget now has two different row idioms, and the claim that the entry page is "the only delete
path" is true for income/expenses but not for asset costs. This inconsistency was introduced by
that change and should be closed either way the owner prefers.

### 8. Expenses tab carries a switches card the prototype excludes  **[verified]**

`budget-expenses.html:12-15`: *"BudgetTabExpenses carries no switches of its own (those live only
on the Income tab), so this page omits that card rather than duplicating it."*

`BudgetView.xaml:398-519` has a full switches card with "Include asset expenses" and "Show income
after expenses" — the duplication the note says to avoid.

### 9. Expenses tab's third summary box shows the wrong quantity  **[verified]**

Prototype's third box is **net / month** (`budget-expenses.html:37`). The app's third box
(`BudgetView.xaml:387-394`) binds `Expense.TotalIncomeMonthlyWithComma` under the label "Monthly
Income" — a gross income figure, not a net. Worth a look on its own merits: an expenses screen
showing raw income in its summary bar is a slightly odd read.

### 10. Settings "Defaults" groups are still double-labelled  **[verified]**

`settings.html:9-19` states the fix being tested: *"today every group is labelled twice… Here the
section header IS the label"*, with bulkier groups becoming rows that open their own page.

The app applies this to Appearance only. The three Defaults groups still show a section header
*and* an expander title:

- `SettingsView.xaml:390` `Text="STEP SIZES"` + `:403` `Text="Step sizes"`
- same pattern for WHAT-IF DEFAULTS (`:507`) and CALCULATOR DEFAULTS (`:588`)

### 11. No version label in Settings  **[verified]**

`settings.html:120` shows "Version 40" as micro text below the data section. No version string
exists anywhere in `SettingsView.xaml`.

---

## Smaller / cosmetic

- **Loan page** has no page-head subtitle, no section headers ("The purchase" / "The borrowing" /
  "Terms"), no "Total cost" row, no `100%` slider end-label, and is missing two micro notes — the
  one explaining that upfront costs fold into the loan, and the one saying running costs moved to
  Budget. The schedule row also sits near the bottom rather than directly under the hero boxes.
  **[reported]**
- **Section headers missing above all three Budget row lists** — "Sources", "Living expenses",
  "Asset expenses". **[reported]**
- **Insights export button position**: prototype puts it last, after all nine panels; the app puts
  it first. The app also adds a disclaimer banner and a chart inside "Loan Insights" that the
  prototype does not have. **[reported]**
- **Projection** is missing its intro and closing micro-copy, and adds two year-by-year data-grid
  sections the prototype never shows. **[reported]**
- **Scenario detail**: the monthly-change figure is a half-width tile rather than the prototype's
  full-width hero callout, and the "Compare" cross-links to related scenarios are absent (verified
  for Rate change only). **[reported]**
- **Premium** uses image assets where the prototype uses glyphs, and omits the prototype's
  inter-row dividers. **[reported]**
- **Loan Details** hint label is at full opacity where the prototype has `.75` — already documented
  in-code as deliberate — and uses an accent colour rather than the default foreground.
  **[reported]**

---

## No tablet layout exists anywhere

All five `tablet-*.html` surfaces describe layouts the app does not implement: a left rail, Budget
Summary as hero+chart side by side, What If as a master-detail scenario split, Settings as
sidebar+detail, and Schedule as a flat six-column sticky-header table. The app uses one
single-column layout with a bottom `TabBar` for every idiom.

This is expected rather than a regression — tablet was prototyped to see the width, and no port was
ever planned — but it is the largest single block of unbuilt design, and the app does ship on iPad.

---

## Matches cleanly (no action)

Splash, first-launch disclaimer, legal popups, Loan Details, Upfront Costs (including its
deliberate double-Done pattern), Transaction Entry, the nine Insights panel titles and their order,
the nine scenario rows and their accent-stripe colour cycling, and both sub-tab orders
(Budget: Income · Expenses · Summary · Asset costs; What If: Answer · Scenarios · Projection ·
Insights).

**Token cross-check passed.** 21 `LoanApp*` keys sampled across the popup, purchase, export, chart
and slider families all exist in all four theme files, and every sampled Dark value in `tokens.css`
matches `Theme.Dark.xaml` exactly.

---

## Reported but false — do not chase

- *"The What If four-way sub-tab split does not exist."* It does — `WhatIfView.xaml` has
  `WhatIfTabAnswer/Scenarios/Projection/Insights`, and the sub-tabs are visible in
  `TestResults/uitest-artifacts/iOS-09b-whatif-scenarios.png`. The claim came from reading
  `AppShell.xaml` alone, which shows only the single `//WhatIf` route.
- *"The scenario index and per-scenario pages are not implemented."* Both exist
  (`ScenarioPage.xaml` + nine `Controls/Scenarios/*.xaml`) and are captured as `09b`/`09c`.
- *"Insights is not implemented as a What If sub-view."* It is, with all nine panels.
