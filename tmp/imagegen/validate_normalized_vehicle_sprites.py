from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--width", type=int, required=True)
    parser.add_argument("--height", type=int, required=True)
    parser.add_argument("--baseline", type=int, required=True)
    args = parser.parse_args()

    files = sorted(args.directory.glob("*.png"))
    if len(files) != 6:
        raise SystemExit(f"Expected six PNGs in {args.directory}, found {len(files)}")

    for path in files:
        image = Image.open(path).convert("RGBA")
        if image.size != (args.width, args.height):
            raise SystemExit(f"{path.name}: wrong canvas {image.size}")
        alpha = image.getchannel("A")
        bounds = alpha.point(lambda value: 255 if value >= 24 else 0).getbbox()
        if bounds is None or bounds[3] != args.baseline:
            raise SystemExit(f"{path.name}: wrong baseline {bounds}")
        corners = (
            alpha.getpixel((0, 0)),
            alpha.getpixel((args.width - 1, 0)),
            alpha.getpixel((0, args.height - 1)),
            alpha.getpixel((args.width - 1, args.height - 1)),
        )
        if any(corners):
            raise SystemExit(f"{path.name}: nontransparent corner {corners}")

        visible = partial = opaque = green_dominant = 0
        for red, green, blue, value in image.getdata():
            if value:
                visible += 1
                if value == 255:
                    opaque += 1
                else:
                    partial += 1
                if green >= 80 and green - red >= 30 and green - blue >= 30:
                    green_dominant += 1
        if green_dominant:
            raise SystemExit(f"{path.name}: {green_dominant} saturated-green pixels remain")
        if opaque < visible * 0.90:
            raise SystemExit(
                f"{path.name}: interior is not sufficiently opaque ({opaque}/{visible})"
            )
        print(
            f"{path.name}|canvas={image.width}x{image.height}|bounds={bounds}|"
            f"opaque={opaque}|partial={partial}|green=0|corners=0"
        )


if __name__ == "__main__":
    main()
