import sys,os
from PIL import Image, ImageOps, ImageDraw
REF={'p':r'C:\Users\Kullanici\Downloads\WhatsApp Image 2026-11-22 at 00.17.29.jpeg','t':r'C:\Users\Kullanici\Downloads\WhatsApp Image 2026-11-22 at 00.32.41.jpeg'}
src,dst=sys.argv[1],sys.argv[2]; os.makedirs(dst,exist_ok=True)
for f in sorted(os.listdir(src)):
    if not f.endswith('.png'): continue
    im=Image.open(os.path.join(src,f)).convert('RGB')
    if im.size[1]<im.size[0]*1.0 and False: pass
    im=ImageOps.flip(im) if '--flip' in sys.argv else im
    kind='t' if f.startswith('t') else 'p'
    ref=Image.open(REF[kind]).convert('RGB')
    # scale capture to reference height, centre-crop/pad to reference width
    h=ref.size[1]; w=round(im.size[0]*h/im.size[1]); im2=im.resize((w,h),Image.LANCZOS)
    canvas=Image.new('RGB',ref.size,(0,0,0)); canvas.paste(im2,((ref.size[0]-w)//2,0))
    blend=Image.blend(ref,canvas,.5)
    d=ImageDraw.Draw(blend)
    for y in range(0,h,100): d.line([(0,y),(12,y)],fill=(255,0,0)); d.text((14,y-6),str(y),fill=(255,0,0))
    side=Image.new('RGB',(ref.size[0]*3,h)); side.paste(ref,(0,0)); side.paste(canvas,(ref.size[0],0)); side.paste(blend,(ref.size[0]*2,0))
    side.save(os.path.join(dst,f.replace('.png','_cmp.png')))
    print(f, im.size, '->', w)
