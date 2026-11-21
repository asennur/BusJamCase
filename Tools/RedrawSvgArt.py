"""Rebuild painted UI as source-contour regions with sampled local gradients.

No embedded raster, SVG filters or runtime generation. Source pixels determine
region boundaries and gradient stops; translucent nested contours retain shadows.
Run with Python, Pillow, numpy, OpenCV and vtracer. Preview/render uses Unity ZAA.
"""
from pathlib import Path
from PIL import Image, ImageFilter
import numpy as np
import cv2, vtracer
import xml.etree.ElementTree as ET
import re,json,sys,subprocess

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/_BusJam/UI/Vector'
EVIDENCE=ROOT.parent/'BusJamCase_ZVEC_Review/svgr'
SOURCE=ROOT/'Assets/_BusJam/Art/UI'
NS='http://www.w3.org/2000/svg'
ET.register_namespace('',NS)

def color(c):return '#'+''.join(f'{round(float(v)):02x}' for v in np.clip(c,0,255))
def tag(n):return '{'+NS+'}'+n

def contour_path(mask):
 contours,hierarchy=cv2.findContours(np.uint8(mask)*255,cv2.RETR_CCOMP,cv2.CHAIN_APPROX_SIMPLE)
 parts=[]
 for c in contours:
  if cv2.contourArea(c)<12:continue
  pts=cv2.approxPolyDP(c,1.15,True)[:,0,:].astype(float)
  if len(pts)<3:continue
  parts.append('M'+' L'.join(f'{p[0]:.2f} {p[1]:.2f}' for p in pts)+'Z')
 return ' '.join(parts)

