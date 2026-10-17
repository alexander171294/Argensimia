import bpy, bmesh, math, random, json
from mathutils import Matrix, Vector
import gen_assets as GA
from gen_assets import A, cube, cyl, sph, pyr, R
LAY=json.load(open(GA.ROOT+"BlenderSrc/plaza_layout.json"))

def _disp(b,amt,seed):
    rnd=random.Random(seed)
    for v in b.verts:
        k=1+rnd.uniform(-amt,amt); v.co*=k
    return b
def blob(r,sub,amt,seed,scale=(1,1,1)):
    b=sph(r,sub); _disp(b,amt,seed); bmesh.ops.scale(b,vec=scale,verts=b.verts); return b

def lamp():
    a=A("lamp")
    a.cy('M_Iron',(0,0,0.12),0.32,0.32,0.24,12); a.cy('M_Iron',(0,0,0.45),0.26,0.16,0.45,12); a.cy('M_Iron',(0,0,0.75),0.19,0.19,0.18,12)
    a.cy('M_Iron',(0,0,2.25),0.085,0.06,2.9,10); a.cy('M_Iron',(0,0,1.15),0.12,0.1,0.5,10)
    a.cy('M_Iron',(0,0,3.75),0.1,0.14,0.16,10); a.cy('M_Iron',(0,0,3.9),0.17,0.11,0.16,10)
    a.sp('M_GlobeGlow',(0,0,4.28),0.34,3); a.cy('M_Iron',(0,0,4.66),0.12,0.06,0.12,10); a.sp('M_Iron',(0,0,4.76),0.05,1)
    return a.finish()
def _leaflet_frond(L,droop,n=18,leaf=0.75,seed=0):
    rnd=random.Random(seed); b=bmesh.new()
    def P(t):  # rib curve
        return Vector((0,L*t,-droop*t*t+0.55*t))
    # rib
    prev=None
    for i in range(n+1):
        t=i/n; p=P(t); w=0.05*(1-t)+0.015
        v1=b.verts.new(p+Vector((-w,0,0))); v2=b.verts.new(p+Vector((w,0,0)))
        if prev: b.faces.new((prev[0],prev[1],v2,v1))
        prev=(v1,v2)
    for i in range(1,n):
        t=i/n; p=P(t); p2=P(min(1,t+0.6/n)); ll=leaf*math.sin(math.pi*min(1,0.15+t*0.9))*(1-0.35*t)+0.12
        for sd in (-1,1):
            tip=p+Vector((sd*ll,0.35*ll,-0.25*ll-0.1*rnd.random()))
            a_=b.verts.new(p); c_=b.verts.new(p2); d_=b.verts.new(tip)
            b.faces.new((a_,c_,d_) if sd>0 else (c_,a_,d_))
    return b
def palm():
    a=A("palm"); rnd=random.Random(5); h=0; x=0
    for i in range(10):
        r=0.4-i*0.017; seg=0.75; x+=0.03*i
        a.cy('M_PalmTrunk' if i%2==0 else 'M_PalmTrunk2',(x,0,h+seg/2),r,r-0.03,seg,12); h+=seg
        a.cy('M_PalmTrunk2',(x,0,h),r+0.03,r+0.03,0.08,12)
    top=(x,0,h); a.add(blob(0.55,2,0.1,3),'M_PalmTrunk2',top)
    for k in range(4): a.sp('M_Coconut',(x+0.35*math.cos(k*1.6),0.35*math.sin(k*1.6),h-0.25),0.15,1)
    for ring,(cnt,tilt,L,dr) in enumerate(((10,-5,4.6,2.6),(8,25,3.8,1.4),(5,55,2.6,0.4))):
        for k in range(cnt):
            mx=Matrix.Translation(top)@Matrix.Rotation(R(k*360/cnt+ring*17+rnd.uniform(-8,8)),4,'Z')@Matrix.Rotation(R(tilt+rnd.uniform(-8,8)),4,'X')
            a.put(_leaflet_frond(L*rnd.uniform(0.9,1.1),dr*rnd.uniform(0.85,1.15),seed=k+ring*20),'M_PalmLeaf' if (k+ring)%3 else 'M_Leaf2',mx)
    return a.finish()
