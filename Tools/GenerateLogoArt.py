"""Construct the Start logo from real Fredoka/Lilita glyphs and authored shapes.

Uses the same fontTools outline/transformation and layered shading method as
GenerateResultArt.py. No tracing, SVG text, filters, or embedded bitmap data.
"""
from pathlib import Path
import json
import math
import uuid
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.boundsPen import BoundsPen
from fontTools.misc.transform import Transform
from fontTools.pens.basePen import BasePen
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/_BusJam/UI/Vector/logo_constructed.svg'
EVIDENCE = ROOT.parent / 'BusJamCase_ZVEC_Review/logo'
FONT = TTFont(ROOT / 'Assets/_BusJam/Art/Fonts/LilitaOne-Regular.ttf')
GLYPHS, CMAP = FONT.getGlyphSet(), FONT.getBestCmap()
ROUND_FONT = TTFont(ROOT / 'Assets/_BusJam/Art/Fonts/Fredoka-Bold.ttf')
body, defs = [], []


def path(d, fill, stroke=None, width=0, extra=''):
    return f'<path d="{d}" fill="{fill}"' + (f' stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round" stroke-linecap="round"' if stroke else '') + f' {extra}/>'


def add(d, fill, stroke=None, width=0, extra=''):
    body.append(path(d, fill, stroke, width, extra))


def gradient(name, stops, horizontal=False):
    coords='gradientUnits="userSpaceOnUse" x1="0" y1="240" x2="0" y2="840"' if name=='purple' else f'x1="0" y1="0" x2="{1 if horizontal else 0}" y2="{0 if horizontal else 1}"'
    defs.append(f'<linearGradient id="{name}" {coords}>' + ''.join(f'<stop offset="{pos}" stop-color="{colour}"/>' for pos, colour in stops) + '</linearGradient>')


def ellipse(cx, cy, rx, ry, fill, stroke=None, width=0, extra=''):
    add(f'M{cx-rx} {cy} a{rx} {ry} 0 1 0 {2*rx} 0 a{rx} {ry} 0 1 0 {-2*rx} 0Z', fill, stroke, width, extra)


class SoftOutlinePen(BasePen):
    """Round glyph corners along the real outline, without rasterizing it."""
    def __init__(self, glyphs):
        super().__init__(glyphs); self.contours=[]; self.points=[]
    def _moveTo(self,p): self.points=[p]
    def _lineTo(self,p): self.points.append(p)
    def _curveToOne(self,a,b,c):
        p=self.points[-1]
        for t in np.linspace(0,1,25)[1:]:
            self.points.append(tuple((1-t)**3*p[i]+3*(1-t)**2*t*a[i]+3*(1-t)*t*t*b[i]+t**3*c[i] for i in (0,1)))
    def _qCurveToOne(self,a,b):
        p=self.points[-1]
        for t in np.linspace(0,1,19)[1:]:
            self.points.append(tuple((1-t)**2*p[i]+2*(1-t)*t*a[i]+t*t*b[i] for i in (0,1)))
    def _closePath(self): self.contours.append(self.points)
    def _endPath(self): self._closePath()