def rebuild(name,source,width=800,colors=96,speckle=10):
 im=Image.open(SOURCE/(source+'.png')).convert('RGBA');original=im.size
 scale=min(1,width/max(im.size));im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
 w,h=im.size;rgba=np.array(im);alpha=rgba[:,:,3];rgb=im.convert('RGB').filter(ImageFilter.MedianFilter(3))
 a=np.array(rgb);opaque=alpha>=245
 # A common source-sampled palette keeps every region's colour family intact.
 sample=Image.fromarray(a[opaque].reshape(1,-1,3))
 pal=sample.quantize(colors=colors,method=Image.Quantize.MEDIANCUT)
 q=rgb.quantize(palette=pal,dither=Image.Dither.NONE).convert('RGB')
 quant=np.array(q);qi=q.convert('RGBA');qi.putalpha(Image.fromarray(np.uint8(opaque)*255))
 work=EVIDENCE/'work';work.mkdir(exist_ok=True,parents=True)
 qi.save(work/(name+'.png'));trace=work/(name+'.svg')
 vtracer.convert_image_to_svg_py(str(work/(name+'.png')),str(trace),colormode='color',hierarchical='stacked',mode='spline',filter_speckle=speckle,color_precision=8,layer_difference=0,corner_threshold=75,length_threshold=3.5,max_iterations=10,splice_threshold=45,path_precision=2)
 root=ET.parse(trace).getroot();root.set('viewBox',f'0 0 {w} {h}');root.set('width',str(original[0]));root.set('height',str(original[1]))
 title=ET.Element(tag('title'));title.text=name.replace('_',' ')+' — sampled painted contours and shading';root.insert(0,title)
 defs=ET.Element(tag('defs'));root.insert(1,defs)
 # Keep alpha shadows and rim coverage which opaque-only tracing used to discard.
 shadow_pixels=rgba[(alpha>12)&(alpha<180),:3]
 shadow_color=np.median(shadow_pixels,axis=0) if len(shadow_pixels) else [30,20,35]
 prev=0.;shadow_count=0
 for level in [.02,.045,.08,.13,.20,.30,.43,.58,.73,.86,.94,.975]:
  d=contour_path(alpha>=round(level*255))
  if not d:continue
  e=ET.Element(tag('path'),{'d':d,'fill':color(shadow_color),'fill-rule':'evenodd','opacity':f'{(level-prev)/(1-prev):.5f}'})
  root.insert(2+shadow_count,e);shadow_count+=1;prev=level
 gradient_count=0;stops_total=0
 for i,e in enumerate(list(root)):
  if not e.tag.endswith('path') or e.get('opacity') is not None:continue
  fill=e.get('fill','')
  if not fill.startswith('#'):continue
  tx,ty=(list(map(float,re.findall(r'[-+]?\d*\.?\d+',e.get('transform','translate(0,0)'))))+[0,0])[:2]
  nums=np.array(list(map(float,re.findall(r'[-+]?\d*\.?\d+',e.get('d','')))))
  if not len(nums) or len(nums)%2:continue
  points=nums.reshape(-1,2)+[tx,ty];low=np.maximum(np.floor(points.min(0)-1).astype(int),0);hi=np.minimum(np.ceil(points.max(0)+2).astype(int),[w,h])
  x0,y0=low;x1,y1=hi
  if x1<=x0 or y1<=y0:continue
  target=np.array(tuple(bytes.fromhex(fill[1:])))
  mask=np.all(quant[y0:y1,x0:x1]==target,axis=2)&opaque[y0:y1,x0:x1]
  yy,xx=np.where(mask)
  if len(xx)<12:continue
  coords=np.column_stack((xx+x0-tx,yy+y0-ty)).astype(float);pixels=a[y0+yy,x0+xx].astype(float)
  centre=coords.mean(0);co=np.linalg.lstsq(np.column_stack((coords-centre,np.ones(len(xx)))),pixels,rcond=None)[0]
  u,sv,vh=np.linalg.svd(co[:2],full_matrices=False);axis=u[:,0]
  proj=(coords-centre)@axis;lo,hi=np.percentile(proj,[0,100]);span=hi-lo
  if span<1:continue
  # Five bins preserve curved gloss/shadow ramps rather than reducing them to endpoints.
  stops=[]
  for fraction in [0,.25,.5,.75,1]:
   p=lo+span*fraction;sel=np.abs(proj-p)<max(1,span*.19)
   if sel.sum()<3:c=co[2]+axis*p@co[:2]
   else:c=np.median(pixels[sel],axis=0)
   stops.append((fraction,np.clip(c,0,255)))
  if np.max(np.ptp(np.array([c for _,c in stops]),axis=0))<2:continue
  p0=centre+axis*lo;p1=centre+axis*hi
  g=ET.SubElement(defs,tag('linearGradient'),{'id':f'paint{i}','gradientUnits':'userSpaceOnUse','x1':f'{p0[0]:.3f}','y1':f'{p0[1]:.3f}','x2':f'{p1[0]:.3f}','y2':f'{p1[1]:.3f}'})
  for f,c in stops:ET.SubElement(g,tag('stop'),{'offset':str(f),'stop-color':color(c)})
  e.set('fill',f'url(#paint{i})');gradient_count+=1;stops_total+=len(stops)
 dest=OUT/(name+'.svg');ET.ElementTree(root).write(dest,encoding='unicode');dest.write_text(dest.read_text(encoding='utf-8')+'\n',encoding='utf-8')
 result={'asset':name,'source':source,'sourceSize':original,'workingSize':[w,h],'paths':sum(e.tag.endswith('path') for e in root.iter()),'gradients':gradient_count,'stops':stops_total,'shadowLayers':shadow_count,'palette':colors}
 print(json.dumps(result),flush=True);return result

