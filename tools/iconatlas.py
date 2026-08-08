"""Bakes the mod's button icons into one PNG strip.

The icons are Bootstrap Icons (MIT, see assets/icons/LICENSE), which is the point: they are a
set, drawn on one grid at one weight by people who do that for a living, and five buttons in a
row only look like five buttons when their symbols agree about how much ink a symbol is.

The strip is what the plugin embeds. Imui has no SVG and no icon font - a glyph the font does
not have is a tofu box on the player's screen - so the paths are rasterised here, once, into
white on transparent, and the game tints the white.

Run this after changing ICONS or CELL:

    python tools/iconatlas.py

It reads assets/icons/svg/*.svg and writes assets/icons/icons.png. Both are committed, so a
build never needs this script or a network.
"""

from __future__ import annotations

import math
import re
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
SVG_DIR = ROOT / "assets" / "icons" / "svg"
ATLAS = ROOT / "assets" / "icons" / "icons.png"

# The order is UiIcon's, minus None. The C# side indexes cells by that enum, so this list and
# UiIcon.cs are the same list written twice and have to stay that way.
ICONS = [
    ("Play", "play-fill"),
    ("Pause", "pause-fill"),
    ("Stop", "stop-fill"),
    ("Skip", "skip-forward-fill"),
    ("Restart", "arrow-clockwise"),
    ("Warning", "exclamation-triangle-fill"),
    ("Info", "info-circle-fill"),
    ("Stopwatch", "stopwatch-fill"),
]

# The side of one cell in the strip. Icons are drawn a few dozen pixels tall on a button, and
# this is the next power of two up from that.
CELL = 128

# How much bigger the cell is rasterised before it is scaled down. All the antialiasing comes
# from here; there is none in the polygon fill.
SUPERSAMPLE = 8

# Segments per curve. The curves are at most a cell wide, so this is well past the point where
# more of them changes a pixel.
SEGMENTS = 48

NUMBER = re.compile(r"[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?")
COMMAND = re.compile(r"([MmLlHhVvCcSsQqTtAaZz])")
PATH = re.compile(r"<path\b([^>]*)>", re.S)
ATTRIBUTE = re.compile(r'(\w[\w-]*)\s*=\s*"([^"]*)"', re.S)
VIEWBOX = re.compile(r'viewBox\s*=\s*"([^"]*)"')


