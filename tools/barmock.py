"""Renders the ATH run bar from the same numbers RunOverlay.cs uses.

This is a layout check, not a renderer check: the geometry, the shares and the
constants are copied from the C#, the font is not Imui's, and the medals are the
fallback dots rather than the game's art.
"""
import os
from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(REPO, "tools", "out")
os.makedirs(OUT, exist_ok=True)

SCREEN_W, SCREEN_H = 1920, 1080

# Imui theme (ImThemeBuiltin, the 23pt variant Zeepkist uses)
TEXT_SIZE = 23.0
SPACING = 5.0
INNER = 6.5
ROW = 32.0  # lineHeight(23) + ExtraRowHeight, measured about here

# RunOverlay.cs constants
WIDTH_FRACTION, MIN_W, MAX_W = 0.58, 640.0, 1180.0
CONTENT_ROWS = 2.7
MINIMAL_ROWS = 1.9
SWAP_AT = 0.5
RULE_FRACTION = 0.16
CHAMFER_FRACTION = 0.45
BADGE_ASPECT = 16.0 / 9.0
PENNANT_WIDTH_FRACTION = 0.34
PENNANT_ROWS = 0.55
CLOCK_SHARE = 0.55
CLOCK_SIZE = 1.3
TITLE_SHARE = 0.5
CLOCK_COLUMNS = 6.4
MEDAL_COLUMNS = 7.4
SKIP_COLUMNS = 9.4
LEVEL_TIME_COLUMNS = 11.4

# SplitsOverlay.cs
SPLITS_FRACTION, SPLITS_MIN_W, SPLITS_MAX_W = 0.18, 240.0, 380.0
SPLITS_CHROME_ROWS = 2.7
SPLITS_CHAMFER = 0.09
SPLITS_RULE_ROWS = 0.35
SPLITS_WEIGHTS = [0.13, 0.42, 0.28, 0.17]
SPLITS_HEIGHT_CAP = 0.7

# ControlPanel.cs
TILE_ASPECT = 1.05
TILE_ROWS = 1.25
CAPTION_ROWS = 0.75
BUTTONS = 5

# ColorExtensions.cs
PANEL = (12, 14, 18, 214)
TRACK = (255, 255, 255, 38)
TILE = (255, 255, 255, 20)
WHITE = (255, 255, 255, 255)
MUTED = (153, 153, 153, 255)
LEVEL_NAME = (100, 210, 255, 255)
AUTHOR_NAME = (255, 215, 0, 255)
JOINER = (255, 150, 60, 255)
MEDAL_AUTHOR = (134, 56, 147, 255)
MEDAL_GOLD = (255, 214, 0, 255)
GOOD = (66, 179, 54, 255)
ALERT = (255, 0, 0, 255)
PENALTY = (200, 90, 60, 255)
FREE_SKIP = (90, 170, 220, 255)
POSITIVE = (80, 228, 81, 255)
BAD = (255, 90, 90, 255)
ACTION = {
    "Skip": (46, 104, 168, 255),
    "Broken": (168, 106, 34, 255),
    "Pause": (140, 118, 26, 255),
    "Restart": (78, 78, 122, 255),
    "Stop": (150, 46, 46, 255),
}

# UiIconAtlas.cs: the strip tools/iconatlas.py bakes, one cell per UiIcon in that enum's order.
ATLAS = os.path.join(REPO, "assets", "icons", "icons.png")
ICON_ORDER = ["Play", "Pause", "Stop", "Skip", "Restart", "Warning", "Info", "Stopwatch"]

# ControlPanel.cs: which icon each of the five buttons carries.
BUTTON_ICON = {"Skip": "Skip", "Broken": "Warning", "Pause": "Pause", "Restart": "Restart",
               "Stop": "Stop"}

FONT_PATH = r"C:\Windows\Fonts\segoeuib.ttf"
FONT_REG = r"C:\Windows\Fonts\segoeui.ttf"


def font(size, bold=True):
    return ImageFont.truetype(FONT_PATH if bold else FONT_REG, int(round(size)))


