"""Rebuild monochrome effects from sampled alpha contours and radial profiles."""
from pathlib import Path
import sys,uuid
sys.path.insert(0,str(Path(__file__).parent))
from RedrawSvgArt import *

def alpha_art(name,source,tint='#ffffff'):
 im=Image.open(SOURCE/(source+'.png')).convert('RGBA');orig=im.size;im.thumbnail((1000,1000),Image.Resampling.LANCZOS);a=np.array(im)[:,:,3];w,h=im.size
 body=[];prev=0
 # Nested offsets simulate a soft blur without unsupported SVG filters.
 for level in np.linspace(.006,.995,48):
  d=contour_path(a>=round(level*255))
  if d:body.append(f'<path d="{d}" fill="{tint}" fill-rule="evenodd" opacity="{(level-prev)/(1-prev):.5f}"/>');prev=level
 s=f'<svg xmlns="{NS}" width="{orig[0]}" height="{orig[1]}" viewBox="0 0 {w} {h}"><title>{name.replace("_"," ")} sampled soft alpha</title>'+''.join(body)+'</svg>\n'
 (OUT/(name+'.svg')).write_text(s)

def radial_glow():
 im=Image.open(SOURCE/'glow_soft.png').convert('RGBA');a=np.array(im)[:,:,3];c=np.array([127.5,127.5]);yy,xx=np.indices(a.shape);rr=np.sqrt((xx-c[0])**2+(yy-c[1])**2)/128
 stops=[]
 for r in np.linspace(0,1,17):
  sel=np.abs(rr-r)<.025;opacity=np.median(a[sel])/255 if sel.any() else 0
  stops.append(f'<stop offset="{r:.4f}" stop-color="#ffffff" stop-opacity="{opacity:.5f}"/>')
 (OUT/'glow_soft.svg').write_text(f'<svg xmlns="{NS}" width="256" height="256" viewBox="0 0 256 256"><title>Soft glow sampled radial falloff</title><defs><radialGradient id="light">'+''.join(stops)+'</radialGradient></defs><ellipse cx="128" cy="128" rx="128" ry="128" fill="url(#light)"/></svg>\n')

if __name__=='__main__':
 alpha_art('fx_sparkle','fx_sparkle');alpha_art('glow_rays','glow_rays');alpha_art('result_rays','glow_rays','#cf8bff');radial_glow()
 rebuild('fx_confetti_rect','fx_confetti_rect',256,8,1)
 rebuild('fx_confetti_star','fx_confetti_star',256,12,1)
 rebuild('ui_passenger_icon','ui_passenger_icon',128,8,1)
 # Heart/disc confetti have no PNG originals; keep the source family's white tint mask.
 for n,d in [('fx_confetti_disc','M32 5 A27 27 0 1 1 31.99 5Z'),('fx_confetti_heart','M32 57 C-18 26 13 -7 32 14 C51 -7 82 26 32 57Z')]:
  (OUT/(n+'.svg')).write_text(f'<svg xmlns="{NS}" width="64" height="64" viewBox="0 0 64 64"><title>{n.replace("_"," ")} white tint mask</title><defs><linearGradient id="paper" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#ffffff"/><stop offset=".5" stop-color="#ffffff"/><stop offset="1" stop-color="#ffffff"/></linearGradient></defs><path d="{d}" fill="url(#paper)"/></svg>\n')
 for palette in ['green','red','yellow']:
  normal=ET.parse(OUT/f'button_{palette}_normal.svg').getroot()
  for state in ['hover','pressed']:
   root=ET.fromstring(ET.tostring(normal));root.find(tag('title')).text=f'{palette} button {state}'
   if state=='pressed':
    g=ET.Element(tag('g'),{'transform':'translate(0 7) scale(1 .96)'})
    for e in list(root):
     if e.tag.endswith('path'):root.remove(e);g.append(e)
    root.append(g)
   else:
    # Subtle extra gloss; original palette remains source sampled.
    for e in root.iter(tag('stop')):
     col=e.get('stop-color');c=np.array(tuple(bytes.fromhex(col[1:])));e.set('stop-color',color(c*.97+255*.03))
   ET.ElementTree(root).write(OUT/f'button_{palette}_{state}.svg',encoding='unicode')
 for p in OUT.glob('surface_*.svg'):
  meta=p.with_suffix('.svg.meta')
  if not meta.exists():
   s=(OUT/'panel_tip.svg.meta').read_text(encoding='utf-8');s=re.sub(r'guid: [0-9a-f]{32}','guid: '+uuid.uuid4().hex,s,count=1);meta.write_text(s)
 print('Rebuilt source effects, derived tint masks, button states and surface metadata.')
