# Navigation proposal

Stage 3 of the redesign. This is the approval gate: once the structure below is agreed, the full
prototype set gets built against it. Nothing here is implemented in the app yet.

Grounded in the 18 screenshots in `design/screenshots/`, captured from the running app, plus the
surface inventory.

---

## What's actually wrong today

Five problems, in the order they cost the user something. The first two are structural, which is
why navigation is in scope rather than just styling.

**1. Depth is inconsistent, so the tab bar doesn't tell you where things are.**
Loan and Budget each hide four sub-tabs behind a second tab strip. What If and Settings have none.
So 8 of the 11 primary surfaces live at a depth the bottom bar gives no hint of, and there is no
way to reach a sub-tab directly.

**2. Two surfaces are enormous single scrolls with no overview.**
What If is nine scenario cards where *one card is taller than the screen* — roughly nine screens of
scrolling, with no index and no way to jump to the scenario you want. Loan Insights is the same
shape: nine expanders in one scroll. In both cases the page is a list of things pretending to be a
page.

**3. Related figures are split across tabs; unrelated ones share one.**
Running Cost (an ongoing outflow) sits under **Loan**, while the other outflows sit under
**Budget**. Projection sits under Budget, while the other forward-looking tool (What If) is its own
tab. Amortisation — a reference table — sits beside the main data-entry form.

**4. Charts render as bright light panels on a dark page.**
Visible in Budget Summary, What If, Insights and Amortisation. `LoanAppChartCardBackground` is
`#DDE6ED` in the *Dark* theme, so every chart is a white box, and chart text is dark to compensate.
It is the single most conspicuous visual defect in the app.

**5. Settings labels everything twice.**
A section header reading `STEP SIZES` sits above a card whose only row reads `Step sizes`. Same for
What-If Defaults and Calculator Defaults. Three full screens of settings for a modest number of
controls.

Things that are *not* wrong and should survive: the modal data-entry pattern (tap a figure, edit it
on a real page, Done) now works well and is the right shape. The three-box hero summary reads
clearly. The colour-coded accent stripe per What-If scenario is good.

---

## The proposal

**Reorganise around the question the user is asking, not around which file the data lives in.**

Today's tabs are named after data types — Loan, Budget, What If. Someone opening this app wants to
know *"can I afford this, and what breaks it?"*. Four destinations answer that in order:

| # | Destination | Answers | Holds |
|---|---|---|---|
| 1 | **Loan** | What am I buying and borrowing? | Asset value, deposit/loan split, rate, term, frequency, upfront costs |
| 2 | **Budget** | What goes in and out each month? | Income, expenses, the asset's running costs, net surplus |
| 3 | **What If** | Can I afford it, and what breaks it? | Affordability, insights, stress test, scenarios, export |
| 4 | **Settings** | — | Preferences, data, support |

Three changes do most of the work:

**Running Cost moves from Loan to Budget.** It is a monthly outflow. Putting it beside income and
expenses means every recurring figure lives in one place, and the surplus can finally be computed
from one screen's worth of data rather than two tabs.

**Amortisation and Projection become reference views inside What If**, not peers of data entry.
Neither is something you *edit*; both are things you *read* once the numbers are set.

**What If and Insights merge into What If as a scenario index, not a scroll.** What If opens on the
affordability answer. Beneath it, scenarios are a *list* — nine rows, each showing its headline
delta (`Rate +0.5% → +$161/mo`) — and tapping one opens that scenario full-screen. This is the
single biggest usability win available: it turns nine screens of scrolling into one screen of
choosing, and it reuses the modal-page pattern that already works for data entry.

### Why four and not three

A three-tab version (merge Settings into an overflow) tested badly against the inventory: Settings
holds the destructive data-delete actions and the premium restore, both of which want a stable,
findable home. Four is also what the app has today, so the bottom bar stays familiar even though
what sits behind each tab changes.

### What this does not change

Shell tabs stay a bottom bar of four. Modal data entry stays modal. The four themes, eight fonts
and all 198 design tokens are untouched — this is a re-grouping plus a chart-colour fix, not a new
visual language from scratch.

---

## Coverage map

Every surface in the inventory, and where it lands. Nothing is dropped except the two already-dead
views.

