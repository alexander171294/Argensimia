# plaza_design_v2.py — plaza a partir del mapa dibujado por Alex (plaza_map.svg)
# canteros = polígonos verdes; carriles de manifestantes (4) y entradas de refuerzo (2) quedan libres de objetos
import json, math, random
from shapely.geometry import Point, LineString, Polygon, box, MultiPolygon
from shapely.ops import unary_union
import plaza_map as PM
ROOT=PM.ROOT; PX,PZ=PM.PX,PM.PZ
YL,YP=0.44,0.16
CIRCLE=9.5              # explanada central libre
RING=(4.2,5.4,5.6,5.75) # cantero de flores del monumento: interior, exterior, cordón, reja
LANE_CLEAR=2.4; REINF_CLEAR=1.8

raw_l,raw_lanes,raw_rf=PM.load()
field=box(-PX+1.4,-PZ+1.4,PX-1.4,PZ-1.4)
center=Point(0,0).buffer(CIRCLE,64)
lawns=[]
for c in raw_l:
    g=Polygon(c).buffer(0)
    g=g.intersection(field).difference(center)
    for p in (g.geoms if isinstance(g,MultiPolygon) else [g]):
        p=p.buffer(-0.7,quad_segs=8).buffer(0.7,quad_segs=8).buffer(0.6,quad_segs=8).buffer(-0.6,quad_segs=8)
        if p.is_empty or p.area<12: continue
        if isinstance(p,MultiPolygon): p=max(p.geoms,key=lambda q:q.area)
        lawns.append(p.simplify(0.03))
LAWN=unary_union(lawns)
lanes=[LineString(l) for l in raw_lanes]; reinf=[LineString(l) for l in raw_rf]
LANES=unary_union([l.buffer(LANE_CLEAR) for l in lanes]+[r.buffer(REINF_CLEAR) for r in reinf])
LANES_B=unary_union([l.buffer(1.7) for l in lanes]+[r.buffer(1.5) for r in reinf])
rnd=random.Random(11)
items=[]
def add(m,x,z,y,yaw=0.0,s=1.0): items.append([m,x,y,z,yaw,s])
def far(p,lst,d): return all((p[0]-q[0])**2+(p[1]-q[1])**2>=d*d for q in lst)
def yaw_face(dx,dz): return math.degrees(math.atan2(dx,dz))

# faroles: sobre el cordón de cada cantero, cada ~10 m
lamps=[]
for g in lawns:
    r=g.buffer(-0.55).exterior; n=max(1,int(r.length/10.0)); off=rnd.uniform(0,r.length/n)
    for k in range(n):
        p=r.interpolate(off+k*r.length/n); q=(p.x,p.y)
        if far(q,lamps,6.5): lamps.append(q); add('lamp',q[0],q[1],YL)
for a in (45,135,225,315):
    q=(6.6*math.cos(math.radians(a)),6.6*math.sin(math.radians(a)))
    if not LANES.contains(Point(q)) and not LAWN.contains(Point(q)): lamps.append(q); add('lamp',q[0],q[1],YP)
per=[]
for x in range(-30,37,9):
    for z in (-PZ+1.0,PZ-1.0):
        q=(float(x),z)
        if not LANES.contains(Point(q)) and far(q,lamps+per,4): per.append(q); add('lamp',q[0],q[1],YP)
for z in range(-21,22,7):
    q=(PX-1.0,float(z))
    if not LANES.contains(Point(q)) and far(q,lamps+per,4): per.append(q); add('lamp',q[0],q[1],YP)
lampsall=lamps+per

# bancos: en el borde pavimentado de cada cantero, de espaldas al pasto, fuera de los carriles
benches=[]
for g in lawns:
    r=g.buffer(0.55,quad_segs=8).exterior; n=max(1,int(r.length/9.0)); off=rnd.uniform(0,r.length/n)
    for k in range(n):
        p=r.interpolate(off+k*r.length/n); q=(p.x,p.y)
        if LANES_B.contains(p) or not field.buffer(-0.5).contains(p) or LAWN.distance(p)<0.45: continue
        if p.distance(Point(0,0))<CIRCLE+0.8 or not far(q,lampsall,2.2) or not far(q,benches,7): continue
        n_=g.exterior.interpolate(g.exterior.project(p)); dx,dz=p.x-n_.x,p.y-n_.y; L=math.hypot(dx,dz) or 1
        benches.append(q); add('bench',q[0],q[1],YP,yaw_face(dx/L,dz/L))
        if len(benches)%2: add('trash_bin',q[0]-dz/L*1.5,q[1]+dx/L*1.5,YP)

