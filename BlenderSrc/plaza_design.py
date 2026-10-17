# plaza_design.py — diseño formal y simétrico de la Plaza de Mayo (genera plaza_layout.json + plaza_props.txt)
import json, math, random, site, sys
if site.getusersitepackages() not in sys.path: sys.path.append(site.getusersitepackages())
from shapely.geometry import Point, LineString, Polygon, box, MultiPolygon
from shapely.ops import unary_union

ROOT="/Users/alexander/git/argensimia/Argensimia/BlenderSrc/"
PX,PZ=40.0,26.0          # plaza (paving) half extents
LX,LZ=36.5,22.5          # lawn field half extents -> perimeter walk 3.5m
CR=9.5                   # central paved circle radius
EA,EB=24.0,14.5          # elliptical promenade semi-axes
W_AX,W_NS,W_EL,W_DG=4.6,4.0,3.6,3.0
YL,YP=0.44,0.16          # lawn top / paving top
ROUND=1.6

def ellipse_pts(t0,t1,n=120):
    return [(EA*math.cos(t0+(t1-t0)*i/n), EB*math.sin(t0+(t1-t0)*i/n)) for i in range(n+1)]
def bez(p0,p1,p2,n=60):
    return [((1-t)**2*p0[0]+2*(1-t)*t*p1[0]+t*t*p2[0], (1-t)**2*p0[1]+2*(1-t)*t*p1[1]+t*t*p2[1]) for t in [i/n for i in range(n+1)]]
TD=math.radians(34)
def diag_q():   # quadrant (+,+) corner path
    p0=(EA*math.cos(TD),EB*math.sin(TD)); return bez(p0,(31.5,12.5),(LX+2.5,LZ+2.5))

# ---- quadrant (+x,+z) path centerlines: (points, width, sides)
Q_PATHS=[
    ([(0,0),(PX+2,0)], W_AX, (1,)),
    ([(0,0),(0,PZ+2)], W_NS, (-1,)),
    (ellipse_pts(0,math.pi/2), W_EL, (1,-1)),
    (diag_q(), W_DG, (1,-1)),
]
def mir(pts,sx,sz): return [(x*sx,z*sz) for x,z in pts]
def all_paths():
    out=[LineString([(-PX-2,0),(PX+2,0)]).buffer(W_AX/2), LineString([(0,-PZ-2),(0,PZ+2)]).buffer(W_NS/2),
         LineString(ellipse_pts(0,2*math.pi,480)).buffer(W_EL/2), Point(0,0).buffer(CR,64)]
    for sx in (1,-1):
        for sz in (1,-1): out.append(LineString(mir(diag_q(),sx,sz)).buffer(W_DG/2))
    return unary_union(out)

field=box(-LX,-LZ,LX,LZ).buffer(-4).buffer(4,quad_segs=12)
PATHS=all_paths()
raw=field.difference(PATHS)
lawns=[]
for g in (raw.geoms if isinstance(raw,MultiPolygon) else [raw]):
    g=g.buffer(-ROUND,quad_segs=10).buffer(ROUND,quad_segs=10)
    if g.is_empty or g.area<20: continue
    g=g.simplify(0.03)
    lawns.append(g)
LAWN=unary_union(lawns)
_er={}
def inl(x,z,m):
    if m not in _er: _er[m]=LAWN.buffer(-m)
    return _er[m].contains(Point(x,z))

def frame(line,s):
    p=line.interpolate(s); a=line.interpolate(max(0,s-0.2)); b=line.interpolate(min(line.length,s+0.2))
    tx,tz=b.x-a.x,b.y-a.y; L=math.hypot(tx,tz) or 1; tx/=L; tz/=L
    return p.x,p.y,tx,tz,-tz,tx   # pos, tangent, normal(left)
def yaw_face(dx,dz): return math.degrees(math.atan2(dx,dz))
def far(pt,lst,d): return all((pt[0]-q[0])**2+(pt[1]-q[1])**2>=d*d for q in lst)

items=[]   # (model,x,y,z,yaw,scale)  -- quadrant only, mirrored later
def add(m,x,z,y,yaw,s=1.0): items.append([m,x,y,z,yaw,s])