| Today | Becomes | Note |
|---|---|---|
| Loan ▸ Asset | **Loan** (main) | Keeps the hero summary and the tappable figures |
| Loan ▸ Running Cost | **Budget** ▸ Asset costs | Moved: it is an outflow |
| Loan ▸ Amortisation | **What If** ▸ Schedule | Reference table |
| Loan ▸ Insights (9 expanders) | **What If** (main) + rows | Becomes the affordability answer, not a scroll |
| Budget ▸ Income | **Budget** ▸ Income | |
| Budget ▸ Expenses | **Budget** ▸ Expenses | |
| Budget ▸ Summary | **Budget** (main) | The surplus answer, including asset running costs |
| Budget ▸ Projection | **What If** ▸ Projection | Forward-looking, sits with scenarios |
| What If (9 cards) | **What If** ▸ scenario list → 9 detail pages | The scroll becomes an index |
| What If empty state | **What If** empty state | Gains a working link to Loan |
| Settings | **Settings** | Double-labelling removed |
| Settings ▸ Disclaimer / Privacy | unchanged (modal) | One layout, two footer states |
| Modal — Loan Details | unchanged | Opened from Loan |
| Modal — Upfront Costs | unchanged | Opened from Loan |
| Modal — Transaction Entry | unchanged | Opened from Budget (3 call sites, one layout) |
| First-launch disclaimer | unchanged | |
| Premium upsell | unchanged (modal) | Still reachable from all four tabs |
| Splash | unchanged | |
| Disclaimer/Instructions banner | component, reused | |
| `IncomeView`, `ExpenseView` | **dropped** | Already unreachable: commented out of the shell, no DI registration |

Surface count stays about the same; nine What-If scenarios become addressable pages instead of
sections of one scroll, which is a net gain in reachability rather than in volume.

---

## Also fixed as part of this — decided values

These two were left to me rather than needing a design call from the owner. Both are settled, so
the port is mechanical.

### Charts go dark-on-dark — and it is ONE theme, not four

Checking all four themes changed the scope. Light, Forest and Warm **already** follow the right
pattern: chart card in the page's own colour family, with contrasting ink.

| theme | page bg | chart card | axis ink | verdict |
|---|---|---|---|---|
| Light | `#F8FAFC` | `#f7fafe` | `#2E4053` | fine |
| Forest | `#183d3d` | `#586a63` | `#bed0c9` | fine |
| Warm | `#F1D9D9` | `#FBEDED` | `#7d4a4a` | fine |
| **Dark** | `#222831` | **`#DDE6ED`** | **`#27374D`** | **broken — near-white card** |

So only `Theme.Dark.xaml` changes. Eight values, chosen to match the tone Dark already uses for
its other cards (`BorderControlGradientBackground` is `#27374D` → `#393E46`), so charts stop
looking like a foreign element:

| key | from | to |
|---|---|---|
| `LoanAppChartCardBackground` | `#DDE6ED` | `#27374D` |
| `LoanAppChartInnerBorderGradientStart` | `#DDE6ED` | `#27374D` |
| `LoanAppChartInnerBorderGradientEnd` | `#9DB2BF` | `#393E46` |
| `LoanAppChartAxisTextColor` | `#27374D` | `#DDE6ED` |
| `LoanAppChartLegendTextColor` | `#27374D` | `#DDE6ED` |
| `LoanAppChartTitleTextColor` | `#27374D` | `#DDE6ED` |
| `LoanAppChartDataMarkerTextColor` | `#27374D` | `#DDE6ED` |
| `LoanAppDonutCenterTextColor` | `#27374D` | `#DDE6ED` |

**Deliberately unchanged:** the three series colours (`#5FBDB0` / `#E0A85A` / `#A98FD0`) are already
designed for a dark ground; `LoanAppChartDataMarkerTextColor1..3` sit *on* those series colours, not
on the card, so they stay dark; and the outer border gradient is a stroke that reads fine either way.

### One separator treatment

Today: 55 inline `BoxView` separators at five different opacities (0.08 / 0.1 / 0.12 / 0.15 / 0.20)
with no shared style. Decided: **two keyed styles, both 1px of `LoanAppDefaultForegroundColor` at
opacity 0.1** — the most common existing value, so the majority of instances do not visibly change:

- `LoanAppSeparatorStyle` — no margin. The default, for separating blocks.
- `LoanAppSeparatorInsetStyle` — `Margin="16,0"`. For separating rows *inside* a card, where the
  rule must align with the rows' own `Padding="16,…"`.

Two traps for whoever does the sweep:
- Settings' **17** row separators are the inset kind — that margin is deliberate row alignment,
  documented in `CLAUDE.md`, not page padding.
- What If has **7 `BoxView`s that are not separators** — they are 4pt vertical accent stripes in a
  `Grid ColumnDefinitions="4,*"`. A blanket find-and-replace on `BoxView` corrupts them.

---

## Revision after owner review — depth and the verdict

Two problems were found on review, both valid. The first was a regression; the second was a real
flaw in the proposal above, so the structure changed.

**1. The affordability verdict had ended up only inside What If.** The user configures the loan on
one tab and had to switch tabs to learn whether it works — and today's app already shows an
Affordability box on the Loan page, so this was worse than the status quo, not better. Fixed by
putting a **tappable verdict in the hero row of both Loan and Budget**, leading through to the full
answer. Summary-and-detail, not duplication. On Budget it does real work: the net figure there does
*not* account for the loan repayment, so without the chip that page quietly overstates what is
spare.

**2. Four features were demoted from tabs to third-level rows.** This proposal criticised the old
IA for inconsistent depth and then buried Insights, Schedule, Projection and Export under a
weakly-labelled "Reference" heading, below a hero and nine scenario rows. Amortisation and Insights
are *tabs* today. That trades What If's long scroll for four undiscoverable features, which is not
a net win.

