# UI test troubleshooting

Organised by the symptom you actually see. Every entry below is a failure that has really
happened in this repo, with the diagnosis that resolved it.

Per-test objectives, steps and failure triage live in [TEST-CASES.md](TEST-CASES.md).

**Start here, always:**

1. Read the **failure message**. The driver distinguishes "missing", "present but invisible"
   and "the driver itself erred" — that alone usually names the cause.
2. Open the **`.xml`** in `TestResults/uitest-artifacts/` and search for the id the test
   wanted. This is the single highest-value diagnostic and people skip it.
3. Check the **resolved configuration** printed at the top of the run. Wrong device or wrong
   app path explains a lot of "impossible" failures.

**Jump to:**
[Element not found](#element-not-found) ·
[Stale build](#the-suite-is-testing-the-wrong-build) ·
[App never starts](#the-app-never-becomes-ready) ·
[Android accessibility](#android-every-lookup-times-out) ·
[Text entry](#text-entry-problems) ·
[Combo boxes](#combo-box-and-picker-problems) ·
[Tabs](#tab-navigation-problems) ·
[Flaky / inconsistent](#tests-pass-sometimes) ·
[Appium & devices](#appium-server-and-device-problems) ·
[All tests ignored](#every-test-is-reported-as-ignored)

---

## Element not found

The driver produces three different messages here. **Which one you get is the diagnosis.**

### `No visible element with AutomationId 'X' appeared within 20s. If the control exists, it may be missing an AutomationId in XAML.`

Nothing matched at all — not even invisibly.

| Cause | Fix |
|---|---|
| **The app was not rebuilt after you added the `AutomationId`** — by far the most common | Re-run without `--no-build`. The script warns when XAML is newer than the installed app |
| The id is misspelled, or differs in case | Search the `.xml` artifact for a near-match |
| The attribute is on an element MAUI does not surface | Move it to a `Label`, `Button` or `Entry` nearby |
| You tagged a `Span` | `Span` does not support `AutomationId`. Tag the parent `Label` |
| You tagged a drawn Syncfusion control (chart) | Anchor on a neighbouring label or button |

### `... It IS in the element tree but is not visible — it is probably on an inactive tab or scrolled off-screen.`

The element exists; you just cannot interact with it yet. This is normal and expected.

| Cause | Fix |
|---|---|
| It is below the fold | `App.ScrollTo(id)` first |
| It is inside a collapsed `SfExpander` | Tap the expander header first |
| It is on an inactive `SfTabView` tab | Open that tab first — `SfTabView` keeps all tabs in the tree |
| A popup is covering it | Dismiss the popup; check the screenshot |

Real example: `LoanInsightsChart` is inside a collapsed expander partway down the Insights
tab, so it is always in the tree and never visible on arrival. The fix was to anchor that tab
on `LoanInsightsExportButton` at the top instead.

### `... Every lookup failed on the driver side, so the element may well exist. Last driver error: …`

The lookups never completed. This is not your locator — it is the session. See
[Android: every lookup times out](#android-every-lookup-times-out) and
[Appium server and device problems](#appium-server-and-device-problems).

---

## The suite is testing the wrong build

**Symptom:** an `AutomationId` you definitely added is "not found"; or a bug you definitely
fixed still fails; or behaviour makes no sense against the current source.

This has bitten twice here, from two different directions.

### Appium skipped the install

`noReset` takes precedence over `enforceAppInstall`, so when Appium owns installation it
**silently skips it whenever the bundle id is already present** — and tests the previous
build.

Already fixed: the driver is given only a bundle id, no `app` capability, and
`run-uitests.sh` does the install itself. If you bypass the script, install by hand first:

```bash
xcrun simctl install <udid> src/LoanCalculator/bin/Debug/net10.0-ios26.5/iossimulator-arm64/LoanCalculatorMaui.app
adb -s <serial> install -r src/LoanCalculator/bin/Debug/net10.0-android36.0/*-Signed.apk
```

### A scroll types digits into a field instead of scrolling

If a test's values come out wrong and a screenshot shows the numeric keypad up, check whether a
swipe is landing on it. `SwipePage` used to drag from `0.75 × height` to `0.25 × height`; on an
874pt screen that starts at **y 655**, and the iOS keyboard occupies **583–816**. The gesture
therefore pressed keypad keys — a drag down the middle column reads as `2` then `5` — corrupting
whichever field had focus while the target never scrolled into view.

This is not "a keyboard was open at the time": `forceSimulatorSoftwareKeyboardPresence` keeps the
software keyboard on screen for the **whole session**, so the bottom ~27% is always covered, even
with nothing focused. `DismissKeyboard()` cannot help — that capability puts it straight back.

`SwipePage` now calls `KeyboardTop()` and keeps the whole drag above the keyboard's top edge. If
you add another gesture helper, do the same; do not assume the lower screen is touchable.

### "It IS in the element tree but is not visible" — usually the keyboard

An element reported as present-but-invisible is **obscured, not missing**. On a full-screen page the
software keyboard owns roughly the lower third, so any control there is invisible to XCUITest after
an `EnterText`. Confirm it from the captured artifact rather than guessing:

```bash
f=TestResults/uitest-artifacts/iOS-<TestName>-<time>.xml
grep -o 'type="XCUIElementTypeKeyboard"[^>]*' $f        # its y and height
grep -o '<[^>]*name="YourAutomationId"[^>]*>' $f        # the element's y and height
```

If the element's `y` falls inside the keyboard's band, that is the whole story. Fix with
`App.DismissKeyboard()` (plus `ScrollTo` if it is in scrollable content).

This bit the Quick Setup wizard: `WizardCalculateButton` at `y=776` under a keyboard spanning
`y 583–816` on an iPhone 16 Pro. It only appeared when the wizard moved from a 470pt centred
`SfPopup` — whose buttons incidentally sat above the keyboard — to a full-screen page with a
bottom-docked button. **A full-screen MAUI page cannot dock a button above the keyboard**, so put
primary actions at the end of the scrollable content instead. Note the Quick Input page is immune
because its Apply button is in the header row, at the top of the screen.

### You cannot type into a field on the iOS simulator

Check the simulator's keyboard before anything else:

```bash
defaults read com.apple.iphonesimulator ConnectHardwareKeyboard   # 0 = your Mac keyboard is not connected
```

If it is `0`, focus Simulator and press **⇧⌘K**. Nothing you type reaches the app until you do.

Note that **the UI suite cannot detect this** — Appium's `EnterText` injects text through XCUITest
without a keyboard, so every test passes while a human cannot type a character. "The tests are
green" is not evidence that manual input works. See CLAUDE.md § *"iOS: you cannot type into any
field"* for the full list of misleading symptoms, and why Android appearing to work is the most
deceptive one.

### A test still fails after you fixed the bug it reports

**Check the build timestamp before re-diagnosing anything.** `--no-build` tests whatever was
built last, so an app fix that is not in that build fails as *the very bug it fixes* — and the
failure message, being written to describe that bug, reads as a perfectly convincing confirmation
that your fix did not work. This has cost real time here twice.

```bash
# Is the thing you are testing older than the fix you just made?
stat -f '%Sm %N' -t '%F %T' src/LoanCalculator/bin/Debug/net10.0-android36.0/*-Signed.apk
stat -f '%Sm %N' -t '%F %T' src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs
```

If the APK is older, the run proved nothing. Rebuild and re-run before touching the diagnosis —
a fix is only *untested*, not disproven.

The script warns about this for both XAML and C# (app `View`/`Controls` plus all of
`LoanCalculator.Core`, where the ViewModels and so most behaviour fixes live).

### You used `--no-build` after editing XAML

The script now warns:

```
WARNING: --no-build, but these sources are NEWER than the app you are about to test:
```

**Confirm what is actually installed** rather than guessing — the decisive check:

```bash
# iOS
strings "$(xcrun simctl get_app_container <udid> com.pj.loan.afford.calc)/LoanCalculatorMaui.dll" \
  | grep -c YourNewAutomationId          # 0 means the installed app predates your edit

# compare against what you just built
strings src/LoanCalculator/bin/Debug/net10.0-ios26.5/iossimulator-arm64/LoanCalculatorMaui.app/LoanCalculatorMaui.dll \
  | grep -c YourNewAutomationId
```

---

## The app never becomes ready

```
The app did not reach the Loan tab within 90s. It may have crashed on launch,
or stalled on the splash screen — check the captured page source artifact.
```

`AppLaunch.WaitUntilReady` waits for either the launch disclaimer or the Loan tab's `FabWizard`
(the page's only FAB since Quick Setup and Quick Input merged). Reaching neither means the app
is not up.

**Look at the screenshot artifact first** — it immediately distinguishes these:

| Screenshot shows | Meaning |
|---|---|
| The MAUI splash (logo, title, three dots) | Splash never handed off — see the subsection below |
| The native splash (plain centred icon) | MAUI never rendered at all — startup hang or crash |
| The app, looking normal | The *anchor* is wrong, not the app. Check `ShellTabs.AnchorOf` |
| A disclaimer or popup | It has no `AutomationId`, so it could not be dismissed |

Then get the real reason:

```bash
# iOS — crash logs
xcrun simctl spawn <udid> log show --last 5m --predicate 'process == "LoanCalculatorMaui"'

# Android
adb logcat -d | grep -iE "loan.afford|DOTNET|AndroidRuntime|FATAL" | grep -v Sentry
adb shell dumpsys window | grep mCurrentFocus
```

Genuinely slow startup (a cold Debug build on a slow emulator) is a real possibility — raise
`UITEST_LAUNCH_TIMEOUT_SECONDS` before concluding it is hung.

### Stuck on the MAUI splash — on iOS too, not just Android

Seen 2026-10-06 on iOS, in `SettingsPageRendersItsAppearancePickers`, after the theme tests had
cycled `terminate_app`/`activate_app` a dozen times. **Check the screenshot's opacity**: if the
splash is washed out / nearly white, the final `this.FadeTo(0, 280)` *rendered* but never
returned, so `NavigateToShell()` was never reached. A splash at full opacity instead points at
`PreWarmAsync`.

Either way the cause is an unbounded `await` on a MAUI animation whose completion is never
signalled — the same mechanism as the Android `animator_duration_scale=0` entry below, which is
why that entry should not be read as Android-only.

**The startup path no longer awaits any animation at all** — the entrance animation and exit fade
were deleted, and the page is composed at its resting state in XAML. So a splash that lingers now
means `PreWarmAsync` is slow or stuck, not an animation. The splash should last
`max(PreWarmAsync, 700ms)`, roughly 0.7–1 s.

Two numbers worth recognising:

| Dwell | Meaning |
|---|---|
| ~700 ms | The floor. Normal — the pre-warm finished faster than that |
| **exactly ~6 s** | `SplashDeadline` firing, i.e. `PreWarmAsync` stalled, or an `await` on an animation crept back onto the startup path |
| **forever, at full opacity** | The deadline itself is not running. See below |

**If the splash hangs at full opacity and the 6 s deadline never fires**, the UI thread is starved
— the deadline is awaited with the UI `SynchronizationContext` captured, so its continuation cannot
run on a saturated thread. Check `AnimateDotsAsync` first: its awaits must be **bare**. Racing them
against a timeout makes the loop abandon each in-flight animation and start another every ~340 ms
on a device whose animator is not ticking, accumulating thousands of registered animations (~9,000
over an 18-minute run) and starving the thread the deadline depends on. This was shipped briefly
and cost a full 37-minute suite run; see CLAUDE.md, "Third wrong shape".

Note the trigger is usually environmental — a simulator that has been up for many hours, or one
whose WebDriverAgent has crashed, stops completing animations, which is the iOS analogue of the
Android `animator_duration_scale=0` entry below. **A healthy app must survive that**, so treat a
hung splash as an app defect even when a stale simulator provoked it. Reboot the simulator before
re-running, or the next run reproduces the trigger rather than testing the fix.

Historic note, because the symptom is misleading: when the exit fade still existed, this presented
as a *washed-out, nearly white* splash. That was the dark brand gradient dissolved to near-zero
opacity, not a light-themed screen.

What this looks like in the Appium log is worth recognising, because it reads like a dead
simulator: **a single WebDriver request hanging for minutes with no log traffic at all** (240 s
here), then the follow-up `PageSource` failing with `XCTPerformOnMainRunLoop work timed out after
60.0s` and a stale-element error for the application node. Find the stalls rather than reading
forward through the log:

```bash
sed -E 's/\x1b\[[0-9;]*m//g' TestResults/uitest-artifacts/appium-server.log \
  | grep -oE '^2026-[0-9-]+ [0-9]+:[0-9]+:[0-9]+' \
  | python3 -c "
import sys, datetime
prev = None
for l in sys.stdin:
    t = datetime.datetime.strptime(l.strip(), '%Y-%m-%d %H:%M:%S')
    if prev and (t - prev).total_seconds() > 40:
        print('%6.0fs stall  %s -> %s' % ((t - prev).total_seconds(), prev.time(), t.time()))
    prev = t
"
```

Note the log clock may not match the artifact filenames' wall clock (it was offset 11 h here);
line up the two by ordering, not by absolute time.

### Android: stuck on the native splash forever — check the animation scales first

**This is the single highest-value entry in this document.** If the Android app never renders,
check this before anything else:

```bash
adb shell settings get global animator_duration_scale    # must NOT be 0
adb shell settings put global animator_duration_scale 1.0
```

**Why.** MAUI's animation framework (`FadeTo`, `ScaleTo`, `TranslateTo`) is driven by the
platform animator. At scale `0` those animations **never signal completion**, so
`SplashPage.RunSplashAsync` never finishes awaiting them, `NavigateToShell()` never runs, and
`MainPage` is never handed to the Shell. The app never renders a frame.

The signature, all of which this produced:

- The Android **native** splash (plain centred icon) — never the MAUI splash with the dots.
- `adb shell dumpsys window | grep mCurrentFocus` → `null`, while `mFocusedApp` *is* MainActivity.
- Main thread logging `Explicit concurrent mark compact GC freed ~64KB` every ~1.6 s forever
  — that is `AnimateDotsAsync`'s loop spinning, each `FadeTo` returning instantly but the
  sequence never completing.
- Every Appium lookup failing with `Timed out waiting for the root AccessibilityNodeInfo`,
  because a window with no focus exposes no accessibility tree.
- All managed startup code completing normally, which is what makes it so confusing.

**It is easy to do to yourself, and it persists.** These are *global* settings: they survive
app reinstall, uninstall, and emulator reboot. So the app looks permanently broken long after
the run that set them. Two ways in:

1. `adb shell settings put global animator_duration_scale 0`, the usual "make tests less
   flaky" incantation. `run-uitests.sh` now zeroes only `window_animation_scale` and
   `transition_animation_scale` — which are safe — and restores them on exit via a trap.
2. **Appium's `appium:disableWindowAnimation=true`**, which zeroes *all three* scales. It is
   deliberately not used here; see the comment in `AppDriver.BuildAndroidOptions` before you
   add it back.

### Android build fails inside `MonoAndroidHelper`

```
error XAWAS7023: System.IO.DirectoryNotFoundException: Could not find a part of the path
'…/obj/Debug/net10.0-android36.0/android/assets/x86_64/he/Microsoft.Maui.Controls.resources.dll'
```

Stale incremental state referencing an ABI that is no longer being built. Clear the Android
output and rebuild:

```bash
rm -rf src/LoanCalculator/obj/Debug/net10.0-android36.0 \
       src/LoanCalculator/bin/Debug/net10.0-android36.0
```

Both are gitignored build artifacts, so this is safe.

---

## Android: every lookup times out

```
Got response with status 500: … "Timed out after 10060ms waiting for the root AccessibilityNodeInfo"
```

UiAutomator2 cannot obtain the accessibility tree. **This is almost never a locator problem.**

| Cause | Check | Fix |
|---|---|---|
| **No focused window** — usually the app never finished starting | `adb shell dumpsys window \| grep mCurrentFocus` → `null` | Fix the app startup; see above |
| Screen off or locked | `adb shell dumpsys power \| grep mWakefulness` | `adb shell input keyevent 82` |
| The window never goes idle (continuous animation) | — | Already handled: `settings[waitForIdleTimeout]=100` is set in `BuildAndroidOptions` |
| UiAutomator2 server wedged | `adb shell pm list packages \| grep uiautomator2` | `adb uninstall io.appium.uiautomator2.server` (and `.test`), then re-run |

Note the driver retries these for the whole timeout rather than failing instantly — so a
hard-failing session looks like a slow hang. The `Last driver error:` line in the final
message is what tells you it was a server error, not a missing element.

---

## Text entry problems

### `'X' still reads '…' after typing '700000' twice.`

`EnterText` types, dismisses the keyboard, reads back and retries once before failing.

| Cause | Fix |
|---|---|
| A hardware keyboard is capturing keys | Already handled: `connectHardwareKeyboard=false`. In the Simulator UI, **I/O → Keyboard → Connect Hardware Keyboard** must be off |
| The field reformats input in a way the digit comparison rejects | Check the read-back in the message; the comparison is digits-only |
| The keystrokes went to a different field | Check the screenshot |

### The value was typed but nothing recalculated

`SfNumericEntry.ValueChanged` fires on Enter, the spin buttons, or **focus change only** —
never per keystroke. Inside an `SfPopup`, tapping Save before unfocusing means the value is
never written back (this is in `CLAUDE.md` too).

The add/edit forms already avoid this by using a plain `Entry` bound to a string property.
`EnterText` also dismisses the keyboard, which forces the focus change. If you add a new
`SfNumericEntry`, expect to need an explicit blur.

### A field change corrupted a different field

Not necessarily a test bug — it can be real app behaviour, and here it was. Clearing the
asset field to empty and retyping makes the **deposit** become 100% of the new asset value,
because the intermediate empty state resets the deposit percentage.

The lesson for tests: coupled fields must be set **together**, in a defined order, after any
change to either. `LoanCalculationTests.SetAssetAndDeposit` does exactly that.

### `Scrolled 12 times without bringing 'X' into view on iOS.`

**First question: did it scroll at all?** Open the failure's `.xml` artifact and look at the y of
an element near the *top* of the page. If that element is still at its unscrolled position, the
swipes did nothing — and the message is misleading, because it describes a target that is
out of reach rather than a gesture that never moved.

Zero scroll on iOS means the drag landed on the keyboard. **`XCUIElementTypeKeyboard` is not the
top of the keyboard assembly**, which is what made this expensive: the numeric pad's input
accessory (the "Done" toolbar) is a **sibling above** it, and the keyboard's `inputView` container
spans the gap between the two. Measured on iPhone 17 Pro, keyboard forced present:

| element | y range |
|---|---|
| `ScrollView` (app content) | 116 … 840 |
| **`Toolbar`** — input accessory | **522 … 566** |
| `Other` — `inputView` container | 566 … 874 |
| `XCUIElementTypeKeyboard` | 583 … 816 |

`KeyboardTop()` used to return 583, so `SwipePage` started its drag at `583-12 = 571` — inside
`inputView`. The keyboard swallowed every drag, `ScrollTo` spent all 12 swipes with the content at
offset 0, and `WizardCalculateButton` sat at y=643 the whole time. It now also considers any
displayed `Toolbar` whose bottom is within 80pt above the keyboard, giving 522 and a drag that
starts clear of the whole stack.

If this returns, dump the keyboard assembly from a failure artifact before touching `ScrollTo`:

```bash
python3 - <<'PY'
import xml.etree.ElementTree as ET
r = ET.parse('TestResults/uitest-artifacts/<artifact>.xml').getroot()
for e in r.iter():
    t = e.get('type', '').replace('XCUIElementType', '')
    if t in ('Keyboard', 'Toolbar', 'ScrollView') and e.get('height'):
        print('%-11s y=%-5s bottom=%s' % (t, e.get('y'), int(e.get('y')) + int(e.get('height'))))
PY
```

Note this is **not** the same failure as a button genuinely below the keyboard. `LoanDetailsPage`
carries `Padding="12,4,12,300"` precisely so its last control clears the keyboard at full scroll,
and a human can always scroll by dragging the content above the accessory. The bug was only ever
in where the synthetic drag started.

---

## Combo box and picker problems

### `Timed out … waiting for 'SettingsThemeCombo' to show the selection 'Dark'`

**An `SfComboBox`'s selected value cannot be read at all.** Syncfusion *draws* it instead of
exposing an accessible text node. The element tree for a collapsed combo is just:

```xml
<XCUIElementTypeOther name="SettingsThemeCombo" label="Drop down button pressed">
  <XCUIElementTypeOther name="SettingsThemeCombo_DownButtonView"/>
</XCUIElementTypeOther>
```

Note the label reports control *state*, not content, and there is no text child.

So: drive the selection, then assert on what it **changes**. Never on what the combo
displays. `SelectFromCombo` confirms completion by waiting for the drop-down to close
(`WaitUntilTextGone`) — the option rows *are* real accessible nodes, which is why tapping
them works.

This is the same family as the `SyncfusionIosTouchFix` already in the repo: Syncfusion's iOS
drawing overlays both swallow taps and hide content from accessibility.

### Android: tapping an SfComboBox does not open its drop-down

On Android the list never enters the element tree — the page source after the tap shows the
combo still collapsed (`EditText text='Dark'`) and no list container. Tapping the container and
tapping the `<id>_DownButtonView` child both fail. This is the same family as the
`SyncfusionIosTouchFix` overlay problem already in this repo, on the other platform.

Consequence: **theme and currency selection cannot be driven on Android.** The two theme tests
call `SkipOnAndroid(...)` and are reported as *ignored* there, with the reason in the run
output, rather than silently passing. They still run on iOS, which is where that coverage lives.

If you want to fix it properly, the likely route is the same as the iOS fix: walk the native
tree for the combo and neutralise whatever overlay is eating the touch, or drive the selection
through the view model instead of the UI.

### Android: reading an SfComboBox's value

Unlike iOS, Android *does* expose it — but in an `android.widget.EditText` child, not a
`TextView` (SfComboBox is editable underneath). `PageBase.ComboText` searches both.

### The drop-down opened but the option was not tappable

Options are matched by visible text, since rows come from an `ItemTemplate` with no
`AutomationId`. If the list is long the row may need scrolling, or the text may not match
exactly — check the `.xml` for the real string (watch for trailing spaces and symbols, e.g.
currency rows are `"Euro (€)"`, not `"EUR"`).

---

## Tab navigation problems

### A tab test passes but clearly did not do anything

`OpenTab` now throws if the content anchor equals the tab id:

```
The content anchor for 'BudgetTabProjection' must be an element inside the tab's
content, not the tab header itself.
```

A tab **header** stays visible whichever tab is selected, so using it as its own "did it
load?" anchor makes `OpenTab` return without ever tapping. Three tests passed this way before
the guard existed. Always anchor on something *inside* the tab's content.

### `Could not find the 'What If' tab in the Shell tab bar`

MAUI puts no `AutomationId` on Shell tabs, so `ShellTabs` matches them by title and
disambiguates against same-worded page labels by taking the **bottom-most** match on screen.

| Cause | Fix |
|---|---|
| A title changed in `AppShell.xaml` | Update `ShellTabs.TitleOf` |
| A tab was commented out of `AppShell.xaml` | Update `AppTab` and `TitleOf` |
| A modal popup is covering the tab bar | Dismiss it first |

### Navigation reports success but the wrong page is showing

`ShellTabs.GoTo` waits for `AnchorOf(tab)`. If that anchor also exists on another page, the
check passes wrongly. Anchors must be unique to their page.

---

## Tests pass sometimes

Genuine flake. In rough order of likelihood:

| Cause | Fix |
|---|---|
| Asserting an absolute value against persisted data | Assert the delta. Budget and loan data survive between runs |
| Reading a value immediately after an action | `WaitForText(...)`. Saves are debounced 600 ms |
| A `Thread.Sleep` crept in | Replace with an explicit wait |
| Test order dependence | `UITEST_RESTART_PER_TEST` defaults to `true`; do not disable it in CI |
| Leftover data from previous runs skewing totals | `--fresh` |
| Animations | `reduceMotion` on iOS. On Android the script zeroes **two** scales (`window_animation_scale`, `transition_animation_scale`) and restores them on exit. It never sets `animator_duration_scale` — that one hangs the app — but it does check for a stray `0` and repair it. `disableWindowAnimation` is deliberately **not** used, because it zeroes all three |
| A cached element going stale | `AppDriver` never caches — but if you held an `AppiumElement` yourself, stop |

If a test is flaky only in a full run but solid alone, suspect leaked state, then the tab
anchor.

---

## A failing test takes 20 minutes instead of 90 seconds

The per-command WebDriverAgent budget. Appium's `wdaConnectionTimeout` defaults to **240 s** and is
charged *per proxied command*, so a sick WDA bills it over and over: in one 41-test run three
failures hit five or six timeouts each and consumed **65 of the run's 100 minutes**.

It is now capped at 60 s via `UITEST_WDA_CONNECTION_TIMEOUT_SECONDS`. Signatures that this is what
you are looking at:

- `Could not proxy command to the remote server. Original error: timeout of 240000ms exceeded`
- artifact **PNGs write but XMLs do not** — screenshots use a lighter WDA endpoint than the
  accessibility-tree snapshot
- a `WebDriverAgentLib` stack trace in `appium-server.log`, often inside `FBXPath snapshotWith…`
- repeated stalls of *exactly* the timeout value, visible with the stall-finding command above

Raise it only if a first-ever run against a brand-new simulator reports connection timeouts rather
than a build failure — building WDA is budgeted by `wdaLaunchTimeout` (240 s), not by this.

Note the failure that results is reported as `WaitUntilReady` timing out, whose message blames a
crash or the splash. **Check a PNG artifact before believing it**: if the app looks healthy, WDA
was blind, not the app.

## Appium server and device problems

### `No Appium server at http://127.0.0.1:4723 and UITEST_AUTOSTART_APPIUM is off`

```bash
cd src/Tests/LoanCalculator.UITests && npm run start
```

### Every Android test fails in `OneTimeSetUp` with `Neither ANDROID_HOME nor ANDROID_SDK_ROOT ... was exported`

The Appium server cannot see the Android SDK, so no session can start — and because it fails in
`OneTimeSetUp`, NUnit repeats the identical stack trace under **every** test. 39 failures, one cause.

Look for this line earlier in the run:

```
[appium] attaching to existing server at http://127.0.0.1:4723/
```

That is the tell. A server **you** started by hand does not inherit the `ANDROID_HOME` that
`run-uitests.sh` exports for servers it starts itself — which is why an auto-started run works and an
attached one does not. UiAutomator2 needs it to locate `adb`.

Fix either way:

```bash
# restart your long-lived server — `npm run start` now exports ANDROID_HOME itself
npm run start

# or stop it and let the script start its own
./run-uitests.sh --android
```

The script now prints a NOTE whenever it is about to attach to an already-running server on Android,
since another process's environment cannot be inspected from outside.

### `Appium drivers are not installed at ./.appium`

```bash
npm install && npm run drivers
```

### `The Appium server exited with code N during startup`

Read `TestResults/uitest-artifacts/appium-server.log`. Usually the port is taken:

```bash
lsof -i :4723            # find it
UITEST_APPIUM_URL=http://127.0.0.1:4725 ./run-uitests.sh --ios   # or just use another port
```

### iOS: WebDriverAgent fails or takes forever

The first run builds WDA — several minutes, and it is not hung. If it *fails*:

```bash
xcodebuild -version                                    # Xcode present and licensed?
sudo xcodebuild -license accept
rm -rf ~/Library/Developer/Xcode/DerivedData/WebDriverAgent-*
```

Keeping `npm run start` alive across runs avoids repeating this.

### `ERROR: no simulator matching 'iPhone 17 Pro'`

```bash
xcrun simctl list devices available | grep -E "iPhone|iPad"
./run-uitests.sh --ios --device "iPhone 17 Pro"
```

### Android: `no AVDs found` / build fails on the manifest merger

The manifest merger needs a JDK 17+. The script probes the same locations as
`run-android.ps1`; force one with `JAVA_HOME`. For AVDs, create one in Android Studio's Device
Manager, or pass `--avd <name>`.

### Every test fails instantly with `OneTimeSetUp: … /session … response ended prematurely`

Two runs are competing for the same Appium server on port 4723 — the first one's teardown kills the
server the second is using, and every test fails in seconds without touching the app. Wait for one
run to finish, or give the second its own port:

```bash
UITEST_APPIUM_URL=http://127.0.0.1:4725 ./run-uitests.sh --ios
```

The tell is the duration: a whole suite "failing" in under 30 s never reached the device.

### The app shows "An unexpected error occurred" and then everything fails

The driver detects this alert and reports its text as the failure, because it is modal and blocks
every subsequent lookup — so the *first* failing test names the real cause and the rest are
collateral. Fix the app exception, do not chase the later failures.

Seen for real: `FormatException: Unrecognized Guid format` after an `AutomationId` was changed on a
Budget row button. **`AutomationId` used to be a data channel in `BudgetView.xaml.cs`** — the edit
and delete handlers did `Guid.Parse(btn.AutomationId)`, so retagging those buttons broke deletion.
They now take the row from `btn.BindingContext`, leaving `AutomationId` free for accessibility and
tests. The same pattern still exists in `LoanView.xaml.cs`, `IncomeView.xaml.cs` and
`ExpenseView.xaml.cs` — **do not retag row buttons in those views** without changing the handler
first.

### Android: `am start-activity … timed out after 20000ms` and no test runs

```
Cannot start the 'com.pj.loan.afford.calc' application … Error executing adbExec …
'adb … shell am start-activity -W -n …' timed out after 20000ms
```

Appium's `adbExecTimeout` defaults to **20 s**, and this app's cold start was measured at **23.1 s**
in Debug on an unloaded machine (`am start -W` TotalTime) — XAML inflation of the Syncfusion-heavy
pages dominates it. So a cold launch exceeds the default and the session dies in `OneTimeSetUp`,
failing every test before any of them run. Warm starts squeak under the limit, which is why this
appears intermittent.

Already raised in `BuildAndroidOptions` (`adbExecTimeout` 180 s, `appWaitDuration` 120 s). If you
still see it, the emulator or host is in trouble rather than the app being slow:

```bash
uptime                                   # load average — anything near your core count is trouble
adb shell dumpsys window | grep mCurrentFocus   # "Application Not Responding" ⇒ emulator is wedged
```

A wedged emulator does not recover on its own — kill and restart it. Spotlight indexing
(`mds_stores`) and endpoint-security agents can each take 50–80% CPU and will make everything here
slow and flaky.

### Android: `NotImplementedException: 'value' attribute is unknown for the element`

iOS exposes a MAUI `Entry`'s content as the `value` attribute; **Android has no `value` attribute at
all**, and UiAutomator2 throws rather than returning null — the message helpfully lists every
attribute it does support. So an iOS-shaped fallback is a crash on Android, not a fallback.

`AppDriver.Text()` now returns empty on Android instead of asking. If you add another attribute
read, check it against that supported list first.

### Android runs spend most of their time launching the app

The app is relaunched before every test (`UITEST_RESTART_PER_TEST`, on by default) and a Debug cold
start measured **23.1 s**. Across 39 tests that is roughly 15 minutes of pure launching — a full
Android run took ~30 minutes.

For a faster local loop, at the cost of letting UI state leak between tests:

```bash
UITEST_RESTART_PER_TEST=false ./run-uitests.sh --android --filter "TestCategory=Smoke"
```

Never do that in CI — the isolation is what makes failures mean anything. The real fix is making the
app start faster; see the XAML-inflation findings.

### The session dies mid-run

Look for `newCommandTimeout` (set to 300 s) in the server log, or a simulator/emulator crash.
A long build inside a test would do it — builds belong in the script, not in tests.

---

## Every test is reported as ignored

```
UI tests are opt-in because they need a booted simulator/emulator.
```

`UITEST_PLATFORM` is not set. This is deliberate, so a solution-wide `dotnet test` skips them
instead of hanging. Use `run-uitests.sh`, or export `UITEST_PLATFORM=ios` when running from an
IDE.

---

## Diagnostics worth knowing

```bash
# The element tree, live, without running a test — the fastest way to find a real id
curl -s -X POST http://127.0.0.1:4723/session \
  -H 'Content-Type: application/json' \
  -d '{"capabilities":{"firstMatch":[{"platformName":"iOS","appium:automationName":"XCUITest",
       "appium:deviceName":"iPhone 17 Pro","appium:bundleId":"com.pj.loan.afford.calc",
       "appium:noReset":true}]}}' | python3 -c "import json,sys;print(json.load(sys.stdin)['value']['sessionId'])"
# then GET /session/<id>/source, and DELETE /session/<id> when done

# Every AutomationId the app actually ships
strings src/LoanCalculator/bin/Debug/net10.0-ios26.5/iossimulator-arm64/LoanCalculatorMaui.app/LoanCalculatorMaui.dll | sort -u | less

# Android: what is on screen right now
adb shell screencap -p /sdcard/s.png && adb pull /sdcard/s.png

# Which ids exist in a failure dump
grep -o 'name="[^"]*"' TestResults/uitest-artifacts/<file>.xml | sort -u
```

For reading a failure's `.xml`, that last command is usually enough to settle whether an id
exists at all.
