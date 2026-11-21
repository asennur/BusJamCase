"""ZUIR-3 constructed modal details; shares the Result/HUD SVG and ZAA pipeline."""
import argparse
from pathlib import Path
from GenerateHudArt import save, gradient, rect, ellipse, path


def lives():
    defs=gradient('blue',['#4a99ff','#3486fa','#2878ee','#265bcf','#184bc4'])
    body=''
    for i in range(7,0,-1):
        body+=rect(12-i,15-i,476+2*i,740+2*i,37+i,'#061b40',extra='opacity=".035"')
    body+=rect(8,8,484,748,41,'#163d94')+rect(13,12,474,738,36,'url(#blue)')
    body+=path('M17 90 Q17 17 88 17 H412 Q483 17 483 90 Q469 26 412 26 H88 Q32 26 17 90Z','#7cc9ff',extra='opacity=".6"')
    body+=path('M17 680 Q17 746 88 746 H412 Q483 746 483 680 Q473 737 412 737 H88 Q28 737 17 680Z','#10359a',extra='opacity=".6"')
    save('surface_lives_inner',500,770,defs,body)
    defs='<radialGradient id="halo"><stop stop-color="#fff6a7" stop-opacity=".85"/><stop offset=".25" stop-color="#d7ffd9" stop-opacity=".65"/><stop offset=".58" stop-color="#9aeaff" stop-opacity=".36"/><stop offset="1" stop-color="#7bdcff" stop-opacity="0"/></radialGradient>'
    save('lives_halo',500,500,defs,ellipse(250,250,245,245,'url(#halo)'))
    # Compose the same outer bezel with a separate blue socket and halo path.
    import xml.etree.ElementTree as ET
    folder=Path(__file__).resolve().parents[1]/'Assets/_BusJam/UI/Vector'
    parts=[];all_defs=''
    for name,transform in [('surface_result_modal',None),('surface_lives_inner','translate(32 180) scale(1.072 1)'),('lives_halo','translate(50 210)')]:
        root=ET.parse(folder/(name+'.svg')).getroot()
        children=''
        for child in root:
            tag=child.tag.split('}')[-1]
            if tag=='defs':
                import re
                definitions=''.join(ET.tostring(x,encoding='unicode') for x in child)
            elif tag!='title':children+=ET.tostring(child,encoding='unicode')
        ids=re.findall(r'id="([^"]+)"',definitions)
        for ident in ids:
            definitions=definitions.replace(f'id="{ident}"',f'id="{name}_{ident}"').replace(f'url(#{ident})',f'url(#{name}_{ident})')
            children=children.replace(f'url(#{ident})',f'url(#{name}_{ident})')
        all_defs+=definitions
        parts.append('<g'+(f' transform="{transform}"' if transform else '')+'>'+children+'</g>')
    save('surface_lives_modal',600,1000,all_defs.replace('ns0:','').replace(':ns0',''),''.join(parts).replace('ns0:','').replace(':ns0',''))


def toggle():
    for state,colors in [('on',['#bdf3ff','#69baff','#388bfa','#265bcf','#163da4']),('off',['#9babc7','#778bab','#546f9b','#3c537b','#233b61'])]:
        defs=gradient('rim',['#9cdfff','#6678ff','#3462d4','#183673'])+gradient('face',colors)
        body=''
        for i in range(7,0,-1):body+=ellipse(64,68,53+i,53+i,'#071630',extra='opacity=".03"')
        body+=ellipse(64,68,56,56,'#102951')+ellipse(64,62,56,56,'url(#rim)')+ellipse(64,62,47,47,'#123981')+ellipse(64,61,44,44,'url(#face)')
        body+=path('M22 58 C21 16 85 5 104 42 C79 20 40 19 22 58Z','#dcf7ff',extra='opacity=".55"')
        body+=path('M25 81 Q64 120 104 81 Q93 111 64 112 Q36 110 25 81Z','#112e73',extra='opacity=".55"')
        save('toggle_round_'+state,128,132,defs,body)


def tip():
    defs=gradient('face',['#51536e','#4d4f68','#484a63','#42445b','#393b50'])
    body=''
    for i in range(5,0,-1):body+=rect(7-i,10-i,486+2*i,182+2*i,18+i,'#190e1e',extra='opacity=".045"')
    body+=rect(5,8,490,184,18,'#262332')+rect(5,4,490,184,18,'#181822')+rect(8,7,484,176,15,'url(#face)')
    body+=path('M11 28 Q11 10 28 10 H472 Q489 10 489 28 Q483 15 470 15 H30 Q16 15 11 28Z','#77778c',extra='opacity=".35"')
    body+=path('M10 157 Q10 182 30 182 H470 Q490 182 490 157 Q487 186 470 186 H30 Q13 186 10 157Z','#262536')
    save('panel_tip',500,200,defs,body)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('family',choices=['lives','toggle','tip']);a=p.parse_args();globals()[a.family]()
