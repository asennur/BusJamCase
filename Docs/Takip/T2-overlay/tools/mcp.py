import json, sys, urllib.request
URL='http://127.0.0.1:8080/mcp'
def post(body, sid=None):
    h={'Content-Type':'application/json','Accept':'application/json, text/event-stream'}
    if sid: h['mcp-session-id']=sid
    r=urllib.request.urlopen(urllib.request.Request(URL,json.dumps(body).encode(),h),timeout=600)
    sid=r.headers.get('mcp-session-id',sid); txt=r.read().decode('utf-8','replace')
    out=None
    for line in txt.splitlines():
        if line.startswith('data:'):
            d=json.loads(line[5:])
            if 'id' in d: out=d
    return out,sid
def session():
    _,sid=post({"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"t2","version":"1"}}})
    post({"jsonrpc":"2.0","method":"notifications/initialized"},sid)
    return sid
def call(name,args,sid=None):
    sid=sid or session()
    r,_=post({"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":name,"arguments":args}},sid)
    return r
def read(uri):
    sid=session(); r,_=post({"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":uri}},sid); return r
if __name__=='__main__':
    if sys.argv[1]=='list':
        sid=session(); r,_=post({"jsonrpc":"2.0","id":2,"method":"tools/list"},sid)
        for t in r['result']['tools']: print(t['name'],'-',t['description'][:150].replace('\n',' '))
    elif sys.argv[1]=='read': print(json.dumps(read(sys.argv[2]))[:6000])
    elif sys.argv[1]=='schema':
        sid=session(); r,_=post({"jsonrpc":"2.0","id":2,"method":"tools/list"},sid)
        for t in r['result']['tools']:
            if t['name']==sys.argv[2]: print(json.dumps(t['inputSchema'])[:4000])
    else:
        args=json.loads(open(sys.argv[2],encoding='utf-8').read()) if sys.argv[2].endswith('.json') else json.loads(sys.argv[2])
        r=call(sys.argv[1],args)
        s=json.dumps(r,ensure_ascii=False)
        print(s[:int(sys.argv[3]) if len(sys.argv)>3 else 8000])
