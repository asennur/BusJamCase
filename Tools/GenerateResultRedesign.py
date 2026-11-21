"""ZUIR-2 constructed SVGs. See external reference crops/checklists; no tracing.

Uses the existing Result/Fredoka path and HUD gradient/ZAA construction pipeline.
Run complete, fail or effects independently; never changes the timeline or logo.
"""
import argparse
import math
from GenerateResultArt import glyphs, star, path
from GenerateHudArt import save, gradient, rect, ellipse


def shadow(d, dy=12, color='#190e1e'):
    return ''.join(path(d, color, color, i*2, f'opacity=".025" transform="translate(0 {dy+i})"') for i in range(8,0,-1))


def complete():
    letters=glyphs('LEVEL',750,225,62,1740)+glyphs('COMPLETED!',1600,265,350,1740)
    shapes=[d for d,*_ in letters]+[star(325,215,76),star(1415,215,76)]
    defs=gradient('face',['#fff09a','#ffd96b','#f2b646','#ffb900','#f59b00'])
    body=''
    for d in shapes: body+=path(d,"#190e1e","#190e1e",72,'opacity=".22" transform="translate(0 64)"')
    for offset in (54,28):
        shade='#290545' if offset>40 else '#41066c'
        body+=''.join(path(d,shade,shade,58,f'transform="translate(0 {offset})"') for d in shapes)
    body+=''.join(path(d,'#652397','#b766d7',62) for d in shapes)
    body+=''.join(path(d,'#652397','#652397',48) for d in shapes)
    body+=''.join(path(d,'#fff2ad','#fff2ad',8,'transform="translate(0 -3)"') for d in shapes)
    body+=''.join(path(d,'url(#face)','#f9cd62',7) for d in shapes)
    save('title_complete',1740,800,defs,body)

    defs=gradient('band',['#c05bff','#aa38f1','#a32ae9','#8820d7','#7617bc'])+gradient('fold',['#491070','#591282','#7e1eb4','#9229ce'])
    tails='M255 216 Q115 191 34 264 Q26 280 48 306 L103 379 L60 504 Q157 509 354 469 L348 275Z'
    front='M228 182 C242 19 1278 19 1292 182 L1253 426 C1218 332 302 332 267 426Z'
    body=shadow(front,22)
    body+=path(tails,'url(#band)')+path(tails,'url(#band)',extra='transform="translate(1520 0) scale(-1 1)"')
    body+=path('M267 426 Q273 483 359 474 Q430 462 366 419 L319 388Z','url(#fold)')
    body+=path('M1253 426 Q1247 483 1161 474 Q1090 462 1154 419 L1201 388Z','url(#fold)')
    body+=path(front,'url(#band)')
    body+=path('M267 426 C302 332 1218 332 1253 426 Q1247 450 1216 456 C1140 365 380 365 304 456 Q273 450 267 426Z','#581080')
    body+=path('M241 180 C259 35 1255 35 1279 180 C1195 71 328 71 257 197Z','#d881ff',extra='opacity=".78"')
    body+=path('M61 496 Q210 483 354 460 L357 473 Q181 515 60 507Z','#b156de',extra='opacity=".55"')
    body+=path('M1459 496 Q1310 483 1166 460 L1163 473 Q1339 515 1460 507Z','#b156de',extra='opacity=".55"')
    save('ribbon_purple',1520,540,defs,body)
    coin_pile()


def gold_defs():
    return gradient('gold',['#fff6a1','#ffe93b','#ffcc08','#ffb007','#e48b04'])+gradient('side',['#ffb800','#ffcf13','#fff548','#ffc20b','#e89605'])+gradient('disk',['#ffed43','#ffe62a','#ffd015','#ffbd05'])


def bus():
    silhouette=rect(83,62,90,119,22,'#ffdf24')+rect(105,49,46,15,7,'#ffdf24')+rect(74,89,14,26,6,'#ffdf24')+rect(168,89,14,26,6,'#ffdf24')+rect(87,168,18,23,8,'#ffdf24')+rect(151,168,18,23,8,'#ffdf24')
    return '<g transform="translate(1 4)">'+silhouette.replace('#ffdf24','#b77902')+'</g>'+silhouette+rect(97,81,62,45,9,'#d38a09')+rect(113,52,29,7,3,'#bf7b05')+ellipse(99,151,7,7,'#cb8305')+ellipse(157,151,7,7,'#cb8305')


def face():
    return ellipse(128,138,114,114,'#d18a05')+ellipse(128,126,114,114,'url(#gold)')+ellipse(128,126,103,103,'#fff272')+ellipse(128,126,96,96,'#d39307')+ellipse(128,126,89,89,'url(#disk)')+path('M33 125 C25 53 77 15 128 16 C68 36 42 76 41 132Z','#fff9d7')+path('M205 70 C229 115 215 167 197 184 C211 145 213 100 205 70Z','#fff9bd')+bus()


