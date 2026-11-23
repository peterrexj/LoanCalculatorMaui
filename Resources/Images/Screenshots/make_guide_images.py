#!/usr/bin/env python3
"""
Turns the guide walkthrough captures into web-ready images for the user-guide page.

    python3 make_guide_images.py [output_dir]

Input   Captures/ios-iphone-6.9-guide/iOS-guide-NN-name.png   (UITests GuideCapture fixture)
Output  <output_dir>/NN-name.webp   framed iPhone, clean 9:41 status bar, transparent corners

Default output is the pages repo: ../../../../YourSimpleAppsPages/images/loanaffordcalc/guide
Shares the device frame and status-bar repaint with make_store_images.py, so guide and store
images look like the same app on the same phone.
"""
import glob
import os
import sys

from PIL import Image

import make_store_images as store

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "Captures", "ios-iphone-6.9-guide")
DEFAULT_OUT = os.path.abspath(os.path.join(
    HERE, "..", "..", "..", "..", "YourSimpleAppsPages", "images", "loanaffordcalc", "guide"))

WIDTH = 520      # px; shown at ~260-300 css px so it stays sharp on retina


def main():
    out = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_OUT
    os.makedirs(out, exist_ok=True)
    cfg = store.DEVICES["iphone"]

    files = sorted(glob.glob(os.path.join(SRC, "iOS-guide-*.png")))
    if not files:
        sys.exit(f"No guide captures in {SRC}")

    total = 0
    for path in files:
        name = os.path.basename(path)[len("iOS-guide-"):-len(".png")]       # 05-loan-overview
        dev = store.framed_device(path, cfg, WIDTH)
        # A little breathing room so the frame is not clipped by the page's image box.
        canvas = Image.new("RGBA", (dev.width + 16, dev.height + 16), (0, 0, 0, 0))
        canvas.alpha_composite(dev, (8, 8))
        dest = os.path.join(out, f"{name}.webp")
        canvas.save(dest, "WEBP", quality=86, method=6)
        total += os.path.getsize(dest)

    print(f"{len(files)} images, {total // 1024} KB -> {out}")


if __name__ == "__main__":
    main()
