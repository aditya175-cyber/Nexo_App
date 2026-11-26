import os
from PIL import Image

assets_dir = os.path.join(os.path.dirname(__file__), "src", "WinIsland.UI", "Assets")
png_path = os.path.join(assets_dir, "logo.png")
ico_path = os.path.join(assets_dir, "app.ico")

if not os.path.exists(png_path):
    print(f"Error: {png_path} not found!")
    exit(1)

img = Image.open(png_path)
# Ensure RGBA for transparent logos
if img.mode not in ("RGB", "RGBA"):
    img = img.convert("RGBA")

# Generate multi-resolution icon
sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
img.save(ico_path, format="ICO", sizes=sizes)
print(f"Berhasil mengonversi {png_path} ({img.size[0]}x{img.size[1]}) ke multi-resolution Windows icon {ico_path}!")
