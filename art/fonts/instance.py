"""Cut fixed-weight Baloo 2 files from the variable font for Unity's legacy Text, which cannot pick a weight.

Usage (needs fontTools: `python3 -m pip install fonttools`):
    python3 art/fonts/instance.py
"""
from pathlib import Path

from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

here = Path(__file__).resolve().parent
out = here.parent.parent / "unity/MissionSplat/Assets/Art/Resources/Fonts"
for name, weight in (("Regular", 400), ("SemiBold", 600), ("ExtraBold", 800)):
    font = instantiateVariableFont(TTFont(here / "Baloo2[wght].ttf"), {"wght": weight}, updateFontNames=True)
    font.save(out / f"Baloo2-{name}.ttf")
