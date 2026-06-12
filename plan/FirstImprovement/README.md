# First Improvement — Plan Index

**Goal:** Fix the highest-impact production issues causing sluggishness, crashes, incorrect data behavior, and visual inconsistencies — without touching the IAP/premium flow and without breaking any existing behavior.

**Approach:** Safe incremental. Every change is self-contained. No architecture rewrites. The IAP/trial flow is untouched.

---

## Status Overview

| # | File | Area | Priority | Status |
|---|------|------|----------|--------|
| 01 | [01-performance-blocking-calls.md](01-performance-blocking-calls.md) | Performance — blocking `.Wait()` calls | **Critical** | ✅ Done (1 minor gap: sync `IsPremiumUser` overload) |
| 02 | [02-performance-save-debounce.md](02-performance-save-debounce.md) | Performance — SaveData on every keystroke | **Critical** | ✅ Done |
| 03 | [03-performance-chart-collections.md](03-performance-chart-collections.md) | Performance — chart ObservableCollections rebuilt every get | High | ✅ Done |
| 04 | [04-performance-property-notification-batching.md](04-performance-property-notification-batching.md) | Performance — 20+ OnPropertyChanged fired per keystroke | High | ✅ Done |
| 05 | [05-crash-async-void-thread-safety.md](05-crash-async-void-thread-safety.md) | Crashes — async void + OnPropertyChanged from background threads | **Critical** | ✅ Done |
| 06 | [06-crash-maccatalyst-missing-services.md](06-crash-maccatalyst-missing-services.md) | Crashes — MacCatalyst missing DI registrations | High | ✅ Done |
| 07 | [07-ux-splash-and-load-feedback.md](07-ux-splash-and-load-feedback.md) | UX — splash timing, load state feedback | High | ✅ Done |
| 08 | [08-ux-tab-load-redundancy.md](08-ux-tab-load-redundancy.md) | UX — redundant OnAppearing data reloads | High | ✅ Done |
| 09 | [09-ux-design-improvements.md](09-ux-design-improvements.md) | UX/Design — layout consistency, OnIdiom verbosity, typography | Medium | ⚠️ Partial — tokens added; page titles + inline styles remain |
| 10 | [10-ux-engagement-features.md](10-ux-engagement-features.md) | UX — features to increase engagement and return visits | Medium | ❌ Not started |
| 11 | [11-data-integrity.md](11-data-integrity.md) | Data — incorrect save/load ordering, stale cross-tab data | **Critical** | ✅ Done |
| 12 | [12-dead-code-cleanup.md](12-dead-code-cleanup.md) | Cleanup — dead files, commented code | Low | ⚠️ Partial |
| 13 | [13-secrets-and-security.md](13-secrets-and-security.md) | Security — hardcoded credentials and keys | Medium | ❌ Not done — passwords still in `.csproj` |
| — | *(unplanned)* | Bug — Income/Expense/Loan edit popup: amount not saving, frequency not showing | — | ✅ Done today |
| — | *(unplanned)* | Crash — Income page crash on open (null `IncomeExpenseEntry`) | — | ✅ Done today |
| — | *(unplanned)* | UX — `AlertService` using deprecated `MainPage` | — | ✅ Done today |

---

## Next Actions

### Remaining open items (smallest effort first)

1. **13 — Move Android signing password to `local.props`** — required before any new contributor or public repo access.
2. **09 — Finish view migration**: remove remaining `OnIdiom` inline usages; update `ContentPage Title` values; audit inline `FontSize=` / `TextColor=` overrides.
3. **01 — `IsPremiumUser()` sync overload** — convert to fully async before releasing with `TESTING_PREMIUM_OVERRIDE = false`.
4. **10 / Second Improvement — Engagement features** — start with 18 (Smart empty states) and 16 (Share loan summary).
5. **12 — Verify dead file deletions.**

Each file is self-contained: it describes the problem, the exact files and lines affected, the proposed fix, and how to verify it.
