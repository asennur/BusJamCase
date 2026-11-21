"""Construct ZUIR HUD/button SVGs from the video checklist, using the ZAA pipeline.

Same explicit paths, layered translucent shadows and gradient stops as
GenerateResultArt; never trace or embed source video pixels. Run selected families.
"""
from pathlib import Path
import argparse
import math
import uuid

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/_BusJam/UI/Vector'

def path(d,fill,stroke=None,width=0,extra=''):
    return f'<path d="{d}" fill="{fill}"'+(f' stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round" stroke-linecap="round"' if stroke else '')+f' {extra}/>'

def rect(x,y,w,h,r,fill,extra=''):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" {extra}/>'

def ellipse(x,y,rx,ry,fill,extra=''):
    return f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="{fill}" {extra}/>'

def gradient(name,colors,radial=False):
    kind='radialGradient' if radial else 'linearGradient'
    attrs='cx=".32" cy=".2" r=".85"' if radial else 'x1="0" y1="0" x2="0" y2="1"'
    return f'<{kind} id="{name}" {attrs}>'+''.join(f'<stop offset="{i/(len(colors)-1):.3f}" stop-color="{c}"/>' for i,c in enumerate(colors))+f'</{kind}>'

def save(name,w,h,defs,body):
    (OUT/f'{name}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><title>{name.replace("_"," ")} — constructed video reference</title><defs>{defs}</defs>{body}</svg>\n',encoding='utf-8')
    meta=OUT/f'{name}.svg.meta'
    if not meta.exists():
        template=(OUT/'icon_coin.svg.meta').read_text(encoding='utf-8')
        import re
        template=re.sub(r'(?m)^guid: \w+',f'guid: {uuid.uuid4().hex}',template,count=1)
        meta.write_text(template,encoding='utf-8')

def button(palette,state):
    start=palette=='start'
    colors={'green':['#c9ff4c','#a8f51b','#51d51c','#3eb81f','#238b16'],
            'yellow':['#fff68c','#ffe75d','#fbda3c','#ffc222','#f6a70c'],
            'red':['#ff7e65','#f7462a','#e22b1d','#c21e19','#a8141b'],
            'start':['#d4ff55','#aefe18','#55dc15','#34b516','#23840f']}[palette]
    base,outer,light,inner=('#342047','#7f24c6','#d283ff','#45125e') if start else ('#ac4b13','#ff9517','#fff38b','#965109')
    depth={'green':'#116009','yellow':'#c37306','red':'#740e0a','start':'#0e5908'}[palette]
    defs=gradient('face',colors)+gradient('rim',[light,'#c379ff','#7e21bd','#4b146c'] if start else ['#fff28c','#ffcf38','#efb92a','#f58e18'])
    defs+=gradient('inset',[inner,depth,depth])+gradient('gloss',['#fff','#eaffac' if palette!='red' else '#ffbca6',colors[1]])
    body=''
    for i in range(7,0,-1):
        body+=rect(20-i*2,26-i,960+i*4,365+i*3,150+i*2,'#190e1e',f'opacity="{.012+i*.004:.3f}"')
    body+=rect(18,30,964,370,150,base)
    body+=rect(18,12,964,370,150,outer)
    body+=rect(23,13,954,356,146,'url(#rim)')
    body+=rect(37,28,926,330,133,light)
    body+=rect(43,35,914,320,128,'url(#rim)')
    body+=rect(51,41,898,308,122,'url(#inset)')
    body+=rect(66,51,868,278,109,'url(#face)')
    # Separate curved upper gloss and slim rim reflections; no rectangular overlay.
    body+=path('M79 148 Q79 62 174 62 H826 Q911 62 921 143 Q906 83 827 80 H175 Q106 80 79 148Z','url(#gloss)',extra='opacity=".86"')
    body+=path('M48 139 Q48 31 173 31 H824 Q939 31 952 135 Q938 41 824 41 H173 Q69 41 48 139Z','#fff',extra='opacity=".58"')
    body+=path('M91 262 Q108 323 182 325 H817 Q884 325 908 269 Q890 339 817 340 H182 Q111 340 91 262Z',depth,extra='opacity=".65"')
    if state=='hover':body+=rect(66,51,868,278,109,'#fff',extra='opacity=".065"')
    if state=='pressed':
        # Retain the native button's 5px translation; the artwork only changes light/depth.
        body+=rect(66,51,868,278,109,'#10320b' if palette!='red' else '#480504',extra='opacity=".12"')
        body+=path('M79 148 Q79 62 174 62 H826 Q911 62 921 143 Q900 99 826 98 H174 Q110 98 79 148Z',depth,extra='opacity=".16"')
    save(f'button_{palette}_{state}',1000,420,defs,body)

