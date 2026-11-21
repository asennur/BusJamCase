import os,sys,time,json; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__))); import mcp
out=sys.argv[1]; spec=sys.argv[2] if len(sys.argv)>2 else "p1170_L6,1170,2532,141,102,5;p1170_L21,1170,2532,141,102,20;p1080_L6,1080,1920,0,0,5;t1668_L21,1668,2388,48,40,20;t1668_L6,1668,2388,48,40,5;t1536_L21,1536,2048,40,0,20"
def st():
  try: return json.loads(mcp.read('mcpforunity://editor/state')['result']['contents'][0]['text'])['data']['editor']['play_mode']['is_playing']
  except Exception: return False
if not st():
    print(mcp.call('manage_editor',{'action':'play'}))
    for i in range(60):
        time.sleep(1)
        if st(): break
    time.sleep(4)
code='return T2LayoutCapture.Start("%s","%s");'%(out.replace(chr(92),"/"),spec)
print(json.dumps(mcp.call('execute_code',{'action':'execute','code':code,'safety_checks':False}))[:600])
for i in range(240):
    time.sleep(1)
    if os.path.exists(out+'/done.txt') or os.path.exists(out+'/error.txt'): break
print(os.listdir(out))
if os.path.exists(out+'/error.txt'): print(open(out+'/error.txt').read()[:2000])
if len(sys.argv)<4: print(mcp.call('manage_editor',{'action':'stop'}))