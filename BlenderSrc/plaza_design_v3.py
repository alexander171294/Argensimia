# plaza_design_v3.py — versión armónica del boceto: 4 carriles suaves y simétricos, caminos anchos,
# explanada central centrada; escribe el SVG limpio (plaza_map.svg) que luego consume plaza_design_v2.py
import math, site, sys, shutil, os
if site.getusersitepackages() not in sys.path: sys.path.append(site.getusersitepackages())
from shapely.geometry import LineString, Point, box, Polygon, MultiPolygon
from shapely.ops import unary_union
ROOT="/Users/alexander/git/argensimia/Argensimia/BlenderSrc/"
PX,PZ=40.0,26.0
FX,FZ=37.0,23.0          # borde de canteros -> paseo perimetral de 3 m
W_LANE=4.6; W_RF=3.6; W_CON=3.2; CR=9.5
def xs(a=-PX-2,b=PX+2,n=169): return [a+(b-a)*i/(n-1) for i in range(n)]
def bump(x): return math.exp(-(x/12.0)**2)
def z_outer(x): return 15.5+1.6*math.sin(math.pi*(x+5)/20.0)
def z_inner(x): return 5.8+3.4*bump(x)+0.7*math.sin(math.pi*(x-5)/20.0)*(1-bump(x))
LANES=[]
for sg in (1,-1):
    LANES.append([(x,sg*z_outer(x)) for x in xs()])
    LANES.append([(x,sg*z_inner(x)) for x in xs()])
REINF=[[(0,PZ),(0,CR-0.5)],[(0,-PZ),(0,-(CR-0.5))]]
CONN=[]
for sx in (1,-1):
    for sg in (1,-1):
        x=sx*23.0; CONN.append([(x,sg*z_inner(x)),(x,sg*z_outer(x))])
paths=unary_union([LineString(l).buffer(W_LANE/2,quad_segs=12) for l in LANES]+
                  [LineString(l).buffer(W_RF/2,cap_style=2) for l in REINF]+
                  [LineString(l).buffer(W_CON/2) for l in CONN]+[Point(0,0).buffer(CR,96)])
field=box(-FX,-FZ,FX,FZ).buffer(-3).buffer(3,quad_segs=12)
raw=field.difference(paths); lawns=[]
for g in (raw.geoms if isinstance(raw,MultiPolygon) else [raw]):
    g=g.buffer(-1.3,quad_segs=10).buffer(1.3,quad_segs=10)
    if g.is_empty or g.area<10: continue
    for p in (g.geoms if isinstance(g,MultiPolygon) else [g]): lawns.append(p.simplify(0.04))
# ---- SVG limpio (misma convención que el boceto: arriba = sur, derecha = oeste/Casa Rosada) ----
S=11.25; OX,OY=45,25
def P(x,z): return (OX+(PX-x)*S, OY+(z+PZ)*S)
def d_poly(pts,close=True):
    q=[P(x,z) for x,z in pts]; return 'M'+' L'.join('%.1f %.1f'%p for p in q)+(' Z' if close else '')
W,H=int(2*OX+2*PX*S),int(2*OY+2*PZ*S)
out=['<svg width="%d" height="%d" viewBox="0 0 %d %d" fill="none" xmlns="http://www.w3.org/2000/svg">'%(W,H,W,H)]
out.append('<path d="%s" fill="#C99472"/>'%d_poly([(PX,-PZ),(-PX,-PZ),(-PX,PZ),(PX,PZ)]))
for g in lawns: out.append('<path d="%s" fill="#738F2D" stroke="#B28857"/>'%d_poly(list(g.exterior.coords)[:-1]))
for l in LANES: out.append('<path d="%s" stroke-width="10" stroke="#B25757"/>'%d_poly(l,False))
for l in REINF: out.append('<path d="%s" stroke-width="5" stroke="#B25757"/>'%d_poly(l,False))
out.append('</svg>')
if not os.path.exists(ROOT+'plaza_map_boceto.svg'): shutil.copy(ROOT+'plaza_map.svg',ROOT+'plaza_map_boceto.svg')
open(ROOT+'plaza_map.svg','w').write('\n'.join(out)+'\n')
QUAD=[P(PX,-PZ),P(-PX,-PZ),P(-PX,PZ),P(PX,PZ)]
# chequeo: ancho mínimo de pasto entre carriles
def min_width(g):
    lo,hi=0,6
    for _ in range(20):
        m=(lo+hi)/2
        if g.buffer(-m).is_empty: hi=m
        else: lo=m
    return round(2*lo,2)
REPORT={'lawns':len(lawns),'green_area':round(sum(g.area for g in lawns)),'plaza_area':round(4*PX*PZ),'min_widths':sorted(min_width(g) for g in lawns),'quad':QUAD}
