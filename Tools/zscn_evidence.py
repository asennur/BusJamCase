from pathlib import Path
from PIL import Image, ImageDraw
import subprocess, json, math

root=Path(r'C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-1')
refs=Path(r'C:/Users/Kullanici/Documents/BusJamCase_Blender_Refs/kutlama')
phases=['before','entry','stop','hold','leaving','exit']
reference=[1,5,8,15,21,25]
# Raw native captures are never rewritten. Only these labelled comparison crops are flipped.
for w,h in [(580,1250),(1080,1920),(2560,1080)]:
    sheet=Image.new('RGB',(6*340,2*430+80),(24,20,34)); d=ImageDraw.Draw(sheet)
    for i,(phase,frame) in enumerate(zip(phases,reference)):
        ref=Image.open(refs/f'k_{frame:03}.png').convert('RGB')
        ours=Image.open(root/f'{phase}-{w}x{h}.png').convert('RGB')
        # Match the lower result area at every aspect; do not alter raw output pixels.
        ours=ours.crop((0,0,w,int(h*.65))).transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        ref=ref.crop((0,int(ref.height*.35),ref.width,ref.height))
        for row,im in enumerate((ref,ours)):
            im.thumbnail((332,410));sheet.paste(im,(i*340+(340-im.width)//2,35+row*450))
        d.text((i*340+5,8),f'{phase} | k_{frame:03}',fill='white')
        d.text((i*340+5,460),f'ours {w}x{h} lower crop',fill='white')
    sheet.save(root/f'timeline-reference-vs-ours-{w}x{h}.png')

frames=root/'complete-frames'
if len(list(frames.glob('frame-*.png')))>=108:
    import imageio_ffmpeg
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-framerate','30','-i',str(frames/'frame-%03d.png'),
                    '-vf','crop=1080:1700:0:110,vflip','-c:v','libx264','-crf','18','-pix_fmt','yuv420p',
                    '-movflags','+faststart',str(root/'Complete-sequence-crop.mp4')],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)

# Point/clamp/uncompressed means each sampled palette cell must survive byte-for-byte.
palette={}
for name,path in [('EnvPalette','Environment/EnvPalette.png'),('MenuPalette','Menu/MenuPalette.png')]:
    im=Image.open(Path('Assets/_BusJam/Art/Models')/path).convert('RGB')
    palette[name]={'dimensions':im.size,'colours':sorted(set(im.getdata()))}
(root/'source-palette-colours.json').write_text(json.dumps(palette,indent=2))