class Pen:
    """Walks one path's commands and leaves a list of closed polygons behind it."""

    def __init__(self) -> None:
        self.subpaths: list[list[tuple[float, float]]] = []
        self.current: list[tuple[float, float]] = []
        self.x = 0.0
        self.y = 0.0
        self.start = (0.0, 0.0)
        # The last curve's second control point, for the shorthand S and T commands.
        self.reflect_cubic: tuple[float, float] | None = None
        self.reflect_quad: tuple[float, float] | None = None

    def close(self) -> None:
        if len(self.current) > 2:
            self.subpaths.append(self.current)

        self.current = []

    def move(self, x: float, y: float) -> None:
        self.close()
        self.x, self.y = x, y
        self.start = (x, y)
        self.current = [(x, y)]

    def line(self, x: float, y: float) -> None:
        self.x, self.y = x, y
        self.current.append((x, y))

    def cubic(self, x1: float, y1: float, x2: float, y2: float, x: float, y: float) -> None:
        x0, y0 = self.x, self.y

        for i in range(1, SEGMENTS + 1):
            t = i / SEGMENTS
            u = 1.0 - t
            px = u * u * u * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x
            py = u * u * u * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y
            self.current.append((px, py))

        self.x, self.y = x, y
        self.reflect_cubic = (x2, y2)
        self.reflect_quad = None

    def quad(self, x1: float, y1: float, x: float, y: float) -> None:
        x0, y0 = self.x, self.y

        for i in range(1, SEGMENTS + 1):
            t = i / SEGMENTS
            u = 1.0 - t
            px = u * u * x0 + 2 * u * t * x1 + t * t * x
            py = u * u * y0 + 2 * u * t * y1 + t * t * y
            self.current.append((px, py))

        self.x, self.y = x, y
        self.reflect_quad = (x1, y1)
        self.reflect_cubic = None

    def arc(self, rx: float, ry: float, rotation: float, large: bool, sweep: bool, x: float,
            y: float) -> None:
        """SVG's endpoint arc, turned into the centre form the sampling needs.

        Straight out of the implementation notes in the SVG specification, including the
        correction that scales the radii up when they are too small to reach the endpoint.
        """
        x0, y0 = self.x, self.y

        if rx == 0 or ry == 0 or (x0 == x and y0 == y):
            self.line(x, y)
            return

        rx, ry = abs(rx), abs(ry)
        phi = math.radians(rotation)
        cos_phi, sin_phi = math.cos(phi), math.sin(phi)

        dx = (x0 - x) / 2.0
        dy = (y0 - y) / 2.0
        x1 = cos_phi * dx + sin_phi * dy
        y1 = -sin_phi * dx + cos_phi * dy

        overshoot = (x1 * x1) / (rx * rx) + (y1 * y1) / (ry * ry)

        if overshoot > 1:
            scale = math.sqrt(overshoot)
            rx *= scale
            ry *= scale

        numerator = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1
        denominator = rx * rx * y1 * y1 + ry * ry * x1 * x1
        factor = math.sqrt(max(0.0, numerator / denominator))

        if large == sweep:
            factor = -factor

        cx1 = factor * rx * y1 / ry
        cy1 = -factor * ry * x1 / rx

        cx = cos_phi * cx1 - sin_phi * cy1 + (x0 + x) / 2.0
        cy = sin_phi * cx1 + cos_phi * cy1 + (y0 + y) / 2.0

        start = math.atan2((y1 - cy1) / ry, (x1 - cx1) / rx)
        end = math.atan2((-y1 - cy1) / ry, (-x1 - cx1) / rx)
        sweep_angle = end - start

        if not sweep and sweep_angle > 0:
            sweep_angle -= 2 * math.pi

        if sweep and sweep_angle < 0:
            sweep_angle += 2 * math.pi

        for i in range(1, SEGMENTS + 1):
            angle = start + sweep_angle * i / SEGMENTS
            px = cos_phi * rx * math.cos(angle) - sin_phi * ry * math.sin(angle) + cx
            py = sin_phi * rx * math.cos(angle) + cos_phi * ry * math.sin(angle) + cy
            self.current.append((px, py))

        self.x, self.y = x, y
        self.reflect_cubic = None
        self.reflect_quad = None


def tokenize(data: str) -> list[tuple[str, list[float]]]:
    """The path's d attribute as (command letter, its numbers) pairs, in order."""
    parts = [part for part in COMMAND.split(data) if part.strip()]
    steps: list[tuple[str, list[float]]] = []

    index = 0

    while index < len(parts):
        letter = parts[index]
        index += 1

        numbers: list[float] = []

        if index < len(parts) and not COMMAND.fullmatch(parts[index]):
            numbers = [float(match) for match in NUMBER.findall(parts[index])]
            index += 1

        steps.append((letter, numbers))

    return steps


# How many numbers each command consumes before it repeats. A command letter followed by more
# than that many numbers is the same command again, which is how "m" starts a subpath and then
# draws lines.
ARITY = {"M": 2, "L": 2, "H": 1, "V": 1, "C": 6, "S": 4, "Q": 4, "T": 2, "A": 7, "Z": 0}


def walk(pen: Pen, letter: str, numbers: list[float]) -> None:
    relative = letter.islower()
    upper = letter.upper()

    if upper == "Z":
        pen.line(*pen.start)
        pen.close()
        pen.x, pen.y = pen.start
        return

    size = ARITY[upper]
    chunks = [numbers[i:i + size] for i in range(0, len(numbers), size)] or [[]]

    for position, chunk in enumerate(chunks):
        if len(chunk) < size:
            continue

        # A repeated M is an L, which is the one place the letter does not mean what it says.
        step = "L" if upper == "M" and position > 0 else upper

        apply(pen, step, chunk, relative)


