# Google Play — Resubmission & Store Listing Guide

**App:** Loan Affordability Calculator
**Package:** `com.pj.loan.afford.calc`
**Version at time of writing:** 1.12.0
**Support email:** yoursimpleapps@gmail.com
**Privacy policy URL:** https://www.yoursimpleapps.com/privacy-policy-viewer.html?id=loanaffordcalc

> This is a step-by-step checklist for **you** to work through in the Google Play Console for
> the resubmission. Paste-ready store copy is in Section 2, and the reviewer appeal note is in
> Section 3. Nothing here changes the app code — it's all Console configuration + listing text.

---

## 0. Why it was rejected & the strategy

The **Financial features** declaration was contradictory: "provides financial features" was set
to *yes*, but no financial service was selected. Google asked us to correct it.

Strategy: position the app honestly as a **calculator / estimation tool — not a lender, broker,
or adviser**. The in-app changes already done (estimate qualifiers, softened verdicts,
consolidated disclaimer, and a new Privacy Policy) all support this. The Console work below has
to tell the same story consistently, because a mismatch between the declaration, the Data safety
form, and the store listing is what re-triggers rejections.

---

## 1. Pre-submission checklist (do all of these)

- [ ] Update the **Store listing** text (Section 2).
- [ ] Set the **Financial features** declaration correctly (Section 4).
- [ ] Add the **reviewer appeal note** to the declaration/appeal (Section 3).
- [ ] Update the **Data safety** form (Section 5).
- [ ] Set the **Privacy policy URL** in App content (Section 6).
- [ ] Confirm the **Content rating** questionnaire is consistent (Section 7).
- [ ] Confirm **category & contact details** (Section 8).
- [ ] Build a new release **with an incremented version code** and upload (Section 9).
- [ ] Resubmit and, if the declaration form forces the wrong option, file the appeal.

---

## 2. Store listing copy (paste-ready)

### App name (max 30 characters)
```
Loan Affordability Calculator
```
*(29 characters — fits.)*

### Short description (max 80 characters)
```
Estimate loan repayments, affordability & budgets. A calculator, not advice.
```
*(76 characters.)*

Alternate:
```
Loan repayment, affordability & budget calculator. Estimates, not advice.
```

### Full description (max 4000 characters)
```
Loan Affordability Calculator helps you estimate loan repayments and see how a new loan might fit alongside your income and everyday expenses — all on your device, with no account required.

It is a calculator and planning tool. It does not offer, arrange, broker, or recommend any loan or financial product, and it does not connect you with lenders. Every figure it shows is an estimate based on the information you enter, for general information only — not financial advice.

WHAT YOU CAN DO
• Estimate loan repayments from the amount, interest rate and term you enter
• See an estimated monthly position once your income and expenses are included
• Track income and expenses across different frequencies (weekly, fortnightly, monthly, yearly)
• Build a simple budget and see where your money goes
• Explore "what-if" scenarios: test an interest-rate rise, add extra repayments, or change repayment frequency to see the estimated impact
• Get an optional stamp duty estimate for Australian property purchases
• Read plain-language insights about your estimated affordability
• Export a PDF summary of your figures to keep or share
• Choose your currency, pick from several themes, and set your preferred font

PRIVATE BY DESIGN
• The income, expense and loan figures you enter stay on your device
• No account, no sign-in, and no advertising or cross-app tracking
• Crash and diagnostic reports (via Sentry) help us fix problems; they do not include the figures you enter
• View the in-app Privacy Policy and Disclaimer any time from Settings

IMPORTANT
This app provides estimates for general informational purposes only. It is not financial, legal or tax advice, and it is not an offer of any loan, credit or other financial product. Results depend on the information you enter and may differ from figures provided by a lender or adviser. Always consult a qualified financial professional before making any financial decision.
```

### Listing do / don't (keeps the "not a financial service" story consistent)
- **DO** use: *estimate, calculator, planning tool, informational, explore, plan, for general
  information only.*
- **DON'T** use: *get a loan, apply, best/lowest rate, approved, guaranteed, qualify, lender
  matching, financial advice/adviser service.* Those imply a regulated financial service and
  contradict the declaration.

---

## 3. Reviewer appeal / declaration note (paste-ready)

Use this in the rejection reply, the declaration "additional details" box, or an appeal. Replace
the bracketed category with whatever option you actually selected.

```
Loan Affordability Calculator is a standalone, offline loan-affordability calculator. It does not
offer, originate, broker, arrange, advertise, or recommend any loan, credit, insurance, investment,
or other financial product, and it does not connect users with lenders or financial institutions.
It collects no financial-account credentials and performs no transactions. All outputs are estimates
generated from figures the user enters, for general informational purposes only. The app displays
prominent in-app disclaimers stating that it does not provide financial advice and that users should
consult a qualified financial adviser, and it includes an in-app Privacy Policy. We have selected the
closest available category ([the loan information / tools option]); please advise if a more
appropriate informational-tool classification applies.
```