def text(d, rect, s, colour, size, align="left", bold=True):
    """Mirrors UiText: vertically centred in the rect, aligned on x."""
    x, y, w, h = rect
    f = font(size, bold)
    box = d.textbbox((0, 0), s, font=f)
    tw, th = box[2] - box[0], box[3] - box[1]
    ty = y + (h - th) / 2 - box[1]
    tx = {"left": x, "center": x + (w - tw) / 2, "right": x + w - tw}[align]
    d.text((tx, ty), s, font=f, fill=colour)


def medal(d, rect, colour):
    """The fallback dot UiWidgets.Medal draws when the game art is not loaded."""
    x, y, w, h = rect
    r = min(w, h) * 0.3
    cx, cy = x + w / 2, y + h / 2
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=colour)


def stopwatch(d, rect, colour):
    x, y, w, h = rect
    t = max(1.0, w * 0.09)
    radius = w * 0.5 - t * 0.5
    crown = w * 0.22
    cx, cy = x + w / 2, y + h - radius
    d.rectangle([cx - crown / 2, y, cx + crown / 2, y + t * 1.6], fill=colour)
    d.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], outline=colour, width=int(round(t)))
    d.line([cx, cy, cx + radius * 0.42, cy - radius * 0.52], fill=colour, width=int(round(t)))


_cells = {}


def icon_cell(name):
    if not _cells:
        strip = Image.open(ATLAS).convert("RGBA")
        side = strip.height
        for i, key in enumerate(ICON_ORDER):
            _cells[key] = strip.crop((i * side, 0, (i + 1) * side, side))

    return _cells[name]


def glyph(layer, rect, kind, colour):
    """What UiIcons.Draw does: the atlas cell, squared into the rect at full size, tinted."""
    x, y, w, h = rect
    side = int(round(min(w, h)))
    if side <= 0:
        return
    cell = icon_cell(BUTTON_ICON[kind]).resize((side, side), Image.LANCZOS)
    tinted = Image.new("RGBA", (side, side), tuple(colour))
    tinted.putalpha(cell.getchannel("A"))
    layer.alpha_composite(tinted, (int(round(x + (w - side) / 2)), int(round(y + (h - side) / 2))))


def rounded(d, rect, colour, radius, outline=None, width=1):
    x, y, w, h = rect
    d.rounded_rectangle([x, y, x + w, y + h], radius=radius, fill=colour, outline=outline, width=width)


def take_left(rect, amount, gap=0.0):
    x, y, w, h = rect
    return (x, y, amount, h), (x + amount + gap, y, w - amount - gap, h)


def take_right(rect, amount, gap=0.0):
    x, y, w, h = rect
    return (x + w - amount, y, amount, h), (x, y, w - amount - gap, h)


def take_top(rect, amount, gap=0.0):
    x, y, w, h = rect
    return (x, y, w, amount), (x, y + amount + gap, w, h - amount - gap)


def column(rect, index, count, gap=INNER):
    x, y, w, h = rect
    cw = (w - gap * (count - 1)) / count
    return (x + index * (cw + gap), y, cw, h)


def background(img):
    """Something to judge contrast against: a sky, a road, a bit of scenery."""
    d = ImageDraw.Draw(img)
    for i in range(SCREEN_H):
        t = i / SCREEN_H
        d.line([(0, i), (SCREEN_W, i)], fill=(int(96 + 90 * t), int(140 + 70 * t), int(190 + 40 * t)))
    d.polygon([(0, 1080), (1920, 1080), (1250, 470), (700, 470)], fill=(120, 118, 112))
    d.polygon([(0, 1080), (330, 1080), (760, 470), (700, 470)], fill=(96, 140, 76))
    d.polygon([(1600, 1080), (1920, 1080), (1250, 470), (1195, 470)], fill=(96, 140, 76))
    for k in range(9):
        yy = 470 + k * k * 9
        d.rectangle([955 - k * 1.6, yy, 965 + k * 1.6, yy + 6 + k], fill=(240, 240, 240))


