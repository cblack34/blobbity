import re, sys
R = sys.argv[1]
SPLAT = re.search(r' d="([^"]+)"', open(f"{R}/art/splat.svg").read()).group(1)
def mark(name):
    s = open(f"{R}/art/mark-{name}.svg").read()
    return re.search(r"<svg[^>]*>(.*)</svg>", s, re.S).group(1)
MARKS = {k: mark(k) for k in ("heart", "triangle", "square", "star", "sparkle")}
INK, PAGE, MUTED = "#1d1d1b", "#f2f7ff", "#4a5b6b"
GAME = [("Red", "#e03131", "#9a0110", "heart"), ("Blue", "#1c7ed6", "#04508e", "triangle"),
        ("Green", "#2f9e44", "#006a21", "square"), ("Purple", "#9c36b5", "#66007b", "star")]
CRIT = [("Seat 1", "#ffb703"), ("Seat 2", "#fb8500"), ("Seat 3", "#2ec4b6"), ("Seat 4", "#ff70a6")]

def splat(fill, m, deep, size, turn=0, wild=False):
    # The mark sits on the body centre (71.5, 73.5) of the 148x141 splat and stays upright.
    body = (f'<g transform="rotate({turn} 71.5 73.5)"><path d="{SPLAT}" fill="{fill}"/></g>' if not wild else
            "".join(f'<clipPath id="q{size}{i}"><rect x="{x}" y="{y}" width="{w}" height="{h}"/></clipPath>'
                    f'<path d="{SPLAT}" fill="{c}" clip-path="url(#q{size}{i})"/>'
                    for i, (c, x, y, w, h) in enumerate([("#e03131", 0, 0, 74, 70.5), ("#1c7ed6", 74, 0, 80, 70.5),
                                                          ("#2f9e44", 0, 70.5, 74, 80), ("#9c36b5", 74, 70.5, 80, 80)])))
    if m:
        k = 64 / 148 * 148 / 64  # mark box 64 units spans 64/148 of the splat width
        tint = (f'stroke="{deep}" fill="none"' if m != "sparkle" else f'fill="{deep}"')
        inner = MARKS[m].replace('stroke="#fff"', f'stroke="{deep}"').replace('fill="#fff"', f'fill="{deep}"')
        body += f'<g transform="translate({71.5 - 32} {73.5 - 32})">{inner}</g>'
    return f'<svg viewBox="-4 -4 156 149" width="{size}" height="{size*149/156:.0f}">{body}</svg>'

def critter(c, size):
    eyes = "".join(f'<circle cx="{x}" cy="{60}" r="19" fill="{INK}"/><circle cx="{x}" cy="60" r="14" fill="#fff"/>'
                   f'<circle cx="{x+4}" cy="65" r="7" fill="{INK}"/>' for x in (56, 92))
    return f'<svg viewBox="-4 -4 156 149" width="{size}" height="{size*149/156:.0f}"><path d="{SPLAT}" fill="{c}"/>{eyes}</svg>'

def swatch(hexv, name, role, dark=False):
    fg = "#fff" if dark else INK
    return (f'<div class="sw"><div class="chip" style="background:{hexv};color:{fg}">{hexv}</div>'
            f'<b>{name}</b><span>{role}</span></div>')

wild = splat(None, "sparkle", "#000", 70, wild=True).replace('fill="#000"', 'fill="#2a1f3d"')
tile = "".join(splat(c, m, d, 62, t) for (_, c, d, m), t in zip(GAME, (20, -50, 80, 160)))
card = "".join(splat("#1c7ed6", "triangle", "#04508e", 46, t) for t in (10, 70, 140, 220))

