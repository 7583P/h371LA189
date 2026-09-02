from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter


def largest_component(alpha: Image.Image, threshold: int) -> tuple[Image.Image, tuple[int, int, int, int]]:
    width, height = alpha.size
    source = alpha.tobytes()
    foreground = bytearray(1 if value >= threshold else 0 for value in source)
    visited = bytearray(width * height)
    largest: list[int] = []

    for start in range(width * height):
        if not foreground[start] or visited[start]:
            continue
        queue = deque([start])
        visited[start] = 1
        current: list[int] = []
        while queue:
            index = queue.popleft()
            current.append(index)
            y, x = divmod(index, width)
            for neighbour in (index - 1, index + 1, index - width, index + width):
                if neighbour < 0 or neighbour >= width * height:
                    continue
                neighbour_y, neighbour_x = divmod(neighbour, width)
                if abs(neighbour_x - x) + abs(neighbour_y - y) != 1:
                    continue
                if foreground[neighbour] and not visited[neighbour]:
                    visited[neighbour] = 1
                    queue.append(neighbour)
        if len(current) > len(largest):
            largest = current

    if not largest:
        raise ValueError("Sprite contains no foreground component")

    cleaned = bytearray(width * height)
    min_x = width
    min_y = height
    max_x = -1
    max_y = -1
    for index in largest:
        y, x = divmod(index, width)
        cleaned[index] = 255
        min_x = min(min_x, x)
        min_y = min(min_y, y)
        max_x = max(max_x, x)
        max_y = max(max_y, y)

    mask = Image.frombytes("L", (width, height), bytes(cleaned))
    return mask, (min_x, min_y, max_x + 1, max_y + 1)


def threshold_bbox(image: Image.Image, threshold: int) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value >= threshold else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError("Reference sprite contains no visible pixels")
    return bounds


def strip_saturated_green_spill(source: Image.Image) -> Image.Image:
    cleaned = []
    for red, green, blue, alpha in source.getdata():
        if alpha and green >= 80 and green - red >= 30 and green - blue >= 30:
            cleaned.append((0, 0, 0, 0))
        else:
            cleaned.append((red, green, blue, alpha))
    result = Image.new("RGBA", source.size)
    result.putdata(cleaned)
    return result


def fill_small_enclosed_holes(source: Image.Image, mask: Image.Image,
                              bounds: tuple[int, int, int, int],
                              maximum_area: int = 2200) -> tuple[Image.Image, Image.Image]:
    width, height = mask.size
    mask_data = bytearray(mask.tobytes())
    visited = bytearray(width * height)
    pixels = list(source.getdata())
    left, top, right, bottom = bounds

    for start_y in range(top, bottom):
        for start_x in range(left, right):
            start = start_y * width + start_x
            if mask_data[start] or visited[start]:
                continue
            queue = deque([start])
            visited[start] = 1
            hole: list[int] = []
            touches_outside = False
            while queue:
                index = queue.popleft()
                hole.append(index)
                y, x = divmod(index, width)
                if x <= left or x >= right - 1 or y <= top or y >= bottom - 1:
                    touches_outside = True
                for neighbour in (index - 1, index + 1, index - width, index + width):
                    if neighbour < 0 or neighbour >= width * height:
                        continue
                    neighbour_y, neighbour_x = divmod(neighbour, width)
                    if abs(neighbour_x - x) + abs(neighbour_y - y) != 1:
                        continue
                    if not mask_data[neighbour] and not visited[neighbour]:
                        visited[neighbour] = 1
                        queue.append(neighbour)

            if touches_outside or len(hole) > maximum_area:
                continue

            hole_set = set(hole)
            fill_queue = deque()
            assigned: set[int] = set()
            for index in hole:
                y, x = divmod(index, width)
                neighbour_colors = []
                for neighbour in (index - 1, index + 1, index - width, index + width):
                    if neighbour < 0 or neighbour >= width * height or neighbour in hole_set:
                        continue
                    neighbour_y, neighbour_x = divmod(neighbour, width)
                    if abs(neighbour_x - x) + abs(neighbour_y - y) == 1 and mask_data[neighbour]:
                        neighbour_colors.append(pixels[neighbour][:3])
                if neighbour_colors:
                    count = len(neighbour_colors)
                    rgb = tuple(sum(color[channel] for color in neighbour_colors) // count
                                for channel in range(3))
                    pixels[index] = (*rgb, 255)
                    assigned.add(index)
                    fill_queue.append(index)

            while fill_queue:
                index = fill_queue.popleft()
                y, x = divmod(index, width)
                for neighbour in (index - 1, index + 1, index - width, index + width):
                    if neighbour not in hole_set or neighbour in assigned:
                        continue
                    neighbour_y, neighbour_x = divmod(neighbour, width)
                    if abs(neighbour_x - x) + abs(neighbour_y - y) != 1:
                        continue
                    pixels[neighbour] = (*pixels[index][:3], 255)
                    assigned.add(neighbour)
                    fill_queue.append(neighbour)

            for index in hole:
                mask_data[index] = 255

    repaired_source = Image.new("RGBA", source.size)
    repaired_source.putdata(pixels)
    repaired_mask = Image.frombytes("L", mask.size, bytes(mask_data))
    return repaired_source, repaired_mask


