O=C:/Users/Kullanici/Documents/BusJamCase/Docs/Takip/T2-overlay; N=$1
py -3 -I mcp.py refresh_unity '{"mode":"force","compile":"request","wait_for_ready":true}' 300
py -3 -I ex.py reset.cs 800; py -3 -I mcp.py read_console '{"action":"get","types":["error"],"count":10}' 1500
rm -rf $O/$N $O/$N-cmp; timeout 400 py -3 -I cap.py "$O/$N" "${2:-p1170_L6,1170,2532,141,102,5;p1170_L21,1170,2532,141,102,20;p1080_L6,1080,1920,0,0,5;t1668_L21,1668,2388,48,40,20;t1668_L6,1668,2388,48,40,5;t1536_L21,1536,2048,40,0,20}" > /dev/null && py -3 -I overlay.py "$O/$N" "$O/$N-cmp" --flip > /dev/null; ls $O/$N | head -3