def draw_bar(img, open_amount, stage=1.0):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    wide = stage >= SWAP_AT
    pad = SPACING
    rule = max(3.0, ROW * RULE_FRACTION) * stage
    band_h = ROW * (MINIMAL_ROWS + (CONTENT_ROWS - MINIMAL_ROWS) * stage) + pad * 2 + rule
    width = min(max(SCREEN_W * WIDTH_FRACTION, MIN_W), MAX_W)
    bx = (SCREEN_W - width) / 2
    cut = min(band_h * CHAMFER_FRACTION, width * 0.03)
    pennant_h = ROW * PENNANT_ROWS

    # ---- drawer (behind the band) -------------------------------------
    skip_row = ROW * CAPTION_ROWS
    strip_h = ROW * TILE_ROWS
    full = skip_row + SPACING + strip_h + SPACING + SPACING * 2
    drawer_h = full * open_amount
    if drawer_h > 0:
        clip = Image.new("RGBA", img.size, (0, 0, 0, 0))
        cd = ImageDraw.Draw(clip)
        top = band_h + drawer_h - full
        dcut = min(cut, drawer_h)
        cd.polygon([(bx + dcut, band_h + drawer_h), (bx + width - dcut, band_h + drawer_h),
                    (bx + width, band_h + drawer_h - dcut), (bx + width, band_h),
                    (bx, band_h), (bx, band_h + drawer_h - dcut)], fill=PANEL)
        inner_x = bx + SPACING * 2
        inner_w = width - SPACING * 4
        text(cd, (inner_x, top + SPACING, inner_w, skip_row), "Penalty Skip", PENALTY,
             TEXT_SIZE * 0.8, "center")
        side = strip_h * TILE_ASPECT
        total = side * BUTTONS + INNER * (BUTTONS - 1)
        sx = inner_x + (inner_w - total) / 2
        sy = top + SPACING + skip_row + SPACING
        for i, name in enumerate(["Skip", "Broken", "Pause", "Restart", "Stop"]):
            tx = sx + i * (side + INNER)
            rounded(cd, (tx, sy, side, strip_h), ACTION[name], strip_h * 0.16)
            tp = strip_h * 0.12
            label = min(TEXT_SIZE * 0.75, strip_h * 0.26)
            lh = label * 1.2
            glyph(clip, (tx + tp, sy + tp, side - tp * 2, strip_h - tp * 2 - lh), name, WHITE)
            text(cd, (tx + tp, sy + strip_h - tp - lh, side - tp * 2, lh), name, WHITE, label, "center")
        mask = Image.new("L", img.size, 0)
        ImageDraw.Draw(mask).rectangle([bx, band_h, bx + width, band_h + drawer_h], fill=255)
        layer.paste(clip, (0, 0), mask)
        d = ImageDraw.Draw(layer)

    # ---- band ----------------------------------------------------------
    d.polygon([(bx + cut, band_h), (bx + width - cut, band_h), (bx + width, band_h - cut),
               (bx + width, 0), (bx, 0), (bx, band_h - cut)], fill=PANEL)

    # ---- content -------------------------------------------------------
    ix = bx + pad + cut
    iy = pad
    iw = width - (pad + cut) * 2
    ih = band_h - pad - (rule + pad)

    badge_w = ih * BADGE_ASPECT
    badge = ((bx + width / 2) - badge_w / 2, iy, badge_w, ih)
    wing = (iw - badge_w) / 2 - pad

    # pennant
    pw = badge_w * PENNANT_WIDTH_FRACTION
    cx = bx + width / 2
    d.polygon([(cx - pw / 2, band_h), (cx - pw / 2, band_h + pennant_h * 0.45),
               (cx, band_h + pennant_h), (cx + pw / 2, band_h + pennant_h * 0.45),
               (cx + pw / 2, band_h)], fill=MEDAL_GOLD)

    # badge
    rounded(d, badge, TILE, badge[3] * 0.14, outline=MEDAL_GOLD, width=max(1, int(badge[3] * 0.045)))
    logo_path = os.path.join(REPO, "assets", "thumbnail", "ATH_16-9.png")
    if os.path.exists(logo_path):
        p = max(2.0, badge[3] * 0.09)
        box_w, box_h = badge[2] - p * 2, badge[3] - p * 2
        logo = Image.open(logo_path).convert("RGBA")
        scale = min(box_w / logo.width, box_h / logo.height)
        logo = logo.resize((max(1, int(logo.width * scale)), max(1, int(logo.height * scale))), Image.LANCZOS)
        layer.paste(logo, (int(badge[0] + p + (box_w - logo.width) / 2),
                           int(badge[1] + p + (box_h - logo.height) / 2)), logo)
        d = ImageDraw.Draw(layer)

    # left wing
    hunt = (ix, iy, wing, ih)
    clock_r, below_r = take_top(hunt, ih * CLOCK_SHARE)
    clock_r, _ = take_left(clock_r, min(clock_r[2], ROW * CLOCK_COLUMNS))
    medals_r, _ = take_left(below_r, min(below_r[2], ROW * MEDAL_COLUMNS))
    skip_r, _ = take_left(below_r, min(below_r[2], ROW * SKIP_COLUMNS))
    icon = clock_r[3] * 0.8
    watch, rest = take_left(clock_r, icon, INNER)
    light, clock = take_right(rest, icon, INNER)
    stopwatch(d, watch, GOOD)
    text(d, clock, "41:07", GOOD, TEXT_SIZE * CLOCK_SIZE, "left")
    lr = light[3] * 0.25
    d.ellipse([light[0] + light[2] / 2 - lr, light[1] + light[3] / 2 - lr,
               light[0] + light[2] / 2 + lr, light[1] + light[3] / 2 + lr], fill=ALERT)
    if wide:
        for i, (col, n) in enumerate([(MEDAL_AUTHOR, "3"), (MEDAL_GOLD, "5"), (PENALTY, "1")]):
            cell = column(medals_r, i, 3)
            isz = min(cell[3], cell[2] * 0.5)
            ic, restc = take_left(cell, isz, INNER)
            medal(d, ic, col)
            text(d, restc, n, col, TEXT_SIZE * 1.5, "left")
    else:
        gi = skip_r[3] * 0.7
        gr, lr_ = take_left(skip_r, gi, INNER)
        glyph(layer, gr, "Skip", FREE_SKIP)
        d = ImageDraw.Draw(layer)
        text(d, lr_, "Free Skip (2x)", FREE_SKIP, TEXT_SIZE * 0.9, "left")

    # right wing
    lvl = (ix + iw - wing, iy, wing, ih)
    title_r, times_r = take_top(lvl, ih * TITLE_SHARE)

    # One line, measured from the right: author, then " by ", then the level name.
    size = TEXT_SIZE * 1.05
    f = font(size)
    aw = d.textlength("Maki", font=f)
    bw = d.textlength(" by ", font=f)
    text(d, title_r, "Maki", WHITE, size, "right")
    text(d, (title_r[0], title_r[1], title_r[2] - aw, title_r[3]), " by ", JOINER, size, "right")
    text(d, (title_r[0], title_r[1], title_r[2] - aw - bw, title_r[3]), "Skyline Sprint", WHITE, size, "right")

    times_r, _ = take_right(times_r, min(times_r[2], ROW * LEVEL_TIME_COLUMNS))
    for i, (col, tm) in enumerate([(MEDAL_AUTHOR, "00:24.148"), (MEDAL_GOLD, "00:27.600")]):
        cell = column(times_r, i, 2)
        ic, restc = take_left(cell, cell[3], INNER)
        medal(d, ic, col)
        text(d, restc, tm, col, TEXT_SIZE * 0.9, "left")

    # timeline rule
    if wide:
        tl = (bx + cut, pad * 0.4 + ih + pad, width - cut * 2, rule)
        rounded(d, tl, TRACK, rule / 2)
        segs = [(0.22, MEDAL_AUTHOR), (0.14, MEDAL_GOLD), (0.09, FREE_SKIP), (0.11, GOOD), (0.06, (191, 57, 57, 255))]
        sx = tl[0]
        for frac, col in segs:
            w = tl[2] * frac
            rounded(d, (sx, tl[1], max(1, w - 2), tl[3]), col, rule / 2)
            sx += w

    return Image.alpha_composite(img.convert("RGBA"), layer)