html = f'''<!doctype html><html><head><meta charset="utf-8"><style>
@font-face{{font-family:B;src:url(Baloo.ttf)}} @font-face{{font-family:A;src:url(Atkinson.ttf)}}
*{{box-sizing:border-box}} body{{margin:0;width:1700px;background:{PAGE};color:{INK};font-family:A;padding:40px}}
h1{{font:800 96px/0.9 B;margin:0}} h2{{font:800 30px B;margin:0 0 14px}} .tag{{font:600 15px A;color:{MUTED};letter-spacing:.12em;text-transform:uppercase}}
.toy{{background:#fff;border:5px solid {INK};border-radius:26px;box-shadow:8px 8px 0 {INK};padding:22px}}
.grid{{display:grid;gap:30px}} .sw{{display:flex;flex-direction:column;gap:3px;font-size:14px;width:150px}}
.sw .chip{{height:64px;border-radius:14px;border:3px solid {INK};display:flex;align-items:end;padding:6px 9px;font:700 13px A}}
.sw span{{color:{MUTED};font-size:13px;line-height:1.25}} .row{{display:flex;gap:16px;flex-wrap:wrap}}
.note{{background:#fff4d6;border:3px dashed {INK};border-radius:14px;padding:8px 12px;font-size:14px}}
.tile{{display:grid;grid-template-columns:1fr 1fr;place-items:center;width:150px;height:150px;background:#fff;border:5px solid {INK};border-radius:22px;box-shadow:6px 6px 0 {INK};padding:6px}}
.bcard{{width:118px;height:165px;background:#fff;border:5px solid {INK};border-radius:16px;box-shadow:6px 6px 0 {INK};display:flex;flex-direction:column;align-items:center;justify-content:space-evenly;transform:rotate(-4deg)}}
.btn{{font:800 30px B;border:4px solid {INK};border-radius:999px;box-shadow:4px 4px 0 {INK};padding:10px 30px;display:inline-block}}
.spot{{width:110px;height:110px;border:4px dashed {INK};border-radius:20px;display:grid;place-items:center;font:300 54px A;color:#adb5bd}}
.badge{{width:54px;height:54px;border-radius:50%;border:4px solid {INK};display:grid;place-items:center;font:800 28px B}}
.phone{{width:330px;border:10px solid {INK};border-radius:46px;overflow:hidden;box-shadow:10px 10px 0 {INK};background:#fff}}
.phone img{{width:100%;display:block}} ul{{margin:6px 0 0 18px;padding:0;font-size:16px;line-height:1.45}}
</style></head><body>
<div style="display:flex;justify-content:space-between;align-items:end;margin-bottom:30px">
 <div><div class="tag">Brand board · draft for review · 2026-10-16</div><h1>Blobbity</h1>
 <div style="font:600 24px A;margin-top:10px">A tile game for 2–4 players on one phone: splat the board, claim your secret blobbities.</div></div>
 <div class="note" style="max-width:420px">Wordmark, logo lockups and the app icon arrive with #81. The word here is Baloo 2 ExtraBold, set as a stand-in.</div>
</div>
<div class="grid" style="grid-template-columns:1fr 360px">
<div class="grid">
 <div class="grid" style="grid-template-columns:1.25fr 1fr">
  <div class="toy"><h2>Game colours and their marks</h2>
   <div class="row" style="align-items:center">{"".join(f'<div style="text-align:center;font-size:14px">{splat(c,m,d,96)}<br><b>{n}</b> · {m}<br><span style="color:{MUTED}">{c} / mark {d}</span></div>' for n,c,d,m in GAME)}
   <div style="text-align:center;font-size:14px">{wild}<br><b>Wild</b> · sparkle</div></div>
   <div style="font-size:14px;color:{MUTED};margin-top:10px">One splat for every colour. The mark is an outline in a deeper shade of the same hue and stays upright while the splat turns.</div></div>
  <div class="toy"><h2>The critters</h2>
   <div class="row">{"".join(f'<div style="text-align:center;font-size:14px">{critter(c,92)}<br><b>{n}</b><br><span style="color:{MUTED}">{c}</span></div>' for n,c in CRIT)}</div>
   <div style="font-size:14px;color:{MUTED};margin-top:8px">Players are splats with googly eyes, coloured by seat order and never by a game colour. They never carry a mark.</div></div>
 </div>
 <div class="toy"><h2>Palette roles</h2>
  <div class="row">
   {swatch(INK,"Ink","Every outline, hard shadow and text",True)}{swatch("#ffffff","Paper","Cards, tray, panels")}{swatch(PAGE,"Page","Table background")}{swatch(MUTED,"Muted","Secondary text",True)}{swatch("#adb5bd","Gray","Spent powers, faint +")}
   {swatch("#ffd166","Sunny","Spin button · rotate badge ⚠ two jobs")}{swatch("#8fe3a0","Go","Place, Start, Play again")}{swatch("#8ecae6","Stack badge","Stack power mark")}{swatch("#ffafcc","Bounce badge","Bounce power mark")}{swatch("#2f9bd8","Stack ring","Tile the drawn tile would cover")}{swatch("#fb8500","Target ring","Tile a power can act on ⚠ = Seat 2")}
  </div>
  <div class="note" style="margin-top:14px">⚠ Open from #80: each colour should have one job. Sunny is both Spin and the rotate badge; the target ring is the same orange as Seat 2's critter. The guide will propose a fix for your approval.</div></div>
 <div class="grid" style="grid-template-columns:1fr 1.1fr">
  <div class="toy"><h2>Type</h2>
   <div style="font:800 54px/1 B">Kofi's turn!</div><div class="tag" style="margin:2px 0 14px">Baloo 2 ExtraBold · headings, winner</div>
   <div style="font:600 30px/1 B">Rosa · Spin · Place</div><div class="tag" style="margin:2px 0 14px">Baloo 2 SemiBold · names, buttons</div>
   <div style="font:400 22px/1.3 A">Use a power, then place your tile.</div><div style="font:400 16px/1.3 A;color:{MUTED}">Rosa placed a tile and claimed a blobbity.</div>
   <div class="tag" style="margin-top:4px">Atkinson Hyperlegible Next · instructions, labels, small text</div></div>
  <div class="toy"><h2>Toy pieces</h2>
   <div class="row" style="align-items:center;gap:26px">
    <div style="text-align:center;font-size:13px"><div class="tile">{tile}</div><br>Tile card</div>
    <div style="text-align:center;font-size:13px"><div class="bcard">{card}</div><br>Blobbity card</div>
    <div style="text-align:center;font-size:13px"><div class="spot">+</div><br>Legal spot</div></div>
   <div class="row" style="align-items:center;gap:18px;margin-top:22px">
    <div class="btn" style="background:#ffd166">Spin</div><div class="btn" style="background:#8fe3a0">Place</div>
    <div class="badge" style="background:#ffd166">↻</div><div class="badge" style="background:#8ecae6">+</div><div class="badge" style="background:#ffafcc">−</div></div>
   <div style="font-size:14px;color:{MUTED};margin-top:12px">White face, thick ink edge, hard offset shadow, chunky corners.</div></div>
 </div>
 <div class="grid" style="grid-template-columns:1fr 1fr">
  <div class="toy"><h2 style="color:#2f9e44">Do</h2><ul><li>Draw every outline and shadow in ink.</li><li>Keep a mark on every game-colour splat.</li><li>Give each colour one job.</li><li>Use one plain sentence per instruction.</li></ul></div>
  <div class="toy"><h2 style="color:#e03131">Don't</h2><ul><li>Use soft blurred shadows or gradients.</li><li>Colour a critter with a game colour, or give it a mark.</li><li>Tint a tile to highlight it; ring it.</li><li>Use anything from the physical game: name, art, characters.</li></ul></div>
 </div>
</div>
<div class="grid" style="align-content:start;justify-items:center">
 <div class="phone"><img src="file://{R}/unity/Blobbity/Assets/Captures/secrets.png"></div>
 <div style="font-size:14px;color:{MUTED}">In play: a 4-seat table, secret blobbities in the tray.</div>
</div>
</div></body></html>'''
open(sys.argv[2], "w").write(html)
