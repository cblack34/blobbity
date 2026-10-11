#!/bin/sh
# Rasterize the SVG sources in art/ into the white PNG sprites the Unity player tints.
# Needs rsvg-convert (brew install librsvg). Re-run after editing an SVG and commit both.
set -eu
here=$(cd "$(dirname "$0")" && pwd)
out="$here/../unity/MissionSplat/Assets/Art/Resources"
mkdir -p "$out"
# The sed below turns the black fill white. Without it the splat would export black and would not tint.
grep -q 'fill="#000"' "$here/splat.svg" || { echo 'art/splat.svg has no fill="#000"; the export would not be tintable.' >&2; exit 1; }
# The splat source is black; export it white so a tint gives the exact colour.
sed 's/fill="#000"/fill="#fff"/' "$here/splat.svg" | rsvg-convert -w 256 -h 244 -o "$out/Splat.png"
rsvg-convert -w 128 -h 128 -o "$out/TileCard.png" "$here/tile-card.svg"
rsvg-convert -w 200 -h 280 -o "$out/MissionCard.png" "$here/mission-card.svg"
