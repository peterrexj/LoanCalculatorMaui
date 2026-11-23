"""
The catalogue of store-image styles used by make_store_images.py.

A style = a LAYOUT (how the device and headline are arranged) + a PALETTE (colours).
To add one, add a line to STYLES at the bottom, e.g.

    (17, "ocean-poster", "Poster: huge headline at the bottom", poster(PALETTES["ocean"])),

To add a colour scheme, add an entry to PALETTES. To add a new arrangement, write a function that
takes a palette and returns  run(cfg, files) -> (list of (filename, image), full_image_or_None).

`cfg` is the per-device config from make_store_images.DEVICES; `files` is [(capture path, slug,
headline)] in listing order. Everything is drawn at the store's required pixel size.
"""
import math

from PIL import Image, ImageDraw, ImageFilter

import make_store_images as b

# ── Palettes ─────────────────────────────────────────────────────────────────
# bg    gradient stops, top to bottom (or left to right in panoramas)
# glow  accent used for halos, ribbons and rings
# ink   headline colour
PALETTES = {
    "ocean":    dict(bg=[b.NAVY, (12, 58, 74), b.TEAL],              glow=b.GLOW,           ink=(255, 255, 255)),
    "sunset":   dict(bg=[(48, 16, 70), (150, 40, 90), (244, 120, 80)], glow=(255, 170, 90),  ink=(255, 255, 255)),
    "grape":    dict(bg=[(24, 12, 56), (70, 30, 120), (120, 60, 170)], glow=(190, 110, 255), ink=(255, 255, 255)),
    "midnight": dict(bg=[(6, 8, 14), (14, 18, 32), (24, 30, 52)],     glow=(80, 140, 255),  ink=(255, 255, 255)),
    "forest":   dict(bg=[(8, 32, 24), (16, 70, 52), (40, 110, 80)],   glow=(120, 220, 150), ink=(255, 255, 255)),
    "paper":    dict(bg=[(250, 250, 252), (236, 240, 248), (222, 228, 242)], glow=(110, 130, 255), ink=(18, 24, 40)),
    "mint":     dict(bg=[(232, 250, 244), (196, 238, 228), (160, 222, 214)], glow=(60, 190, 170),  ink=(14, 40, 52)),
    "rose":     dict(bg=[(255, 236, 240), (255, 214, 224), (250, 190, 208)], glow=(255, 120, 150), ink=(60, 16, 30)),
}


def _is_light(pal):
    return sum(pal["ink"]) < 300           # dark ink means a light background


# ── Small helpers ────────────────────────────────────────────────────────────

# make_store_images.py sets OVERRIDE["skin"] when you pick a phone type on the command line or in
# the menu; it wins over the skin a style was written with.
OVERRIDE = {"skin": None}


def _fd(path, cfg, width, skin):
    return b.framed_device(path, cfg, width, skin=OVERRIDE["skin"] or skin)


def _canvas(pal, size):
    return b.gradient(size, pal["bg"]).convert("RGBA")


def _title(canvas, text, cx, top, fs, ink, maxw=None):
    """Centre-aligned headline; returns the y below the last line. maxw shrinks it to fit."""
    d = ImageDraw.Draw(canvas)
    if maxw:
        while fs > 20 and max(b.font(fs).getlength(t) for t in text.split("\n")) > maxw:
            fs = int(fs * 0.95)
    f = b.font(fs)
    y = top
    for line in text.split("\n"):
        if sum(ink) > 600:                                           # soft lift on light ink only
            d.text((cx + 2, y + 3), line, font=f, fill=(0, 0, 0, 90), anchor="mt")
        d.text((cx, y), line, font=f, fill=ink + (255,), anchor="mt")
        y += int(fs * 1.2)
    return y


def _name(i, slug):
    return f"{i + 1:02d}-{slug}.png"