def flat_coin(x,y,w=165):
    sx=w/256
    body=ellipse(x,y+22,w/2,32,'#cb7505')
    body+=rect(x-w/2,y,w,22,0,'url(#side)')+ellipse(x,y+18,w/2,29,'url(#side)')
    body+=ellipse(x,y,w/2,28,'#ec9a05')+ellipse(x,y-2,w/2-3,25,'url(#gold)')+ellipse(x,y-2,w/2-17,18,'#c68704')+ellipse(x,y-3,w/2-22,15,'#f8c020')
    body+=f'<g transform="translate({x-128*sx} {y-22}) scale({sx} .15)">{bus()}</g>'
    body+=path(f'M{x-w*.43} {y+8} Q{x-w*.38} {y+24} {x-w*.25} {y+28} L{x-w*.25} {y+40} Q{x-w*.4} {y+37} {x-w*.43} {y+18}Z','#fff78b',extra='opacity=".8"')
    return body


def coin_pile():
    body=''
    for i in range(7,0,-1): body+=ellipse(540,487,440+i*5,25+i*3,'#190e1e',extra='opacity=".035"')
    # Occlusion is back-to-front, with low elliptical stacks and three oblique face coins.
    stacks=[(280,445,4),(405,445,7),(530,445,10),(655,445,8),(780,445,6),(900,445,2),(160,455,1),(1010,450,1)]
    for x,y,n in stacks:
        for j in range(n): body+=flat_coin(x,y-j*34)
    for x,y,scale,angle in [(575,130,.72,-15),(625,290,.78,14),(415,367,.63,17)]:
        body+=f'<g transform="translate({x} {y}) rotate({angle}) scale({scale*.85} {scale})">{face()}</g>'
    save('coin_pile',1120,560,gold_defs(),body)


def fail():
    defs=gradient('bezel',['#4e9bff','#6974ff','#3f65ef','#265bcf','#4470e9'])+gradient('blue',['#388cff','#2879f5','#266de4','#265bcf','#1851c8'])
    body=''
    for i in range(8,0,-1): body+=rect(15-i,25-i,570+2*i,950+i*2,63+i,'#071630',extra='opacity=".025"')
    body+=rect(10,18,580,962,67,'#153db2')+rect(10,8,580,962,67,'url(#bezel)')+rect(32,29,536,918,49,'#1545b1')+rect(36,33,528,910,45,'url(#blue)')
    body+=path('M20 111 Q20 18 112 18 H488 Q580 18 580 111 Q566 36 488 34 H112 Q35 34 20 111Z','#92aaff',extra='opacity=".55"')
    save('surface_result_modal',600,1000,defs,body)
    defs=gradient('header',['#031b42','#05234e','#052c63','#052c63','#052a60'])
    shape='M18 125 Q18 14 125 14 H415 Q522 14 522 125 V162 Q522 190 495 190 H45 Q18 190 18 162Z'
    body=path(shape,'#5484ef','#4a83ee',7)+path(shape,'url(#header)','#103b80',3)
    body+=path('M23 123 Q23 20 125 20 H415 Q516 20 517 123 Q499 33 415 32 H125 Q42 32 23 123Z','#020f2a',extra='opacity=".35"')
    save('surface_result_header',540,210,defs,body)
    defs=gradient('socket',['#4752a2','#7165aa','#3254a6','#244ec0'])+gradient('white',['#fff','#f5f5f5','#f2f2f2','#f2f2f2','#dedede'])
    body=''
    for i in range(6,0,-1):body+=rect(13-i,19-i,474+i*2,444+i*2,57+i,'#071934',extra='opacity=".035"')
    body+=rect(8,8,484,452,64,'url(#socket)')+rect(15,13,470,442,57,'#abaec5')+rect(20,16,460,432,52,'#b4b5b9')+rect(22,18,456,423,50,'url(#white)')
    body+=path('M29 105 Q29 26 108 26 H392 Q471 26 471 105 Q455 42 392 42 H108 Q46 42 29 105Z','#fff')
    body+=path('M28 365 Q28 433 105 433 H395 Q472 433 472 365 Q462 444 395 444 H105 Q38 444 28 365Z','#b6b6b8',extra='opacity=".48"')
    save('surface_result_inner',500,480,defs,body)
    broken_heart()