def tree(name,c1,c2,seed=1,h=3.2,r=2.4):
    rnd=random.Random(seed); a=A(name)
    a.cy('M_Bark',(0,0,h/2),0.28,0.2,h,10)
    for k in range(3):
        ang=k*120+rnd.uniform(-20,20)
        a.cy('M_Bark',(0.5*math.cos(R(ang)),0.5*math.sin(R(ang)),h+0.5),0.12,0.06,1.6,6,(0,35,ang))
    for i in range(16):
        th=rnd.uniform(0,2*math.pi); rr=rnd.uniform(0,r*0.75); z=h+0.9+rnd.uniform(-0.3,1.9)*(1-rr/(r*1.2))+rr*0.1
        rad=rnd.uniform(0.9,1.4)*(r/2.4)
        # gradiente de copa: abajo el tono oscuro, arriba el claro (se lee el volumen desde arriba)
        top_=(z-(h+0.6))/2.2
        a.add(blob(rad,2,0.12,seed*100+i,(1,1,0.85)),c2 if top_+rnd.uniform(-0.25,0.25)>0.55 else c1,(math.cos(th)*rr,math.sin(th)*rr,z))
    return a.finish()
def flowerbed(name,seed,cols):
    rnd=random.Random(seed); a=A(name)
    a.add(blob(0.95,2,0.06,seed,(1.35,1.0,0.42)),'M_Hedge',(0,0,0.1))
    for i in range(46):
        th=rnd.uniform(0,2*math.pi); rr=math.sqrt(rnd.random())*1.0
        x=math.cos(th)*rr*1.25; y=math.sin(th)*rr*0.9; z=0.1+0.4*math.sqrt(max(0,1-rr*rr))+0.03
        a.sp(rnd.choice(cols),(x,y,z),rnd.uniform(0.07,0.12),1)
    return a.finish()
def shrub(name,seed,c):
    rnd=random.Random(seed); a=A(name)
    for i in range(5):
        zz=0.3+i*0.07; a.add(blob(rnd.uniform(0.38,0.55),2,0.12,seed*10+i),c if zz>0.48 else 'M_Hedge',(rnd.uniform(-0.4,0.4),rnd.uniform(-0.4,0.4),zz))
    return a.finish()
def iron_fence():
    a=A("iron_fence"); L=2.0
    for z in (0.18,1.05,1.2): a.box('M_Iron',(0,0,z),(L,0.06,0.05))
    n=11
    for i in range(n):
        x=-L/2+0.09+i*(L-0.18)/(n-1); a.cy('M_Iron',(x,0,0.7),0.018,0.018,1.1,6); a.cy('M_Iron',(x,0,1.3),0.04,0.0,0.15,6)
    a.box('M_Iron',(-L/2,0,0.75),(0.12,0.12,1.5)); a.cy('M_Iron',(-L/2,0,1.52),0.09,0.09,0.05,8); a.sp('M_Iron',(-L/2,0,1.62),0.08,1)
    a.box('M_Stone' if False else 'M_Curb',(0,0,0.06),(L,0.3,0.12))
    return a.finish()
def monument():
    a=A("monument")
    a.cy('M_Marble',(0,0,0.12),3.9,3.9,0.24,40); a.box('M_Marble',(0,0,0.45),(5.2,5.2,0.42)); a.box('M_Marble',(0,0,0.85),(4.2,4.2,0.38))
    a.box('M_Marble',(0,0,2.1),(2.8,2.8,2.1)); a.box('M_Cornice',(0,0,3.25),(3.2,3.2,0.24)); a.box('M_Cornice',(0,0,1.12),(3.1,3.1,0.18))
    for k in range(4):
        t=R(k*90); a.box('M_Gold',(1.42*math.cos(t),1.42*math.sin(t),2.1),(0.8 if k%2 else 0.05,0.05 if k%2 else 0.8,0.8))
    a.add(pyr(1.9,1.9,7.0,0.6),'M_Marble',(0,0,6.85)); a.add(pyr(1.3,1.3,0.5,0.3),'M_Marble',(0,0,10.6))
    a.cy('M_Marble',(0,0,11.15),0.36,0.36,0.6,12)
    a.cy('M_Marble',(0,0,11.95),0.2,0.3,1.0,10); a.sp('M_Marble',(0,0,12.62),0.17,2)
    a.cy('M_Marble',(0.18,0,12.75),0.04,0.04,0.7,6,(0,-25,0)); a.cy('M_Marble',(-0.2,0.05,12.1),0.04,0.04,1.3,6,(0,-8,0))
    return a.finish()
def building_corner():
    o=GA.building("building_corner",5,'M_Facade',cupola=True,w=18.0,seed=9,awn='M_AwningRed')
    return o