# ---- lamps lining both sides of every path (on the lawn curb)
lamps=[]
STEP=11.0
for pts,w,sides in Q_PATHS:
    line=LineString(pts); s=5.5
    while s<line.length-1:
        x,z,tx,tz,nx,nz=frame(line,s)
        for sd in sides:
            lx,lz=x+nx*sd*(w/2+0.55), z+nz*sd*(w/2+0.55)
            if lx>0.5 and lz>0.5 and inl(lx,lz,0.3) and far((lx,lz),lamps,5.0): lamps.append((lx,lz))
        s+=STEP
for (lx,lz) in lamps: add('lamp',lx,lz,YL,0)
# perimeter promenade lamps (paving, outer edge)
per=[]
for x in (6.0,15.0,24.0,33.0): per.append((x,PZ-1.4))
for z in (7.5,16.0): per.append((PX-1.4,z))
per.append((PX-1.6,PZ-1.6))
for (x,z) in per: add('lamp',x,z,YP,0)
# central circle lamps at diagonals
for k in range(1):
    a=math.radians(45); add('lamp',8.3*math.cos(a),8.3*math.sin(a),YP,0)

# ---- benches on the paving edge, between lamps, facing the walk
benches=[]
def clear_of_junction(x,z,own):
    for pts,w,_ in Q_PATHS:
        if pts is own: continue
        if LineString(pts).distance(Point(x,z))<w/2+2.2: return False
    return Point(x,z).distance(Point(0,0))>CR+1.5
for pts,w,sides in Q_PATHS:
    line=LineString(pts); s=10.5; k=0
    while s<line.length-2:
        x,z,tx,tz,nx,nz=frame(line,s)
        sd=sides[k%len(sides)]; k+=1
        bx,bz=x+nx*sd*(w/2-0.45), z+nz*sd*(w/2-0.45)
        if bx>0.8 and bz>0.8 and clear_of_junction(bx,bz,pts) and inl(x+nx*sd*(w/2+0.6),z+nz*sd*(w/2+0.6),0.1) \
           and far((bx,bz),lamps+per,2.2) and far((bx,bz),benches,6):
            benches.append((bx,bz)); add('bench',bx,bz,YP,yaw_face(-nx*sd,-nz*sd))
            if len(benches)%2==1:
                add('trash_bin',bx+tx*1.55,bz+tz*1.55,YP,0)
        s+=STEP
# benches around the monument, facing it
for ang in (22.0,68.0):
    a=math.radians(ang); bx,bz=8.6*math.cos(a),8.6*math.sin(a)
    add('bench',bx,bz,YP,yaw_face(-bx,-bz))

# ---- trees: palms in the inner ring, mixed canopy outside, jacaranda accents
rnd=random.Random(7)
quad=box(0.2,0.2,PX,PZ)
ql=[g for g in lawns if g.intersects(quad) and g.intersection(quad).area>15]
ell=Polygon(ellipse_pts(0,2*math.pi,240))
canopy=[]
def poisson(poly,margin,spacing,maxn,avoid,hard):
    inner=poly.buffer(-margin)
    if inner.is_empty: return []
    minx,minz,maxx,maxz=inner.bounds; out=[]
    for it in range(maxn):
        best=None;bd=-1
        for c in range(80):
            p=(rnd.uniform(minx,maxx),rnd.uniform(minz,maxz))
            if not inner.contains(Point(p)) or not far(p,hard,2.8): continue
            d=min([math.dist(p,q) for q in out+avoid]+[99])
            if d>bd: bd,best=d,p
        if best is None or bd<spacing: break
        out.append(best)
    return out
lampsall=lamps+per
for g in ql:
    gq=g.intersection(quad)
    inner=ell.contains(g.representative_point())
    if inner: pts=poisson(gq,2.0,4.8,4,canopy,lampsall)
    else: pts=poisson(gq,2.3,6.2,max(2,int(gq.area/42)),canopy,lampsall)
    for i,(x,z) in enumerate(pts):
        if inner: m='palm'; sc=rnd.uniform(1.0,1.15)
        else:
            m='tree_jacaranda' if i==0 and gq.area>120 else ('tree' if i%2 else 'tree_b'); sc=rnd.uniform(0.92,1.08)
        canopy.append((x,z)); add(m,x,z,YL,rnd.uniform(0,360),sc)

