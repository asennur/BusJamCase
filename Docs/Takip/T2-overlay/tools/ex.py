import os,sys; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sys, json, mcp
code=open(sys.argv[1],encoding='utf-8').read()
r=mcp.call('execute_code',{'action':'execute','code':code,'safety_checks':False})
print(json.dumps(r,ensure_ascii=False)[:int(sys.argv[2]) if len(sys.argv)>2 else 6000])