# SplitsOverlay.cs: the list down the left edge, one row per checkpoint plus the finish.
SPLIT_ROWS = [
    ("1", "00:04.221", "-0.113", POSITIVE, "78", POSITIVE),
    ("2", "00:09.874", "+0.042", BAD, "64", BAD),
    ("3", "00:14.500", "-0.006", POSITIVE, "71", POSITIVE),
    ("4", "--:--.---", "", MUTED, "", MUTED),
    ("5", "--:--.---", "", MUTED, "", MUTED),
    ("F", "--:--.---", "", MUTED, "", MUTED),
]


def cell(rect, index):
    x, y, w, h = rect
    offset = sum(SPLITS_WEIGHTS[:index])
    return (x + w * offset, y, w * SPLITS_WEIGHTS[index], h)


def draw_splits(img, slide=1.0):
    """Built like the band: no window frame, a cut-cornered shape, docked to the left edge."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    rows = len(SPLIT_ROWS)
    lines = rows + SPLITS_CHROME_ROWS
    pad = SPACING
    allowed = SCREEN_H * SPLITS_HEIGHT_CAP - (ROW + SPACING * 2)
    line = ROW if ROW * lines <= allowed else allowed / lines

    content = line * lines
    width = min(max(SCREEN_W * SPLITS_FRACTION, SPLITS_MIN_W), SPLITS_MAX_W)
    height = content + pad * 2
    x = -width + width * slide
    y = (SCREEN_H - height) / 2
    cut = min(width, height) * SPLITS_CHAMFER

    d.polygon([(x, y), (x + width - cut, y), (x + width, y + cut),
               (x + width, y + height - cut), (x + width - cut, y + height), (x, y + height)], fill=PANEL)

    area = (x + pad, y + pad, width - pad * 2 - cut, content)
    top = area[1]

    def row(scale):
        nonlocal top
        r = (area[0], top, area[2], line * scale)
        top += line * scale
        return r

    text(d, row(1.0), "SPLITS", MUTED, TEXT_SIZE * 0.8, "left")
    rule_r = row(SPLITS_RULE_ROWS)
    d.rectangle([rule_r[0], rule_r[1] + rule_r[3] * 0.4, rule_r[0] + rule_r[2], rule_r[1] + rule_r[3] * 0.6],
                fill=TRACK)

    for i, (label, tm, gap, gap_col, speed, speed_col) in enumerate(SPLIT_ROWS):
        if i == rows - 1:
            rule_r = row(SPLITS_RULE_ROWS)
            d.rectangle([rule_r[0], rule_r[1] + rule_r[3] * 0.4, rule_r[0] + rule_r[2], rule_r[1] + rule_r[3] * 0.6],
                        fill=TRACK)
        r = row(1.0)
        text(d, cell(r, 0), label, MUTED, TEXT_SIZE * 0.9, "left")
        text(d, cell(r, 1), tm, WHITE if tm != "--:--.---" else MUTED, TEXT_SIZE, "left")
        text(d, cell(r, 2), gap, gap_col, TEXT_SIZE, "right")
        text(d, cell(r, 3), speed, speed_col, TEXT_SIZE * 0.9, "right")

    return Image.alpha_composite(img.convert("RGBA"), layer)


def render(open_amount, name, stage=1.0, splits=False, slide=1.0):
    img = Image.new("RGB", (SCREEN_W, SCREEN_H))
    background(img)
    out = draw_bar(img, open_amount, stage)
    if splits:
        out = draw_splits(out, slide)
    out.convert("RGB").save(os.path.join(OUT, name + "_full.png"))
    out.convert("RGB").crop((250, 0, 1670, 300)).save(os.path.join(OUT, name + "_detail.png"))


render(0.0, "shut")
render(1.0, "open")
render(0.0, "driving", stage=0.0, splits=True)
render(0.0, "between", stage=1.0, splits=True)
render(0.0, "arriving", stage=0.0, splits=True, slide=0.45)
render(1.0, "mouse", stage=1.0, splits=True)
print("ok")