def plate():
    defs=gradient('rim',['#35d6ff','#469eff','#2f6efa','#5773ff'])+gradient('body',['#092b4b','#123d5c','#174361','#15405b','#10324f'])
    # Preserve every rim/shadow contour, with a common transparent interior cutout.
    def ring(x,y,w,h,r,fill,extra=''):
        d=f'M{x+r} {y} H{x+w-r} A{r} {r} 0 0 1 {x+w} {y+r} V{y+h-r} A{r} {r} 0 0 1 {x+w-r} {y+h} H{x+r} A{r} {r} 0 0 1 {x} {y+h-r} V{y+r} A{r} {r} 0 0 1 {x+r} {y} Z'
        hole='M49 21 A28 28 0 0 0 21 49 V115 A28 28 0 0 0 49 143 H471 A28 28 0 0 0 499 115 V49 A28 28 0 0 0 471 21 H49 Z'
        return path(d+' '+hole,fill,extra='fill-rule="evenodd" '+extra)
    body=''
    for i in range(5,0,-1):body+=ring(7-i,15-i,506+2*i,150+2*i,40+i,'#06162e',f'opacity="{.025:.3f}"')
    body+=ring(5,6,510,154,43,'#102b66')+ring(9,9,502,146,39,'url(#rim)')
    body+=ring(16,16,488,132,33,'#071f43')+rect(21,21,478,122,28,'url(#body)',extra='opacity=".40"')
    body+=path('M14 44 Q14 13 47 13 H473 Q503 13 507 43 Q500 20 473 20 H47 Q23 20 14 44Z','#9ce9ff',extra='opacity=".7"')
    save('hud_counter',520,180,defs,body)

def badge():
    defs=gradient('rim',['#af55ff','#8b10bd','#9d29cd','#5e1788'])+gradient('white',['#fff','#fff','#f8f8f7','#f3f2f1','#d6d4d5'])
    body=''
    for i in range(6,0,-1):body+=rect(12-i,19-i,216+2*i,216+2*i,57+i,'#231536',extra='opacity=".035"')
    body+=rect(8,7,224,226,60,'url(#rim)')+rect(13,11,214,216,55,'#d9b7f2')+rect(17,14,206,208,51,'url(#white)')
    body+=path('M23 69 Q23 21 70 21 H168 Q210 21 215 65 Q203 30 168 29 H70 Q37 29 23 69Z','#fff')
    save('hud_level_badge',240,246,defs,body)

HEART='M128 218 C104 218 17 157 15 93 C12 30 83 11 128 58 C171 11 244 30 241 93 C238 157 151 218 128 218Z'
def heart():
    defs=gradient('heart',['#ff8077','#ff4942','#e72323','#b80d15','#780d17'],True)+gradient('glint',['#fff7ed','#ffc2b8','#ff7770'])
    body=''
    for i in range(5,0,-1):body+=path(HEART,'#260d22',extra=f'opacity=".04" transform="translate(0 {i+5})"')
    body+=path(HEART,'url(#heart)','#610c15',5)
    body+=path('M22 91 C25 30 85 32 114 61 C73 43 35 53 22 91Z','#ffb0a0',extra='opacity=".6"')
    for i in range(10,0,-1):body+=ellipse(68,70,19+i*2.2,15+i*2.2,'#fff3e8',extra='opacity=".045" transform="rotate(-24 68 70)"')
    body+=path('M34 141 Q64 187 115 208 Q72 198 34 141Z','#ff6861',extra='opacity=".22"')
    save('icon_heart',256,240,defs,body)

