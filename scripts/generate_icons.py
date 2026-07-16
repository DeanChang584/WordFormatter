"""Generate AppIcon.ico and PNG assets from WordFormatter_Logo.png"""
from PIL import Image
import os

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(BASE)
SRC = os.path.join(ROOT, "WordFormatter_Logo.png")
ASSETS = os.path.join(ROOT, "frontend", "Assets")

img = Image.open(SRC).convert("RGBA")

# ── 1. Generate AppIcon.ico (multi-res) ──────────────────────────────
ico_sizes = [16, 24, 32, 48, 64, 128, 256]
ico_images = []
for s in ico_sizes:
    resized = img.resize((s, s), Image.LANCZOS)
    # ICO requires 32-bit BGRA
    ico_images.append(resized)

ico_path = os.path.join(ASSETS, "AppIcon.ico")
ico_images[0].save(
    ico_path,
    format="ICO",
    sizes=[(s, s) for s in ico_sizes],
    append_images=ico_images[1:],
)
print(f"✓ {ico_path}  ({len(ico_sizes)} sizes)")

# ── 2. Generate PNG assets ──────────────────────────────────────────
# Package.appxmanifest references:
#   Square44x44Logo.png  (88×88 = 44×44 @ 200%)
#   Square150x150Logo.png  (300×300 = 150×150 @ 200%)
#   Wide310x150Logo.png  (620×300, crop centre)
#   StoreLogo.png  (100×100)
#   LockScreenLogo.scale-200.png  (160×160)
#   SplashScreen.scale-200.png  (keep existing, or generate 1240×600)

png_specs = [
    ("Square44x44Logo.scale-200.png", 88, 88, True),
    ("Square44x44Logo.targetsize-24_altform-unplated.png", 48, 48, True),
    ("Square44x44Logo.targetsize-48_altform-lightunplated.png", 96, 96, True),
    ("Square150x150Logo.scale-200.png", 300, 300, True),
    ("Wide310x150Logo.scale-200.png", 620, 300, False),
    ("StoreLogo.png", 100, 100, True),
    ("LockScreenLogo.scale-200.png", 160, 160, True),
    ("Logo.png", 400, 400, True),
    ("SplashScreen.scale-200.png", 1240, 600, False),
]

for name, w, h, square in png_specs:
    if square:
        resized = img.resize((w, h), Image.LANCZOS)
    else:
        # Crop centre to fit aspect ratio
        iw, ih = img.size
        target_ratio = w / h
        src_ratio = iw / ih
        if src_ratio > target_ratio:
            new_w = int(ih * target_ratio)
            offset = (iw - new_w) // 2
            crop = img.crop((offset, 0, offset + new_w, ih))
        else:
            new_h = int(iw / target_ratio)
            offset = (ih - new_h) // 2
            crop = img.crop((0, offset, iw, offset + new_h))
        resized = crop.resize((w, h), Image.LANCZOS)
    out = os.path.join(ASSETS, name)
    resized.save(out, "PNG")
    print(f"✓ {out}  ({w}×{h})")

print("\nDone!")