from __future__ import annotations

import sys
from collections import deque
from pathlib import Path

from PIL import Image


def components(mask: bytearray, width: int, height: int):
    visited = bytearray(width * height)
    found = []
    for start in range(width * height):
        if not mask[start] or visited[start]:
            continue
        queue = deque([start])
        visited[start] = 1
        area = 0
        min_x = width
        min_y = height
        max_x = -1
        max_y = -1
        while queue:
            index = queue.popleft()
            y, x = divmod(index, width)
            area += 1
            min_x = min(min_x, x)
            min_y = min(min_y, y)
            max_x = max(max_x, x)
            max_y = max(max_y, y)
            for neighbour in (index - 1, index + 1, index - width, index + width):
                if neighbour < 0 or neighbour >= width * height:
                    continue
                neighbour_y, neighbour_x = divmod(neighbour, width)
                if abs(neighbour_x - x) + abs(neighbour_y - y) != 1:
                    continue
                if mask[neighbour] and not visited[neighbour]:
                    visited[neighbour] = 1
                    queue.append(neighbour)
        found.append((area, (min_x, min_y, max_x, max_y)))
    return sorted(found, reverse=True)


def main() -> None:
    threshold = int(sys.argv[1])
    for raw_path in sys.argv[2:]:
        path = Path(raw_path)
        image = Image.open(path).convert("RGBA")
        alpha = image.getchannel("A")
        histogram = alpha.histogram()
        total_visible = sum(histogram[1:])
        partial = sum(histogram[1:255])
        opaque = histogram[255]
        mask = bytearray(1 if value >= threshold else 0 for value in alpha.getdata())
        found = components(mask, image.width, image.height)
        summary = "; ".join(
            f"{area}@{bounds[0]},{bounds[1]}-{bounds[2]},{bounds[3]}"
            for area, bounds in found[:8]
        )
        print(
            f"{path}|{image.width}x{image.height}|visible={total_visible}|"
            f"partial={partial}|opaque={opaque}|components={len(found)}|top={summary}"
        )


if __name__ == "__main__":
    main()
