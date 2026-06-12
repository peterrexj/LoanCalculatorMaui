# Second Improvement — Plan Index

**Goal:** Elevate the app beyond bug fixes into a product people come back to, recommend, and pay for. These plans target four orthogonal dimensions.

**Approach:** Additive and layered. No rearchitecting. Each plan is independent — ship in any order. IAP/trial flow untouched.

---

## Status Overview

| # | File | Dimension | Priority | Status |
|---|------|-----------|----------|--------|
| 14 | [14-feature-loan-comparison.md](14-feature-loan-comparison.md) | Feature — side-by-side scenario comparison | High | ❌ Not started |
| 15 | [15-feature-extra-repayment.md](15-feature-extra-repayment.md) | Feature — extra repayment & time-saved calculator | High | ❌ Not started |
| 16 | [16-feature-share-and-export.md](16-feature-share-and-export.md) | Feature — share loan summary; export improvements | Medium | ❌ Not started |
| 17 | [17-retention-onboarding.md](17-retention-onboarding.md) | User Retention — first-launch onboarding walkthrough | High | ❌ Not started |
| 18 | [18-retention-smart-empty-states.md](18-retention-smart-empty-states.md) | User Retention — smart empty states with CTAs | High | ❌ Not started |
| 19 | [19-retention-rate-nudge.md](19-retention-rate-nudge.md) | User Retention — rate-change session nudge | Medium | ❌ Not started |
| 20 | [20-ux-modern-style.md](20-ux-modern-style.md) | UX Modern Style — visual refresh, micro-animations, haptics | Medium | ❌ Not started |
| 21 | [21-ux-usability.md](21-ux-usability.md) | UX Usability — input friction, accessibility, gestures | Medium | ❌ Not started |

---

## Execution Order (recommended)

1. **18** (Smart empty states) — lowest effort, highest immediate retention impact
2. **17** (Onboarding) — changes first-session conversion; needed before marketing push
3. **15** (Extra repayment) — core calculation feature, low surface area
4. **16** (Share) — zero new dependencies, native API call
5. **14** (Loan comparison) — most complex feature, ship after basics are solid
6. **20** (Modern style) — visual polish; do after features are stable
7. **21** (Usability) — accessibility + gesture refinements
8. **19** (Rate nudge) — engagement; lowest urgency