# ---- flower borders along path edges + shrubs along the perimeter edge
fl=[]; cols=['flowers_a','flowers_b','flowers_c']; ci=0
edgebox=box(-LX+0.6,-LZ+0.6,LX-0.6,LZ-0.6)
for g in ql:
    ring=g.buffer(-1.25).exterior if not g.buffer(-1.25).is_empty else None
    if ring is None: continue
    L=ring.length; s=0.0
    while s<L:
        p=ring.interpolate(s); x,z=p.x,p.y
        if x>0.6 and z>0.6:
            near_perim=not edgebox.buffer(-0.8).contains(Point(x,z))
            if near_perim:
                if far((x,z),canopy,2.2) and far((x,z),fl,2.9) and far((x,z),lampsall,1.6):
                    fl.append((x,z)); add('shrub_a' if len(fl)%2 else 'shrub_b',x,z,YL,rnd.uniform(0,360),rnd.uniform(0.9,1.05))
            elif far((x,z),canopy,2.4) and far((x,z),fl,2.75) and far((x,z),lampsall,1.5):
                a=ring.interpolate(max(0,s-0.3)); b=ring.interpolate(min(L,s+0.3))
                fl.append((x,z)); add(cols[ci%3],x,z,YL,math.degrees(math.atan2(-(b.y-a.y),b.x-a.x)),rnd.uniform(0.95,1.1)); ci+=1
        s+=1.1

# ---- mirror quadrant -> 4 quadrants
out=[]
for m,x,y,z,yaw,s in items:
    for sx in (1,-1):
        for sz in (1,-1):
            yy=yaw
            if sx<0: yy=-yy
            if sz<0: yy=180-yy
            out.append([m,x*sx,y,z*sz,yy,s])

# ---- fixed pieces: monument, ring fence, ring flowers, perimeter fence
fixed=['monument 0.000 0.160 0.000 0.0 1.000']
for k in range(12):
    a=math.radians(15+k*30); c=['flowers_a','flowers_b','flowers_c'][k%3]
    fixed.append('%s %.3f 0.580 %.3f %.1f 1.150'%(c,5.45*math.cos(a),5.45*math.sin(a),-math.degrees(a)+90))
for k in range(24):
    a=math.radians(7.5+k*15)
    if any(abs(((math.degrees(a)-d+180)%360)-180)<14 for d in (0,90,180,270)): continue
    fixed.append('iron_fence %.3f 0.160 %.3f %.1f 0.910'%(7.0*math.cos(a),7.0*math.sin(a),90-math.degrees(a)))
for x in range(-34,35,2):
    if abs(x)<4: continue
    for z in (-25.4,25.4): fixed.append('iron_fence %.3f 0.160 %.3f 0.0 1.000'%(x,z))
for z in range(-20,21,2):
    if abs(z)<4: continue
    fixed.append('iron_fence 39.400 0.160 %.3f 90.0 1.000'%z)

lines=fixed+['%s %.3f %.3f %.3f %.1f %.3f'%tuple(r) for r in out]
open(ROOT+'plaza_props.txt','w').write('\n'.join(lines)+'\n')
lay={'plaza':[PX,PZ],'field':[LX,LZ],'center_r':CR,'ellipse':[EA,EB],
     'widths':{'axis':W_AX,'ns':W_NS,'ellipse':W_EL,'diag':W_DG},'paved':'plaza_rect',
     'lawns':[[[round(x,3),round(z,3)] for x,z in list(g.exterior.coords)[:-1]] for g in lawns]}
json.dump(lay,open(ROOT+'plaza_layout.json','w'))
from collections import Counter
SUMMARY={'lawns':len(lawns),'areas':sorted(round(g.area) for g in lawns),'counts':dict(Counter(r[0] for r in out))}