def ground():
    a=A("ground"); a.bevel=False
    GA.rect(a,'M_Asphalt',-130,170,-110,110,-0.3,0.0)
    for (x0,x1,z0,z1,kind) in GA.layout_blocks():
        GA.rect(a,'M_Sidewalk',x0,x1,z0,z1,0.0,0.15)
        GA.rect(a,'M_Curb',x0-0.18,x1+0.18,z0-0.18,z1+0.18,0.0,0.14)
    GA.rect(a,'M_PlazaPaving',-52,40,-26,26,0.0,0.16); GA.rect(a,'M_Curb',-52.2,40.2,-26.2,26.2,0.0,0.15)
    GA.rect(a,'M_PathStone',-100,-52,-26,26,0.0,0.155)
    for coords in LAY['lawns']:
        b=bmesh.new(); n=len(coords)
        top=[b.verts.new((x,z,0.44)) for x,z in coords]; bot=[b.verts.new((x,z,0.15)) for x,z in coords]
        ft=b.faces.new(top); fb=b.faces.new(bot[::-1])
        for i in range(n):
            j=(i+1)%n; b.faces.new((bot[i],bot[j],top[j],top[i]))
        bmesh.ops.recalc_face_normals(b,faces=list(b.faces))
        bmesh.ops.triangulate(b,faces=[ft,fb],quad_method='BEAUTY',ngon_method='BEAUTY')
        a.put2(b,'M_Lawn','M_Curb')
    # ---- identidad del piso: cordón ancho de piedra alrededor de cada cantero, discos de piedra sobre los carriles, guarda perimetral
    import site,sys
    if site.getusersitepackages() not in sys.path: sys.path.append(site.getusersitepackages())
    from shapely.geometry import Polygon as _Poly, LineString as _LS
    for coords in LAY['lawns']:
        g=_Poly(coords).buffer(0.45,quad_segs=6).simplify(0.03)
        pts=list(g.exterior.coords)[:-1]
        b=bmesh.new(); n=len(pts)
        top=[b.verts.new((x,z,0.215)) for x,z in pts]; bot=[b.verts.new((x,z,0.15)) for x,z in pts]
        ft=b.faces.new(top)
        for i in range(n):
            j=(i+1)%n; b.faces.new((bot[i],bot[j],top[j],top[i]))
        bmesh.ops.recalc_face_normals(b,faces=list(b.faces)); bmesh.ops.triangulate(b,faces=[ft],quad_method='BEAUTY',ngon_method='BEAUTY')
        a.put(b,'M_Curb',GA.Matrix.Identity(4))
    for (x0,x1,z0,z1) in ((-40,40,24.8,26),(-40,40,-26,-24.8),(38.8,40,-26,26)):
        GA.rect(a,'M_Border',x0,x1,z0,z1,0.0,0.168)
    ring=GA.cyl(5.4,5.4,0.42,48); bmesh.ops.translate(ring,vec=(0,0,0.37),verts=ring.verts)
    rb=GA.bool_mesh(ring,[GA.cyl(4.2,4.2,3,48)]); a.put2(rb,'M_Soil','M_Curb')
    a.cy('M_Curb',(0,0,0.2),5.6,5.6,0.12,48)
    for (ax,c,f,t,wd) in GA.layout_roads():
        L=t-f; n=int(L/6)
        for i in range(n):
            p=f+i*6+1.5; mat='M_RoadWhite'
            if ax=='x': GA.rect(a,mat,p,p+3,c-0.12,c+0.12,0.0,0.02)
            else: GA.rect(a,mat,c-0.12,c+0.12,p,p+3,0.0,0.02)
    for (cx,cz,alongx,l) in GA.layout_crosswalks():
        for i in range(int(l)):
            o=-l/2+0.5+i
            if alongx: GA.rect(a,'M_RoadWhite',cx-2,cx+2,cz+o-0.25,cz+o+0.25,0.0,0.02)
            else: GA.rect(a,'M_RoadWhite',cx+o-0.25,cx+o+0.25,cz-2,cz+2,0.0,0.02)
    return a.finish(flip=True)

def build_all():
    out=[]
    for f in (lambda: lamp(), lambda: palm(), lambda: tree("tree",'M_Leaf','M_Leaf2',1), lambda: tree("tree_b",'M_Leaf3','M_Leaf2',2,3.0,2.1),
              lambda: tree("tree_jacaranda",'M_Jacaranda','M_FlowerPurple',3,3.0,2.3),
              lambda: flowerbed("flowers_a",1,['M_FlowerPink','M_FlowerPurple','M_FlowerWhite']),
              lambda: flowerbed("flowers_b",2,['M_FlowerYellow','M_FlowerRed','M_FlowerPink']),
              lambda: flowerbed("flowers_c",3,['M_FlowerPurple','M_FlowerWhite','M_FlowerRed','M_FlowerYellow']),
              lambda: shrub("shrub_a",4,'M_Leaf'), lambda: shrub("shrub_b",5,'M_FlowerPink'),
              lambda: iron_fence(), lambda: monument(), lambda: building_corner(), lambda: ground()):
        o=f(); out.append((o.name,len(o.data.vertices)))
    return out
