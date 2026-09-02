from pathlib import Path

from PIL import Image


DIRECTORY = Path("Assets/Art/Vehicles/Glitch Phase/Rally")
ALPHA_THRESHOLD = 16
TARGET_BASELINE = 303


for path in sorted(DIRECTORY.glob("*.png")):
    image = Image.open(path).convert("RGBA")
    pixels = []
    removed_green = 0
    for red, green, blue, alpha in image.getdata():
        if alpha and green >= 80 and green - red >= 30 and green - blue >= 30:
            pixels.append((0, 0, 0, 0))
            removed_green += 1
        else:
            pixels.append((red, green, blue, alpha))
    if removed_green:
        cleaned = Image.new("RGBA", image.size, (0, 0, 0, 0))
        cleaned.putdata(pixels)
        image = cleaned

    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0).getbbox()
    if bounds is None:
        raise ValueError(f"{path} contains no visible pixels")

    current_baseline = bounds[3] - 1
    shift_y = TARGET_BASELINE - current_baseline
    if shift_y:
        aligned = Image.new("RGBA", image.size, (0, 0, 0, 0))
        aligned.alpha_composite(image, (0, shift_y))
        image = aligned

    if shift_y or removed_green:
        image.save(path, "PNG", optimize=True)

    final_image = Image.open(path).convert("RGBA")
    final_bounds = final_image.getchannel("A").point(
        lambda value: 255 if value >= ALPHA_THRESHOLD else 0
    ).getbbox()
    print(
        f"{path.name}|shift_y={shift_y}|removed_green={removed_green}|bounds={final_bounds}"
    )