The specific error was moving Amortisation and Projection into What If at all: **Schedule is purely
loan-derived and belongs with the loan, where it already is.** Projection is forward-looking and
does pair with scenarios. So one of those two moves was right and one was wrong.

### Revised structure

| Destination | Sub-views |
|---|---|
| **Loan** | Single screen *(+ verdict chip)*, with Schedule reached by a row |
| **Budget** | Summary *(+ verdict chip)* · Income · Expenses · Asset costs |
| **What If** | Answer · Scenarios · Projection · Insights |
| **Settings** | — |

Everything is now at two levels, nothing deeper than today, every destination at four or fewer.
Export becomes an action on the Answer view rather than a row. The scenario index — the single
biggest usability win — is preserved as its own sub-view.

The depth rule becomes stateable rather than arbitrary: **a destination with multiple peer views
gets a sub-tab strip; a single-screen destination does not.** Loan is the test of that rule — it
briefly had an Asset · Schedule strip, which was removed because one real view plus one reference
table does not justify the chrome. The schedule is reached by a row instead, the same way Export
is, which keeps Loan a single purposeful screen.

The name **Insights** is kept rather than renamed to "Breakdown". Renaming it made it unfindable in
review, and it contradicts the decision to keep the familiar tab names so nothing has to be
relearned. **All nine panels are kept** — see the correction under "Decisions taken since this was
written"; the reduced set proposed earlier was based on a wrong reading of what duplicates what.

Schedule no longer moving also removes a chunk of the test churn described in `TEST-IMPACT.md`.

### Contextual discovery, independent of structure

Structure gets people somewhere deliberately; contextual links get them there accidentally, which
is how most features are actually found. Added at the point of the number each one explains:
*"See the full schedule ›"* beneath the repayment on Loan, *"See the projection ›"* beneath the
surplus on Budget.

## Decisions taken since this was first written

- **Tab names stay exactly as they are**: Loan / Budget / What If / Settings. The earlier working
  names (Deal / Money / Verdict) were dropped as too idiomatic for non-native speakers. The
  regrouping behind each tab is unchanged. A useful side effect: `ShellTabs.TitleOf` in the UI test
  suite hardcodes those four titles, so keeping them removes most of the test churn a rename
  would have caused.
  - Known mismatch, accepted: **What If** now *leads* with the affordability answer and also holds
    Projection and Insights. The label describes the scenarios rather than the answer, so someone
    asking "can I afford this?" may not look there. (Schedule is **not** here — it stayed with
    Loan as a pushed page, per the sub-tab rule above.)
- **Insights becomes a sub-tab under What If, and keeps all nine panels.** This reverses what the
  section above originally proposed. Two claims that justified cutting four of them were checked
  against the code and both were wrong:
  - *"The PDF report is generated from the same breakdown, so nothing that appears only there was
    dropped."* **False.** `PdfInsightsGenerator` builds its own `PdfDataInsightsModel` and never
    reads `InsightsDetails`, so the report is fully decoupled — which cuts the other way from how
    it was used: dropping a UI panel could not have removed a report line, so the PDF was never
    the safety net the argument treated it as.
  - *"Panels 6 and 7 merely restate what Budget shows."* **False.** Budget's income/expense
    figures are mutated by the `ShowIncomeAfterExpense` / `ShowPropertyExpense` toggles, so those
    two panels are the only *unconditional* source for them.

  Net: nine panels in the app and nine in `insights.html`.
- **Tablet is now prototyped**, not just phone. The app ships on iPad and none of the redesign had
  been seen at that width. See the `tablet-*.html` set, which uses the real Tablet token column
  (body text 12→18, hero amount 18→32, chart height 250→350).

## What I need from you

Approve or redirect the four destinations and the three moves (Running Cost → Budget,
Amortisation/Projection → What If, What If → scenario index).

Alongside this proposal there are three clickable prototypes demonstrating the structure — a dense
read-only surface, the scenario index that replaces the long scroll, and a settings-style list. Open
`design/prototype/index.html`. Once you are happy, Stage 4 builds the remaining surfaces against it.

---

## Figure corrections made while reconciling the prototypes

Worth recording, because two of these were wrong in every prototype that showed them:

- **Base-rate headroom is $1,708/mo**, not $1,547. $1,708 = 6000 income − 1500 expenses − 2792
  repayment. The $1,547 figure is the headroom at the **stressed** 5.5% rate (repayment $2,953)
  and now appears only in the rate-change scenario, where it belongs. It had been propagated
  onto the Loan and Budget verdict chips and the What If hero.
- **The rate-change scenario said $1,386**, which reconciles with nothing; corrected to $1,547.
- **Principal repaid is $520,112** — it equals the loan, by definition. One Insights panel still
  carried $533,484 from before the schedule totals were corrected.

The whole chain now reconciles: $2,792.07 × 360 = $1,005,147 repaid, less $520,112 principal =
$485,035 interest, matching the Schedule's three decades of $335,049.