---

## 4. Financial features declaration (the core fix)

**Path:** Play Console → your app → **Policy → App content → Financial features**.

Recommended answers:
- When asked whether the app provides financial features / is a financial product, choose the
  option that matches an **information / calculator tool about loans**, **not** "my app offers or
  originates personal loans."
- Decision tree against the live form:
  - If it distinguishes *"provides personal loans"* vs *"provides information or tools about loans"*
    → pick the **information / tools** variant. The app computes estimates; it does not lend.
  - If the only loan option is origination (*"my app offers personal loans"*) → **do not select it.**
    That triggers Personal Loan App requirements (APR disclosure, repayment terms ≥ 60 days, lender
    identity, etc.) a calculator cannot meet. This is exactly the point to raise in the appeal
    (Section 3), and reconsider whether *"does not provide financial features"* is the more accurate
    answer.
  - **Do not** select any *financial advice / advisory* option — the app explicitly disclaims giving
    advice.
- Make sure the top-level yes/no answer and the sub-selection **agree** (the original mismatch is
  what got it rejected).

> The exact option labels change over time; apply the logic above to whatever the form currently
> shows.

---

## 5. Data safety form

**Path:** Play Console → **Policy → App content → Data safety.**

Declare it truthfully and consistently with the Privacy Policy:

| Question | Answer |
|---|---|
| Does your app collect or share user data? | **Yes** (because of crash diagnostics) |
| Financial info the user enters (income/expense/loan) | **Not collected** — stored locally on device only, never transmitted to us |
| Crash logs / diagnostics (Sentry) | **Collected** — App info & performance → *Crash logs* and *Diagnostics*. Purpose: **App functionality / analytics**. Not shared for ads. Not linked to identity. |
| Purchases (Google Play Billing) | Payment handled by Google Play; the app only receives purchase status. Declare per the form's in-app purchase questions. |
| Is data encrypted in transit? | **Yes** (Sentry uses HTTPS). |
| Can users request deletion? | Local data is removed by uninstalling / clearing app data; there are no server-side accounts to delete. |
| Advertising / tracking IDs | **No.** |

---

## 6. Privacy policy URL

**Path:** Play Console → **Policy → App content → Privacy policy.**
```
https://www.yoursimpleapps.com/privacy-policy-viewer.html?id=loanaffordcalc
```
- Make sure that hosted page is live and reachable **before** submitting (reviewers open it).
- The app now also shows this policy in **Settings → Privacy Policy** (in-app), which is good
  practice and reinforces consistency.

---

## 7. Content rating

**Path:** Play Console → **Policy → App content → Content rating.**
- Re-run/confirm the IARC questionnaire. This is a **utility/finance calculator**: no gambling, no
  simulated gambling, no real-money features beyond the premium in-app purchase.
- Answer the "does the app contain financial/gambling features" style questions consistently with
  the Financial features declaration (calculator, not a financial service, no gambling).

---

## 8. Category, tags & contact

- **Category:** Finance (or Tools if you prefer to de-emphasise "finance"; Finance is fine given
  the calculator framing).
- **Tags:** choose calculator / budgeting / personal finance tool tags; avoid loan-origination or
  lending tags.
- **Contact details:** support email `yoursimpleapps@gmail.com`; website if you have one.
- Store listing graphics/screenshots: make sure captions/overlays also say "estimate" / "calculator"
  and don't imply you provide or arrange loans.

---

## 9. Build & upload

- Increment the **version code** (currently `1`) for the new build — Play rejects a re-upload with
  the same version code.
- Upload the new AAB to the track you're submitting (Production / Closed testing as appropriate).
- Double-check the release notes don't claim lending/advice features.

---

## 10. Final consistency pass (the thing that prevents a repeat rejection)

Before hitting submit, confirm these four all tell the **same** story:
1. **Financial features declaration** → information/tools calculator, not a lender/adviser.
2. **Data safety** → local-only financial inputs + Sentry diagnostics + Play Billing.
3. **Privacy policy** (hosted + in-app) → matches Data safety.
4. **Store listing** (short + full description, screenshots) → "estimator/calculator, not advice."

If any one of these implies the app *is* a financial service or lender while another says it isn't,
that inconsistency is the most common cause of re-rejection.

---

*Note: this is product/compliance guidance, not legal advice. For the privacy policy and disclaimer
wording, a review by a qualified professional before release is worthwhile.*