def coin():
    defs=gradient('gold',['#fffba3','#ffe126','#ffd00c','#ffa507','#da7405'],True)+gradient('rim',['#fff8aa','#ffe333','#ffc517','#ffed57','#ff9d00'])+gradient('disk',['#ffe449','#ffc81b','#ffb106','#f09b06','#e78c09'])
    body=''
    for i in range(5,0,-1):body+=ellipse(129,139,112+i,117+i,'#311c09',extra='opacity=".03"')
    body+=ellipse(128,131,113,120,'#d67a03')+ellipse(128,125,113,119,'#ef9a00')+ellipse(128,122,108,114,'url(#rim)')+ellipse(128,122,93,100,'url(#gold)')+ellipse(128,122,82,88,'#e58b04')+ellipse(128,125,77,82,'url(#disk)')
    body+=path('M33 128 C26 53 71 13 128 16 C72 25 42 60 39 132Z','#fffce4')
    body+=path('M205 80 C225 118 215 166 198 182 C208 149 213 116 205 80Z','#fff9cf')
    # Embossed bus: roof sign, mirrors, windshield, lights and wheels.
    bus=rect(83,62,90,119,22,'#ffdf24')+rect(105,49,46,15,7,'#ffdf24')+rect(74,89,14,26,6,'#ffdf24')+rect(168,89,14,26,6,'#ffdf24')+rect(87,168,18,23,8,'#ffdf24')+rect(151,168,18,23,8,'#ffdf24')
    body+='<g transform="translate(1 4)">'+bus.replace('#ffdf24','#c17a02')+'</g>'+bus
    body+=rect(97,81,62,45,9,'#d98605')+rect(113,52,29,7,3,'#c27803')+ellipse(99,151,7,7,'#d48604')+ellipse(157,151,7,7,'#d48604')
    save('icon_coin',256,262,defs,body)

def plus():
    defs=gradient('green',['#77f13c','#21ce22','#0bbb1e','#039519','#02751a'])
    body=rect(5,9,86,84,25,'#073e18')+rect(5,4,86,84,25,'#153d19')+rect(9,7,78,77,21,'#75b93e')+rect(12,10,72,71,18,'url(#green)')
    body+=path('M18 35 Q18 15 38 15 H62 Q77 15 80 32 Q72 21 62 21 H38 Q25 21 18 35Z','#b1ff8c',extra='opacity=".8"')
    cross='M45 24 H51 Q55 24 55 29 V41 H67 Q72 41 72 46 V51 Q72 55 67 55 H55 V68 Q55 73 50 73 H45 Q41 73 41 68 V55 H28 Q23 55 23 50 V46 Q23 41 28 41 H41 V29 Q41 24 45 24Z'
    body+=path(cross,'#075e25',extra='transform="translate(0 3)"')+path(cross,'#fff8e6')
    save('hud_coin_plus',96,98,defs,body)

def gear(state):
    defs=gradient('purple',['#c978ff','#b83cff','#9726ed','#7810bd','#500784'],True)+gradient('ivory',['#fffde7','#f5f0d6','#dedcc7','#c5c6b7','#babdac'])
    body=''
    for i in range(5,0,-1):body+=ellipse(128,140,112+i,111+i,'#241335',extra='opacity=".025"')
    body+=ellipse(128,133,115,115,'#490965')+ellipse(128,124,114,114,'#6f1595')+ellipse(128,122,109,109,'url(#purple)')
    body+=path('M30 106 C40 29 113 3 174 35 C104 10 53 42 30 106Z','#edbaff',extra='opacity=".55"')
    points=[]
    for i in range(8):
        for a,r in [(i*45-18,53),(i*45-12,72),(i*45+12,72),(i*45+18,53)]:
            a=math.radians(a);points.append((128+math.cos(a)*r,121+math.sin(a)*r))
    d='M'+' L'.join(f'{x:.2f} {y:.2f}' for x,y in points)+'Z'
    body+=path(d,'#30234d','#30234d',7,extra='transform="translate(0 5)"')+path(d,'url(#ivory)','#53556c',3)
    body+=ellipse(128,121,27,27,'#4d157e')+ellipse(128,124,23,23,'#902ce4')
    if state!='normal':body+=ellipse(128,122,109,109,'#fff' if state=='hover' else '#21043e',extra='opacity=".07"')
    save('btn_settings'+('' if state=='normal' else '_'+state),256,262,defs,body)

