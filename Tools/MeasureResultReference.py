"""Measure the 20 fps reference crops and build labelled reference contact sheets."""
from pathlib import Path
import json
from PIL import Image, ImageDraw

FOLDER = Path(__file__).resolve().parents[1].parent / 'BusJamCase_ZVEC_Review/win'
REF = FOLDER / 'reference'

def frame(sequence,time):
    return Image.open(REF/f'{sequence}_{round(time*20)+1:04}.jpg').convert('RGB')

def mask_bounds(im, roi, predicate):
    x0,y0,x1,y1=roi
    mask=Image.new('1',(x1-x0,y1-y0)); mask.putdata([predicate(*c) for c in im.crop(roi).get_flattened_data()])
    box=mask.getbbox()
    if not box:return None
    box=[box[0]+x0,box[1]+y0,box[2]+x0,box[3]+y0]
    return {'pixels':box,'normalized':[round(box[0]/im.width,4),round(box[1]/im.height,4),round((box[2]-box[0])/im.width,4),round((box[3]-box[1])/im.height,4)]}

def roi(im,xywh):
    x,y,w,h=xywh;return tuple(round(p) for p in (x*im.width,y*im.height,(x+w)*im.width,(y+h)*im.height))

measurements={}
for label,times in [('complete',[1.75,1.8,1.85,1.9,1.95,2,2.05,2.1,2.55,2.6,2.7,2.85,3,3.15,3.3,3.45]),('fail',[.55,.6,.65,.7,.75,.8,3.7,3.75,3.8,3.9,4,4.2,4.4,4.9,5,5.1,5.3])]:
    records=[]
    for time in times:
        im=frame(label,time)
        record={'relative_time':time,'video_time':round((82 if label=='complete' else 102)+time,2)}
        if label=='complete':
            record['title_yellow']=mask_bounds(im,roi(im,(.16,.105,.68,.18)),lambda r,g,b:r>170 and g>100 and b<110 and r>b*1.5)
            record['coin_gold']=mask_bounds(im,roi(im,(.18,.40,.65,.20)),lambda r,g,b:r>180 and g>110 and b<100)
            record['continue_green']=mask_bounds(im,roi(im,(.17,.73,.34,.11)),lambda r,g,b:g>155 and g>r*1.12 and b<120)
        else:
            record['panel_blue']=mask_bounds(im,roi(im,(.25,.20,.5,.6)),lambda r,g,b:b>130 and b>r*1.4 and b>g*1.1)
            record['floating_heart']=mask_bounds(im,roi(im,(0,0,.24,.06)),lambda r,g,b:r>135 and r>g*1.8 and r>b*1.2)
        records.append(record)
    measurements[label]=records
    thumbw=218;thumbh=315;cols=6;rows=(len(times)+cols-1)//cols
    sheet=Image.new('RGB',(cols*thumbw,rows*(thumbh+28)),'#171526');draw=ImageDraw.Draw(sheet)
    for i,time in enumerate(times):
        im=frame(label,time); im.thumbnail((thumbw,thumbh))
        x=i%cols*thumbw;y=i//cols*(thumbh+28)
        sheet.paste(im,(x+(thumbw-im.width)//2,y+28));draw.text((x+8,y+7),f'{(82 if label=="complete" else 102)+time:.2f} s',fill='white')
    sheet.save(FOLDER/f'reference-{label}-steps.jpg',quality=95)
(FOLDER/'reference-measurements.json').write_text(json.dumps(measurements,indent=2))
print('Measured colour bounds around every title, reward, button and failure transition at 0.05 s spacing.')
