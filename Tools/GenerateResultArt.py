"""Construct result SVGs from Fredoka outlines; no bitmap tracing or embedded images."""
from pathlib import Path
import math
import uuid
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.boundsPen import BoundsPen
from fontTools.misc.transform import Transform
from PIL import Image
import json

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/_BusJam/UI/Vector'
EVIDENCE = ROOT.parent / 'BusJamCase_ZVEC_Review/win'
FONT = TTFont(ROOT / 'Assets/_BusJam/Art/Fonts/Fredoka-Bold.ttf')
GLYPHS = FONT.getGlyphSet()
CMAP = FONT.getBestCmap()

def path(d, fill, stroke=None, width=0, extra=''):
    return f'<path d="{d}" fill="{fill}"' + (f' stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round" stroke-linecap="round"' if stroke else '') + f' {extra}/>'

def glyphs(word, width, height, top, canvas):
    bounds = []
    for letter in word:
        g = GLYPHS[CMAP[ord(letter)]]
        b = BoundsPen(GLYPHS); g.draw(b)
        bounds.append((g, b.bounds))
    # Individually position and gently fan the letters, as in the source art.
    tracking = 26
    raw = sum(b[2]-b[0] for g,b in bounds)
    sx = (width-tracking*(len(word)-1))/raw
    sy = height/max(b[3]-b[1] for g,b in bounds)
    x = (canvas-width)/2
    result = []
    for i,(g,b) in enumerate(bounds):
        w = (b[2]-b[0])*sx
        fan = (i/(len(word)-1)-.5) if len(word)>1 else 0
        y = top + (62 if len(word)>6 else 36)*fan*fan*4
        angle = fan*14
        t = Transform().translate(x+w/2, y+height/2).rotate(math.radians(angle)).translate(-w/2,-height/2).scale(sx,-sy).translate(-b[0],-b[3])
        p = SVGPathPen(GLYPHS); g.draw(TransformPen(p,t))
        result.append((p.getCommands(),x,y,w,height))
        x += w+tracking
    return result

def star(cx,cy,r):
    pts=[]
    for i in range(10):
        a=-math.pi/2+i*math.pi/5; radius=r if i%2==0 else r*.51
        pts.append((cx+math.cos(a)*radius,cy+math.sin(a)*radius))
    rounded=[]
    for i,(x,y) in enumerate(pts):
        prev,nxt=pts[i-1],pts[(i+1)%len(pts)]
        incoming=(x+(prev[0]-x)*.18,y+(prev[1]-y)*.18)
        outgoing=(x+(nxt[0]-x)*.18,y+(nxt[1]-y)*.18)
        rounded.append((incoming,(x,y),outgoing))
    return 'M'+','.join(map(str,rounded[0][0]))+' '+ ' '.join(f'L{a[0]:.2f},{a[1]:.2f} Q{b[0]:.2f},{b[1]:.2f} {c[0]:.2f},{c[1]:.2f}' for a,b,c in rounded)+'Z'

def title(name, completed):
    w,h=(1740,800) if completed else (1490,830)
    letters=glyphs('LEVEL',740 if completed else 940,226 if completed else 250,62,w)
    letters+=glyphs('COMPLETED!' if completed else 'FAILED',1610 if completed else 1320,283 if completed else 330,354,w)
    outline,depth,edge,face=('#650482','#280736','#c22dd5',('#fff94d','#ffcc00','#ff8a00')) if completed else ('#09265e','#020d2b','#365699',('#f8fcff','#d6e0f3','#9daccb'))
    allpaths=[d for d,*_ in letters]
    if completed:
        allpaths += [star(343,228,82),star(1397,228,82)]
    defs=f'<linearGradient id="face" x1="0" y1="0" x2="0" y2="1"><stop stop-color="{face[0]}"/><stop offset=".4" stop-color="{face[1]}"/><stop offset="1" stop-color="{face[2]}"/></linearGradient>'
    defs+=f'<linearGradient id="depth" x1="0" y1="0" x2="0" y2="1"><stop stop-color="{outline}"/><stop offset="1" stop-color="{depth}"/></linearGradient>'
    defs+=''.join(f'<clipPath id="g{i}">{path(d,"white")}</clipPath>' for i,(d,*_) in enumerate(letters))
    body=[]
    for offset in range(66,0,-6):
        ratio=offset/66
        a,b=((105,0,133),(39,5,53)) if completed else ((14,48,116),(2,12,40))
        shade='#'+''.join(f'{round(x+(y-x)*ratio):02x}' for x,y in zip(a,b))
        body += [path(d,shade,shade,62, f'transform="translate(0 {offset})"') for d in allpaths]
    body += [path(d,outline,edge,64,'transform="translate(0 9)"') for d in allpaths]
    body += [path(d,outline,outline,58) for d in allpaths]
    body += [path(d,face[2],face[2],19,'transform="translate(0 7)"') for d in allpaths]
    body += [path(d,face[0],face[0],19,'transform="translate(0 -1)"') for d in allpaths]
    body += [path(d,'url(#face)','url(#face)',16) for d in allpaths]
    for i,(d,x,y,gwidth,gh) in enumerate(letters):
        # A curved, clipped top glint follows each real glyph, including counters.
        gloss=f'M{x+12},{y+gh*.24} Q{x+gwidth*.18},{y-3} {x+gwidth*.82},{y+13} Q{x+gwidth*.44},{y+25} {x+gwidth*.23},{y+gh*.20} Q{x+10},{y+gh*.31} {x+12},{y+gh*.24}Z'
        body.append(path(gloss,'#fff',extra=f'opacity=".8" clip-path="url(#g{i})"'))
    if completed:
        for cx in (343,1397):
            body.append(path(f'M{cx-43} 222 Q{cx-7} 212 {cx-1} 170','none','#fff',12,'opacity=".85"'))
        for flip in (False,True):
            body.append('<g'+(' transform="translate(1740 0) scale(-1 1)"' if flip else '')+'>')
            for d in ('M220 182 C166 142 159 116 182 112 C209 109 219 141 226 174Z','M192 221 C124 191 115 208 127 228 C137 239 165 238 192 232Z','M203 268 C153 263 148 287 168 291 C182 291 196 279 203 268Z'):
                body.append(path(d,'url(#face)','#edaa00',3))
            body.append('</g>')
    save(name,w,h,defs,''.join(body))

