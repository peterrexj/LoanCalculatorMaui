# Play Store Financial Declaration + Advisory-Safe Content Pass

## Context

The app was rejected from Google Play because the **Financial Features declaration** was
internally contradictory: the top-level "provides financial features" answer was set to *yes*,
but no listed financial service was selected. Google asked us to review the declaration and pick
the appropriate value.

Separately, the app's framing ("loan **affordability**", What-If verdicts like *"At risk"* /
*"Unaffordable"*, Insights saying *"how much you can comfortably manage"*) reads in places like
financial *advice / a verdict stated as fact* rather than a neutral estimator. That is both a
policy risk (reinforces the "financial service" perception) and a liability risk.

**Goal:** (1) give a defensible declaration strategy + a reviewer appeal note; (2) make the
in-app content consistently read as *estimates from user input, not advice*, which also supports
the declaration; (3) consolidate and clean up the duplicated disclaimer content.

Decisions from the user:
- Declaration: **pick the closest category + appeal** (not "declare none").
- Reframe depth: **thorough advisory reframe** (qualifiers + softened verdicts + microcopy; keep useful labels).
- Disclaimers: **consolidate to one source + fix issues**.

> Note: web access was unavailable during planning, so the exact live Console option labels
> could not be confirmed. Part A is guidance to apply against the current form; the code work
> (Parts B–D) is unaffected.

---

## Part A — Play Store declaration (manual, in Play Console; no code)

**Recommended selection:** the **Loans** family, choosing the *information / tools about loans*
sub-option — **NOT** "Personal loans (my app offers/originates loans)".

Rationale / decision tree to apply against the live form:
- If the form distinguishes "provides personal loans" vs. "provides **information or tools** about
  loans" → select the **information/tools** variant. The app computes estimates; it does not lend.
- If the only loan option is origination ("my app offers personal loans") → selecting it wrongly
  triggers the **Personal Loan App** requirements (APR disclosure, repayment terms ≥ 60 days,
  lender identity, etc.) that a calculator cannot satisfy. In that case this is precisely the point
  to raise in the appeal, and consider whether "does not provide financial features" is the more
  honest answer after all.
- Do **not** select "financial advice/advisory" — implies regulated, personalised advice the app
  explicitly disclaims.

**Also verify for consistency** (a mismatch here can re-trigger rejection):
- Content rating (IARC) questionnaire — financial/simulated-gambling questions.
- Data safety section — the app stores data locally only; declare accordingly.
- Store listing copy — ensure it describes an *estimator/calculator*, not a lending/advice service.

**Draft reviewer appeal note** (paste into the rejection reply / declaration notes):

> "[App name] is a standalone, offline loan-affordability **calculator**. It does not offer,
> originate, broker, arrange, advertise, or recommend any loan, credit, insurance, investment, or
> other financial product, and it does not connect users with lenders or financial institutions.
> It collects no financial-account credentials and performs no transactions. All outputs are
> **estimates** generated from figures the user enters, for general informational purposes only.
> The app displays prominent in-app disclaimers stating it does not provide financial advice and
> that users should consult a qualified financial adviser. We have selected the closest available
> category ([the loan-information/tools option]); please advise if a more appropriate
> informational-tool classification applies."

Adjust bracketed text to the actual option label chosen.

---

## Part B — Consolidate + revise the disclaimer content

Canonical long-form source stays the embedded HTML, which already feeds both in-app long-form
surfaces; the PDF is switched to derive from the same source so all long-form copies share one
origin.

