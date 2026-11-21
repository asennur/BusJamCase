"""Build labelled comparison crops; leave every native Game View readback untouched."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, hashlib, numpy as np

ROOT=Path(__file__).resolve().parents[1]
OUT=Path('C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-2')
SIZES=[(580,1250),(1080,1920),(2560,1080),(1080,1080)]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',19)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
reference=Image.open(ROOT/'Assets/_BusJam/Art/Textures/GameBG.png').convert('RGB')
rows=[]
for w,h in SIZES:
    sheet=Image.new('RGB',(1320,900),(28,24,38)); draw=ImageDraw.Draw(sheet)
    draw.text((16,12),f'ZSCN-2 | {w} x {h} | Edit mode, native Game View, UI hidden',font=font,fill='white')
    labels=['GameBG.png source','Before: old quad + authored curb','After: modular geometry + approved kit']
    images=[reference]
    for prefix in ('before','after-final'):
        im=Image.open(OUT/f'{prefix}-{w}x{h}.png').convert('RGB')
        assert im.size==(w,h)
        # A full-viewport comparison crop, corrected only in this derived artifact.
        images.append(im.crop((0,0,w,h)).transpose(Image.Transpose.FLIP_TOP_BOTTOM))
    for col,(im,label) in enumerate(zip(images,labels)):
        im=im.copy(); im.thumbnail((420,785),Image.Resampling.LANCZOS)
        x=col*440+(440-im.width)//2;y=70+(785-im.height)//2
        sheet.paste(im,(x,y));draw.text((col*440+12,44),label,font=small,fill='white')
    draw.text((16,864),'Source colours / shapes differ from lit low-poly meshes. Raw captures are not flipped or resized.',font=small,fill=(210,205,220))
    sheet.save(OUT/f'comparison-{w}x{h}.png');rows.append(sheet.resize((880,600)))
contact=Image.new('RGB',(1760,1200),(28,24,38))
for i,row in enumerate(rows):contact.paste(row,((i%2)*880,(i//2)*600))
contact.save(OUT/'comparison-all-sizes.png')

source=np.array(json.loads((OUT/'EnvironmentKit-layout.json').read_text())['known_vertices'])
unity=np.loadtxt(OUT/'unity-tree-vertices.csv',delimiter=',')
dist=np.sqrt(((source[:,None,:]-unity[None,:,:])**2).sum(2)).min(1)
assert dist.max()<1e-5
(OUT/'coordinate-validation.txt').write_text(f'{len(source)} Blender vertices vs {len(unity)} Unity seam-split vertices; maximum nearest distance={dist.max():.9g} units.\nConversion (-X,Z,-Y), before the scene framing transform.\n')
before=json.loads((OUT/'protected-before.json').read_text())
after={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in before}
(OUT/'protected-after.json').write_text(json.dumps(after,indent=2))
assert before==after, [p for p in before if before[p]!=after[p]]
(OUT/'protected-validation.txt').write_text(f'{len(before)} protected file hashes unchanged, including every ArtSource file, sprite atlas, ResultTimeline, protected materials/data and GameBG.png.\n')
stats={}
for line in (OUT/'stats.tsv').read_text().splitlines():
    fields=line.split('\t');stats[fields[0]]=list(map(int,fields[1:]))
lines=['resolution\tdraw_calls_before\tdraw_calls_after\tdelta\ttriangles_before\ttriangles_after\tbatches_before\tbatches_after']
for w,h in SIZES:
    b=stats[f'before-{w}x{h}'];a=stats[f'after-final-{w}x{h}'];assert a[0]-b[0]<=10 and a[1]<50000
    lines.append(f'{w}x{h}\t{b[0]}\t{a[0]}\t{a[0]-b[0]}\t{b[1]}\t{a[1]}\t{b[2]}\t{a[2]}')
(OUT/'budget.tsv').write_text('\n'.join(lines)+'\n')
print('\n'.join(lines));print('Protected hashes and coordinate conversion passed.')