def normalize(source_path: Path, reference_path: Path, output_path: Path,
              threshold: int, source_padding: int, target_inset: int) -> None:
    source = strip_saturated_green_spill(Image.open(source_path).convert("RGBA"))
    reference = Image.open(reference_path).convert("RGBA")
    component_mask, component_bounds = largest_component(source.getchannel("A"), threshold)
    source, component_mask = fill_small_enclosed_holes(source, component_mask, component_bounds)

    left = max(0, component_bounds[0] - source_padding)
    top = max(0, component_bounds[1] - source_padding)
    right = min(source.width, component_bounds[2] + source_padding)
    bottom = min(source.height, component_bounds[3] + source_padding)
    crop_box = (left, top, right, bottom)

    # A solid interior with only a sub-pixel outer feather prevents the garage backdrop from
    # bleeding through the vehicle while retaining smooth sprite edges.
    clean_alpha = component_mask.filter(ImageFilter.GaussianBlur(radius=0.65))
    red, green, blue, _ = source.split()
    cleaned_source = Image.merge("RGBA", (red, green, blue, clean_alpha)).crop(crop_box)

    reference_bounds = threshold_bbox(reference, threshold)
    available_left = reference_bounds[0] + target_inset
    available_top = reference_bounds[1] + target_inset
    available_right = reference_bounds[2] - target_inset
    available_bottom = reference_bounds[3] - target_inset
    available_width = max(1, available_right - available_left)
    available_height = max(1, available_bottom - available_top)
    scale = min(available_width / cleaned_source.width, available_height / cleaned_source.height)
    resized_size = (
        max(1, round(cleaned_source.width * scale)),
        max(1, round(cleaned_source.height * scale)),
    )

    # Resize premultiplied RGBA to avoid dark or colored fringes around transparent pixels.
    resized = cleaned_source.convert("RGBa").resize(
        resized_size, Image.Resampling.LANCZOS
    ).convert("RGBA")
    x = available_left + (available_width - resized.width) // 2
    y = available_bottom - resized.height
    canvas = Image.new("RGBA", reference.size, (0, 0, 0, 0))
    canvas.alpha_composite(resized, (x, y))
    canvas = strip_saturated_green_spill(canvas)

    placed_bounds = canvas.getchannel("A").point(
        lambda value: 255 if value >= threshold else 0
    ).getbbox()
    if placed_bounds is None:
        raise ValueError("Normalized sprite contains no visible pixels")
    desired_center_x = (available_left + available_right) / 2.0
    placed_center_x = (placed_bounds[0] + placed_bounds[2]) / 2.0
    shift_x = round(desired_center_x - placed_center_x)
    shift_y = available_bottom - placed_bounds[3]
    if shift_x or shift_y:
        aligned = Image.new("RGBA", reference.size, (0, 0, 0, 0))
        aligned.alpha_composite(canvas, (shift_x, shift_y))
        canvas = aligned

    output_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output_path, "PNG", optimize=True)
    alpha_bounds = canvas.getchannel("A").point(lambda value: 255 if value >= threshold else 0).getbbox()
    print(
        f"{source_path.name}|source={source.size}|component={component_bounds}|"
        f"output={canvas.size}|visible={alpha_bounds}|scale={scale:.5f}"
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--reference", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--threshold", type=int, default=24)
    parser.add_argument("--source-padding", type=int, default=5)
    parser.add_argument("--target-inset", type=int, default=6)
    args = parser.parse_args()

    for source_path in sorted(args.source_dir.glob("*.png")):
        normalize(
            source_path,
            args.reference,
            args.output_dir / source_path.name,
            args.threshold,
            args.source_padding,
            args.target_inset,
        )


if __name__ == "__main__":
    main()