def broken_heart():
    left='M252 441 C148 400 35 314 24 198 C10 85 77 34 141 38 C179 36 208 51 236 75 L251 124 L231 185 L251 226 L234 271 L251 325 L240 375Z'
    right='M264 441 L253 375 L275 324 L255 274 L274 228 L254 184 L276 127 L270 76 C302 48 334 36 369 39 C441 38 494 104 480 199 C465 308 354 405 264 441Z'
    defs=gradient('red',['#ff7d89','#ff3955','#ff1538','#fb0020','#df0013'],True)+gradient('pink',['#fff2e8','#ffacb2','#ff677d','#ff3d64'],True)
    body=shadow(left,18,'#707070')+shadow(right,18,'#707070')
    body+=path(left,'#bcbcbc',extra='transform="translate(-4 19)"')+path(right,'#bcbcbc',extra='transform="translate(4 19)"')
    body+=path(left,'url(#red)')+path(right,'url(#red)')
    body+=path('M236 75 L251 124 L231 185 L251 226 L234 271 L251 325 L240 375 L252 441 L247 325 L225 272 L239 228 L220 185 L239 124Z','#c40017')
    body+=path('M270 76 L276 127 L254 184 L274 228 L255 274 L275 324 L253 375 L264 441 L263 376 L284 325 L267 275 L285 229 L265 184 L287 127Z','#b00010')
    for cx,cy,rx,ry in [(105,100,60,41),(366,102,54,43)]:
        for j in range(9,0,-1):body+=ellipse(cx,cy,rx*j/9,ry*j/9,'#ffe3d4',extra='opacity=".045"')
    save('icon_heart_broken',510,490,defs,body)


def effects():
    defs='<radialGradient id="halo" gradientUnits="userSpaceOnUse" cx="500" cy="500" r="500"><stop stop-color="#f8c4ff" stop-opacity=".45"/><stop offset=".23" stop-color="#dc9df3" stop-opacity=".4"/><stop offset=".55" stop-color="#b664d1" stop-opacity=".16"/><stop offset="1" stop-color="#8820d7" stop-opacity="0"/></radialGradient><radialGradient id="ray" gradientUnits="userSpaceOnUse" cx="500" cy="500" r="500"><stop stop-color="#fbe7ff" stop-opacity=".32"/><stop offset=".35" stop-color="#dfb0ef" stop-opacity=".14"/><stop offset=".75" stop-color="#bf7bd7" stop-opacity=".07"/><stop offset="1" stop-color="#8820d7" stop-opacity="0"/></radialGradient>'
    body=ellipse(500,500,495,410,'url(#halo)')
    for i in range(18):
        d='M495 503 Q475 400 430 42 Q475 6 520 27 L505 503Z'
        body+=path(d,'url(#ray)',extra=f'transform="rotate({i*20} 500 500)"')
    save('result_rays',1000,1000,defs,'<g transform="translate(0 250) scale(1 .5)">'+body+'</g>')
    defs='<radialGradient id="halo"><stop stop-color="#fffbd6" stop-opacity=".75"/><stop offset=".2" stop-color="#fff5b6" stop-opacity=".25"/><stop offset="1" stop-color="#e2adff" stop-opacity="0"/></radialGradient>'
    sparkle='M64 10 Q67 56 75 59 L113 64 Q75 67 71 74 L64 119 Q61 76 54 70 L15 64 Q54 61 59 54Z'
    body=ellipse(64,64,61,61,'url(#halo)')+path(sparkle,'#fff7c9')+ellipse(64,64,4,4,'#fff')
    save('fx_result_sparkle',128,128,defs,body)
    defs=gradient('paper',['#fff','#f9f9f9','#e0e0e0','#b2b2b2'])
    save('fx_confetti_star',64,64,defs,path(star(32,32,28),'url(#paper)','#595959',2.5))
    save('fx_confetti_heart',64,64,gradient('paper',['#fff','#fff','#ffe0d5','#d98369','#aa533f']),path('M32 58 C21 54 4 42 5 25 C4 9 22 3 32 16 C42 3 60 9 59 25 C60 42 43 54 32 58Z','url(#paper)','#595959',2.5))
    save('fx_confetti_disc',64,64,defs,ellipse(32,32,27,27,'url(#paper)',extra='stroke="#595959" stroke-width="2.5"'))
    save('fx_confetti_rect',64,76,defs,path('M12 6 Q37 0 54 7 Q60 10 58 22 L55 61 Q53 71 44 69 L13 66 Q5 65 7 54 L5 20 Q4 10 12 6Z','url(#paper)','#595959',2.5)+path('M12 11 Q35 5 54 12 L52 18 Q31 12 12 18Z','#fff',extra='opacity=".4"'))
    defs=gold_defs()+'<radialGradient id="glow"><stop stop-color="#fff38b" stop-opacity=".25"/><stop offset="1" stop-color="#ffe143" stop-opacity="0"/></radialGradient>'
    body=ellipse(128,130,128,128,'url(#glow)')+f'<g transform="translate(19 6) scale(.84 .94)">{face()}</g>'
    save('fx_flying_coin',256,262,defs,body)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('family',choices=['complete','fail','effects']);a=p.parse_args()
    globals()[a.family]()
    print('Constructed '+a.family+' result family; import through ZAA.')
