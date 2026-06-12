# 10 — UX: Engagement Features

**Priority:** Medium  
**Status:** ❌ NOT STARTED  
**Goal:** Give users reasons to return to the app, explore it more deeply, and share it — without touching the IAP/trial flow.

---

## Feature Ideas by Category

These are proposed additions. Each is scoped as an independent, self-contained feature so they can be shipped one at a time.

---

### A — Loan Comparison (Side-by-Side)

**What:** Let the user save their current loan scenario and compare it against a second set of inputs. A simple "Compare" button opens a split view or summary table showing Scenario A vs Scenario B.

**Why it drives engagement:** Users often want to compare "what if I get a better rate" or "20% vs 10% deposit". Currently they have to manually re-type and remember the old numbers.

**Implementation sketch:**
- Add a `LoanScenario` model (property amount, rate, term, deposit — a subset of `LoanViewModel`)
- Add "Save as Scenario A / B" buttons on the Asset tab
- Add a Comparison section at the bottom of the Asset tab (or as tab 5) showing a 2-column comparison of key metrics
- Scenario state is persisted as `scenarioA.json` / `scenarioB.json` via `ILocalStorage`
- No changes to `LoanViewModel` core calculations

---

### B — Repayment Goal Tracker

**What:** Display a progress bar on the Amortization tab showing "You are X% through your loan term" with an estimated payoff date. If the user sets an "extra repayment" amount, show how many months/years it shaves off.

**Why it drives engagement:** Connects an abstract number to a real timeline. Motivates users to return and adjust.

**Implementation sketch:**
- Two new computed properties on `LoanViewModel`: `ExtraRepaymentAmount` (user input), `TimeShaved` (calculated)
- Calculation in `HomeLoanCalculator.cs` — add an overload that accepts `extraRepayment`
- Progress bar already exists in the dependency tree (`Syncfusion.Maui.ProgressBar`) — wire it up
- Single `SfLinearProgressBar` on the Amortization tab header

---

### C — Smart Affordability Prompt

**What:** When the user has not entered income/expense data and visits the Insights tab, instead of just showing "record your income & expenses", show a dismissable card with a clear call-to-action: "See your affordability — takes 2 minutes" with a direct link/button to the Income tab.

**Why it drives engagement:** Reduces friction between "I see an empty state" and "I actually fill in data". The empty state currently just shows a label — it dead-ends the user.

**Implementation sketch:**
- New `EmptyStateCard` `ContentView` control with an icon, headline, body text, and a `Command` property
- Replace the `{Binding AffordabilityTextDescription}` label in the Insights tab header with this control when `IsAffordabilityAvailable == false`
- Button command calls `Shell.Current.GoToAsync("///income")` (Shell route navigation)
- No ViewModel changes beyond exposing the command

---

### D — Share Loan Summary

**What:** A "Share" button on the Insights tab that generates a plain-text or image summary of the current loan (key numbers) and invokes the native share sheet.

**Why it drives engagement:** Sharing = organic app marketing. Users share with partners or brokers. `Share.RequestAsync` is already available via MAUI.

**Implementation sketch:**
- Add `ShareSummaryCommand` to `LoanViewModel`
- Method builds a formatted string of key metrics (property value, loan amount, repayments, total interest)
- Calls `await Share.RequestAsync(new ShareTextRequest { Title = "My Loan Summary", Text = summary })`
- No new dependencies — `Share` is in `Microsoft.Maui.ApplicationModel`
- Button lives in the Insights tab toolbar or as a `SfButton` near the export PDF button

---

### E — Rate Alert / "What-if" Nudge

**What:** A subtle banner on the Asset tab that says "Rates have moved — see how your repayments change" that appears periodically (once per week) and pre-fills a scenario for the user. This does not require a live rate feed — it can use a simple "last time you opened this app, rates were X; now they are Y" pattern using a stored rate.

**Why it drives engagement:** Gives users a reason to reopen the app even if they're not actively house-hunting.

**Implementation sketch:**
- Store the last-used interest rate in `Preferences` when the user changes it
- On each session start, compare stored rate to current `InterestRate` in the ViewModel
- If they differ by >0.25%, show the `DisclaimerBannerView` (Information type) with the comparison
- Dismissable — store dismissal date so it doesn't show more than once per week
- No new controls needed — `DisclaimerBannerView` already supports Information type and is used in `LoanView.xaml`

---

### F — Onboarding Walkthrough (First Launch Only)

**What:** A 3-step coach mark overlay on first launch guiding users through: (1) Enter your property value, (2) Set your deposit, (3) Check Insights for affordability.

**Why it drives engagement:** New users often don't know the app shows affordability. The onboarding makes the value proposition clear immediately.

**Implementation sketch:**
- Use `PopupDisclaimerView` pattern (already exists) or a new `OnboardingOverlay` ContentView with 3 steps
- State stored in `NameValueDataService.NameValueDataModel` (already has `HasShowAppLaunchDisclaimer` — add `HasCompletedOnboarding`)
- Show only once; user can dismiss at any step
- Highlight the relevant input control using a semi-transparent overlay with a "hole" (using `Clip` on a covering `Grid`)

---

## Prioritization Recommendation

| Feature | Effort | Engagement Impact | Suggested Order |
|---------|--------|------------------|-----------------|
| C — Smart empty state CTA | Low | High | 1st |
| D — Share loan summary | Low | High | 2nd |
| B — Repayment goal tracker | Medium | High | 3rd |
| F — Onboarding walkthrough | Medium | High | 4th |
| A — Loan comparison | High | Very High | 5th |
| E — Rate alert nudge | Medium | Medium | 6th |
