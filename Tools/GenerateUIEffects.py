"""Generate tintable UI effects at their final 256-pixel resolution."""
from pathlib import Path
import math

from PIL import Image, ImageDraw


OUTPUT = Path(__file__).resolve().parents[1] / "Assets/_BusJam/Art/UI"
SIZE = 256
SCALE = 4


def generate():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    glow = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
    for y in range(SIZE):
        for x in range(SIZE):
            radius = math.hypot(x - 127.5, y - 127.5) / 120
            alpha = round(255 * max(0, 1 - radius * radius) ** 3)
            glow.putpixel((x, y), (255, 255, 255, alpha))
    glow.save(OUTPUT / "glow_soft.png")

    star = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (255, 255, 255, 0))
    points = []
    for i in range(10):
        angle = -math.pi / 2 + i * math.pi / 5
        radius = (108 if i % 2 == 0 else 48) * SCALE
        points.append((128 * SCALE + math.cos(angle) * radius,
                       128 * SCALE + math.sin(angle) * radius))
    draw = ImageDraw.Draw(star)
    draw.polygon(points, fill=(255, 255, 255, 255))
    draw.line(points + [points[0]], fill=(185, 185, 185, 255),
              width=6 * SCALE, joint="curve")
    star.resize((SIZE, SIZE), Image.Resampling.LANCZOS).save(OUTPUT / "fx_confetti_star.png")

    rect = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (255, 255, 255, 0))
    ImageDraw.Draw(rect).rounded_rectangle(
        (80 * SCALE, 32 * SCALE, 176 * SCALE - 1, 224 * SCALE - 1),
        radius=12 * SCALE, fill=(255, 255, 255, 255))
    rect.resize((SIZE, SIZE), Image.Resampling.LANCZOS).save(OUTPUT / "fx_confetti_rect.png")


if __name__ == "__main__":
    generate()