def _place(base, overlay, cx, top):
    """Paste a padded (shadowed / rotated) overlay centred on cx, its content starting at `top`."""
    b._paste_clipped(base, overlay, cx - overlay.width // 2, top)


# ── Layouts ──────────────────────────────────────────────────────────────────

def bleed(pal, tw=None, tilt=0.0, shift=0.0, ring=False, skin=None):
    """Headline on top, device bleeding off the bottom. Optional tilt (alternating) and halo ring."""
    def run(cfg, files):
        W, H = cfg["size"]
        fs = int(W * cfg["fs"])
        out = []
        for i, (path, slug, text) in enumerate(files):
            bg = _canvas(pal, (W, H))
            bottom = int(H * 0.05) + int(fs * 1.2) * len(text.split("\n"))
            top = bottom + int(H * 0.03)
            width = int(W * (tw or cfg["tw"]))
            dev = _fd(path, cfg, width, skin)
            if ring:
                cy = top + dev.height // 3
                r2 = min(int(W * 0.78), cy - bottom - int(H * 0.01))   # keep the ring below the headline
                r = int(r2 * 0.80)
                d = ImageDraw.Draw(bg)
                d.ellipse((W // 2 - r, cy - r, W // 2 + r, cy + r), fill=pal["glow"] + (55,))
                d.ellipse((W // 2 - r2, cy - r2, W // 2 + r2, cy + r2), outline=pal["glow"] + (90,), width=max(3, W // 220))
            else:
                b.glow(bg, W // 2, top + dev.height // 3, int(W * 0.55), int(H * 0.28), pal["glow"], 70)
            _title(bg, text, W // 2, int(H * 0.05), fs, pal["ink"])
            sd = b.shadowed(dev)
            sign = -1 if i % 2 == 0 else 1
            if tilt:
                sd = sd.rotate(sign * tilt, resample=Image.BICUBIC, expand=True)
            cx = W // 2 + int(sign * shift * W)
            b._paste_clipped(bg, sd, cx - sd.width // 2, top - (sd.height - dev.height) // 2)
            out.append((_name(i, slug), bg.convert("RGB")))
        return out, None
    return run


def floating(pal, skin=None):
    """The whole device visible, smaller, floating under the headline with generous margins."""
    def run(cfg, files):
        W, H = cfg["size"]
        fs = int(W * cfg["fs"])
        out = []
        for i, (path, slug, text) in enumerate(files):
            bg = _canvas(pal, (W, H))
            bottom = _title(bg, text, W // 2, int(H * 0.05), fs, pal["ink"])
            avail = H - bottom - int(H * 0.06)
            dev = _fd(path, cfg, int(W * 0.70), skin)
            if dev.height > avail:                               # keep the full device on canvas
                width = int(dev.width * avail / dev.height)
                dev = _fd(path, cfg, width, skin)
            top = bottom + int(H * 0.025)
            b.glow(bg, W // 2, top + dev.height // 2, int(W * 0.5), int(H * 0.25), pal["glow"], 50)
            sd = b.shadowed(dev, spread=0.035, alpha=110 if _is_light(pal) else 150)
            b._paste_clipped(bg, sd, (W - sd.width) // 2, top - (sd.height - dev.height) // 2)
            out.append((_name(i, slug), bg.convert("RGB")))
        return out, None
    return run


def duo(pal, skin=None):
    """Two devices per image: this screen in front, the next one peeking behind it."""
    def run(cfg, files):
        W, H = cfg["size"]
        fs = int(W * cfg["fs"])
        out = []
        for i, (path, slug, text) in enumerate(files):
            bg = _canvas(pal, (W, H))
            bottom = _title(bg, text, W // 2, int(H * 0.05), fs, pal["ink"])
            top = bottom + int(H * 0.05)
            nxt = files[(i + 1) % len(files)][0]
            b.glow(bg, W // 2, top + int(H * 0.25), int(W * 0.55), int(H * 0.28), pal["glow"], 60)

            back = b.shadowed(_fd(nxt, cfg, int(W * 0.62), skin)).rotate(8, resample=Image.BICUBIC, expand=True)
            b._paste_clipped(bg, back, int(W * 0.68) - back.width // 2, top + int(H * 0.07))
            front = b.shadowed(_fd(path, cfg, int(W * 0.68), skin)).rotate(-5, resample=Image.BICUBIC, expand=True)
            b._paste_clipped(bg, front, int(W * 0.40) - front.width // 2, top - int(H * 0.01))
            out.append((_name(i, slug), bg.convert("RGB")))
        return out, None
    return run


def poster(pal, skin=None):
    """Device fills the canvas from the top; a huge headline sits over a fade at the bottom."""
    def run(cfg, files):
        W, H = cfg["size"]
        fs = int(W * cfg["fs"] * 1.2)
        out = []
        for i, (path, slug, text) in enumerate(files):
            bg = _canvas(pal, (W, H))
            dev = _fd(path, cfg, int(W * 0.82), skin)
            b.glow(bg, W // 2, int(H * 0.3), int(W * 0.55), int(H * 0.28), pal["glow"], 60)
            sd = b.shadowed(dev)
            b._paste_clipped(bg, sd, (W - sd.width) // 2, int(H * 0.035) - (sd.height - dev.height) // 2)
            # fade from transparent to the palette's last colour behind the headline
            end = pal["bg"][-1]
            fade = Image.new("RGBA", (W, H), end + (0,))
            alpha = b.gradient((W, H), [(0, 0, 0), (0, 0, 0), (0, 0, 0), (210, 210, 210), (245, 245, 245)]).convert("L")
            fade.putalpha(alpha)
            bg.alpha_composite(fade)
            lines = text.split("\n")
            total = int(fs * 1.2) * len(lines)
            _title(bg, text, W // 2, H - int(H * 0.045) - total, fs, pal["ink"], maxw=W * 0.92)
            out.append((_name(i, slug), bg.convert("RGB")))
        return out, None
    return run


def slim(pal, skin=None):
    """Barely-there bezel and a heavy shadow: the screen itself is the hero."""
    def run(cfg, files):
        W, H = cfg["size"]
        fs = int(W * cfg["fs"])
        slim_cfg = dict(cfg, bezel=0.001)
        out = []
        for i, (path, slug, text) in enumerate(files):
            bg = _canvas(pal, (W, H))
            bottom = _title(bg, text, W // 2, int(H * 0.05), fs, pal["ink"])
            top = bottom + int(H * 0.035)
            dev = _fd(path, slim_cfg, int(W * 0.84), skin)
            b.glow(bg, W // 2, top + dev.height // 3, int(W * 0.5), int(H * 0.25), pal["glow"], 60)
            sd = b.shadowed(dev, spread=0.05, alpha=190)
            b._paste_clipped(bg, sd, (W - sd.width) // 2, top - (sd.height - dev.height) // 2)
            out.append((_name(i, slug), bg.convert("RGB")))
        return out, None
    return run


def panorama(pal, skin=None):
    """One wide canvas; devices alternately tilted, a glow ribbon through them, sliced into panels."""
    def run(cfg, files):
        W, H = cfg["size"]
        n = len(files)
        s = pal["bg"]
        full = b.gradient((W * n, H), [s[0], s[2], s[1], s[2], s[0], s[1], s[2], s[0]], horizontal=True).convert("RGBA")
        if not _is_light(pal):                                       # darken top/bottom on dark palettes
            shade = Image.new("RGBA", full.size, (0, 0, 0, 0))
            shade.putalpha(b.gradient(full.size, [(70, 70, 70), (0, 0, 0), (60, 60, 60)]).convert("L"))
            full.alpha_composite(shade)

        fs = int(W * cfg["fs"])
        tw = int(W * 0.86)
        tilts = [-8, 8] * 4
        lift = [0.0, 0.05] * 4

        ribbon = Image.new("RGBA", full.size, (0, 0, 0, 0))
        rd = ImageDraw.Draw(ribbon)
        pts = [(x, int(H * 0.60 + math.sin(x / (W * 1.7) * math.pi * 2) * H * 0.10))
               for x in range(0, full.width, 20)]
        rd.line(pts, fill=pal["glow"] + (110,), width=int(H * 0.07), joint="curve")
        full.alpha_composite(ribbon.filter(ImageFilter.GaussianBlur(H * 0.03)))
        rd.line(pts, fill=(255, 255, 255, 130), width=int(H * 0.004), joint="curve")
        full.alpha_composite(ribbon.filter(ImageFilter.GaussianBlur(2)))

        for i, (path, slug, text) in enumerate(files):
            dev = _fd(path, cfg, tw, skin)
            sd = b.shadowed(dev, alpha=110 if _is_light(pal) else 140).rotate(tilts[i % 8], resample=Image.BICUBIC, expand=True)
            cx = int(W * (i + 0.5))
            b._paste_clipped(full, sd, cx - sd.width // 2,
                             int(H * (0.215 + lift[i % 8])) - (sd.height - dev.height) // 2)
        for i, (path, slug, text) in enumerate(files):
            _title(full, text, int(W * (i + 0.5)), int(H * 0.05), fs, pal["ink"])

        panels = [(_name(i, slug), full.crop((i * W, 0, (i + 1) * W, H)).convert("RGB"))
                  for i, (_, slug, _) in enumerate(files)]
        return panels, full
    return run


# ── The catalogue ────────────────────────────────────────────────────────────
# (number, folder name, one-line description, layout function)
# 1 and 2 keep the original folder names so earlier uploads still make sense.
# Phone type / case: the skin= argument (see SKINS in make_store_images.py), or override every
# style at run time with --skin, or pick one in the menu.
P = PALETTES
STYLES = [
    (1,  "classic",          "Ocean gradient, headline on top, device bleeding off the bottom",   bleed(P["ocean"])),
    (2,  "panorama",         "Ocean, one angled strip that spans all 8 images",                   panorama(P["ocean"])),
    (3,  "clean-light",      "Bright and minimal, whole device floating, silver phone",           floating(P["paper"], skin="silver")),
    (4,  "sunset-tilt",      "Warm sunset gradient, tilted devices, titanium phone",              bleed(P["sunset"], tw=0.80, tilt=7, shift=0.03, skin="titanium")),
    (5,  "grape-duo",        "Purple, two devices per image: this screen plus the next",          duo(P["grape"])),
    (6,  "mint-circle",      "Fresh mint, halo circle, phone in a coral-red case",                bleed(P["mint"], ring=True, skin="case-red")),
    (7,  "midnight-poster",  "Near-black, device up top, huge headline at the bottom",            poster(P["midnight"])),
    (8,  "rose-slim",        "Soft rose, ultra-slim bezel, deep shadow",                          slim(P["rose"])),
    (9,  "forest-classic",   "Deep green classic layout, gold phone",                             bleed(P["forest"], skin="gold")),
    (10, "sunset-panorama",  "Sunset colours, spanning panorama, blue phones",                    panorama(P["sunset"], skin="blue")),
    (11, "paper-panorama",   "Light, airy spanning panorama, white phones",                       panorama(P["paper"], skin="white")),
    (12, "grape-tilt",       "Purple, tilted devices with a halo ring, clear case",               bleed(P["grape"], tw=0.80, tilt=6, shift=0.02, ring=True, skin="case-clear")),
    (13, "ocean-duo",        "Ocean, two devices per image, navy cases",                          duo(P["ocean"], skin="case-navy")),
    (14, "mint-float",       "Mint, whole device floating, mint case",                            floating(P["mint"], skin="case-mint")),
    (15, "midnight-slim",    "Dark with a slim bezel, screen as the hero",                        slim(P["midnight"])),
    (16, "rose-circle",      "Rose with a halo circle, rugged case",                              bleed(P["rose"], ring=True, skin="case-rugged")),
    (17, "forest-poster",    "Green, huge headline at the bottom, notch phone",                   poster(P["forest"], skin="notch")),
    (18, "paper-duo",        "Light, two devices per image, silver phones",                       duo(P["paper"], skin="silver")),
    (19, "ocean-rugged",     "Ocean classic with a thick rugged case",                            bleed(P["ocean"], skin="case-rugged")),
    (20, "sunset-float",     "Sunset, whole device floating, clear case",                         floating(P["sunset"], skin="case-clear")),
]