def save(name,w,h,defs,body):
    (OUT/f'{name}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><title>{name.replace("_"," ")}</title><defs>{defs}</defs>{body}</svg>\n',encoding='utf-8')
    meta=OUT/f'{name}.svg.meta'
    if not meta.exists():
        template=(OUT/'coin_pile.svg.meta').read_text()
        template=template.replace('004e24079bb97bf408f0c444800e6f31',uuid.uuid4().hex)
        meta.write_text(template)

title('title_complete',True)
title('title_failed',False)
defs='<linearGradient id="band" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#ffb2ff"/><stop offset=".16" stop-color="#e23bfa"/><stop offset=".65" stop-color="#b500ed"/><stop offset="1" stop-color="#7200ad"/></linearGradient><linearGradient id="fold"><stop stop-color="#4a0466"/><stop offset="1" stop-color="#ad05df"/></linearGradient>'
body=path('M240 210 Q75 221 35 263 L111 345 L54 451 Q32 501 87 480 Q231 431 364 455 L345 285Z','url(#band)','#510273',13)
body+=path('M1280 210 Q1445 221 1485 263 L1409 345 L1466 451 Q1488 501 1433 480 Q1289 431 1156 455 L1175 285Z','url(#band)','#510273',13)
body+=path('M258 351 Q455 361 366 451 Q429 422 279 408Z','url(#fold)','#510273',8)
body+=path('M1262 351 Q1065 361 1154 451 Q1091 422 1241 408Z','url(#fold)','#510273',8)
body+=path('M233 153 C256 -13 1264 -13 1287 153 L1248 376 C1215 280 305 280 272 376Z','url(#band)','#510273',14)
body+=path('M272 376 C305 280 1215 280 1248 376 Q1240 407 1161 416 C1210 337 310 337 359 416 Q280 407 272 376Z','url(#fold)','#7200ad',7)
body+=path('M285 133 C406 23 1137 25 1237 130 C1080 55 438 49 304 152 Q276 165 285 133Z','#fff',extra='opacity=".62"')
body+=path('M65 266 Q148 225 236 231 L244 251 Q129 248 77 285Z','#fff',extra='opacity=".4"')
body+=path('M1284 231 Q1382 225 1455 266 L1443 285 Q1390 248 1276 251Z','#fff',extra='opacity=".4"')
save('ribbon_purple',1520,520,defs,body)
save('fx_confetti_heart',64,64,'',path('M32 57 C-18 26 13 -7 32 14 C51 -7 82 26 32 57Z','#fff'))
save('fx_confetti_disc',64,64,'','<circle cx="32" cy="32" r="27" fill="#fff"/>')
defs='<radialGradient id="halo"><stop stop-color="#df9dff" stop-opacity=".65"/><stop offset=".4" stop-color="#a83eff" stop-opacity=".3"/><stop offset="1" stop-color="#7200bd" stop-opacity="0"/></radialGradient>'
body='<circle cx="500" cy="500" r="495" fill="url(#halo)"/>'
for i in range(16):
    body+=path('M488 490 L454 55 Q500 5 546 55 L512 490Z','url(#halo)',extra=f'transform="rotate({i*22.5} 500 500)"')
save('result_rays',1000,1000,defs,body)

# Record representative opaque source colours independently of the vector construction.
samples={}
for name in ('title_complete','title_failed','ribbon_purple'):
    im=Image.open(ROOT/f'Assets/_BusJam/Art/UI/{name}.png').convert('RGBA')
    colors=im.getcolors(im.width*im.height)
    samples[name]=[{'rgba':c,'pixels':n} for n,c in sorted(colors,reverse=True) if c[3]>240][:16]
EVIDENCE.mkdir(parents=True,exist_ok=True)
(EVIDENCE/'source-colours.json').write_text(json.dumps(samples,indent=2))
print('Constructed six UI Toolkit vector images from Fredoka glyphs and hand-authored shapes.')
