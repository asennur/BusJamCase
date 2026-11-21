import sys,os
D=sys.argv[1]
REF={'p':{'W':739,'level-badge':(90,72,100,99),'gear_c':(614,128),'bar_top':1312,'banner_top':1442,'slot_y':515,'grid_top':645,'bus_top':205,'grid_bottom':1300},
     't':{'W':1112,'level-badge':(277,72,98,99),'gear_c':(800,128),'bar_top':1312,'banner_top':1443,'slot_y':515,'grid_top':645,'bus_top':205,'grid_bottom':1300}}
for f in sorted(os.listdir(D)):
    if not f.endswith('.tsv'): continue
    name=f[:-4]; k=name[0]; W,H=map(int,name[1:].split('_')[0:1]*0+[name.split('_')[0][1:],0]) if False else (0,0)
    rows={l.split('\t')[0]:l.rstrip('\n').split('\t')[1:] for l in open(os.path.join(D,f))}
    sizes={'p1170':(1170,2532),'p1080':(1080,1920),'t1668':(1668,2388),'t1536':(1536,2048)}
    W,H=sizes[name.split('_')[0]]; s=1600/H; ox=(REF[k]['W']-W*s)/2
    r=REF[k]; out=[]
    lb=[float(v) for v in rows['level-badge']]; out.append('card x %+.0f y %+.0f w %+.0f'%(lb[0]*s+ox-r['level-badge'][0], lb[1]*s-r['level-badge'][1], lb[2]*s-r['level-badge'][2]))
    g=[float(v) for v in rows['pause-settings']]; out.append('gear c %+.0f,%+.0f'%((g[0]+g[2]/2)*s+ox-r['gear_c'][0],(g[1]+g[3]*.48)*s-r['gear_c'][1]))
    b=[float(v) for v in rows['booster-bar']]; out.append('bar top %+.0f'%(b[1]*s-r['bar_top']))
    a=[float(v) for v in rows['ad-banner']]; out.append('banner top %+.0f'%(a[1]*s-r['banner_top']))
    anchors=[v for kk,v in rows.items() if kk.startswith('anchor')]
    out.append('bus top %+.0f grid top %+.0f grid8 bottom %+.0f'%(float(anchors[0][0])-r['bus_top'],float(anchors[1][0])-r['grid_top'],float(anchors[2][0])-r['grid_bottom']))
    out.append('slot y %+.0f'%(float(rows['slot0'][1])*s-r['slot_y']))
    print(name,' | '.join(out))