def glyph(letter, x, y, w, h, angle):
    font=FONT if letter in 'BJA' else ROUND_FONT
    glyphset=font.getGlyphSet();g = glyphset[font.getBestCmap()[ord(letter)]]
    b = BoundsPen(glyphset); g.draw(b)
    left, bottom, right, top = b.bounds
    t = Transform().translate(x+w/2, y+h/2).rotate(math.radians(angle)).translate(-w/2, -h/2).scale(w/(right-left), -h/(top-bottom)).translate(-left, -top)
    pen = SoftOutlinePen(glyphset);g.draw(TransformPen(pen,t))
    contours=[]; contour_data=[]
    for contour in pen.contours:
        points=np.array(contour);points=np.vstack([points,points[0]])
        # A mild balloon warp gives straight cap edges the source's pillowy crown.
        centre=np.array([x+w/2,y+h/2]);a=math.radians(angle)
        rotation=np.array([[math.cos(a),-math.sin(a)],[math.sin(a),math.cos(a)]])
        local=(points-centre)@rotation+centre
        u=np.clip((local[:,0]-x)/w,0,1);v=np.clip((local[:,1]-y)/h,0,1)
        local[:,0]+=(u-.5)*w*.05*np.sin(math.pi*v)
        local[:,1]-=h*np.where(v<.5,.04,.015)*np.cos(math.pi*v)*np.sin(math.pi*u)
        points=(local-centre)@rotation.T+centre
        distance=np.r_[0,np.cumsum(np.sqrt(np.sum(np.diff(points,axis=0)**2,axis=1)))]
        length=distance[-1]; count=max(24,round(length/3))
        samples=np.linspace(0,length,count,endpoint=False)
        points=np.column_stack([np.interp(samples,distance,points[:,i]) for i in (0,1)])
        # A continuous circular kernel softens angular type terminals and counters.
        sigma=(6 if letter=='A' else 4) if length>350 else 1.3
        kernel=np.arange(-math.ceil(sigma*3),math.ceil(sigma*3)+1)
        weights=np.exp(-.5*(kernel/sigma)**2);weights/=sum(weights)
        points=sum(np.roll(points,int(k),axis=0)*weight for k,weight in zip(kernel,weights))
        points=points[::6]
        # Quadratics through edge midpoints retain continuous tangents after rounding.
        mids=(points+np.roll(points,-1,axis=0))/2
        contour_path=f'M{mids[-1,0]:.2f} {mids[-1,1]:.2f} '+ ' '.join(f'Q{p[0]:.2f} {p[1]:.2f} {m[0]:.2f} {m[1]:.2f}' for p,m in zip(points,mids))+'Z'
        contours.append(contour_path)
        area=np.sum(points[:,0]*np.roll(points[:,1],-1)-points[:,1]*np.roll(points[:,0],-1))
        contour_data.append((contour_path,area,points.mean(axis=0)))
    outer_sign=np.sign(max(contour_data,key=lambda c:abs(c[1]))[1])
    counters=[(d,float(c[0]),float(c[1])) for d,area,c in contour_data if np.sign(area)!=outer_sign]
    return ' '.join(contours), x, y, w, h, counters


