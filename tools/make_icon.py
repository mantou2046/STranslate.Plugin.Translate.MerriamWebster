"""Generate the Merriam-Webster plugin icon (icon.png).

Design: dark navy rounded square with a white book/dictionary mark and a
red "MW"-style accent bar echoing the Merriam-Webster brand colour.
"""

from PIL import Image, ImageDraw, ImageFont
import os

SIZE = 128
SCALE = 4  # supersample for smooth edges
S = SIZE * SCALE

BG_TOP = (26, 42, 66)       # deep navy
BG_BOTTOM = (16, 26, 44)    # darker navy
ACCENT = (196, 30, 45)      # MW red
PAPER = (247, 249, 252)
PAPER_EDGE = (206, 214, 228)


def rounded_mask(size, radius):
    mask = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(mask)
    d.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def vertical_gradient(size, top, bottom):
    grad = Image.new("RGB", (1, size))
    for y in range(size):
        t = y / max(1, size - 1)
        grad.putpixel(
            (0, y),
            (
                round(top[0] + (bottom[0] - top[0]) * t),
                round(top[1] + (bottom[1] - top[1]) * t),
                round(top[2] + (bottom[2] - top[2]) * t),
            ),
        )
    return grad.resize((size, size))


def main():
    canvas = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # Background with gradient + rounded corners
    bg = vertical_gradient(S, BG_TOP, BG_BOTTOM).convert("RGBA")
    bg.putalpha(rounded_mask(S, radius=int(S * 0.22)))
    canvas.alpha_composite(bg)

    d = ImageDraw.Draw(canvas)

    # Open book: two facing pages
    pad_x = S * 0.17
    top = S * 0.30
    bottom = S * 0.78
    mid = S * 0.5
    curve = S * 0.035

    # Left page
    d.polygon(
        [
            (pad_x, top + curve),
            (mid, top),
            (mid, bottom),
            (pad_x, bottom - curve * 0.6),
        ],
        fill=PAPER,
    )
    # Right page
    d.polygon(
        [
            (S - pad_x, top + curve),
            (mid, top),
            (mid, bottom),
            (S - pad_x, bottom - curve * 0.6),
        ],
        fill=PAPER if False else (232, 238, 248),
    )

    # Spine shadow
    d.line([(mid, top), (mid, bottom)], fill=PAPER_EDGE, width=max(1, int(S * 0.012)))

    # Text lines on the pages
    line_w = int(S * 0.012)
    for i in range(3):
        y = top + S * 0.12 + i * S * 0.11
        d.line(
            [(pad_x + S * 0.05, y), (mid - S * 0.05, y - S * 0.012)],
            fill=(150, 163, 184),
            width=line_w,
        )
        d.line(
            [(mid + S * 0.05, y - S * 0.012), (S - pad_x - S * 0.05, y)],
            fill=(150, 163, 184),
            width=line_w,
        )

    # Red accent bar (underline the book, brand-ish)
    bar_y = S * 0.845
    d.rounded_rectangle(
        (S * 0.28, bar_y, S * 0.72, bar_y + S * 0.055),
        radius=int(S * 0.028),
        fill=ACCENT,
    )

    canvas = canvas.resize((SIZE, SIZE), Image.LANCZOS)

    # 输出到插件项目目录，与脚本所在位置无关
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.join(here, "..", "STranslate.Plugin.Translate.MerriamWebster", "icon.png")
    out = os.path.normpath(out)
    canvas.save(out)
    print(f"{out} written", canvas.size)


if __name__ == "__main__":
    main()