# árboles: palmeras alrededor de la explanada, follaje mixto y jacarandás afuera
canopy=[]
def poisson(poly,margin,spacing,maxn):
    inner=poly.buffer(-margin)
    if inner.is_empty: return []
    mnx,mnz,mxx,mxz=inner.bounds; out=[]
    for _ in range(maxn):
        best=None;bd=-1
        for c in range(90):
            q=(rnd.uniform(mnx,mxx),rnd.uniform(mnz,mxz))
            if not inner.contains(Point(q)) or not far(q,lampsall,2.6): continue
            d=min([math.dist(q,o) for o in out+canopy]+[99])
            if d>bd: bd,best=d,q
        if best is None or bd<spacing: break
        out.append(best)
    return out
for g in sorted(lawns,key=lambda p:p.centroid.distance(Point(0,0))):
    # árboles bajos y espaciados: el centro del cantero, lejos de los caminos
    pts=poisson(g,2.5,7.0,max(1,int(g.area/70)))
    for i,q in enumerate(pts):
        m='tree_jacaranda' if (i==0 and g.area>200) else rnd.choice(['tree','tree_b'])
        canopy.append(q); add(m,q[0],q[1],YL,rnd.uniform(0,360),rnd.uniform(0.82,0.95))

# borde de arbustos casi continuo (marca que por ahí no se pasa) con macizos de flores intercalados
fl=[]; cols=['flowers_a','flowers_b','flowers_c']; ci=0
for g in lawns:
    ib=g.buffer(-0.95)
    if ib.is_empty: continue
    for ring in ([ib.exterior] if isinstance(ib,Polygon) else [p.exterior for p in ib.geoms]):
        n=max(3,int(ring.length/1.75)); step=ring.length/n
        for k in range(n):
            s_=k*step; p=ring.interpolate(s_); q=(p.x,p.y)
            if not far(q,lampsall,1.3) or not far(q,canopy,1.8): continue
            if k%4==2:
                a_=ring.interpolate(max(0,s_-0.3)); b_=ring.interpolate(min(ring.length,s_+0.3))
                add(cols[ci%3],q[0],q[1],YL,math.degrees(math.atan2(-(b_.y-a_.y),b_.x-a_.x)),0.9); ci+=1
            else:
                add('shrub_a' if rnd.random()<0.78 else 'shrub_b',q[0],q[1],YL,rnd.uniform(0,360),rnd.uniform(0.85,1.0))
            fl.append(q)

# fijos: monumento, cantero circular y su reja, reja perimetral con aberturas en carriles y refuerzos
fixed=['monument 0.000 0.160 0.000 0.0 0.850']
for k in range(10):
    a=math.radians(18+k*36); c=cols[k%3]
    fixed.append('%s %.3f 0.580 %.3f %.1f 1.000'%(c,4.8*math.cos(a),4.8*math.sin(a),-math.degrees(a)+90))
for k in range(20):
    a=math.radians(9+k*18)
    if k%5==0: continue
    fixed.append('iron_fence %.3f 0.160 %.3f %.1f 0.900'%(RING[3]*math.cos(a)*1.0,RING[3]*math.sin(a),90-math.degrees(a)))
def gap(x,z): return LANES.buffer(0.8).contains(Point(x,z))
# sin reja perimetral: la plaza queda abierta, el único vallado es el de la Casa Rosada

lines=fixed+['%s %.3f %.3f %.3f %.1f %.3f'%tuple(r) for r in items]
open(ROOT+'plaza_props.txt','w').write('\n'.join(lines)+'\n')
def rr(l): return [[round(x,2),round(z,2)] for x,z in l.coords]
gates=sorted(round(l.coords[-1][1] if l.coords[-1][0]<l.coords[0][0] else l.coords[0][1],2) for l in lanes)
lay={'source':'plaza_map.svg','plaza':[PX,PZ],'center_r':CIRCLE,'ring':RING,'paved':'plaza_rect',
     'lawns':[[[round(x,3),round(z,3)] for x,z in list(g.exterior.coords)[:-1]] for g in lawns],
     'lanes':[rr(l) for l in lanes],'reinforce':[rr(l) for l in reinf],'casa_gates_z':gates}
json.dump(lay,open(ROOT+'plaza_layout.json','w'))
from collections import Counter
SUMMARY={'lawns':len(lawns),'gates':gates,'counts':dict(Counter(r[0] for r in items))}