1. **Revise wording** in `src/LoanCalculator/Extensions/DisclaimerData/AppLaunchDisclaimerData.html`
   (single source for the startup consent popup + Settings "Disclaimer" WebView):
   - Add an explicit **"What this app is"** opening line reinforcing the declaration:
     *"This app is a calculator/estimation tool. It does not offer, arrange, broker, or recommend
     any loan, credit, insurance, investment, or other financial product or service, and does not
     connect you with lenders. All figures are estimates for general informational purposes only."*
   - Fix the **two empty headings** ("No Legal Obligations", "User Responsibilities") — give each a
     body paragraph or remove the heading (the popup parser only emits `<h2>`+`<p>` pairs, so blank
     headings render as empty rows).
   - Replace the placeholder contact `[yoursimpleapps@gmail.com]` with the real support email
     (confirm value) and drop the stray brackets/`<i>` tags.
   - Keep existing strong clauses ("informational purposes only", "estimates", "no financial
     advice", "consult a financial adviser", "at your own risk", "no liability").

2. **Delete the dead file** `src/LoanCalculator/Extensions/DisclaimerData/AppLaunchDisclaimerData.txt`
   (embedded but never read; wording diverges) and remove its `<EmbeddedResource>` line in
   `src/LoanCalculator/LoanCalculatorMaui.csproj` (~L174).

3. **PDF disclaimer → single source.** Today it is a hard-coded C# copy in
   `src/LoanCalculator.Core/Pdf/PdfGeneratorBaseWithDisclaimer.cs` (`GenerateDisclaimerData`).
   - Extract the existing HTML→sections parser out of
     `PopupDisclaimerViewModel.ParseDisclaimerHtml`
     (`src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/PopupDisclaimerViewModel.cs:73-91`)
     into a shared Core helper (e.g. `SharedServiceCore.GetDisclaimerSections()` returning ordered
     `(Heading, Body)` items from `SharedServiceCore.DisclaimerData`).
   - Have both `PopupDisclaimerViewModel` and `PdfGeneratorBaseWithDisclaimer` consume it, so the
     PDF renders the same wording as the HTML (map heading→`DrawH2`, body→paragraph). This removes
     the 4th divergent copy.
   - Lower-risk fallback if PDF re-layout proves fiddly: keep the C# method but paste the revised
     wording verbatim and add a `// keep in sync with AppLaunchDisclaimerData.html` comment.
     (Prefer the shared-helper route since the user chose "consolidate".)

4. **Loan-page banner disclaimers** (short, intentionally concise) at
   `src/LoanCalculator/View/LoanView.xaml:806-809` and `:1820-1823`: revise wording to match tone
   and, to avoid drift between the two identical literals, hoist the string into one shared resource
   / constant referenced by both. Reuses existing `DisclaimerBannerView` control (no new control).

---

## Part C — Advisory-safe reframe of results & verdicts (thorough)

Pattern to apply everywhere a result/verdict is shown: (a) qualify numbers as **estimates**
(extend the existing house style — "STAMP DUTY (ESTIMATE)", PDF "Estimate ONLY", "(estimated)");
(b) reword absolute verdicts into estimate/advisory phrasing; (c) keep useful labels but neutralise
certainty and nudge toward a professional.

Representative changes (not exhaustive — same pattern applies to sibling strings in each file):

- **Affordability caption/value** —
  `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` (`Affordability` L869-877,
  `AffordabilityTextDescription` L884-892): caption *" your monthly affordability status"* →
  *" estimated monthly position (estimate only)"*; ensure an "Estimate" qualifier is visible with the
  bare number (via caption or a small sublabel in `LoanView.xaml` / `BudgetView.xaml.cs` L426-430).

- **Insights copy** — `src/LoanCalculator.Core/Models/ViewModels/InsightsViewModel.cs` L24, L30:
  remove absolute *"you can comfortably manage"* / *"sustainably manage while maintaining your
  financial stability"* → *"an estimate of what may remain each month after expenses and the loan
  repayment; your actual position depends on your circumstances."* Mirror the same edit in the PDF
  Affordability blurb (`PdfInsightsGenerator.cs` L295-296).

- **What-If verdicts (highest priority)** —
  `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/WhatIfViewModel.cs` (L84/89/94 headroom,
  L531-539 stress test) and `src/LoanCalculator/View/WhatIfView.xaml` risk-banner DataTriggers
  (L1003-1017):
  - "Unaffordable — … over budget" → "Estimate: looks unaffordable — about …/mo over budget (estimate only)".
  - Keep "Comfortable / Moderate risk / At risk" labels but append "(estimate)" and reword the
    clause to advisory ("…based on the figures you entered; consider speaking to a financial adviser").
  - "Already unaffordable" / "over limit" → prefix "Estimate:".
  - Info popups in `src/LoanCalculator/View/WhatIfView.xaml.cs` L107-132: soften absolute claims —
    "this typically saves 4–6 years and tens of thousands in interest" → "this could save several
    years and a significant amount of interest — actual results vary"; "Above this rate the loan
    becomes unaffordable" → "above this estimated rate, your repayment would exceed the income you
    entered."

---

## Part D — Persistent "estimate only / not advice" microcopy near results

Add one concise line near the primary result on surfaces that currently lack it, reusing the
existing `DisclaimerBannerView` (Information style) or a small `{DynamicResource}` Label:
- **What-If page** (`WhatIfView.xaml`) — near the stress-test / headroom output.
- **Budget page** (`BudgetView.xaml`) — near the "Loan Affordability" card.
- Loan page already has the banner on Asset + Insights tabs (keep, reworded per Part B4).

Suggested text: *"Estimates only — not financial advice. Figures depend on your inputs. Consult a
qualified financial adviser before deciding."*

---

## Part E — In-app Privacy Policy (new; required for resubmission)

A privacy policy is a separate Play requirement from the financial declaration. The app currently
has none, yet it sends data off-device via **Sentry** (crash/diagnostics, `Sentry.Maui` in
`MauiProgram.cs`/`App.xaml.cs`) and processes purchases via **Google Play Billing**. Decision:
add an **in-app privacy policy + a Settings link**; the user already has a **hosted URL** (to be
provided) which is also set in the Play listing.

1. **Add embedded content** `src/LoanCalculator/Extensions/DisclaimerData/PrivacyPolicyData.html`
   (new EmbeddedResource; add to `LoanCalculatorMaui.csproj` next to the disclaimer resources).
   Content must disclose, in plain language:
   - Financial inputs (income/expense/loan figures) are stored **only locally on the device**; not
     transmitted to us or sold.
   - **Crash/diagnostic reporting via Sentry** (what it collects, why).
   - **Purchases via Google Play Billing** (premium unlock; Google processes payment).
   - No accounts, no ad tracking (confirm), contact email, and a link to the hosted policy URL.
2. **Expose in Core** the same way the disclaimer is: add a `SharedServiceCore.PrivacyPolicyData`
   loader mirroring `DisclaimerData` (`Services/SharedServiceCore.cs:151-162`) — read embedded
   HTML, substitute `{{AppName}}`, swap hex→theme keys.
3. **Settings UI:** add a new **"Privacy Policy"** row in `src/LoanCalculator/View/SettingsView.xaml`
   (mirror the existing "Disclaimer" row at `:707-725`) that opens a full-screen `SfPopup` WebView
   (reuse the disclaimer popup pattern at `SettingsView.xaml:16-75`, Close button). Wire
   `ShowPrivacyPolicyCommand` + `PrivacyPolicyData` in
   `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/SettingsViewModel.cs` (mirror
   `ShowDisclaimerCommand`/`AppLaunchDisclaimerData` at `:63,287,298-307`). Optionally add a
   "View online" link that opens the hosted URL via `Launcher.OpenAsync`.
4. Relabel the existing Settings disclaimer sublabel "Legal information and terms" if it's now
   misleading (disclaimer vs. privacy are separate rows).

> Requires from user: the **hosted privacy-policy URL**, and confirmation of what the policy must
> state (esp. Sentry + any analytics). The **Data safety** form in Play Console must be updated to
> match (declare Sentry crash/diagnostic collection + Play Billing).

---

## Files to modify (summary)

| Area | File(s) |
|---|---|
| Disclaimer source | `Extensions/DisclaimerData/AppLaunchDisclaimerData.html`; delete `.txt` + csproj resource line |
| Shared parser | `.../PrimaryModels/PopupDisclaimerViewModel.cs`, `Services/SharedServiceCore.cs` (new `GetDisclaimerSections`) |
| PDF | `Core/Pdf/PdfGeneratorBaseWithDisclaimer.cs`, `Core/Pdf/PdfInsightsGenerator.cs` |
| Results/verdicts | `.../PrimaryModels/LoanViewModel.cs`, `InsightsViewModel.cs`, `.../PrimaryModels/WhatIfViewModel.cs` |
| Views | `View/LoanView.xaml`, `View/WhatIfView.xaml`(+`.xaml.cs`), `View/BudgetView.xaml`(+`.xaml.cs`) |
| Privacy policy (new) | new `Extensions/DisclaimerData/PrivacyPolicyData.html` + csproj resource; `Services/SharedServiceCore.cs`; `View/SettingsView.xaml`; `.../PrimaryModels/SettingsViewModel.cs` |

## Verification

- Build Core + run unit tests: `dotnet test src/Tests/LoanCalculator.UnitTests/LoanCalculator.UnitTests.csproj`
  (existing theme-key + stamp-duty tests must stay green; no logic changes expected).
- PDF: dry-run via the Playground (`dotnet run --project src/LoanCalculator.Playground/...`) and
  confirm the disclaimer renders correctly from the shared source with the new wording.
- Manual (per CLAUDE.md, only when you choose to build/run): launch the app, confirm the startup
  consent popup shows revised text with no empty headings, Settings → Disclaimer matches, Loan /
  Budget / What-If show "estimate" qualifiers, softened verdicts, and the microcopy line.
- Then apply Part A in Play Console and resubmit with the appeal note.

## User-provided values / external steps (not code)

- Real **support email** for the disclaimer + privacy-policy contact line.
- The **hosted privacy-policy URL** (for Part E and the Play listing).
- In Play Console: set the privacy-policy URL, update the **Data safety** form to declare Sentry
  crash/diagnostic collection + Play Billing, and apply the Part A financial-features selection +
  appeal note.