def main():
    body.clear(); defs.clear()
    gradient('purple', [(0,'#a02cdd'),(.3,'#7811bc'),(1,'#43086b')])
    gradient('gold', [(0,'#ffff61'),(.22,'#ffef13'),(.60,'#ffd300'),(1,'#ffb400')])
    # Continuous-looking narrow ramps model the broad, rounded painted bevels.
    def mix(a,b,t):
        return '#'+''.join(f'{round(int(a[i:i+2],16)*(1-t)+int(b[i:i+2],16)*t):02x}' for i in (1,3,5))
    for i in range(11):
        t=i/10
        gradient(f'goldBevel{i}',[(0,mix('#ffffbc','#ffff61',t)),(.28,mix('#fffb65','#ffef13',t)),(.63,mix('#ffd91c','#ffd300',t)),(1,mix('#f59c00','#ffb400',t))])
        gradient(f'iceBevel{i}',[(0,'#ffffff'),(.3,mix('#ffffff','#f0faff',t)),(1,mix('#71ceff','#b8e1ff',t))])
    gradient('ice', [(0,'#ffffff'),(.35,'#f0faff'),(1,'#b8e1ff')])
    gradient('goldCounter',[(0,'#df8300'),(.5,'#f3ae05'),(1,'#fff87c')])
    gradient('iceCounter',[(0,'#b4e5ff'),(1,'#76c9ff')])
    gradient('blueDepth', [(0,'#32c5ff'),(1,'#0073e9')])
    gradient('green', [(0,'#a8e80a'),(.3,'#79ca00'),(1,'#177400')])
    gradient('leaf', [(0,'#c8f224'),(.4,'#7dcd00'),(1,'#218900')])
    gradient('pink', [(0,'#ffb7ef'),(.3,'#ff78d1'),(1,'#ec2893')])
    gradient('bus', [(0,'#fff954'),(.22,'#ffe51b'),(.72,'#ffc700'),(1,'#f7a000')])
    gradient('glass', [(0,'#7ceaff'),(.35,'#29c6f5'),(1,'#0087d6')])
    gradient('tyre', [(0,'#57506c'),(.55,'#29273e'),(1,'#151427')])
    gradient('hub', [(0,'#e0e6fc'),(.3,'#b8c0dd'),(1,'#727b9c')])
    gradient('road', [(0,'#5bcfff'),(.4,'#2faff8'),(1,'#0872d9')])
    defs.append('<radialGradient id="goldLight" cx=".28" cy=".12" r=".86"><stop stop-color="#ffffb0" stop-opacity=".52"/><stop offset=".48" stop-color="#ffff73" stop-opacity=".14"/><stop offset="1" stop-color="#ffff73" stop-opacity="0"/></radialGradient>')
    defs.append('<radialGradient id="goldShade" cx=".34" cy=".18" r="1.0"><stop stop-color="#f98a00" stop-opacity="0"/><stop offset=".58" stop-color="#f98a00" stop-opacity="0"/><stop offset="1" stop-color="#f98a00" stop-opacity=".30"/></radialGradient>')
    defs.append('<radialGradient id="iceLight" cx=".25" cy=".08" r=".95"><stop stop-color="#ffffff" stop-opacity=".7"/><stop offset=".6" stop-color="#ffffff" stop-opacity=".15"/><stop offset="1" stop-color="#ffffff" stop-opacity="0"/></radialGradient>')

    # The single contour joins bus, foliage, lettering and road into one sticker.
    sticker = 'M662 236 C650 172 655 105 699 78 C745 48 848 24 924 17 C1019 6 1070 35 1083 111 L1090 122 Q1126 118 1129 152 L1130 193 Q1127 217 1101 221 L1105 245 C1191 223 1276 251 1300 317 Q1325 369 1284 429 C1338 394 1380 415 1374 466 Q1430 488 1403 552 Q1434 615 1391 675 Q1359 719 1308 707 C1299 786 1263 844 1199 842 Q1167 840 1139 825 Q986 862 880 847 Q777 848 711 868 Q660 883 632 844 C526 872 450 840 444 776 L438 704 C372 700 335 669 337 615 Q319 572 343 539 Q323 492 359 473 Q332 417 371 399 Q416 380 467 418 Q439 348 464 301 C492 251 580 231 662 236Z'
    add(sticker,'#b7b0d0', '#b7b0d0', 7, 'transform="translate(0 9)"')
    add(sticker,'#ffffff','#ffffff',17)
    add(sticker,'#ffffff','#e4d6f1',3)
    backing='M470 401 C421 292 520 238 618 246 Q695 247 746 291 Q784 266 845 280 L952 281 Q1010 267 1044 282 C1165 211 1301 261 1290 352 Q1289 399 1249 430 L1260 551 Q1218 568 1228 650 L1240 752 Q1264 829 1190 828 Q1169 831 1140 810 Q913 838 733 816 Q597 864 503 816 Q443 801 456 727 L466 661 Q529 648 540 607 L509 537Z'
    add(backing,'url(#purple)','#9b25d1',5)

    # Organic leaves and scalloped shrubs sit behind the type.
    for mirrored in (False, True):
        body.append('<g'+(' transform="translate(1750 28) scale(-1 1)"' if mirrored else '')+'>')
        add('M495 536 C426 522 363 479 366 428 C369 383 450 414 477 452 Q500 481 495 536Z','url(#leaf)','#3f9302',3)
        add('M475 505 Q427 442 389 425 Q439 434 475 480Z','#d4f63c',extra='opacity=".6"')
        shrub='M493 501 C469 469 428 476 416 500 C374 488 345 518 350 553 Q351 575 365 585 C330 624 359 673 397 674 Q415 707 446 691 C485 715 521 672 520 639 Q548 610 523 579 Q547 534 513 513Z'
        add(shrub,'url(#green)','#338905',3)
        ellipse(412,545,44,47,'url(#leaf)')
        ellipse(483,565,39,44,'url(#green)')
        ellipse(393,628,36,34,'url(#green)')
        # Five distinct petals, with a shaded yellow centre.
        for i in range(5):
            a=math.radians(-90+i*72); cx=451+28*math.cos(a);cy=575+28*math.sin(a)
            ellipse(cx,cy,18,22,'url(#pink)','#d62b8e',1.4,f'transform="rotate({i*72} {cx} {cy})"')
        ellipse(451,578,16,16,'#eb9700')
        ellipse(451,574,15,15,'url(#gold)')
        ellipse(447,568,6,3,'#ffffb0',extra='opacity=".85"')
        body.append('</g>')

    road='M636 798 Q746 754 879 769 Q1048 791 1205 765 L1211 823 Q1051 855 894 841 Q777 830 696 858 Q657 871 636 798Z'
    add(road,'url(#road)')
    for d in ['M734 827 Q764 818 791 816','M842 812 Q868 809 891 810','M940 811 L988 812','M1040 811 Q1060 811 1083 806']:
        add(d,'none','#f5fcff',14)

    # Authored three-quarter bus: curved roof, four inset windows and visible hubs.
    bus='M672 249 L677 154 Q678 104 720 86 C767 64 855 42 932 33 Q1032 18 1054 82 Q1069 124 1077 238 Q1081 265 1047 271 L731 278 Q673 278 672 249Z'
    add('M1060 124 Q1103 116 1103 144 L1106 183 Q1106 202 1080 204Z','url(#tyre)','#867caa',5)
    add(bus,'#e98a00','#c57408',4,'transform="translate(0 6)"')
    add(bus,'url(#bus)','#ffbd0a',5)
    add('M684 149 Q685 109 727 92 Q843 45 944 39 Q1021 29 1044 78 Q1016 37 942 48 Q828 62 726 103 Q696 116 684 149Z','#ffffb0',extra='opacity=".9"')
    # Wheel arches expose tyres instead of painting over the wheel faces.
    for x,y in [(726,250),(831,256)]:
        ellipse(x,y,25,40,'url(#tyre)','#21182e',3)
        ellipse(x,y+1,15,25,'url(#hub)','#555374',3)
        add(f'M{x-7} {y-17} Q{x+1} {y-27} {x+8} {y-10}','none','#f1f3ff',3)
        add(f'M{x-31} {y+9} C{x-38} {y-55} {x+34} {y-57} {x+34} {y+10}','none','#ffdd1c',15)
        add(f'M{x-27} {y+5} C{x-29} {y-43} {x+29} {y-45} {x+29} {y+8}','none','#db8b00',4)
    windows=[
        'M695 148 Q696 136 710 132 L728 128 Q737 126 737 137 L735 174 Q735 183 725 185 L706 188 Q694 189 694 178Z',
        'M757 131 Q757 119 768 117 L791 111 Q801 109 801 121 L800 167 Q800 177 788 178 L768 181 Q755 183 755 171Z',
        'M822 116 Q821 103 834 100 L855 96 Q868 93 869 108 L872 157 Q873 172 859 173 L838 175 Q823 176 823 161Z',
        'M912 101 C917 72 959 70 1010 76 Q1040 79 1045 113 L1053 157 Q1058 176 1036 179 L943 184 Q918 184 916 165Z']
    for i,d in enumerate(windows):
        add(d,'#ce8c00','#ffe668',12)
        add(d,'#15405a','#f09c04',7)
        add(d,'url(#glass)','#176391',4)
        defs.append(f'<clipPath id="window{i}">{path(d,"white")}</clipPath>')
        if i==3:
            add('M940 76 L981 75 L931 183 L917 180Z','#baf8ff',extra='opacity=".45" clip-path="url(#window3)"')
            add('M1018 76 L1036 86 L990 182 L963 184Z','#79e7ff',extra='opacity=".25" clip-path="url(#window3)"')
        add(d,'none','#8ceeff',2,'transform="translate(2 2)" opacity=".5"')
    for x,y,r in [(945,225,20),(1053,217,15)]:
        ellipse(x,y,r+4,r+4,'#e7a111')
        ellipse(x,y,r,r,'#fff8ad','#ffffdf',3)
        ellipse(x,y-2,r-5,r-5,'#ffffff')
    for y in (223,238):add(f'M987 {y} L1020 {y-3}','none','#bd6500',8)
    add('M888 259 Q963 275 1065 249','none','#ffdc36',6)
    ellipse(911,52,12,6,'#fff',extra='transform="rotate(-20 911 52)"')

    rows=[('BUS',[(505,270,244,260,-10),(774,291,222,240,1),(1035,278,219,260,10)],'gold'),
          ('JAM',[(503,554,207,209,-10),(728,548,199,207,-2),(949,561,242,204,5)],'ice')]
    letters=[]
    for word, positions, face in rows:
        for ch, pos in zip(word,positions):letters.append((glyph(ch,*pos),face))
    # All broad backing shapes first, so adjacent bevels never clip one another.
    for (d,*_),face in letters:
        add(d,'url(#purple)','url(#purple)',58,'transform="translate(0 18)"')
    for index,((d,x,y,w,h,counters),face) in enumerate(letters):
        defs.append(f'<clipPath id="letter{index}">{path(d,"white")}</clipPath>')
        # Tightly spaced copies create continuous extrusion, including counters.
        for off in range(23,0,-6):
            shade=mix('#ff9900','#b34c00',off/23) if face=='gold' else mix('#15b8ff','#0066ca',off/23)
            add(d,shade,shade,14,extra=f'transform="translate(0 {off})"')
        add(d,'#ec8800' if face=='gold' else '#009cf5', '#ec8800' if face=='gold' else '#0095f1',14,'transform="translate(0 9)"')
        bevel='goldBevel' if face=='gold' else 'iceBevel'
        for level in range(11):
            add(d,f'url(#{bevel}{level})',f'url(#{bevel}{level})',28-level*2.8)
        add(d,f'url(#{face})')
        add(d,'url(#goldLight)' if face=='gold' else 'url(#iceLight)')
        if face=='gold':add(d,'url(#goldShade)')
        # Restore open counters after the convex bevel passes. Their original glyph
        # contours define the concave rim and the dark interior, never painted plugs.
        for hole,cx,cy in counters:
            factor=1.0 if face=='gold' else 1.25
            transform=f'translate({cx} {cy}) scale({factor} {factor}) translate({-cx} {-cy})'
            add(hole,'#3d0751' if face=='gold' else '#08357b','url(#goldCounter)' if face=='gold' else 'url(#iceCounter)',6,f'transform="{transform}"')
        ellipse(x+w*.19,y+h*.075,13,7,'#fff',extra=f'transform="rotate(-25 {x+w*.19} {y+h*.075})" opacity=".83" clip-path="url(#letter{index})"')

    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="1760" height="887" viewBox="0 0 1760 887"><title>Bus Jam constructed logo</title><defs>{"".join(defs)}</defs>{"".join(body)}</svg>\n'
    OUT.write_text(svg,encoding='utf-8')
    meta=OUT.with_suffix('.svg.meta')
    if not meta.exists():
        template=(OUT.parent/'logo.svg.meta').read_text()
        template=template.replace('f2696ae00a6dcc0439c3b15dff088fa3',uuid.uuid4().hex)
        meta.write_text('\n'.join(line.rstrip() for line in template.splitlines())+'\n',encoding='utf-8')
    EVIDENCE.mkdir(exist_ok=True)
    metrics={'font':'Fredoka-Bold.ttf for U/S/M; LilitaOne-Regular.ttf for B/J/A','canvas':[1760,887],'paths':svg.count('<path '),'method':'Smoothed real glyph outlines and authored shapes; no tracing or raster embedding.'}
    (EVIDENCE/'construction.json').write_text(json.dumps(metrics,indent=2))
    print(json.dumps(metrics))


if __name__=='__main__':main()
