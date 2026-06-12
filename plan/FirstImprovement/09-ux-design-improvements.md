# 09 — UX / Design: Layout Consistency, OnIdiom Verbosity, Typography

**Priority:** Medium  
**Status:** ⚠️ PARTIAL — Token system created; view migration in progress  
**Symptoms:** UI looks inconsistent between Phone/Tablet; font sizes feel arbitrary; some labels truncate on small screens; excessive XAML is hard to maintain and introduces subtle per-device bugs.

---

## What Was Done

- **Fix A (token system) — DONE:** `FontSizeSmall`, `FontSizeBody`, `FontSizeMedium`, `MarginSection`, `MarginSectionFlat` and related size tokens added to `Theme.CommonStyles.xaml`.
- **Fix A (view migration) — PARTIAL:** `LoanView.xaml` reduced to ~10 inline `OnIdiom` usages (down from many more). `ExpenseView.xaml` and `IncomeView.xaml` still each have ~2 remaining.
- **Fix D (page titles) — NOT DONE:** `ContentPage Title` values (`Title="LoanView"` etc.) still present.
- **Fix B (HeightOfGridRowToggledByCountryOnStampDuty) — NOT DONE:** Magic-number height property still in `LoanViewModel`. Low priority since it works correctly.
- **Fix C (inline style audit) — NOT DONE:** Inline `FontSize=`, `TextColor=`, `Margin=` overrides remain in all three views. No regression risk but blocks theming completeness.

---

## Problem

### Issue A — Extreme `OnIdiom` verbosity

Nearly every font size, margin, height, and width in every XAML file uses `OnIdiom` with 5–6 explicit values:

```xml
<Span FontSize="{OnIdiom Phone=15, Tablet=26, Desktop=26, TV=26, Watch=13, Default=15}" />
```

This pattern is repeated hundreds of times across `LoanView.xaml`, `IncomeView.xaml`, `ExpenseView.xaml`, and the controls. It means:
- Any design change requires updating dozens of lines
- Values are inconsistent across files (Phone=15 in one place, Phone=14 in another)
- The `Watch` value is always set but MAUI for watchOS is not a target

### Issue B — Magic numbers for heights/widths

`HeightOfGridRowToggledByCountryOnStampDuty` in `LoanViewModel` (line ~440) returns `60`, `110`, or `40` as raw integers based on device idiom. This is a UI layout concern living in the ViewModel — a violation of separation of concerns and hard to discover.

### Issue C — Mixed styling approaches

Some controls use `Style="{DynamicResource ...}"` correctly. Others apply `FontSize`, `TextColor`, `Margin` inline. This produces visual inconsistency and makes theme changes incomplete (inline values override theme).

### Issue D — `Title="LoanView"` on the `ContentPage`

Each view has `Title="LoanView"`, `Title="IncomeView"` etc. as the `ContentPage.Title`. Since `NavigationPage` is not used (Shell hides the nav bar), this title never shows — but if it ever does appear it will look unprofessional.

---

## Fix

### Fix A — Extract `OnIdiom` values into named style resources in `Theme.CommonStyles.xaml`

Create a set of named size tokens in the shared theme file:

```xml
<!-- Theme.CommonStyles.xaml — add once, reuse everywhere -->
<OnIdiom x:Key="FontSizeSmall"   x:TypeArguments="x:Double"
         Phone="13" Tablet="18" Desktop="18" Default="13"/>
<OnIdiom x:Key="FontSizeBody"    x:TypeArguments="x:Double"
         Phone="15" Tablet="20" Desktop="20" Default="15"/>
<OnIdiom x:Key="FontSizeMedium"  x:TypeArguments="x:Double"
         Phone="17" Tablet="22" Desktop="22" Default="17"/>
<OnIdiom x:Key="FontSizeLarge"   x:TypeArguments="x:Double"
         Phone="20" Tablet=28" Desktop="28" Default="20"/>
<OnIdiom x:Key="FontSizeHeading" x:TypeArguments="x:Double"
         Phone="24" Tablet="34" Desktop="34" Default="24"/>

<OnIdiom x:Key="MarginStandard"  x:TypeArguments="Thickness"
         Phone="5,0,5,0" Tablet="20,10,20,10" Desktop="20,10,20,10" Default="5,0,5,0"/>
<OnIdiom x:Key="MarginSection"   x:TypeArguments="Thickness"
         Phone="8,4,8,4" Tablet="24,12,24,12" Desktop="24,12,24,12" Default="8,4,8,4"/>
```

Then replace inline `OnIdiom` blocks with `StaticResource` references:

```xml
<!-- Before -->
<Span FontSize="{OnIdiom Phone=15, Tablet=26, Desktop=26, TV=26, Watch=13, Default=15}" />

<!-- After -->
<Span FontSize="{StaticResource FontSizeBody}" />
```

This is a systematic find-and-replace pass. Do it view-by-view to avoid large diffs.

### Fix B — Move device-adaptive heights to XAML triggers or converters

Remove `HeightOfGridRowToggledByCountryOnStampDuty` from `LoanViewModel`. Replace with an `OnIdiom` in the XAML:

```xml
<!-- LoanView.xaml — the stamp duty state row -->
<RowDefinition Height="{Binding ShowAustralianStateSelectorOnStampDuty,
    Converter={StaticResource BoolToGridLengthConverter},
    ConverterParameter='0,60,110'}" />
```

Or more simply, use a `Trigger`:

```xml
<RowDefinition x:Name="StampDutyRow" Height="0">
    <RowDefinition.Triggers>
        <DataTrigger TargetType="RowDefinition"
                     Binding="{Binding ShowAustralianStateSelectorOnStampDuty}"
                     Value="True">
            <Setter Property="Height" Value="{OnIdiom Phone=60, Tablet=110, Default=60}" />
        </DataTrigger>
    </RowDefinition.Triggers>
</RowDefinition>
```

### Fix C — Audit and remove inline style overrides

Do a search for `FontSize="` and `TextColor="` and `Margin="` in XAML files (outside of Style definitions). Each one should either:
1. Be moved into an appropriate named `Style` in `Theme.CommonStyles.xaml`
2. Reference an existing style via `Style="{DynamicResource ...}"`

Priority files: `LoanView.xaml`, `IncomeView.xaml`, `ExpenseView.xaml`.

### Fix D — Set meaningful page titles

```xml
<!-- LoanView.xaml -->
<ContentPage Title="Loan Calculator" ... />
<!-- IncomeView.xaml -->
<ContentPage Title="Income" ... />
```

---

## Verification

1. Run on both iPhone SE (small) and iPad — layout should look intentional on both, no truncated labels.
2. Switch themes (Dark → Light → Forest) — no residual inline colors from previous theme remain.
3. The stamp duty row appears/disappears correctly for Australian vs non-Australian app variants.
