# Screenshots: scripts, how to run them, what they make

Nothing in `Captures/`, `Store/` or the website guide images is edited by hand. Raw screens come from the
UI-test suite; three Python scripts turn them into finished images and the guide page.

```
Resources/Images/Screenshots/
  Captures/                       RAW screenshots, git-ignored (~240 MB), regenerable
    ios-iphone-6.9/                 store set + design sweep      iPhone 17 Pro Max
    ios-ipad-13/                    store set                      iPad Pro 13" (M5)
    android-phone/                  store set                      Medium_Phone_API_36.1
    ios-iphone-6.9-guide/           user-guide walkthrough, in reading order (iOS-guide-NN-name.png)
  Store/<device>/<style-name>/    FINISHED store images, one folder per style (classic, panorama, sunset-tilt ...)
  make_store_images.py            Captures  ->  Store; the menu/CLI, phone frames, status bar, HERO headlines
  store_styles.py                 the 20 numbered styles (layouts + palettes); add yours here
  make_guide_images.py            guide captures  ->  framed WebP images for the website
  make_guide_page.py              writes the guide page itself (text lives in this file)
  Droid/ Win/ Review/ InAppImage*.png   older, hand-made material; not touched by any script
```

## The three scripts at a glance

| Script | You run it when | Needs | Output |
|---|---|---|---|
| `make_store_images.py` (menu, or `-s 3,5 --skin gold`) | store screenshots changed, or you want to try other styles | Pillow, the store captures | `Store/<device>/<style-name>/` (8 images each) |
| `make_guide_images.py [out_dir]` | any guide screenshot changed | Pillow, the guide captures | 51+ framed `.webp` in `YourSimpleAppsPages/images/loanaffordcalc/guide/` |
| `make_guide_page.py` | you edit the guide text or add an image | nothing extra | `YourSimpleAppsPages/guides/loan-affordability-calculator.html` |

Install once: `pip3 install pillow`. Run the scripts from this folder (`cd Resources/Images/Screenshots`).
All three can be rerun as often as you like; they overwrite their own output only.

Required store sizes, and what we produce: **iPhone 6.9"** 1320x2868, **iPad 13"** 2064x2752,
**Google Play phone** 1080x2160 (Play rejects anything longer than 2:1).

## Part 1: take the raw screenshots (only when the UI or data changed)

Before you shoot:

- `SharedServiceCore.TESTING_PREMIUM_OVERRIDE` must be `true`, or Projection, Insights and the PDF export show a
  paywall in the shots. **It must be `false` in any release build.**
- Rebuild the app first if the UI changed (drop `--no-build`). A stale build photographs the old UI.
- Android's theme dropdown cannot be driven by automation. Set the theme by hand first and do **not** pass `--fresh`
  on that device, because `--fresh` wipes the app's data, including the theme.

Run from `src/Tests/LoanCalculator.UITests/`. Point `UITEST_ARTIFACTS` at where the PNGs should go:

```bash
C=../../../Resources/Images/Screenshots/Captures

# Guide walkthrough: FRESH install, iPhone only (~17 min). Builds the data from empty, so the pictures,
# the figures and the exported PDF all agree.
UITEST_ARTIFACTS=$C/ios-iphone-6.9-guide \
  ./run-uitests.sh --ios --no-build --fresh --device "iPhone 17 Pro Max" --filter "TestCategory=Guide"

# Store set, per device (default theme; iOS also does Light / Forest / Warm)
UITEST_ARTIFACTS=$C/ios-iphone-6.9 \
  ./run-uitests.sh --ios --no-build --device "iPhone 17 Pro Max" --filter "TestCategory=Store"
UITEST_ARTIFACTS=$C/ios-ipad-13 \
  ./run-uitests.sh --ios --no-build --fresh --device "iPad Pro 13-inch (M5)" --filter "TestCategory=Store"
UITEST_ARTIFACTS=$C/android-phone \
  ./run-uitests.sh --android --no-build --avd Medium_Phone_API_36.1 --filter "TestCategory=Store"
```

Each run writes a `*-report.txt` next to its PNGs listing what was captured and what was missed.
**Read it.** A missed screen is a line in that file, not a failed run.

### Taking or replacing a screenshot by hand

Some screens cannot be automated (splash, the theme/font/currency pickers, keypad-free forms). The automated shots are
raw simulator screenshots with no phone around them, and so are yours; the device frame and clean status bar are added
by the scripts.

1. Use the **iPhone 17 Pro Max** simulator so the size matches (1320x2868).
2. Get the screen as you want it. For numeric fields, tap an empty area or a label so the keypad is down.
3. Press **Cmd+S** in Simulator. A PNG lands on your Desktop.
4. Copy it into `Captures/ios-iphone-6.9-guide/`, named `iOS-guide-<NN>-<name>.png` (the same name replaces a shot;
   a new name adds one). Files sort by their number, which is the order they are listed in.
5. Run `python3 make_guide_images.py`.

If a new image should appear on the guide page, also add it to `make_guide_page.py` (see below).

## Part 2: store images (`make_store_images.py` + `store_styles.py`)

There are **20 numbered styles**, and you can also change the **phone type / case** for any of them. Run it with no
options and it shows a menu:

```bash
python3 make_store_images.py                 # menu: pick styles, then phone type, for all three devices
python3 make_store_images.py iphone          # menu, but only for the iPhone
```

```
 1  classic           Ocean gradient, headline on top, device bleeding off the bottom
 2  panorama          Ocean, one angled strip that spans all 8 images
 3  clean-light       Bright and minimal, whole device floating, silver phone
 ...
Pick by number: 3   or  3,5,8   or  2-6   or  all
Styles [1,2]: 
```

Skip the menu with flags (this is also how it runs when there is no terminal):

