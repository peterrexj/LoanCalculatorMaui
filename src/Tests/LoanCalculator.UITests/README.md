# UI tests — one suite, both platforms

Appium-driven UI tests that drive the real app on a simulator or emulator.

**Every test body is platform-agnostic.** Tests address the UI by MAUI `AutomationId`; the
driver layer resolves that to `accessibilityIdentifier` on iOS and to the view's `resource-id`
on Android. There are no platform conditionals in `Tests/` — if you need one, it belongs in
`Infrastructure/`.

These exist to catch what unit tests structurally cannot: a screen that no longer renders, a
binding that silently stopped updating, a theme key that fails to resolve at runtime, a tab
that crashes on re-entry. Those are the failures that show up as "the app is flaky".

- **What does each test cover, and what do I check when it fails?** → [TEST-CASES.md](TEST-CASES.md)
- **Something broken?** → [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- Current status: **iOS 25/25 pass. Android 23 pass, 2 skipped, 0 fail** — the two skips are
  theme selection, which cannot be driven there (see TEST-CASES TC-5.1/TC-5.2).

> **Before you debug an Android "app never renders" failure**, check
> `adb shell settings get global animator_duration_scale`. If it is `0`, MAUI animations never
> complete, the splash never hands off, and *nothing* works. That one setting cost a long
> debugging session here — see [TROUBLESHOOTING.md](TROUBLESHOOTING.md#android-stuck-on-the-native-splash-forever--check-the-animation-scales-first).

```
Tests/            the tests — no platform conditionals allowed here
Pages/            page objects, one per tab, plus Shell navigation and launch handling
Infrastructure/   config, Appium bootstrap, driver wrapper, failure artifacts
run-uitests.sh    the entry point: boots device, builds, installs, runs
package.json      pinned Appium + drivers (installed into ./.appium, never globally)
```

---

## Prerequisites

| Need | Why | Check |
|---|---|---|
| .NET 10 SDK | Matches `global.json` | `dotnet --version` |
| Node 18+ | Runs the Appium server | `node -v` |
| Xcode (iOS) | Provides the simulator and builds WebDriverAgent | `xcodebuild -version` |
| Android SDK (Android) | Provides `adb` + emulator | `$ANDROID_HOME` or `~/Library/Android/sdk` |
| JDK 17+ (Android) | .NET Android's manifest merger needs it | `java -version` |

The script finds the Android SDK and a suitable JDK the same way `run-android.ps1` does, so
if that script works for you, this one will too.

---

## First-time setup

```bash
cd src/Tests/LoanCalculator.UITests
npm install && npm run drivers
```

That installs a pinned Appium 3 plus the `uiautomator2` and `xcuitest` drivers into
`./.appium`. Nothing is installed globally. `package-lock.json` is committed so every machine
and CI runner gets an identical toolchain — if the suite passes here and fails there, it is
not a version difference.

Verify with `npm run drivers:list`.

---

## Running

```bash
./run-uitests.sh --ios                                  # full suite (~22 min)
./run-uitests.sh --ios --filter "TestCategory=Smoke"    # ~4 min — run this per change
./run-uitests.sh --android
```

The script boots the device, disables animations, builds the app, **installs it**, starts
Appium, runs `dotnet test`, then stops Appium — but only if it was the one that started it. A
server you launched by hand in another terminal is left running.

### Flags

| Flag | Effect |
|---|---|
| `--ios` / `--android` | Required. Which platform to run against |
| `--no-build` | Reuse the existing `.app`/`.apk`. Much faster; warns if your XAML is newer |
| `--filter <expr>` | Passed to `dotnet test --filter` (see below) |
| `--device "iPad Pro 13-inch (M5)"` | A different simulator |
| `--avd Medium_Phone_API_36.1` | A specific emulator |
| `--fresh` | Uninstall before installing, so the app starts with no saved data |
| `-h`, `--help` | Usage |

### Filters

```bash
--filter "TestCategory=Smoke"                       # Smoke | Calculation | Settings
--filter "FullyQualifiedName~LoanCalculationTests"  # one fixture
--filter "Name=AppLaunchesToTheLoanTab"             # one test
--filter "TestCategory=Smoke|TestCategory=Settings" # either
```

### Fast inner loop

The first iOS run builds WebDriverAgent, which takes several minutes. Keep a server running
in its own terminal to reuse it:

```bash
npm run start          # terminal 1, leave it

./run-uitests.sh --ios --no-build --filter "TestCategory=Smoke"   # terminal 2, repeatedly
```

With `--no-build` and a warm server, a single test is ~20–40 s, mostly app relaunch.

### Running from an IDE

Set `UITEST_PLATFORM=ios` (or `android`) in the environment, make sure the app is **already
built and installed**, and run the fixtures normally. Without `UITEST_PLATFORM` every test is
reported as ignored — see [Why the suite is opt-in](#why-the-suite-is-opt-in).

---

## What the suites cover

| Suite | Category | What it protects |
|---|---|---|
| `SmokeTests` (9) | `Smoke` | Every Shell tab and in-page tab renders; tabs survive being re-entered; Quick Input opens and closes |
| `LoanCalculationTests` (5) | `Calculation` | Deposit and asset changes move the loan amount by exactly the same amount; amount-in-words renders; values persist across a popup reopen |
| `BudgetTests` (4) | `Calculation` | Adding income/expense moves the monthly totals; net reconciles with income − expenses; yearly = monthly × 12 |
| `WhatIfTests` (4) | `Calculation` | Scenario cards unlock once a loan exists; the rate delta moves the projected repayment in the right direction |
| `SettingsTests` (3) | `Settings` | **All four themes render all four tabs**; the app relaunches cleanly into a persisted theme; appearance pickers exist |

`SmokeTests` is the one to run on every change — the cheapest possible answer to "did I break
a screen".

Two tests deserve specific mention, because they cover failure modes nothing else does:

**`EveryThemeRendersEveryTab`.** Theme resources are embedded XAML resolved by key at
runtime, so a key missing from one theme file fails only when that theme is selected *and* a
page using the key is drawn. The unit test `ThemeFiles_ShouldHave_EqualKeys` proves the key
*sets* match; this proves they actually *resolve*.

**`AppRelaunchesCleanlyIntoAPersistedTheme`.** A saved non-default theme is loaded from
embedded XAML during startup, before any page is drawn. If that load fails the app breaks on
launch — the worst possible failure, and invisible to every other test.

### Why the loan tests assert deltas, not amounts

The loan amount is derived from the **total** asset amount (asset value *plus* upfront costs)
minus the deposit, and upfront costs come from saved settings. An absolute expected figure
would only hold on a device whose upfront costs happen to be zero. The tests therefore assert
invariants — "adding 50,000 to the deposit reduces the loan by exactly 50,000" — which hold
regardless of saved state.

Asset and deposit are also **coupled**: clearing the asset field and retyping it re-derives
the deposit. Every test that changes one sets both.

---

## How it works

```
run-uitests.sh
  ├─ boots simulator/emulator, disables animations
  ├─ builds the app, then INSTALLS it (see below)
  └─ dotnet test
       └─ AppiumSetup           [SetUpFixture] — one server + one session for the whole run
            ├─ AppiumServer     attaches to a running server, or starts ./node_modules one
            └─ AppDriver        the only thing tests and pages talk to
                 └─ UITestBase  [SetUp] relaunches the app; [TearDown] dumps artifacts on failure
                      └─ Pages  ShellTabs, AppLaunch, LoanPage, BudgetPage, WhatIfPage, SettingsPage
```

### The run script installs the app, not Appium

The driver is given only a bundle id — deliberately no `app` capability. When Appium manages
installation, `noReset` takes precedence over `enforceAppInstall`, so it **skips installing
whenever the bundle id is already present** and the suite silently tests the previous build.
That failure presents as a broken locator, which is a genuinely miserable thing to debug.

Consequence: a bare `dotnet test` installs nothing. Use the script, or install by hand first.

### How an AutomationId is resolved

**The two platforms map `AutomationId` to genuinely different things**, so `AppDriver` uses a
different locator chain for each. This is the one place that asymmetry is allowed to exist.

| | iOS | Android |
|---|---|---|
| Maps to | `accessibilityIdentifier`, surfaced as `@name` | the view's **`resource-id`** |
| Reported as | `AssetEntry` | `com.pj.loan.afford.calc:id/AssetEntry` — **package-prefixed** |
| `contentDescription` | n/a | **not set at all** |

So on Android, `accessibility id` (which maps to `content-desc`) matches nothing, and an
un-prefixed `@resource-id` comparison matches nothing either. Both are easy mistakes that
present as "element not found" for every single element.

Per poll, in order — Android: prefixed `resource-id`, then XPath over prefixed/bare
`resource-id` and `content-desc`, then `accessibility id` as a safety net. iOS:
`accessibility id`, then XPath over `@name`/`@label`.

Elements are **never cached**; every call re-resolves. That costs a round trip and removes
`StaleElementReferenceException`, which is the main flake source when a MAUI page re-renders
between two actions.

Only **visible** elements are returned. See the flake rules below for why that matters.

### `AppDriver` API

| Find / read | |
|---|---|
| `Find(id)` | The visible element, or a descriptive failure |
| `Exists(id)` | Is it visible right now? |
| `FindByText(text)` | For chrome MAUI does not tag (Shell tabs) |
| `Text(id)` / `Number(id)` | Displayed text; `Number` strips symbols and separators |
| `PageSource` / `Screenshot()` | Diagnostics |

| Wait | |
|---|---|
| `WaitUntilVisible(id)` / `WaitUntilGone(id)` | Appearance / disappearance |
| `WaitForText(id, predicate, description)` | **Use this for any computed value** |
| `WaitUntilTextGone(text)` | Confirms a drop-down closed |
| `WaitUntil(condition, description)` | Anything else |

| Act | |
|---|---|
| `Tap(id)` / `TapText(text)` / `TapRepeatedly(id, n)` | `TapRepeatedly` drives −/+ steppers |
| `EnterText(id, text)` | Clears, types, dismisses keyboard, **verifies the read-back** |
| `ScrollTo(id)` / `SwipePage()` | Platform-agnostic W3C pointer swipes |
| `RestartApp()` | Terminate + relaunch |

---

## Adding a test

1. **Tag the UI.** Add `AutomationId="SomethingDescriptive"` to the XAML element. Convention
   is `<Page><Thing><Role>`: `BudgetIncomeSaveButton`, `WhatIfRateDeltaPlus`.
2. **Rebuild the app.** A new `AutomationId` does not exist until the app is rebuilt and
   reinstalled. Do not use `--no-build` on the first run after tagging.
3. **Expose it on the page object** in `Pages/`, not in the test. Tests should read as intent;
   selectors and waits belong in the page object.
4. **Assert on a settled value**, never an immediate read.

A worked example — the whole shape of a good test:

```csharp
// Pages/LoanPage.cs — selectors and waits live here
public const string AssetEntry = "AssetEntry";

public void OpenLoanDetails()
{
    App.Tap(LoanDetailsFab);
    App.WaitUntilVisible(AssetEntry, TimeSpan.FromSeconds(15));
}

// Tests/LoanCalculationTests.cs — intent only
[Test]
public void RaisingTheDepositReducesTheLoanByTheSameAmount()
{
    Loan.OpenLoanDetails();

    var low  = SetAssetAndDeposit("800000", "100000");
    var high = SetAssetAndDeposit("800000", "150000");

    Assert.That(high, Is.EqualTo(low - 50_000m).Within(1m),
        "Adding 50,000 to the deposit should reduce the loan by exactly 50,000.");
}
```

### Keeping tests non-flaky

These rules are what the framework is built around. Breaking them is how flake gets back in.

- **Never `Thread.Sleep`.** Use `WaitUntilVisible`, `WaitUntilGone`, `WaitForText` or
  `WaitUntil`. The app debounces saves by 600 ms and recalculates asynchronously, so a bare
  `Text(id)` straight after an action is a race.
- **"Present" is not "visible".** `SfTabView` keeps *every* tab's content in the element tree,
  so an element on an inactive tab is findable but not on screen. `Find`/`Exists` therefore
  require visibility. Anything checking "am I on this tab?" must use an element from *inside*
  that tab's content — never the tab header, which is visible whichever tab is selected.
  `OpenTab` rejects that mistake outright, because it silently makes a test pass.
- **An `SfComboBox`'s selected value cannot be read.** Syncfusion draws it rather than
  exposing an accessible text node; the control's own label reports state (`"Drop down button
  pressed"`) and its subtree holds only the down-button. Drive the selection, then assert on
  what it *changes* — never on what the combo displays.
- **Never cache an element.** `AppDriver` re-resolves on every call on purpose.
- **Assert deltas, not absolutes, for persisted data.** Budget and loan data survive between
  runs, so those tests measure the change an action causes.
- **Confirm navigation landed.** `ShellTabs.GoTo` waits for an anchor on the destination page,
  so a tap swallowed during a transition fails loudly instead of causing a confusing
  downstream error.
- **Give every assertion a message.** The failure text is the whole diagnostic when this runs
  in CI.

---

## Configuration

`run-uitests.sh` sets these. Override them directly for anything unusual.

| Variable | Default | Purpose |
|---|---|---|
| `UITEST_PLATFORM` | **none — required** | `ios` or `android`. Absent ⇒ every test is ignored |
| `UITEST_DEVICE` | `iPhone 17 Pro` / `Android Emulator` | Device name |
| `UITEST_UDID` | — | Pin an exact simulator UDID or emulator serial |
| `UITEST_AVD` | — | AVD name, so Appium can boot it if needed |
| `UITEST_APP` | derived from repo layout | Path to the `.app` or `.apk` (used by the script) |
| `UITEST_APPIUM_URL` | `http://127.0.0.1:4723` | Appium endpoint |
| `UITEST_AUTOSTART_APPIUM` | `true` | Start a server if none is listening |
| `UITEST_FRESH_INSTALL` | `false` | Recorded in the run log; `--fresh` does the uninstall |
| `UITEST_RESTART_PER_TEST` | `true` | Relaunch the app before each test |
| `UITEST_TIMEOUT_SECONDS` | `20` | Default element wait budget |
| `UITEST_LAUNCH_TIMEOUT_SECONDS` | `90` | How long to wait for the app to reach the Loan tab |
| `UITEST_ARTIFACTS` | `TestResults/uitest-artifacts` | Where failure dumps land |

Every run prints the resolved configuration first — check it before trusting a failure.

### Why the suite is opt-in

The tests need a booted device and an installed build, so `AppiumSetup` calls `Assert.Ignore`
unless `UITEST_PLATFORM` is set. That means a solution-wide `dotnet test` **skips** them
cleanly instead of hanging or failing. `run-uitests.sh` sets the variable for you.

---

## When a test fails

Every failure writes to `TestResults/uitest-artifacts/`:

| File | Use |
|---|---|
| `<Platform>-<Test>-<time>.png` | What the screen looked like |
| `<Platform>-<Test>-<time>.xml` | The **full element tree** |
| `appium-server.log` | The whole session's protocol traffic |

**Read the XML first.** It distinguishes "the element is genuinely gone" (a real regression)
from "the element is untagged or invisible" (a test that needs fixing) — and those look
identical in the failure message otherwise. Search it for the id the test wanted.

The driver's own failure messages already tell you which case you are in; they are listed in
[TROUBLESHOOTING.md](TROUBLESHOOTING.md).

---

## Known constraints

- **One device at a time.** A single driver session is shared; tests are not parallelised.
  Covering both platforms means two sequential runs.
- **The full suite is ~22 min**, single-threaded, dominated by the per-test app relaunch. Use
  `--filter` while developing. Setting `UITEST_RESTART_PER_TEST=false` is much faster but lets
  state leak between tests — acceptable for a quick local loop, never for CI.
- **iOS first run is slow.** Appium builds WebDriverAgent once. Later runs reuse it as long as
  the simulator and driver version are unchanged.
- **Android has less usable height than iOS for the same layout.** Popup content and long
  pages that fit on iOS often do not there. `Find` compensates by scrolling a present-but-
  hidden element into view automatically, and `EnterText` closes the keyboard before locating
  a field — but a new page object still needs its anchors chosen with this in mind.
- **Android runs slower**, roughly 2–3× per test versus the iOS simulator.
- **Shell tabs are matched by title**, since MAUI puts no `AutomationId` on them. `ShellTabs`
  disambiguates against same-worded page labels by taking the bottom-most match on screen. If
  the titles in `AppShell.xaml` change, update `ShellTabs.TitleOf`.
- **`SharedServiceCore.TESTING_PREMIUM_OVERRIDE` must stay enabled** for these tests to reach
  premium-gated screens. `CLAUDE.md` notes it must be disabled before a store release —
  doing so will fail premium-gated tests until they are taught to handle the trial state.
- **Charts and drawn Syncfusion content carry no readable value.** Anchor on a neighbouring
  label or button instead.
- **`SfComboBox` cannot be opened on Android** through UiAutomator2, so theme/currency
  selection is iOS-only; the two theme tests report as *ignored* on Android with the reason.
  See TROUBLESHOOTING.
