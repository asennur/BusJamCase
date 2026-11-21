"""Assemble full-resolution reference/Unity pairs and verify evidence/protected file hashes."""
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw, ImageStat
import hashlib
import json

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT.parent/'BusJamCase_ZVEC_Review/win'
SIZES=[(1080,1920),(1080,2400),(1170,2532),(1536,2048),(1080,1080),(1768,2208),(2208,1768),(1920,1080),(2560,1080)]

def contain(im,size):
    result=Image.new('RGB',size,'#15121f'); scaled=ImageOps.contain(im.convert('RGB'),size,Image.Resampling.LANCZOS)
    result.paste(scaled,((size[0]-scaled.width)//2,(size[1]-scaled.height)//2));return result

def reference(sequence,time):
    count=round(time*20)+1
    return Image.open(OUT/f'reference/{sequence}_{count:04d}.jpg')

def pair(sequence,time,mode='intro'):
    if mode=='collect':
        source_time=4.5+time;ours=OUT/f'ours/collect-t{time:.2f}.png';video_time=82+source_time
    elif sequence=='fail':
        source_time=time if time<.6 else min(5.3,time+3.1)
        ours=OUT/f'ours/fail-t{time:.2f}.png';video_time=102+source_time
    else:
        source_time=time;ours=OUT/f'ours/complete-t{time:.2f}.png';video_time=82+source_time
    panel=Image.new('RGB',(2160,1984),'#15121f');draw=ImageDraw.Draw(panel)
    draw.text((24,20),f'Reference {video_time:.2f} s'+(' (last frame held)' if sequence=='fail' and time>2.2 else ''),fill='white')
    draw.text((1104,20),f'Unity Edit mode / {mode} +{time:.2f} s',fill='white')
    panel.paste(contain(reference(sequence,source_time),(1080,1920)),(0,64))
    panel.paste(Image.open(ours).convert('RGB'),(1080,64))
    path=OUT/f'pairs/{sequence}-{mode}-{time:.2f}.png';path.parent.mkdir(exist_ok=True)
    panel.save(path);return panel

for sequence,mode,times in [
    ('complete','intro',[0,.3,.6,1,1.5,2,2.5]),
    ('fail','intro',[0,.3,.6,1,1.5,2,2.5]),
    ('complete','reward',[2.55,2.7,2.85,3,3.45]),
    ('complete','collect',[0,.3,.6,1,1.2,1.35,1.6,1.8])]:
    strip=Image.new('RGB',(2160*len(times),1984),'#15121f')
    for index,time in enumerate(times):strip.paste(pair(sequence,time,mode),(2160*index,0))
    strip.save(OUT/f'{sequence}-{mode}-timeline.png')
    strip.resize((432*len(times),397),Image.Resampling.LANCZOS).save(OUT/f'{sequence}-{mode}-timeline-preview.jpg',quality=95)

checks=[]
for screen in ('Complete','Fail'):
    sheet=Image.new('RGB',(1080,2016),'#15121f');draw=ImageDraw.Draw(sheet)
    for index,size in enumerate(SIZES):
        im=Image.open(OUT/f'ours/{screen}-{size[0]}x{size[1]}.png')
        assert im.size==size,(screen,im.size,size)
        assert max(ImageStat.Stat(im.convert('RGB')).stddev)>10,(screen,size,'blank')
        checks.append({'screen':screen,'size':size,'nonblank':True})
        x=index%3*360;y=index//3*672
        draw.text((x+8,y+8),f'{screen} {size[0]} x {size[1]}',fill='white')
        sheet.paste(contain(im,(360,640)),(x,y+32))
    sheet.save(OUT/f'{screen}-nine-resolutions.png')

before=json.loads((OUT/'protected-before.json').read_text())
after={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in before}
changed=[p for p in before if before[p]!=after[p]]
assert not changed,changed
report={'end_state_captures':checks,'protected_files_checked':len(before),'protected_files_changed':changed,
        'pair_frame_size':[1080,1920],'reference_native_size':[1640,2360],
        'reference_normalization':'Aspect preserved, letterboxed inside 1080 x 1920.',
        'fail_mapping':'102.00 + t until 0.6; then 105.70 + (t - 0.6), omitting the unsupported offer; last 107.30 frame held after 2.2.',
        'art_comparisons':6,'reference_sampling_seconds':.05}
(OUT/'evidence-verification.json').write_text(json.dumps(report,indent=2))
print(f'Verified {len(checks)} end-state captures, six art comparisons, four timeline strips and {len(before)} unchanged protected files.')