```bash
python3 make_store_images.py --list                         # show every style and phone type
python3 make_store_images.py -s 3,5,8                       # styles 3, 5 and 8, all devices
python3 make_store_images.py iphone android -s 2-6          # a range, two devices
python3 make_store_images.py -s all                         # everything (a few minutes)
python3 make_store_images.py iphone -s 4 --skin case-red    # style 4, but in a red phone case
python3 make_store_images.py iphone -s 1 --skin all         # style 1 once per phone type
```

Output goes to `Store/<device>/<style-name>/NN-slug.png`, e.g. `Store/ios-iphone-6.9/sunset-tilt/`. A style run with a
phone type override gets `+<type>` on the folder name (`sunset-tilt+case-red`), so nothing overwrites anything else.
Panorama styles also write `_full.png`, the unsliced strip. Re-running a style overwrites only its own folder.
Styles 1 and 2 keep the original folder names (`classic`, `panorama`).

**The styles.** Each is a *layout* plus a *colour palette*:

| Layouts | What it does |
|---|---|
| bleed | headline on top, device running off the bottom (optionally tilted, optionally with a halo ring) |
| floating | the whole device visible, smaller, floating under the headline |
| duo | two devices per image: this screen in front, the next one peeking behind |
| poster | device up top, a huge headline over a fade at the bottom |
| slim | almost no bezel and a deep shadow: the screen is the hero |
| panorama | one wide canvas, tilted devices and a glow ribbon, sliced into panels that connect |

Palettes: ocean, sunset, grape, midnight, forest (dark) and paper, mint, rose (light).

**Phone types and cases** (`--skin`, or the second menu). They apply to every device (iPhone, iPad and Android) and
override whatever a style uses:

`black` (the plain default), `silver`, `titanium`, `gold`, `blue`, `white`, `case-red`, `case-navy`, `case-mint`,
`case-clear` (a translucent case), `case-rugged` (thick), `notch` (an older-style iPhone notch instead of the
Dynamic Island), `no-camera` (a clean screen with no cut-out). The coloured ones also get side buttons.

**Adding your own.**
- *A new combination:* add a line to `STYLES` at the bottom of `store_styles.py`, e.g.
  `(21, "ocean-gold", "Ocean with gold phones", bleed(PALETTES["ocean"], skin="gold"))`. It appears in the menu at once.
- *A new colour scheme:* add an entry to `PALETTES` (`bg` gradient stops, `glow`, `ink` for the headline).
- *A new phone type or case:* add an entry to `SKINS` in `make_store_images.py` (body colour, edge colour, and an
  optional `case=(colour, thickness)`).
- *A new arrangement:* write a function like `bleed` or `duo` in `store_styles.py`.

Each style takes about 8 to 15 seconds per device, so `-s all` on one device is a few minutes.

**Headlines and screens.** Edit `HERO` at the top of `make_store_images.py`: each entry is
*(capture name, file slug, headline)*. The first slot is what shows in search results, so give it the strongest
promise. Per-device sizes and the status-bar repaint settings are in `DEVICES`.

Then upload the files from the style you like (`Store/<device>/<style-name>/`) to App Store Connect and Play Console.

## Part 3: the website user guide

The guide lives in the pages repo (`YourSimpleAppsPages`) and is built from three things in this folder.

```bash
python3 make_guide_images.py     # captures -> images/loanaffordcalc/guide/*.webp  (framed, 9:41 status bar)
python3 make_guide_page.py       # text + images -> guides/loan-affordability-calculator.html
```

- **Edit the text** in `make_guide_page.py`. Each section is an entry in `SECTIONS`. Helpers: `fig()` is one captioned
  image, `row()` lays images in a line, `purpose()`, `calc()` and `watch()` make the blue, green and amber boxes, and
  `scenario()` builds one What If scenario block.
- **Add an image:** capture it (see above), run `make_guide_images.py`, then reference it as `fig("NN-name.webp", "caption")`.
- **The exported PDF pages** (`pdf-page-NN.webp`) are not made by a script here. To refresh them, export the PDF
  from the app, find it in the simulator's Files (or `find ~/Library/Developer/CoreSimulator/Devices/*/data/Containers
  -iname "*Insights*.pdf"`), render the pages to PNG, and save them as 900-px-wide WebP into
  `images/loanaffordcalc/guide/`. macOS has no poppler by default; a short Swift script using `CGPDFDocument` does it.
- The app's page text (`detailedDescription` in `YourSimpleAppsPages/data/apps.json`) is edited by hand in that file.

## Choosing and wording the 8 store images

The default set is: Know what you can really afford, Set up your loan in seconds, See where every dollar goes,
Nine "what if" scenarios, Test interest rate changes, Stress-test your budget, Project your financial future, Every
repayment year by year. Change them in `HERO` (see Part 2).

## Known traps

- **iOS "Sign in to Apple Account" prompt** covers the app on a simulator with no Apple account. The suite dismisses it
  at launch. If taps seem to do nothing, look at a screenshot.
- **WebDriverAgent can wedge** after about 30 minutes of continuous driving (every command waits its 60 s timeout).
  Kill the run and restart it; the second half of a long run is where it happens.
- The raw status bar shows a real clock and carrier. The scripts repaint it to a clean 9:41, so raw captures are not
  store-ready on their own.
- Test rows named `SeedIncome...` / `SeedExpense...` come from `TestData.EnsureBudget` and show up in screenshots and
  PDFs. The Store and Guide fixtures avoid it; the older design sweep (`TestCategory=Screens`) does not.
- Numeric keypads cover the lower half of the screen and have no Return key, so shots of filled-in forms often include
  one. Retake those by hand if it matters (see "Taking or replacing a screenshot by hand").
- Do not run `--fresh` on a device whose theme or data you set by hand.
