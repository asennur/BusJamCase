"""Assemble native Unity PNG/trace/constructed Start comparisons and audit scope."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps, ImageStat
import hashlib
import json

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT.parent/'BusJamCase_ZVEC_Review/logo'
SIZES=[(1080,1920),(1080,2400),(1170,2532),(1536,2048),(1080,1080),(1768,2208),(2208,1768),(1920,1080),(2560,1080),(2160,3840),(5120,2160)]
VARIANTS=['PNG','Trace','Constructed']


def main():
    checks=[]
    records={kind:[json.loads(line) for line in (OUT/kind/'bounds.jsonl').read_text().splitlines()] for kind in VARIANTS}
    for kind in VARIANTS:
        for size in SIZES:
            im=Image.open(OUT/kind/f'Start-{size[0]}x{size[1]}.png')
            assert im.size==size
            assert max(ImageStat.Stat(im.convert('RGB')).stddev)>15
            checks.append({'variant':kind,'size':size,'nonblank':True})
    for size in [(1080,1920),(2560,1080),(2160,3840),(5120,2160)]:
        name=f'Start-{size[0]}x{size[1]}'
        # Crop identical resolved logo bounds, preserving native pixels at both scales.
        record=next(r for r in records['PNG'] if r['capture']==name and r['element']=='logo')
        x,y,w,h=record['bounds'];box=(round(x),round(y),round(x+w),round(y+h))
        width,height=box[2]-box[0],box[3]-box[1]
        pair=Image.new('RGB',(width*3,height+46),'#211b30');draw=ImageDraw.Draw(pair)
        full=Image.new('RGB',(size[0]*3,size[1]+46),'#211b30');fd=ImageDraw.Draw(full)
        for col,kind in enumerate(VARIANTS):
            im=Image.open(OUT/kind/(name+'.png')).convert('RGB')
            caption={'PNG':'Original PNG','Trace':'ZVEC-4 trace','Constructed':'ZLOGO constructed vector'}[kind]
            draw.text((col*width+12,14),caption,fill='white');pair.paste(im.crop(box),(col*width,46))
            fd.text((col*size[0]+12,14),caption,fill='white');full.paste(im,(col*size[0],46))
        pair.save(OUT/(name+'-logo-comparison.png'))
        full.save(OUT/(name+'-full-comparison.png'))
        preview=ImageOps.contain(pair,(1800,850),Image.Resampling.LANCZOS);preview.save(OUT/(name+'-preview.jpg'),quality=96)
    sheet=Image.new('RGB',(1080,2016),'#211b30');draw=ImageDraw.Draw(sheet)
    for i,size in enumerate(SIZES[:9]):
        im=Image.open(OUT/'Constructed'/f'Start-{size[0]}x{size[1]}.png').convert('RGB')
        thumb=ImageOps.contain(im,(360,640),Image.Resampling.LANCZOS)
        x=i%3*360;y=i//3*672
        draw.text((x+8,y+8),f'Start {size[0]} x {size[1]}',fill='white')
        sheet.paste(thumb,(x+(360-thumb.width)//2,y+32+(640-thumb.height)//2))
    sheet.save(OUT/'Start-nine-resolutions.png')
    before=json.loads((OUT/'protected-before.json').read_text())
    changed=[p for p,h in before.items() if hashlib.sha256((ROOT/p).read_bytes()).hexdigest()!=h]
    assert not changed,changed
    bounds=[]; rounding=[]
    for a,b in zip(records['PNG'],records['Constructed']):
        assert a['capture']==b['capture'] and a['element']==b['element']
        delta=max(abs(x-y) for x,y in zip(a['bounds'],b['bounds']))
        # Native text-baseline pixel rounding can differ by one pixel after reload.
        tolerance=1.01 if a['element'] in ['lives-count','coin-count'] else .01
        assert delta<=tolerance,(a,b)
        if delta>.01:rounding.append({'capture':a['capture'],'element':a['element'],'max_pixels':delta})
        bounds.append(a['capture']+'/'+a['element'])
    svg=(ROOT/'Assets/_BusJam/UI/Vector/logo_constructed.svg').read_text()
    assert svg.count('<path ')<=1200
    assert '<image' not in svg and '<text' not in svg and '<filter' not in svg
    report={'captures':checks,'control_bounds_checked':len(bounds),'text_baseline_rounding':rounding,'protected_hashes_unchanged':len(before),'protected_changes':changed,'paths':svg.count('<path '),'comparisons':'Four native three-column logo crops and four full Start comparisons; genuine 2x render targets, not enlarged screenshots.'}
    (OUT/'verification.json').write_text(json.dumps(report,indent=2))
    print(f'PASS: {len(checks)} captures, {len(bounds)} bounds checked ({len(rounding)} text rounding differences), {len(before)} unchanged protected hashes, {svg.count("<path ")} paths.')


if __name__=='__main__':main()