def restore_detailed_body(name,source):
 # Earlier hand-polished regions are more faithful than a coarser new palette.
 # Retain those contours and reconstruct the discarded source-alpha shadow.
 original_name='logo' if name.startswith('logo') else name
 data=subprocess.check_output(['git','show','c2c63e9:Assets/_BusJam/UI/Vector/'+original_name+'.svg'])
 root=ET.fromstring(data);w,h=map(float,root.get('viewBox').split()[2:]);im=Image.open(SOURCE/(source+'.png')).convert('RGBA').resize((round(w),round(h)),Image.Resampling.LANCZOS)
 rgba=np.array(im);alpha=rgba[:,:,3];pixels=rgba[(alpha>12)&(alpha<180),:3];shade=np.median(pixels,axis=0) if len(pixels) else [30,20,35]
 prev=0;count=0
 for level in [.02,.045,.08,.13,.20,.30,.43,.58,.73,.86,.94,.975]:
  d=contour_path(alpha>=round(level*255))
  if not d:continue
  e=ET.Element(tag('path'),{'d':d,'fill':color(shade),'fill-rule':'evenodd','opacity':f'{(level-prev)/(1-prev):.5f}'})
  root.insert(1+count,e);count+=1;prev=level
 title=ET.Element(tag('title'));title.text=name.replace('_',' ')+' painted regions with restored soft shadow';root.insert(0,title)
 # Expand the existing smooth ramps to five explicit stops without changing their colour endpoints.
 for g in root.iter(tag('linearGradient')):
  stops=list(g)
  if len(stops)!=2:continue
  ca=np.array(tuple(bytes.fromhex(stops[0].get('stop-color')[1:])));cb=np.array(tuple(bytes.fromhex(stops[1].get('stop-color')[1:])))
  for child in list(g):g.remove(child)
  for f in [0,.25,.5,.75,1]:ET.SubElement(g,tag('stop'),{'offset':str(f),'stop-color':color(ca*(1-f)+cb*f)})
 ET.ElementTree(root).write(OUT/(name+'.svg'),encoding='unicode')
 result={'asset':name,'source':source,'paths':sum(e.tag.endswith('path') for e in root.iter()),'shadowLayers':count,'body':'higher-detail baseline regions c2c63e9','gradients':sum(e.tag.endswith('Gradient') for e in root.iter())}
 print(json.dumps(result),flush=True);return result

ASSETS={
 'btn_settings':('btn_settings',512,64), 'icon_coin':('icon_coin',640,72),
 'coin_pile':('coin_pile',950,80), 'fx_tap_hand':('fx_tap_hand',512,56),
 'icon_close':('icon_close',512,72), 'icon_clock':('icon_clock',640,80),
 'icon_heart':('icon_heart',512,64),'icon_heart_broken':('icon_heart_broken',640,72),
 'panel_tip':('panel_tip',1100,64),'title_complete':('title_complete',1200,96),
 'title_failed':('title_failed',1100,80),'ribbon_purple':('ribbon_purple',1200,96),
 'logo':('Renkli Bus Jam Oyun Logosu',1300,112),
 'logo_constructed':('Renkli Bus Jam Oyun Logosu',1300,112),
 'surface_white':('panel_inner',850,64),
 'surface_header':('panel_header',1000,80),'surface_modal':('panel_modal',850,96),
 'surface_badge':('badge_level',720,80),'surface_blue':('btn_round_blue',720,96),
 'button_green_normal':('btn_green',1200,112),'button_red_normal':('btn_red',1200,112),
 'button_yellow_normal':('btn_yellow',1200,112)
}
if __name__=='__main__':
 EVIDENCE.mkdir(exist_ok=True,parents=True)
 selected=sys.argv[1:] or list(ASSETS)
 metrics=json.loads((EVIDENCE/'generation-metrics.json').read_text(encoding='utf-8')) if (EVIDENCE/'generation-metrics.json').exists() else []
 for n in selected:
  result=restore_detailed_body(n,ASSETS[n][0]) if n in ['logo','logo_constructed','coin_pile'] else rebuild(n,*ASSETS[n]);metrics=[m for m in metrics if m['asset']!=n]+[result]
  (EVIDENCE/'generation-metrics.json').write_text(json.dumps(metrics,indent=2))
