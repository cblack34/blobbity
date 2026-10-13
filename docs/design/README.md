# Design reference

`splat-party.png` (source `splat-party.svg`) is the target look for the table screen on a portrait phone, chosen by the maintainer on 2026-10-12 from several directions. It is a reference, not a pixel spec: the redesign slices under #44 build toward it and may adjust details found on the device.

What it fixes as direction:

- Players are splat critters with googly eyes, coloured by seat order (yellow, orange, teal, pink), never one of the four game colours. Each shows its name and four score pips that fill with that player's splat.
- The current player's critter sits in a highlighted box, and a heading names whose turn it is, with one plain line saying what to do.
- The board takes the middle of the screen. The turn tray at the bottom holds the drawn tile, one Spin button, the power chips with their remaining uses, and the secret missions.
- There is no claims row; tapping a player opens their claimed missions.
- Look: big faint splats and drips in the background, toy-like tiles with thick outlines and hard shadows, dashed "+" squares for legal placements, and a chunky, friendly typeface. The mock-up uses fonts installed on the maintainer's Mac; the shipped player bundles freely licensed fonts in the same spirit.

The critters are original artwork built from the project's own splat, not characters from the physical game.
