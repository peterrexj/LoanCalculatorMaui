#!/usr/bin/env python3
"""
Builds the App Store / Google Play listing images from the raw Appium captures.

    python3 make_store_images.py            # all devices, both styles
    python3 make_store_images.py iphone     # one device: iphone | ipad | android

Input   Captures/<device>/<Prefix>-default-NN-*.png   (raw, from the UITests StoreScreenshots fixture)
Output  Store/<device>/classic/NN-slug.png    headline + framed device bleeding off the bottom
        Store/<device>/panorama/NN-slug.png   one continuous angled canvas, sliced into panels, so
                                              the devices straddle the seams when swiped through
        Store/<device>/panorama/_full.png     the unsliced canvas, for checking the spread

Only needs Pillow. Headline font is NotoSans-Bold from the app's own Resources/Fonts (OFL).
Edit HERO to change which captures are used or what the headlines say.
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
FONT_BOLD = os.path.abspath(os.path.join(
    HERE, "..", "..", "..", "src", "LoanCalculator", "Resources", "Fonts", "NotoSans-Bold.ttf"))

# (capture stem, slug, headline). Order = order in the store listing; the first image is what
# people see in search results, so it carries the strongest promise.
HERO = [
    ("06-whatif-answer",         "afford",     "Know what you can\nreally afford"),
    ("01-loan",                  "loan",       "Set up your loan\nin seconds"),
    ("05-budget-summary",        "budget",     "See where every\ndollar goes"),
    ("10-whatif-scenarios",      "scenarios",  "Nine ‘what if’\nscenarios"),
    ("11-scenario-rate-change",  "rates",      "Test interest\nrate changes"),
    ("18-scenario-stress-test",  "stress",     "Stress-test\nyour budget"),
    ("07-whatif-projection",     "projection", "Project your\nfinancial future"),
    ("02-schedule",              "schedule",   "Every repayment,\nyear by year"),
]

# Canvas = the store's required size. fs = headline size and tw = device width, both as a
# fraction of canvas width. status = how to repaint the simulator's status bar (real clock and
# carrier text give away that it is a simulator, and a fixed 9:41 is the convention).
DEVICES = {
    "iphone": dict(
        folder="ios-iphone-6.9", prefix="iOS", size=(1320, 2868), fs=0.078, tw=0.86,
        corner=0.140, bezel=0.016, island=True, punch=False,
        status=dict(fill=130, clock=(250, 97, 56), battery=(1108, 80, 1190, 114), bars=(930, 112)),
    ),
    "ipad": dict(
        folder="ios-ipad-13", prefix="iOS", size=(2064, 2752), fs=0.050, tw=0.88,
        corner=0.030, bezel=0.014, island=False, punch=False,
        status=dict(fill=62, clock=(80, 32, 30), battery=(1950, 20, 2012, 44), bars=None),
    ),
    "android": dict(
        folder="android-phone", prefix="Android", size=(1080, 2160), fs=0.078, tw=0.86,
        corner=0.075, bezel=0.014, island=False, punch=True,
        status=dict(fill=58, clock=(70, 29, 34), battery=(980, 18, 1030, 41), bars=None),
    ),
}

NAVY = (9, 22, 42)
TEAL = (14, 92, 99)
INDIGO = (27, 52, 104)
GLOW = (46, 196, 172)


def font(px):
    return ImageFont.truetype(FONT_BOLD, int(px))


# ── Status bar ───────────────────────────────────────────────────────────────

def repaint_status_bar(im, cfg):
    """Cover the simulator's clock/battery with a clean 9:41 and full battery."""
    s = cfg["status"]
    w = im.width
    top = im.crop((0, 3, w, 4))                      # one row, sampled above any glyph
    im.paste(top.resize((w, s["fill"])), (0, 0))

    avg = im.crop((0, 3, w, 4)).resize((1, 1)).getpixel((0, 0))
    lum = 0.2126 * avg[0] + 0.7152 * avg[1] + 0.0722 * avg[2]
    ink = (20, 24, 32, 255) if lum > 140 else (255, 255, 255, 255)

    d = ImageDraw.Draw(im)
    cx, cy, size = s["clock"]
    f = font(size)
    if cfg["prefix"] == "iOS" and not cfg["island"]:  # iPad: left-aligned clock
        d.text((cx, cy), "9:41", font=f, fill=ink, anchor="lm")
    elif cfg["prefix"] == "Android":
        d.text((cx - 34, cy), "9:41", font=f, fill=ink, anchor="lm")
    else:
        d.text((cx, cy), "9:41", font=f, fill=ink, anchor="mm")

    x0, y0, x1, y1 = s["battery"]
    r = (y1 - y0) // 3
    d.rounded_rectangle((x0, y0, x1, y1), radius=r, outline=ink, width=max(2, (y1 - y0) // 12))
    pad = max(3, (y1 - y0) // 8)
    d.rounded_rectangle((x0 + pad, y0 + pad, x1 - pad, y1 - pad), radius=max(2, r - 3), fill=ink)
    d.rounded_rectangle((x1 + 3, y0 + (y1 - y0) // 3, x1 + 3 + max(3, (y1 - y0) // 8),
                         y1 - (y1 - y0) // 3), radius=2, fill=ink)

    if s["bars"]:                                     # four signal bars, left of the battery
        bx, by = s["bars"]
        for i in range(4):
            h = 14 + i * 7
            d.rounded_rectangle((bx + i * 18, by - h, bx + i * 18 + 11, by), radius=3, fill=ink)


# ── Device frame ─────────────────────────────────────────────────────────────

# "Phone types": body colour, edge highlight, optional case (colour with alpha, thickness as a
# fraction of the device width), and camera shape. skin=None keeps the plain black frame.
# camera: auto (what the device really has) | island | notch | punch | none
SKINS = {
    "black":        dict(body=(12, 13, 16),    edge=(78, 82, 92)),
    "silver":       dict(body=(206, 209, 216), edge=(246, 247, 250)),
    "titanium":     dict(body=(118, 112, 104), edge=(176, 170, 160)),
    "gold":         dict(body=(206, 176, 128), edge=(240, 220, 180)),
    "blue":         dict(body=(30, 58, 104),   edge=(96, 134, 196)),
    "white":        dict(body=(240, 240, 244), edge=(255, 255, 255)),
    "case-red":     dict(body=(12, 13, 16), edge=(78, 82, 92), case=((214, 60, 70, 255), 0.040)),
    "case-navy":    dict(body=(12, 13, 16), edge=(78, 82, 92), case=((30, 50, 96, 255), 0.040)),
    "case-mint":    dict(body=(12, 13, 16), edge=(78, 82, 92), case=((120, 214, 190, 255), 0.040)),
    "case-clear":   dict(body=(12, 13, 16), edge=(78, 82, 92), case=((255, 255, 255, 70), 0.034)),
    "case-rugged":  dict(body=(12, 13, 16), edge=(78, 82, 92), case=((70, 74, 86, 255), 0.062)),
    "notch":        dict(body=(12, 13, 16), edge=(78, 82, 92), camera="notch"),
    "no-camera":    dict(body=(12, 13, 16), edge=(78, 82, 92), camera="none"),
}


def framed_device(path, cfg, tw, skin=None):
    """Return an RGBA image of the capture inside a rounded bezel, `tw` px wide.
    `skin` is a name from SKINS (or a dict of the same shape) for other phone types and cases."""
    sk = SKINS.get(skin, {}) if isinstance(skin, str) else (skin or {})
    cam = sk.get("camera", "auto")
    island, punch, notch = cfg["island"], cfg["punch"], False
    if cam == "none":
        island = punch = False
    elif cam == "notch" and cfg["island"]:
        island, notch = False, True
    elif cam == "island" and cfg["prefix"] == "iOS" and cfg["size"][0] < 1600:
        island = True
    elif cam == "punch" and cfg["prefix"] == "Android":
        punch = True

    shot = Image.open(path).convert("RGBA")
    shot = shot.crop((3, 3, shot.width - 3, shot.height - 3))   # drop the dark corner fringe
    repaint_status_bar(shot, cfg)

    d = ImageDraw.Draw(shot)
    if island:
        iw, ih = int(shot.width * 0.286), int(shot.width * 0.084)
        x = (shot.width - iw) // 2
        d.rounded_rectangle((x, 33, x + iw, 33 + ih), radius=ih // 2, fill=(0, 0, 0, 255))
    if notch:
        nw, nh = int(shot.width * 0.40), int(shot.width * 0.075)
        x = (shot.width - nw) // 2
        d.rounded_rectangle((x, -nh, x + nw, nh), radius=nh // 2, fill=(0, 0, 0, 255))
    if punch:
        r = int(shot.width * 0.017)
        cx, cy = shot.width // 2, int(shot.width * 0.034)
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(0, 0, 0, 255))

    th = round(tw * shot.height / shot.width)
    shot = shot.resize((tw, th), Image.LANCZOS)

    corner = int(tw * cfg["corner"])
    mask = Image.new("L", (tw, th), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, tw - 1, th - 1), radius=corner, fill=255)

    b = max(6, int(tw * cfg["bezel"]))
    body = sk.get("body", (12, 13, 16))
    edge = sk.get("edge", (78, 82, 92))
    case_color, case_t = sk.get("case", (None, 0))
    c = int(tw * case_t) if case_color else 0
    bw = max(3, b // 2) if sk else 0                      # side buttons only on explicit skins
    m = c + bw
    out = Image.new("RGBA", (tw + 2 * b + 2 * m, th + 2 * b + 2 * m), (0, 0, 0, 0))
    od = ImageDraw.Draw(out)
    if bw:                                                # power on the right, volume on the left
        H = th + 2 * b
        for y0, y1, left in ((0.17, 0.23, True), (0.26, 0.34, True), (0.27, 0.40, False)):
            x = c if left else c + bw + tw + 2 * b
            od.rounded_rectangle((x, c + bw + int(H * y0), x + bw, c + bw + int(H * y1)),
                                 radius=bw // 2, fill=edge + (255,))
    if c:
        od.rounded_rectangle((0, 0, out.width - 1, out.height - 1), radius=corner + b + m,
                             fill=case_color, outline=tuple(min(255, v + 50) for v in case_color[:3]) + (case_color[3],),
                             width=max(2, c // 5))
        if case_color[3] < 255:                           # clear case: re-cut the middle so the phone shows through
            hole = Image.new("L", out.size, 0)
            ImageDraw.Draw(hole).rounded_rectangle((c, c, out.width - 1 - c, out.height - 1 - c),
                                                   radius=corner + b + bw, fill=255)
            out.paste(Image.new("RGBA", out.size, (0, 0, 0, 0)), (0, 0), hole)
    od.rounded_rectangle((m, m, out.width - 1 - m, out.height - 1 - m), radius=corner + b,
                         fill=body + (255,), outline=edge + (255,), width=max(2, b // 4))
    out.paste(shot, (m + b, m + b), mask)
    return out


def shadowed(device, spread=0.03, alpha=140):
    """Pad `device` and add a soft drop shadow beneath it."""
    pad = int(device.width * 0.12)
    canvas = Image.new("RGBA", (device.width + 2 * pad, device.height + 2 * pad), (0, 0, 0, 0))
    sh = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    a = device.split()[3].point(lambda v: alpha if v > 8 else 0)
    sh.paste((0, 0, 0, 255), (pad, pad + int(device.width * 0.02)), a)
    sh = sh.filter(ImageFilter.GaussianBlur(device.width * spread))
    canvas.alpha_composite(sh)
    canvas.alpha_composite(device, (pad, pad))
    return canvas


# ── Backgrounds ──────────────────────────────────────────────────────────────

def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def gradient(size, stops, horizontal=False):
    """Piecewise linear gradient through `stops` (list of RGB), vertical by default."""
    w, h = size
    n = (w if horizontal else h)
    line = Image.new("RGB", (n, 1))
    px = line.load()
    segs = len(stops) - 1
    for i in range(n):
        t = i / max(1, n - 1) * segs
        k = min(int(t), segs - 1)
        px[i, 0] = lerp(stops[k], stops[k + 1], t - k)
    if horizontal:
        return line.resize((w, h))
    return line.rotate(-90, expand=True).transpose(Image.FLIP_LEFT_RIGHT).resize((w, h))


def glow(canvas, cx, cy, rx, ry, color, alpha):
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=color + (alpha,))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(min(rx, ry) * 0.45)))


def headline(canvas, text, cx, top, fs):
    d = ImageDraw.Draw(canvas)
    f = font(fs)
    y = top
    for line in text.split("\n"):
        d.text((cx + 2, y + 3), line, font=f, fill=(0, 0, 0, 90), anchor="mt")   # soft lift
        d.text((cx, y), line, font=f, fill=(255, 255, 255, 255), anchor="mt")
        y += int(fs * 1.2)
    return y


# ── Styles ───────────────────────────────────────────────────────────────────

def classic(cfg, files):
    W, H = cfg["size"]
    fs = int(W * cfg["fs"])
    out = []
    for i, (path, slug, text) in enumerate(files):
        bg = gradient((W, H), [NAVY, (12, 58, 74), TEAL]).convert("RGBA")
        bottom = headline(bg, text, W // 2, int(H * 0.05), fs)
        dev = framed_device(path, cfg, int(W * cfg["tw"]))
        top = bottom + int(H * 0.03)
        glow(bg, W // 2, top + dev.height // 3, int(W * 0.55), int(H * 0.28), GLOW, 70)
        sd = shadowed(dev)
        _paste_clipped(bg, sd, (W - sd.width) // 2, top - (sd.height - dev.height) // 2)
        out.append((f"{i + 1:02d}-{slug}.png", bg.convert("RGB")))
    return out


def _paste_clipped(base, overlay, x, y):
    """alpha_composite that tolerates an overlay hanging off the canvas (bleed)."""
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    layer.paste(overlay, (x, y))
    base.alpha_composite(layer)


def panorama(cfg, files):
    W, H = cfg["size"]
    n = len(files)
    full = gradient((W * n, H), [NAVY, TEAL, INDIGO, TEAL, NAVY, INDIGO, TEAL, NAVY],
                    horizontal=True).convert("RGBA")
    # darken top and bottom so the headline and the bleed read cleanly
    shade = gradient((W * n, H), [(0, 0, 0), (0, 0, 0), (0, 0, 0)]).convert("RGBA")
    shade.putalpha(gradient((W * n, H), [(70, 70, 70), (0, 0, 0), (60, 60, 60)]).convert("L"))
    full.alpha_composite(shade)

    fs = int(W * cfg["fs"])
    tw = int(W * 0.86)
    # Evenly spaced (one per panel), alternately tilted and raised. At this width a tilted
    # device's corners reach past the seam, so neighbours visibly continue into each other.
    tilts = [-8, 8, -8, 8, -8, 8, -8, 8]
    drift = [0.0] * 8
    lift = [0.00, 0.05, 0.00, 0.05, 0.00, 0.05, 0.00, 0.05]

    # A glowing wave through the whole canvas, behind every device: the one element that is
    # unmistakably a single image rather than eight, which is what sells the "spanning" look.
    ribbon = Image.new("RGBA", full.size, (0, 0, 0, 0))
    rd = ImageDraw.Draw(ribbon)
    pts = [(x, int(H * 0.60 + math.sin(x / (W * 1.7) * math.pi * 2) * H * 0.10))
           for x in range(0, full.width, 20)]
    rd.line(pts, fill=GLOW + (110,), width=int(H * 0.07), joint="curve")
    full.alpha_composite(ribbon.filter(ImageFilter.GaussianBlur(H * 0.03)))
    rd.line(pts, fill=(255, 255, 255, 120), width=int(H * 0.004), joint="curve")
    full.alpha_composite(ribbon.filter(ImageFilter.GaussianBlur(2)))

    for i, (path, slug, text) in enumerate(files):
        cx = int(W * (i + 0.5 + drift[i]))
        dev = framed_device(path, cfg, tw)
        sd = shadowed(dev).rotate(tilts[i], resample=Image.BICUBIC, expand=True)
        top_target = int(H * (0.215 + lift[i]))
        _paste_clipped(full, sd, cx - sd.width // 2, top_target - (sd.height - dev.height) // 2)

    for i, (path, slug, text) in enumerate(files):   # headlines last so no device covers them
        headline(full, text, int(W * (i + 0.5)), int(H * 0.05), fs)

    panels = []
    for i, (path, slug, text) in enumerate(files):
        panels.append((f"{i + 1:02d}-{slug}.png",
                       full.crop((i * W, 0, (i + 1) * W, H)).convert("RGB")))
    return panels, full


# ── Driver ───────────────────────────────────────────────────────────────────

def _parse_numbers(text, valid):
    """'3,5' / '2-4' / 'all' -> sorted list of numbers present in `valid`."""
    text = text.strip().lower()
    if text in ("all", "a", "*"):
        return sorted(valid)
    picked = set()
    for part in text.replace(" ", "").split(","):
        if "-" in part:
            lo, hi = part.split("-", 1)
            if lo.isdigit() and hi.isdigit():
                picked.update(range(int(lo), int(hi) + 1))
        elif part.isdigit():
            picked.add(int(part))
    return sorted(n for n in picked if n in valid)


def _menu(styles):
    print("\nStore image styles\n" + "-" * 70)
    for num, name, desc, _ in styles:
        print(f"  {num:>2}  {name:<17} {desc}")
    print("\n  Pick by number: 3   or  3,5,8   or  2-6   or  all")
    return input("Styles [1,2]: ").strip() or "1,2"


def _skin_menu():
    names = list(SKINS)
    print("\nPhone type / case  (applies to every device: iPhone, iPad, Android)\n" + "-" * 70)
    print("   0  style default   each style's own phone (most are plain black)")
    for i, n in enumerate(names, 1):
        print(f"  {i:>2}  {n}")
    print("   a  ALL             render each chosen style once per phone type")
    ans = input("Phone type [0]: ").strip().lower() or "0"
    if ans in ("a", "all"):
        return names
    if ans.isdigit() and 1 <= int(ans) <= len(names):
        return [names[int(ans) - 1]]
    if ans in SKINS:
        return [ans]
    return [None]


def _render(cfg, name, files, style, skin_label):
    num, folder, desc, run = style
    folder = folder if not skin_label else f"{folder}+{skin_label}"
    out_dir = os.path.join(HERE, "Store", cfg["folder"], folder)
    os.makedirs(out_dir, exist_ok=True)
    images, full = run(cfg, files)
    for fname, im in images:
        im.save(os.path.join(out_dir, fname), optimize=True)
    if full is not None:
        full.convert("RGB").resize((full.width // 3, full.height // 3), Image.LANCZOS).save(
            os.path.join(out_dir, "_full.png"), optimize=True)
    print(f"  {name}: style {num} {folder}: {len(images)} images -> {out_dir}")


def main():
    import argparse
    import store_styles

    ap = argparse.ArgumentParser(
        description="Build store images. With no --style, shows a menu of numbered styles.")
    ap.add_argument("devices", nargs="*", help="iphone ipad android (default: all)")
    ap.add_argument("-s", "--style", help="style numbers, e.g. 3  or 3,5,8  or 2-6  or all")
    ap.add_argument("-k", "--skin", help="phone type / case for every style: a name, or 'all' "
                                         "(see --list). Default: each style's own")
    ap.add_argument("-l", "--list", action="store_true", help="list styles and phone types, then exit")
    args = ap.parse_args()

    styles = store_styles.STYLES
    valid = {st[0] for st in styles}
    if args.list:
        for num, name, desc, _ in styles:
            print(f"{num:>2}  {name:<17} {desc}")
        print("\nPhone types: " + ", ".join(SKINS))
        return

    devices = args.devices or list(DEVICES)
    bad = [d for d in devices if d not in DEVICES]
    if bad:
        sys.exit(f"Unknown device {bad}. Choose from: {', '.join(DEVICES)}")

    interactive = sys.stdin.isatty() and not args.style
    picked = _parse_numbers(args.style or (_menu(styles) if interactive else "1,2"), valid)
    if not picked:
        sys.exit("No valid style numbers chosen. Run with --list to see them.")

    if args.skin:
        skins = list(SKINS) if args.skin == "all" else [args.skin]
        if any(k not in SKINS for k in skins):
            sys.exit(f"Unknown phone type. Choose from: {', '.join(SKINS)}")
    elif interactive:
        skins = _skin_menu()
    else:
        skins = [None]

    for name in devices:
        cfg = DEVICES[name]
        cap = os.path.join(HERE, "Captures", cfg["folder"])
        files = []
        for stem, slug, text in HERO:
            p = os.path.join(cap, f"{cfg['prefix']}-default-{stem}.png")
            if not os.path.exists(p):
                print(f"  {name}: missing {os.path.basename(p)}, skipped")
                continue
            files.append((p, slug, text))
        if not files:
            continue
        print(f"{name}:")
        for skin in skins:
            store_styles.OVERRIDE["skin"] = skin
            for style in [st for st in styles if st[0] in picked]:
                _render(cfg, name, files, style, skin)
    store_styles.OVERRIDE["skin"] = None


if __name__ == "__main__":
    main()