def close(state):
    defs=gradient('red',['#ff7760','#f94330','#e62a22','#cd1b16','#a30e0c'],True)+gradient('ivory',['#fffce8','#f6f0d3','#e8dfbc','#d4caa4'])
    body=''
    for i in range(5,0,-1):body+=ellipse(128,141,112+i,111+i,'#240e1b',extra='opacity=".03"')
    body+=ellipse(128,132,115,115,'#5b0b0d')+ellipse(128,122,114,114,'#881313')+ellipse(128,121,106,106,'url(#red)')+ellipse(128,121,87,87,'none',extra='stroke="#ff9580" stroke-width="3" opacity=".58"')
    x='M86 67 Q78 60 69 70 Q61 79 68 87 L101 121 L68 155 Q61 164 71 173 Q80 181 88 173 L122 140 L156 174 Q164 181 173 171 Q182 162 174 154 L141 121 L174 87 Q181 79 171 69 Q162 61 154 69 L121 102Z'
    body+=path(x,'#6c211a','#6c211a',5,extra='transform="translate(0 5)"')+path(x,'url(#ivory)','#77432d',3)
    body+=path('M32 104 Q49 27 119 19 Q61 41 45 108Z','#ffb299',extra='opacity=".65"')
    if state!='normal':body+=ellipse(128,121,106,106,'#fff' if state=='hover' else '#410408',extra='opacity=".07"')
    save('icon_close'+('' if state=='normal' else '_'+state),256,262,defs,body)

def clock():
    defs=gradient('gold',['#fff4ac','#ffe235','#ffcb16','#ee9c06','#cb7104'])+gradient('dial',['#fffdf1','#fff6cf','#f7e6aa','#e4cc84','#cfb06b'])
    body=rect(107,7,42,26,8,'#a15d05')+rect(109,4,38,21,7,'url(#gold)')
    for i in range(5,0,-1):body+=ellipse(128,143,109+i,109+i,'#291d16',extra='opacity=".03"')
    body+=ellipse(128,136,111,112,'#a85d03')+ellipse(128,127,111,112,'url(#gold)')+ellipse(128,127,93,94,'#b57b09')+ellipse(128,127,85,87,'url(#dial)')
    body+=path('M35 126 C34 57 79 28 125 24 C70 43 44 74 43 130Z','#fffce0')
    for a in range(0,360,90):body+=path('M128 53 V64','none','#bba264',5,extra=f'transform="rotate({a} 128 127)"')
    body+=path('M128 77 V130 L168 150','none','#7e291b',13,extra='transform="translate(0 3)"')+path('M128 77 V127 L168 147','none','#eb3f29',11)+ellipse(128,127,11,11,'#b52b18')+ellipse(126,124,6,6,'#ff7850')
    save('icon_clock',256,262,defs,body)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('family',choices=['hud','cta','start','all'],default='all',nargs='?');args=parser.parse_args()
    if args.family in ('hud','all'):
        plate();badge();heart();coin();plus();clock()
        for s in ['normal','hover','pressed']:gear(s)
    if args.family in ('cta','all'):
        for p in ['green','yellow','red']:
            for s in ['normal','hover','pressed']:button(p,s)
        for s in ['normal','hover','pressed']:close(s)
    if args.family in ('start','all'):
        for s in ['normal','hover','pressed']:button('start',s)
    print(f'Constructed {args.family} SVG family; import through AntialiasedSvgImporter.')