def apply(pen: Pen, step: str, chunk: list[float], relative: bool) -> None:
    dx = pen.x if relative else 0.0
    dy = pen.y if relative else 0.0

    if step == "M":
        pen.move(chunk[0] + dx, chunk[1] + dy)
        return

    if step == "L":
        pen.line(chunk[0] + dx, chunk[1] + dy)
        return

    if step == "H":
        pen.line(chunk[0] + dx, pen.y)
        return

    if step == "V":
        pen.line(pen.x, chunk[0] + dy)
        return

    if step == "C":
        pen.cubic(chunk[0] + dx, chunk[1] + dy, chunk[2] + dx, chunk[3] + dy, chunk[4] + dx,
                  chunk[5] + dy)
        return

    if step == "S":
        x1, y1 = reflected(pen, pen.reflect_cubic)
        pen.cubic(x1, y1, chunk[0] + dx, chunk[1] + dy, chunk[2] + dx, chunk[3] + dy)
        return

    if step == "Q":
        pen.quad(chunk[0] + dx, chunk[1] + dy, chunk[2] + dx, chunk[3] + dy)
        return

    if step == "T":
        x1, y1 = reflected(pen, pen.reflect_quad)
        pen.quad(x1, y1, chunk[0] + dx, chunk[1] + dy)
        return

    if step == "A":
        pen.arc(chunk[0], chunk[1], chunk[2], chunk[3] != 0, chunk[4] != 0, chunk[5] + dx,
                chunk[6] + dy)


def reflected(pen: Pen, control: tuple[float, float] | None) -> tuple[float, float]:
    """The shorthand curves' implied control point: the last one mirrored through the cursor."""
    if control is None:
        return pen.x, pen.y

    return 2 * pen.x - control[0], 2 * pen.y - control[1]


def polygons(data: str) -> list[list[tuple[float, float]]]:
    pen = Pen()

    for letter, numbers in tokenize(data):
        walk(pen, letter, numbers)

    pen.close()

    return pen.subpaths


def viewbox(svg: str) -> tuple[float, float, float, float]:
    match = VIEWBOX.search(svg)

    if match is None:
        return 0.0, 0.0, 16.0, 16.0

    values = [float(number) for number in NUMBER.findall(match.group(1))]

    return values[0], values[1], values[2], values[3]


def paths(svg: str) -> list[str]:
    return [dict(ATTRIBUTE.findall(attributes)).get("d", "") for attributes in PATH.findall(svg)]


def render(svg: str) -> Image.Image:
    """One icon as an alpha mask the size of a cell.

    Subpaths inside one path are combined with exclusive or, which is what even-odd filling does
    and what non-zero filling also comes to for these icons: every hole in the set is a subpath
    wound against the shape around it. Separate path elements are unioned - they are separate
    pieces of one drawing, like the ring and the arrowhead of the reload arrow.
    """
    side = CELL * SUPERSAMPLE
    left, top, width, height = viewbox(svg)
    scale = side / max(width, height)

    icon = Image.new("1", (side, side), 0)

    for data in paths(svg):
        layer = Image.new("1", (side, side), 0)

        for subpath in polygons(data):
            points = [((x - left) * scale, (y - top) * scale) for x, y in subpath]
            piece = Image.new("1", (side, side), 0)
            ImageDraw.Draw(piece).polygon(points, fill=1)
            layer = ImageChops.logical_xor(layer, piece)

        icon = ImageChops.logical_or(icon, layer)

    return icon.convert("L").resize((CELL, CELL), Image.LANCZOS)


def main() -> None:
    strip = Image.new("RGBA", (CELL * len(ICONS), CELL), (255, 255, 255, 0))

    for index, (name, source) in enumerate(ICONS):
        svg = (SVG_DIR / f"{source}.svg").read_text(encoding="utf-8")
        alpha = render(svg)

        cell = Image.new("RGBA", (CELL, CELL), (255, 255, 255, 0))
        cell.putalpha(alpha)

        strip.paste(cell, (index * CELL, 0))
        print(f"{index}: {name} from {source}.svg")

    ATLAS.parent.mkdir(parents=True, exist_ok=True)
    strip.save(ATLAS)
    print(f"Wrote {ATLAS.relative_to(ROOT)} ({strip.width}x{strip.height})")


if __name__ == "__main__":
    main()
