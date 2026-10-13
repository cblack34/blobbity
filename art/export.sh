#!/bin/sh
# Rasterize the SVG sources in art/ into the PNG sprites the Unity player loads: white ones it tints, and legal-spot and tray-wave with their colours baked in.
# Needs rsvg-convert (brew install librsvg). Re-run after editing an SVG and commit both.
set -eu
here=$(cd "$(dirname "$0")" && pwd)
out="$here/../unity/MissionSplat/Assets/Art/Resources"
mkdir -p "$out"
# The sed below turns the black fill white. Without it the splat would export black and would not tint.
grep -q 'fill="#000"' "$here/splat.svg" || { echo 'art/splat.svg has no fill="#000"; the export would not be tintable.' >&2; exit 1; }
# The splat source is black; export it white so a tint gives the exact colour.
sed 's/fill="#000"/fill="#fff"/' "$here/splat.svg" | rsvg-convert -w 256 -h 244 -o "$out/Splat.png"
rsvg-convert -w 256 -h 256 -o "$out/TileCard.png" "$here/tile-card.svg"
rsvg-convert -w 400 -h 560 -o "$out/MissionCard.png" "$here/mission-card.svg"
rsvg-convert -w 256 -h 256 -o "$out/LegalSpot.png" "$here/legal-spot.svg"
rsvg-convert -w 104 -h 104 -o "$out/TileRing.png" "$here/tile-ring.svg"
rsvg-convert -w 256 -h 256 -o "$out/RotateArrow.png" "$here/rotate-arrow.svg"
rsvg-convert -w 1560 -h 144 -o "$out/TrayWave.png" "$here/tray-wave.svg"
