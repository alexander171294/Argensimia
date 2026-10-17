import bpy, bmesh, math, os, random
from mathutils import Matrix, Vector, Euler
ROOT="/Users/alexander/git/argensimia/Argensimia/"
OUT=ROOT+"Assets/Models/"
os.makedirs(OUT,exist_ok=True)
PAL={}
for ln in open(ROOT+"BlenderSrc/palette.txt"):
    p=ln.split()
    if len(p)>=2:
        h=p[1].lstrip('#'); PAL[p[0]]=tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
def lin(c): return c/12.92 if c<=0.04045 else ((c+0.055)/1.055)**2.4
def M(name):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    c=PAL.get(name,(1,0,1)); cl=(lin(c[0]),lin(c[1]),lin(c[2]),1)
    m.diffuse_color=cl
    try:
        nt=m.node_tree
        if nt is None:
            m.use_nodes=True; nt=m.node_tree
        b=nt.nodes.get("Principled BSDF")
        if b: b.inputs["Base Color"].default_value=cl
    except Exception: pass
    return m
def R(a): return math.radians(a)
def cube(sx,sy,sz):
    b=bmesh.new(); bmesh.ops.create_cube(b,size=1); bmesh.ops.scale(b,vec=(sx,sy,sz),verts=b.verts); return b
def cyl(r1,r2,h,seg=12):
    b=bmesh.new(); bmesh.ops.create_cone(b,cap_ends=True,cap_tris=False,segments=seg,radius1=r1,radius2=r2,depth=h); return b
def sph(r,sub=1):
    b=bmesh.new(); bmesh.ops.create_icosphere(b,subdivisions=sub,radius=r); return b
def pyr(sx,sy,h,top=0.0):
    b=bmesh.new(); bmesh.ops.create_cone(b,cap_ends=True,cap_tris=False,segments=4,radius1=math.sqrt(0.5),radius2=max(top,0.001)*math.sqrt(0.5),depth=h)
    bmesh.ops.rotate(b,cent=(0,0,0),matrix=Matrix.Rotation(R(45),3,'Z'),verts=b.verts)
    bmesh.ops.scale(b,vec=(sx,sy,1),verts=b.verts); return b
class A:
    def __init__(s,name): s.name=name; s.bm=bmesh.new(); s.mats=[]
    def put(s,tb,mat,mx):
        bmesh.ops.transform(tb,matrix=mx,verts=tb.verts)
        if mat not in s.mats: s.mats.append(mat)
        mi=s.mats.index(mat)
        for f in tb.faces: f.material_index=mi
        me=bpy.data.meshes.new("tmp"); tb.to_mesh(me); tb.free(); s.bm.from_mesh(me); bpy.data.meshes.remove(me)
    def add(s,tb,mat,loc=(0,0,0),rot=(0,0,0)):
        s.put(tb,mat,Matrix.LocRotScale(Vector(loc),Euler((R(rot[0]),R(rot[1]),R(rot[2]))),Vector((1,1,1))))
    def box(s,mat,loc,size,rot=(0,0,0)): s.add(cube(*size),mat,loc,rot)
    def cy(s,mat,loc,r1,r2,h,seg=12,rot=(0,0,0)): s.add(cyl(r1,r2,h,seg),mat,loc,rot)
    def sp(s,mat,loc,r,sub=1,scale=(1,1,1)):
        b=sph(r,sub); bmesh.ops.scale(b,vec=scale,verts=b.verts); s.add(b,mat,loc)
    def finish(s,export=True,flip=False):
        if flip: bmesh.ops.rotate(s.bm,cent=(0,0,0),matrix=Matrix.Rotation(R(180),3,'Z'),verts=s.bm.verts)
        me=bpy.data.meshes.new(s.name); s.bm.to_mesh(me); s.bm.free()
        for m in s.mats: me.materials.append(M(m))
        o=bpy.data.objects.new(s.name,me); bpy.context.scene.collection.objects.link(o)
        if export:
            bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
            bpy.ops.export_scene.fbx(filepath=OUT+s.name+".fbx",use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=True,object_types={'MESH'},mesh_smooth_type='FACE',use_mesh_modifiers=True)
        return o
def clear():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    for m in list(bpy.data.meshes): bpy.data.meshes.remove(m)

# ---------------- props (front = Blender -Y = Unity +Z) -----------------
def face_box(a,mat,ang,half,xl,z,size,out=0.0):
    c,s_=math.cos(R(ang)),math.sin(R(ang)); y=-(half+out)
    a.box(mat,(xl*c-y*s_, xl*s_+y*c, z),size,(0,0,ang))

def building(name,floors,facade,cupola=False,w=14.0,roof='M_Roof',awn='M_AwningRed'):
    a=A(name); h=w/2; fh=3.2; top=3.6+floors*fh
    a.box('M_FacadeDark',(0,0,1.8),(w,w,3.6))
    a.box(facade,(0,0,3.6+floors*fh/2),(w-0.3,w-0.3,floors*fh))
    for k in range(floors+1):
        a.box('M_Cornice',(0,0,3.6+k*fh),(w+0.15,w+0.15,0.22))
    a.box('M_Cornice',(0,0,top+0.25),(w+0.5,w+0.5,0.5))
    a.add(pyr((w-0.3),(w-0.3),3.6,0.62),roof,(0,0,top+0.5+1.8))
    cols=[-4.5,-1.5,1.5,4.5] if w>=13 else [-3,0,3]
    for ang in (0,90,180,270):
        for k in range(floors):
            z=3.6+k*fh+1.6
            for xl in cols:
                face_box(a,'M_Window',ang,h-0.15,xl,z,(1.25,0.2,1.9),0.02)
                if k%2==0:
                    face_box(a,'M_Cornice',ang,h-0.15,xl,z-1.05,(1.8,0.6,0.12),0.3)
                    face_box(a,'M_Iron',ang,h-0.15,xl,z-0.75,(1.8,0.05,0.5),0.58)
        for xl in ([-3.3,3.3] if w>=13 else [-2.5,2.5]):
            face_box(a,'M_ShopGlow',ang,h,xl,1.5,(3.2,0.2,2.3),0.02)
            face_box(a,awn,ang,h,xl,3.0,(3.6,1.3,0.1),0.6)
        for xl in ([-4.5,0,4.5] if w>=13 else [-3,3]):
            face_box(a,roof,ang,h-0.3,xl,top+1.5,(1.1,1.2,1.2),-0.75)
            face_box(a,'M_Window',ang,h-0.3,xl,top+1.45,(0.6,0.1,0.75),-0.12)
            face_box(a,'M_Cornice',ang,h-0.3,xl,top+2.15,(1.3,1.3,0.12),-0.7)
    if cupola:
        a.cy(facade,(h-1.8,-(h-1.8),top+1.0),1.9,1.9,2.0,16)
        a.sp(roof,(h-1.8,-(h-1.8),top+2.0),1.95,2,(1,1,1.3))
        a.cy('M_Iron',(h-1.8,-(h-1.8),top+4.6),0.08,0.02,1.4,6)
    return a.finish()

def casa_rosada():
    a=A("casa_rosada")
    a.box('M_RosadaDark',(0,0,0.6),(52,19,1.2))
    a.box('M_Rosada',(0,0,5.6),(50,17,9))
    a.box('M_Cornice',(0,0,10.3),(50.6,17.6,0.6))
    a.add(pyr(50,17,2.4,0.88),'M_RoofGreen',(0,0,11.8))
    # portico
    a.box('M_Rosada',(0,-0.8,7.0),(11,19,12.8))
    a.box('M_Cornice',(0,-0.8,13.6),(11.6,19.6,0.6))
    a.box('M_Rosada',(0,-0.8,14.6),(7,8,1.6))
    a.box('M_Cornice',(0,-0.8,15.5),(7.4,8.4,0.3))
    a.box('M_Window',(0,-10.35,3.6),(4.2,0.3,5.6))
    a.cy('M_Window',(0,-10.35,6.4),2.1,2.1,0.3,16,(90,0,0))
    for x in (-4.2,4.2):
        a.box('M_Cornice',(x,-10.45,6.5),(0.9,0.5,11.5))
    for z in (9.3,):
        for x in (-2.2,0,2.2):
            a.box('M_Window',(x,-10.35,z),(1.3,0.2,2.3))
            a.box('M_Cornice',(x,-10.45,z-1.3),(1.8,0.6,0.12))
    # pavilions
    for sx in (-1,1):
        x=sx*22
        a.box('M_Rosada',(x,0,7.0),(9,19,12.8))
        a.box('M_Cornice',(x,0,13.5),(9.6,19.6,0.6))
        a.add(pyr(9.4,19.4,4.5,0.35),'M_RoofGreen',(x,0,16.0))
        a.box('M_RoofGreen',(x,0,18.6),(1.2,4,1.0))
        for z in (3.4,7.2,11.0):
            for xx in (-2.4,0,2.4):
                a.box('M_Cornice',(x+xx,-9.55,z),(1.8,0.15,2.9))
                a.box('M_Window',(x+xx,-9.62,z),(1.25,0.15,2.3))
            for yy in (-6,-2,2,6):
                a.box('M_Window',(x+sx*4.55,yy,z),(0.15,1.25,2.3))
    for z in (3.4,7.2):
        for i in range(-4,5):
            x=i*2.6
            if abs(x)<6.5 or abs(x)>16.5: continue
            a.box('M_Cornice',(x,-8.55,z),(1.8,0.15,2.9))
            a.box('M_Window',(x,-8.62,z),(1.25,0.15,2.3))
    # balcony
    a.box('M_Cornice',(0,-10.9,8.0),(6,1.2,0.25))
    a.box('M_Iron',(0,-11.45,8.5),(6,0.08,0.8))
    # flag
    a.cy('M_Cornice',(0,-0.8,19.0),0.09,0.06,6.0,8)
    for i,mn in enumerate(('M_Celeste','M_White','M_Celeste')):
        a.box(mn,(1.6,-0.8,21.6-i*0.55),(3.0,0.05,0.55))
    a.sp('M_SunYellow',(1.6,-0.84,21.05),0.18,1,(1,0.3,1))
    a.finish()

def palm():
    a=A("palm"); h=0
    for i in range(5):
        r=0.36-i*0.025; a.cy('M_PalmTrunk',(0.06*i,0,h+0.7),r,r-0.03,1.4,8); h+=1.4
    a.sp('M_PalmLeaf',(0.3,0,h),0.45,1)
    for k in range(9):
        b=cube(0.9,3.8,0.08); bmesh.ops.translate(b,vec=(0,1.9,0),verts=b.verts)
        mx=Matrix.Translation((0.3,0,h))@Matrix.Rotation(R(k*40+random.uniform(-8,8)),4,'Z')@Matrix.Rotation(R(-25-random.uniform(0,20)),4,'X')
        a.put(b,'M_PalmLeaf',mx)
    a.finish()
def tree(name,leaf1,leaf2):
    a=A(name); a.cy('M_Bark',(0,0,1.5),0.28,0.2,3.0,8)
    a.sp(leaf1,(0,0,4.2),2.2,1); a.sp(leaf2,(0.9,0.6,3.6),1.6,1); a.sp(leaf1,(-0.8,-0.5,3.5),1.5,1); a.sp(leaf2,(0.1,-0.2,5.3),1.4,1)
    a.finish()
def lamp():
    a=A("lamp"); a.cy('M_Iron',(0,0,0.3),0.22,0.16,0.6,8); a.cy('M_Iron',(0,0,2.3),0.08,0.07,4.0,8)
    a.box('M_LampGlow',(0,0,4.55),(0.42,0.42,0.6)); a.add(pyr(0.6,0.6,0.35,0.1),'M_Iron',(0,0,5.0)); a.box('M_Iron',(0,0,4.22),(0.5,0.5,0.08))
    a.finish()
def bench():
    a=A("bench")
    for i in range(3): a.box('M_Wood',(0,-0.18+i*0.18,0.48),(2.0,0.14,0.06))
    a.box('M_Wood',(0,0.3,0.75),(2.0,0.06,0.18),(-12,0,0)); a.box('M_Wood',(0,0.33,0.98),(2.0,0.06,0.18),(-12,0,0))
    for x in (-0.85,0.85): a.box('M_Iron',(x,0.05,0.45),(0.08,0.6,0.9))
    a.finish()
def flower(name,seed):
    random.seed(seed); a=A(name); a.sp('M_Hedge',(0,0,0.25),0.7,1,(1.4,1,0.5))
    cols=['M_FlowerPink','M_FlowerPurple','M_FlowerYellow','M_FlowerRed']
    for i in range(7):
        a.sp(random.choice(cols),(random.uniform(-0.8,0.8),random.uniform(-0.5,0.5),0.45+random.uniform(0,0.15)),random.uniform(0.22,0.34),1)
    a.finish()
def hedge():
    a=A("hedge"); a.box('M_Hedge',(0,0,0.4),(3,0.8,0.8)); a.finish()
def monument():
    a=A("monument")
    a.box('M_Marble',(0,0,0.25),(7,7,0.5)); a.box('M_Marble',(0,0,0.75),(5.6,5.6,0.5)); a.box('M_Marble',(0,0,2.0),(3.4,3.4,2.0))
    a.box('M_Cornice',(0,0,3.1),(3.8,3.8,0.25))
    a.add(pyr(2.2,2.2,8.0,0.65),'M_Marble',(0,0,7.2)); a.add(pyr(1.6,1.6,0.6,0.3),'M_Marble',(0,0,11.5))
    a.cy('M_Marble',(0,0,12.6),0.3,0.38,1.6,8); a.sp('M_Marble',(0,0,13.6),0.25,1)
    a.cy('M_Marble',(0.25,0,13.6),0.07,0.07,1.0,6,(0,20,0))
    for k in range(4):
        ang=k*90; c,s_=math.cos(R(ang)),math.sin(R(ang))
        a.box('M_Iron',(2.6*c,2.6*s_,1.2),(0.05 if k%2==0 else 3.4,3.4 if k%2==0 else 0.05,0.9))
    a.finish()
def iron_fence():
    a=A("iron_fence")
    a.box('M_Iron',(0,0,0.15),(4,0.07,0.07)); a.box('M_Iron',(0,0,1.3),(4,0.07,0.07))
    for i in range(17):
        x=-1.9+i*3.8/16; a.cy('M_Iron',(x,0,0.75),0.022,0.022,1.5,5); a.cy('M_Iron',(x,0,1.58),0.05,0.0,0.16,5)
    for x in (-2,2): a.box('M_Iron',(x,0,0.8),(0.12,0.12,1.6))
    a.finish()
def valla():
    a=A("valla")
    a.box('M_Metal',(0,0,1.1),(2.4,0.06,0.06)); a.box('M_Metal',(0,0,0.25),(2.4,0.06,0.06))
    for x in (-1.2,1.2): a.box('M_Metal',(x,0,0.6),(0.07,0.07,1.2)); a.box('M_Metal',(x,0,0.03),(0.1,0.7,0.06))
    for i in range(11): a.box('M_Metal',(-1.0+i*0.2,0,0.67),(0.025,0.025,0.85))
    a.finish()
def truck():
    a=A("police_truck")
    a.box('M_Dark',(0,0.2,0.75),(2.3,7.3,0.6))
    a.box('M_PoliceBlue',(0,1.1,1.95),(2.5,5.0,2.5))
    a.box('M_PoliceBlue',(0,-2.55,1.65),(2.5,2.2,1.9))
    a.box('M_Glass',(0,-3.62,2.05),(2.3,0.1,0.85)); a.box('M_Glass',(1.26,-2.6,2.1),(0.05,1.2,0.7)); a.box('M_Glass',(-1.26,-2.6,2.1),(0.05,1.2,0.7))
    a.box('M_White',(0,1.1,1.45),(2.56,5.02,0.3)); a.box('M_White',(0,-2.55,1.2),(2.56,2.22,0.25))
    a.box('M_LightRed',(-0.45,-2.6,2.67),(0.8,0.3,0.15)); a.box('M_LightBlue',(0.45,-2.6,2.67),(0.8,0.3,0.15))
    a.box('M_LampGlow',(-0.85,-3.66,1.1),(0.4,0.05,0.2)); a.box('M_LampGlow',(0.85,-3.66,1.1),(0.4,0.05,0.2))
    a.cy('M_Dark',(0,-0.2,3.45),0.55,0.45,0.4,10)
    a.cy('M_Dark',(0,-1.1,3.75),0.12,0.08,1.8,8,(80,0,0))
    for y in (-2.5,1.0,2.5):
        for x in (-1.15,1.15): a.cy('M_Tire',(x,y,0.55),0.55,0.55,0.4,12,(0,90,0))
    a.finish()
def officer():
    a=A("police_officer")
    for x in (-0.12,0.12): a.box('M_PoliceNavy',(x,0,0.45),(0.2,0.24,0.9)); a.box('M_HelmetBlack',(x,-0.03,0.06),(0.22,0.32,0.12))
    a.box('M_PoliceNavy',(0,0,1.22),(0.52,0.32,0.68)); a.box('M_HelmetBlack',(0,0,1.25),(0.56,0.36,0.5))
    a.box('M_PoliceNavy',(-0.3,-0.12,1.2),(0.14,0.3,0.55),(30,0,0)); a.box('M_PoliceNavy',(0.3,-0.12,1.2),(0.14,0.3,0.55),(30,0,0))
    a.sp('M_Skin',(0,0,1.68),0.14,1); a.sp('M_HelmetBlack',(0,0.01,1.74),0.18,1,(1,1,0.92)); a.box('M_Glass',(0,-0.16,1.68),(0.26,0.04,0.14))
    a.box('M_Shield',(0,-0.36,1.0),(0.7,0.05,1.2)); a.box('M_White',(0,-0.39,1.45),(0.5,0.02,0.12))
    a.finish()
def person(name,raised=False):
    a=A(name)
    for x in (-0.1,0.1): a.box('M_Pants',(x,0,0.42),(0.17,0.2,0.84)); a.box('M_Dark',(x,-0.03,0.05),(0.18,0.28,0.1))
    a.box('M_Shirt',(0,0,1.15),(0.46,0.26,0.62))
    a.box('M_Shirt',(-0.29,0,1.12),(0.12,0.14,0.56))
    if raised: a.box('M_Shirt',(0.31,0,1.62),(0.12,0.14,0.6),(0,-15,0)); a.sp('M_Skin',(0.38,0,1.95),0.07,1)
    else: a.box('M_Shirt',(0.29,0,1.12),(0.12,0.14,0.56))
    a.sp('M_Skin',(0,0,1.6),0.14,1); a.sp('M_Hair',(0,0.03,1.66),0.145,1,(1,1,0.7))
    a.finish()
def flag(name,kind):
    a=A(name); a.cy('M_Wood',(0,0,1.7),0.03,0.03,3.4,6)
    if kind=='arg':
        for i,mn in enumerate(('M_Celeste','M_White','M_Celeste')): a.box(mn,(0.8,0,3.15-i*0.3),(1.6,0.03,0.3))
        a.sp('M_SunYellow',(0.8,-0.02,2.85),0.1,1,(1,0.3,1))
    else:
        a.box('M_FlagRed',(0.8,0,2.85),(1.6,0.03,0.9))
    a.finish()
def banner():
    a=A("banner")
    for x in (-2,2): a.cy('M_Wood',(x,0,1.2),0.03,0.03,2.4,6)
    a.box('M_White',(0,0,1.75),(4,0.03,1.0)); a.box('M_FlagRed',(0,-0.02,2.1),(4,0.03,0.2)); a.box('M_FlagRed',(0,-0.02,1.4),(2.6,0.03,0.15))
    a.finish()
def taxi():
    a=A("taxi")
    a.box('M_TaxiBlack',(0,0,0.65),(1.75,4.4,0.7)); a.box('M_TaxiYellow',(0,0.2,1.28),(1.6,2.3,0.56)); a.box('M_Glass',(0,0.2,1.25),(1.64,2.0,0.36))
    a.box('M_TaxiYellow',(0,0.2,1.54),(1.6,2.3,0.06)); a.box('M_White',(0,0.1,1.66),(0.5,0.15,0.2))
    a.box('M_LampGlow',(-0.6,-2.21,0.75),(0.3,0.04,0.15)); a.box('M_LampGlow',(0.6,-2.21,0.75),(0.3,0.04,0.15))
    a.box('M_LightRed',(-0.65,2.21,0.75),(0.3,0.04,0.15)); a.box('M_LightRed',(0.65,2.21,0.75),(0.3,0.04,0.15))
    for y in (-1.4,1.4):
        for x in (-0.82,0.82): a.cy('M_Tire',(x,y,0.35),0.35,0.35,0.25,10,(0,90,0))
    a.finish()
def bus():
    a=A("bus")
    a.box('M_BusWhite',(0,0,1.75),(2.5,11,2.9)); a.box('M_Glass',(0,0.3,2.25),(2.54,9.6,1.0)); a.box('M_Glass',(0,-5.52,2.1),(2.3,0.06,1.4))
    a.box('M_BusBlue',(0,0,1.2),(2.54,11.02,0.4)); a.box('M_BusBlue',(0,0,3.25),(2.3,10.5,0.15))
    a.box('M_LampGlow',(0,-5.53,3.0),(1.6,0.04,0.25))
    for y in (-3.6,3.4):
        for x in (-1.15,1.15): a.cy('M_Tire',(x,y,0.5),0.5,0.5,0.35,12,(0,90,0))
    a.finish()
def trash():
    a=A("trash_bin"); a.cy('M_Iron',(0,0,0.45),0.25,0.25,0.9,10); a.cy('M_Metal',(0,0,0.92),0.27,0.2,0.08,10); a.finish()

# ---------------- ground (built in Unity XZ, flipped at end) -----------------
def rect(a,mat,x0,x1,z0,z1,y0,y1): a.box(mat,((x0+x1)/2,(z0+z1)/2,(y0+y1)/2),(x1-x0,z1-z0,y1-y0))
def bool_mesh(base_bm,cutters):
    me=bpy.data.meshes.new("b"); base_bm.to_mesh(me); base_bm.free()
    o=bpy.data.objects.new("b",me); bpy.context.scene.collection.objects.link(o)
    tmp=[]
    for cb in cutters:
        cm=bpy.data.meshes.new("c"); cb.to_mesh(cm); cb.free(); co=bpy.data.objects.new("c",cm); bpy.context.scene.collection.objects.link(co); co.hide_render=True
        md=o.modifiers.new("x",'BOOLEAN'); md.operation='DIFFERENCE'; md.object=co
        try: md.solver='EXACT'
        except Exception: pass
        tmp.append(co)
    dg=bpy.context.evaluated_depsgraph_get(); dg.update()
    nm=bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    for co in tmp: bpy.data.objects.remove(co)
    bpy.data.objects.remove(o)
    b=bmesh.new(); b.from_mesh(nm); bpy.data.meshes.remove(nm); return b

PX,PZ=40,26       # plaza half extents (incl. perimeter walk)
LX,LZ=37,23       # lawn region
def ground():
    a=A("ground")
    rect(a,'M_Asphalt',-130,170,-110,110,-0.3,0.0)
    blocks=layout_blocks()
    for (x0,x1,z0,z1,kind) in blocks:
        rect(a,'M_Sidewalk',x0,x1,z0,z1,0.0,0.15)
        rect(a,'M_Curb',x0-0.05,x1+0.05,z0-0.05,z1+0.05,0.0,0.12)
    # plaza + pedestrian strip in front of Casa Rosada
    rect(a,'M_PlazaPaving',-52,PX,-PZ,PZ,0.0,0.16)
    rect(a,'M_Border',-52,PX,PZ-1.2,PZ,0.0,0.165); rect(a,'M_Border',-52,PX,-PZ,-PZ+1.2,0.0,0.165); rect(a,'M_Border',PX-1.2,PX,-PZ,PZ,0.0,0.165)
    rect(a,'M_Curb',-52.1,PX+0.1,-PZ-0.1,PZ+0.1,0.0,0.13)
    rect(a,'M_PathStone',-100,-52,-PZ,PZ,0.0,0.155)
    # lawns
    diag_ang=math.degrees(math.atan2(LZ,LX))
    for sx in (-1,1):
        for sz in (-1,1):
            base=cube(LX-3.0,LZ-3.0,0.3); bmesh.ops.translate(base,vec=(sx*(LX+3.0)/2,sz*(LZ+3.0)/2,0.28),verts=base.verts)
            ve=[e for e in base.edges if abs(e.verts[0].co.z-e.verts[1].co.z)>0.1]
            bmesh.ops.bevel(base,geom=ve,offset=2.2,offset_type='OFFSET',segments=5,profile=0.5,affect='EDGES')
            c1=cyl(10.5,10.5,3,40)
            c2=cube(60,3.4,3); bmesh.ops.rotate(c2,cent=(0,0,0),matrix=Matrix.Rotation(R(sx*sz*diag_ang),3,'Z'),verts=c2.verts)
            bmesh.ops.translate(c2,vec=(sx*LX/2,sz*LZ/2,0),verts=c2.verts)
            lb=bool_mesh(base,[c1,c2])
            a.put2(lb,'M_Lawn','M_Curb')
    # flower ring around monument
    ring=cyl(6.2,6.2,0.5,40); bmesh.ops.translate(ring,vec=(0,0,0.4),verts=ring.verts)
    rb=bool_mesh(ring,[cyl(4.9,4.9,3,40)]); a.put(rb,'M_Hedge',Matrix.Identity(4))
    a.cy('M_PathStone',(0,0,0.17),10.4,10.4,0.04,48)
    # crosswalks & markings
    def zebra(cx,cz,along_x,length,width=4.0):
        n=int(length/1.0)
        for i in range(n):
            o=-length/2+0.5+i*1.0
            if along_x: rect(a,'M_RoadWhite',cx-width/2,cx+width/2,cz+o-0.25,cz+o+0.25,0.0,0.02)
            else: rect(a,'M_RoadWhite',cx+o-0.25,cx+o+0.25,cz-width/2,cz+width/2,0.0,0.02)
    # roads: (axis, center, from, to, width)
    for (ax,c,f,t,wd) in layout_roads():
        L=t-f; n=int(L/6)
        for i in range(n):
            p=f+i*6+1.5
            if ax=='x': rect(a,'M_RoadWhite' if wd<14 else 'M_RoadYellow',p,p+3,c-0.12,c+0.12,0.0,0.02)
            else: rect(a,'M_RoadWhite' if wd<14 else 'M_RoadYellow',c-0.12,c+0.12,p,p+3,0.0,0.02)
    for (cx,cz,ax,l) in layout_crosswalks(): zebra(cx,cz,ax,l)
    a.finish(flip=True)

def layout_roads():
    return [('x',31,-130,170,10),('x',-31,-130,170,10),('z',45,-110,110,10),('x',0,50,170,12),
            ('z',0,-110,-36,10),('z',0,36,110,10),('z',115,-110,110,10),('z',-105,-110,110,10),('x',75,-130,170,10),('x',-75,-130,170,10)]
def layout_crosswalks():
    out=[(0,31,True,10),(0,-31,True,10),(45,0,False,10),(56,0,True,12),(0,39,False,10),(0,-39,False,10)]
    for sz in (1,-1):
        out+= [(38,31*sz,True,10),(52,31*sz,True,10),(45,38*sz,False,10),(45,24*sz,False,10),(-25,31*sz,True,10)]
    return out
def layout_blocks():
    xs=[(-130,-110),(-100,-5),(5,40),(50,110),(120,170)]
    out=[]
    for (x0,x1) in xs:
        for (z0,z1) in [(36,70),(80,110),(-70,-36),(-110,-80)]:
            out.append((x0,x1,z0,z1,'city'))
    for (x0,x1) in [(-130,-110)]:
        out.append((x0,x1,-26,26,'city'))
    for (x0,x1) in [(50,110),(120,170)]:
        out.append((x0,x1,6,26,'city')); out.append((x0,x1,-26,-6,'city'))
    return out

# ======================= v2 (aesthetic pass) =======================
def _add(s,tb,mat,loc=(0,0,0),rot=(0,0,0),scale=(1,1,1)):
    s.put(tb,mat,Matrix.LocRotScale(Vector(loc),Euler((R(rot[0]),R(rot[1]),R(rot[2]))),Vector(scale)))
A.add=_add
def _cy(s,mat,loc,r1,r2,h,seg=12,rot=(0,0,0),scale=(1,1,1)): s.add(cyl(r1,r2,h,seg),mat,loc,rot,scale)
A.cy=_cy
def _put2(s,tb,top,side):
    for m in (top,side):
        if m not in s.mats: s.mats.append(m)
    it,isd=s.mats.index(top),s.mats.index(side)
    tb.normal_update()
    for f in tb.faces: f.material_index = it if f.normal.z>0.5 else isd
    me=bpy.data.meshes.new("tmp"); tb.to_mesh(me); tb.free(); s.bm.from_mesh(me); bpy.data.meshes.remove(me)
A.put2=_put2

def face_box(a,mat,ang,half,xl,z,size,out=0.0,pitch=0.0):
    c,s_=math.cos(R(ang)),math.sin(R(ang)); y=-(half+out)
    a.box(mat,(xl*c-y*s_, xl*s_+y*c, z),size,(pitch,0,ang))
def face_cy(a,mat,ang,half,xl,z,r,depth,out=0.0,seg=12):
    c,s_=math.cos(R(ang)),math.sin(R(ang)); y=-(half+out)
    a.cy(mat,(xl*c-y*s_, xl*s_+y*c, z),r,r,depth,seg,(90,0,ang))


def tri_prism(w,h,d):
    b=bmesh.new()
    v=[b.verts.new(p) for p in [(-w/2,-d/2,0),(w/2,-d/2,0),(0,-d/2,h),(-w/2,d/2,0),(w/2,d/2,0),(0,d/2,h)]]
    b.faces.new((v[0],v[1],v[2])); b.faces.new((v[5],v[4],v[3]))
    b.faces.new((v[0],v[3],v[4],v[1])); b.faces.new((v[1],v[4],v[5],v[2])); b.faces.new((v[2],v[5],v[3],v[0]))
    bmesh.ops.recalc_face_normals(b,faces=list(b.faces)); return b
def face_put(a,tb,mat,ang,half,xl,z,out=0.0):
    c,s_=math.cos(R(ang)),math.sin(R(ang)); y=-(half+out)
    a.add(tb,mat,(xl*c-y*s_, xl*s_+y*c, z),(0,0,ang))

def building(name,floors,facade,cupola=False,w=14.0,roof='M_Roof',awn='M_AwningRed',seed=0):
    rnd=random.Random(seed); a=A(name); h=w/2; fh=3.2; gf=4.0; top=gf+floors*fh
    a.box('M_FacadeDark',(0,0,gf/2),(w,w,gf))
    for zz in (1.0,2.0,3.0): a.box('M_Plinth',(0,0,zz),(w+0.04,w+0.04,0.07))
    a.box(facade,(0,0,gf+floors*fh/2),(w-0.3,w-0.3,floors*fh))
    a.box('M_Cornice',(0,0,gf),(w+0.3,w+0.3,0.4))
    for k in range(1,floors): a.box('M_Cornice',(0,0,gf+k*fh-0.2),(w-0.1,w-0.1,0.14))
    a.box('M_Cornice',(0,0,top-0.1),(w+0.3,w+0.3,0.3)); a.box('M_Cornice',(0,0,top+0.2),(w+0.8,w+0.8,0.35))
    for cx in (-1,1):
        for cz in (-1,1): a.box('M_Cornice',(cx*(h-0.25),cz*(h-0.25),gf+floors*fh/2),(0.75,0.75,floors*fh))
    # mansarda parisina: faldón inferior casi vertical (pizarra) + faldón superior bajo
    MH=4.0; MT=0.78
    a.add(pyr(w-0.2,w-0.2,MH,MT),'M_Slate',(0,0,top+0.37+MH/2))
    a.box('M_Iron',(0,0,top+0.37+MH),(MT*(w-0.2)+0.12,MT*(w-0.2)+0.12,0.1))
    a.add(pyr(MT*(w-0.2),MT*(w-0.2),1.1,0.25),roof,(0,0,top+0.37+MH+0.55))
    for (cx,cy_) in [(-2.4,1.6),(2.6,-1.9),(0.6,2.7)][:rnd.randint(1,3)]:
        a.box('M_Chimney',(cx*0.75,cy_*0.75,top+5.2),(1.6,0.7,1.9)); a.box('M_Cornice',(cx*0.75,cy_*0.75,top+6.2),(1.8,0.9,0.14))
        for pk in (-0.5,0.0,0.5): a.cy('M_Chimney',(cx*0.75+pk,cy_*0.75,top+6.45),0.13,0.11,0.4,8)
    cols=[-5.2,-2.6,0.0,2.6,5.2] if w>=13 else [-3,0,3]
    shut=rnd.choice([None,'M_Shutter','M_Shutter2','M_Shutter'])
    for ang in (0,90,180,270):
        nm=int((w-0.6)/0.62)
        for j in range(nm): face_box(a,'M_Cornice',ang,h-0.15,-(w-0.6)/2+0.31+j*(w-0.6)/nm,top-0.32,(0.16,0.42,0.3),0.12)
        face_box(a,'M_Cornice',ang,h-0.15,0,gf+fh-0.05,(w-0.4,0.25,0.16),0.06)
        for k in range(floors):
            z=gf+k*fh+1.6
            for i_,xl in enumerate(cols):
                # vano francés: marco claro, carpintería oscura, cristal (a veces iluminado), dintel y balconcito de hierro
                face_box(a,'M_Cornice',ang,h-0.15,xl,z,(1.45,0.12,2.55),0.0)
                face_box(a,'M_Dark',ang,h-0.15,xl,z-0.05,(1.08,0.1,2.25),0.06)
                face_box(a,'M_WindowLit' if rnd.random()<0.22 else 'M_Window',ang,h-0.15,xl,z-0.05,(0.86,0.1,2.05),0.1)
                face_box(a,'M_Dark',ang,h-0.15,xl,z-0.05,(0.07,0.1,2.05),0.13)
                face_box(a,'M_Cornice',ang,h-0.15,xl,z+1.42,(1.8,0.38,0.17),0.15)
                if k>0 and not (k%2==0 or k==floors-1):
                    face_box(a,'M_Iron',ang,h-0.15,xl,z-0.85,(1.25,0.04,0.42),0.42)
                    face_box(a,'M_Cornice',ang,h-0.15,xl,z-1.12,(1.35,0.45,0.08),0.2)
                    for bx in (-0.45,0.0,0.45): face_box(a,'M_Cornice',ang,h-0.15,xl+bx,z-1.24,(0.1,0.3,0.16),0.2)
                if shut and k<floors-1:
                    for sd_ in (-1,1): face_box(a,shut,ang,h-0.15,xl+sd_*0.92,z-0.05,(0.42,0.08,2.15),0.08)
                if k==0:
                    # piso noble: frontones alternados triangular / segmental con ménsulas
                    if i_%2==0: face_put(a,tri_prism(1.85,0.55,0.32),'M_Cornice',ang,h-0.15,xl,z+1.5,0.16)
                    else: face_box(a,'M_Cornice',ang,h-0.15,xl,z+1.62,(1.95,0.36,0.2),0.18)
                    for sd_ in (-1,1): face_box(a,'M_Cornice',ang,h-0.15,xl+sd_*0.82,z+1.15,(0.16,0.3,0.42),0.15)
                elif k==floors-1:
                    face_box(a,'M_Cornice',ang,h-0.15,xl,z-1.35,(0.5,0.16,0.35),0.06)
            if k==0:
                face_box(a,'M_Cornice',ang,h-0.15,0,z-1.18,(w-1.6,0.9,0.15),0.45)
                face_box(a,'M_Iron',ang,h-0.15,0,z-0.85,(w-1.6,0.05,0.55),0.88)
            elif k%2==0 or k==floors-1:
                for xl in cols:
                    face_box(a,'M_Cornice',ang,h-0.15,xl,z-1.18,(1.75,0.6,0.13),0.3)
                    face_box(a,'M_Iron',ang,h-0.15,xl,z-0.88,(1.75,0.05,0.5),0.58)
        for xl in ([-3.6,3.6] if w>=13 else [-2.6,2.6]):
            face_box(a,'M_Iron',ang,h,xl,1.6,(3.2,0.1,2.7),0.0)
            face_box(a,'M_ShopGlow',ang,h,xl,1.55,(2.9,0.1,2.45),0.09)
            face_box(a,awn,ang,h,xl,3.25,(3.4,1.5,0.08),0.72,22)
            face_box(a,'M_White',ang,h,xl,2.97,(3.4,0.05,0.18),1.42)
            face_box(a,'M_ShopSign',ang,h,xl,3.72,(2.4,0.08,0.42),0.05)
            face_box(a,'M_Iron',ang,h,xl,3.72,(2.55,0.06,0.5),0.02)
        face_box(a,'M_Dark',ang,h,0,1.35,(1.3,0.1,2.6),0.08)
        face_box(a,'M_Cornice',ang,h,0,2.8,(1.7,0.25,0.2),0.12)
        for xl in cols:
            # lucarnas con frontón curvo y ventana iluminada a veces
            face_box(a,'M_Cornice',ang,h-0.1,xl,top+1.9,(1.25,1.0,1.7),-0.55)
            face_box(a,'M_WindowLit' if rnd.random()<0.25 else 'M_Window',ang,h-0.1,xl,top+1.85,(0.7,0.1,1.15),0.0)
            face_box(a,'M_Cornice',ang,h-0.1,xl,top+2.82,(1.45,1.05,0.16),-0.5)
            face_cy(a,'M_Slate',ang,h-0.1,xl,top+2.9,0.66,1.0,-0.5,14)
        # pilastras entre vanos y balcón corrido en el último piso
        bays=[(cols[i]+cols[i+1])/2 for i in range(len(cols)-1)]
        for xl in bays:
            face_box(a,'M_Cornice',ang,h-0.15,xl,gf+floors*fh/2,(0.42,0.14,floors*fh-0.3),0.0)
        zt=gf+(floors-1)*fh+0.42
        face_box(a,'M_Cornice',ang,h-0.15,0,zt,(w-1.0,0.95,0.15),0.45)
        face_box(a,'M_Iron',ang,h-0.15,0,zt+0.33,(w-1.0,0.05,0.55),0.9)
    if cupola:
        cx,cy_=h-1.9,-(h-1.9)
        a.cy(facade,(cx,cy_,top+1.4),2.0,2.0,2.4,20); a.cy('M_Cornice',(cx,cy_,top+2.65),2.15,2.15,0.2,20)
        for k in range(8):
            t=R(k*45); a.box('M_Window',(cx+math.cos(t)*2.06,cy_+math.sin(t)*2.06,top+1.4),(0.5,0.1,1.2),(0,0,k*45+90))
        a.sp(roof,(cx,cy_,top+2.7),2.05,2,(1,1,1.25)); a.cy('M_Iron',(cx,cy_,top+5.6),0.07,0.02,1.4,6); a.sp('M_Gold',(cx,cy_,top+5.0),0.15,1)
    return a.finish()

def balustrade(a,p0,p1,z,mat='M_Cornice'):
    x0,y0=p0; x1,y1=p1; L=math.hypot(x1-x0,y1-y0); ang=math.degrees(math.atan2(y1-y0,x1-x0)); n=max(2,int(L/0.5))
    cx,cy_=(x0+x1)/2,(y0+y1)/2
    a.box(mat,(cx,cy_,z+0.82),(L,0.4,0.12),(0,0,ang)); a.box(mat,(cx,cy_,z+0.06),(L,0.36,0.12),(0,0,ang))
    for i in range(n+1):
        t=i/n; a.cy(mat,(x0+(x1-x0)*t,y0+(y1-y0)*t,z+0.44),0.08,0.11,0.64,6)
def arch_win(a,x,y,z,w,hgt,frame='M_Cornice',glass='M_Window',side_axis=False):
    if not side_axis:
        a.box(frame,(x,y+0.02,z),(w+0.36,0.12,hgt+0.1)); a.cy(frame,(x,y+0.02,z+hgt/2),w/2+0.18,w/2+0.18,0.12,14,(90,0,0))
        a.box(glass,(x,y-0.03,z-0.05),(w,0.1,hgt)); a.cy(glass,(x,y-0.03,z+hgt/2-0.05),w/2,w/2,0.1,14,(90,0,0))
    else:
        a.box(frame,(x+0.02,y,z),(0.12,w+0.36,hgt+0.1)); a.cy(frame,(x+0.02,y,z+hgt/2),w/2+0.18,w/2+0.18,0.12,14,(0,90,0))
        a.box(glass,(x-0.03,y,z-0.05),(0.1,w,hgt)); a.cy(glass,(x-0.03,y,z+hgt/2-0.05),w/2,w/2,0.1,14,(0,90,0))

def casa_rosada():
    a=A("casa_rosada"); rnd=random.Random(3)
    a.box('M_RosadaDark',(0,0,0.6),(52,19,1.2))
    a.box('M_Rosada',(0,0,5.7),(48,17,9.2))
    for zz in (1.9,2.8,3.7): a.box('M_RosadaDark',(0,0,zz),(48.06,17.06,0.08))
    a.box('M_Cornice',(0,0,4.6),(48.3,17.3,0.35))
    a.box('M_Cornice',(0,0,10.2),(48.6,17.6,0.5)); a.box('M_Cornice',(0,0,9.85),(48.3,17.3,0.25))
    a.add(pyr(46,15.5,2.0,0.85),'M_RoofGreen',(0,0,11.4))
    balustrade(a,(-17.5,-8.55),(-6.2,-8.55),10.45); balustrade(a,(6.2,-8.55),(17.5,-8.55),10.45)
    for i in range(-6,7):
        x=i*2.6
        if abs(x)<6.4 or abs(x)>17.4: continue
        arch_win(a,x,-8.55,2.9,1.2,2.2)
        a.box('M_Cornice',(x,-8.62,7.2),(1.7,0.12,2.6)); a.box('M_WindowLit' if rnd.random()<0.2 else 'M_Window',(x,-8.68,7.15),(1.15,0.1,2.15))
        a.box('M_Cornice',(x,-8.8,8.65),(2.0,0.45,0.18)); a.box('M_Cornice',(x,-8.85,5.85),(1.9,0.6,0.12)); a.box('M_Iron',(x,-9.12,6.15),(1.9,0.05,0.5))
    # portico
    a.box('M_Rosada',(0,-0.8,7.0),(12,19.2,12.8))
    a.box('M_Cornice',(0,-0.8,13.55),(12.6,19.8,0.6))
    a.add(pyr(12.2,19.4,1.2,0.8),'M_RoofGreen',(0,-0.8,14.45))
    a.box('M_RosadaDark',(0,-10.3,2.2),(12.02,0.4,2.6))
    a.box('M_Window',(0,-10.45,3.6),(4.0,0.3,5.2)); a.cy('M_Window',(0,-10.45,6.2),2.0,2.0,0.3,18,(90,0,0))
    a.cy('M_Cornice',(0,-10.5,6.2),2.35,2.35,0.2,18,(90,0,0))
    for x in (-4.6,-3.2,3.2,4.6):
        a.cy('M_Cornice',(x,-10.9,4.6),0.36,0.32,7.0,12); a.box('M_Cornice',(x,-10.9,1.0),(0.9,0.9,0.4)); a.box('M_Cornice',(x,-10.9,8.25),(0.85,0.85,0.3))
    a.box('M_Cornice',(0,-10.9,8.7),(11.2,1.4,0.5))
    balustrade(a,(-5.4,-11.4),(5.4,-11.4),8.95)
    for x in (-2.4,0,2.4):
        arch_win(a,x,-10.42,11.0,1.2,2.0)
    a.box('M_Rosada',(0,-4.5,15.0),(7.5,4,2.2)); a.box('M_Cornice',(0,-4.5,16.2),(8,4.5,0.3)); a.add(pyr(7.6,4.2,0.8,0.3),'M_RoofGreen',(0,-4.5,16.75))
    balustrade(a,(-5.9,-10.6),(5.9,-10.6),13.85)
    # pavilions
    for sx in (-1,1):
        x=sx*22
        a.box('M_Rosada',(x,0,7.0),(9,19.2,12.8))
        a.box('M_Cornice',(x,0,13.55),(9.6,19.8,0.6)); a.box('M_Cornice',(x,0,4.6),(9.3,19.5,0.35))
        for zz in (1.9,2.8,3.7): a.box('M_RosadaDark',(x,0,zz),(9.06,19.26,0.08))
        a.add(pyr(9.0,19.0,4.6,0.32),'M_RoofGreen',(x,0,16.15))
        a.box('M_RoofGreen',(x,0,18.8),(1.4,4.6,0.9)); a.box('M_Iron',(x,0,19.3),(1.5,4.7,0.1))
        for xx in (-2.7,0,2.7):
            a.box('M_Cornice',(x+xx,-9.62,2.6),(0.1,0.1,0.1))
            arch_win(a,x+xx,-9.62,2.9,1.2,2.2)
            for z in (7.2,11.0):
                a.box('M_Cornice',(x+xx,-9.68,z),(1.7,0.12,2.6)); a.box('M_WindowLit' if rnd.random()<0.2 else 'M_Window',(x+xx,-9.74,z-0.05),(1.15,0.1,2.15))
                a.box('M_Cornice',(x+xx,-9.86,z+1.45),(2.0,0.45,0.18))
        for xx in (-4.1,4.1): a.box('M_Cornice',(x+xx,-9.7,7.0),(0.6,0.3,12.4))
        for yy in (-6.5,-2.2,2.2,6.5):
            arch_win(a,x+sx*4.55,yy,2.9,1.2,2.2,side_axis=False) if False else None
            for z in (2.9,7.2,11.0):
                a.box('M_Cornice',(x+sx*4.56,yy,z),(0.12,1.7,2.6)); a.box('M_Window',(x+sx*4.62,yy,z-0.05),(0.1,1.15,2.15))
    for yy in (-6,-2,2,6):
        for z in (2.9,7.2):
            for sx in (-1,1): pass
    # flag
    a.cy('M_Cornice',(0,-0.8,19.2),0.1,0.06,6.6,8); a.sp('M_Gold',(0,-0.8,22.6),0.16,1)
    _wave(a,['M_Celeste','M_White','M_Celeste'],0.08,-0.8,22.4,3.2,1.8,5,10,sun=True)
    a.finish()

def _wave(a,stripes,x0,y0,ztop,length,height,nseg=5,amp=14,sun=False,phase=0.0):
    seg=length/nseg; x,y=x0,y0; sh=height/len(stripes)
    for i in range(nseg):
        ang=amp*math.sin(i*1.25+phase); zt=ztop-0.04*i
        cx=x+math.cos(R(ang))*seg/2; cy_=y+math.sin(R(ang))*seg/2
        for j,mn in enumerate(stripes): a.box(mn,(cx,cy_,zt-sh*(j+0.5)),(seg*1.05,0.035,sh),(0,0,ang))
        if sun and i==nseg//2: a.sp('M_SunYellow',(cx,cy_-0.03,zt-height/2),height*0.12,1,(1,0.35,1))
        x+=math.cos(R(ang))*seg; y+=math.sin(R(ang))*seg

def flag(name,kind):
    a=A(name); a.cy('M_Wood',(0,0,1.7),0.03,0.025,3.4,6); a.sp('M_Gold',(0,0,3.42),0.05,1)
    if kind=='arg': _wave(a,['M_Celeste','M_White','M_Celeste'],0.03,0,3.3,1.7,1.0,5,16,sun=True)
    else: _wave(a,['M_FlagRed'],0.03,0,3.3,1.7,1.0,5,16,phase=1.0)
    a.finish()
def banner():
    a=A("banner")
    for x in (-2.1,2.1): a.cy('M_Wood',(x,0,1.3),0.035,0.035,2.6,6)
    seg=4.2/6
    for i in range(6):
        x=-2.1+seg*(i+0.5); y=0.12*math.sin(i*1.4)
        a.box('M_Sign',(x,y,1.85),(seg*1.06,0.035,1.1),(0,0,8*math.cos(i*1.4)))
        a.box('M_FlagRed',(x,y-0.03,2.22),(seg*1.06,0.03,0.22),(0,0,8*math.cos(i*1.4)))
        if 0<i<5: a.box('M_Dark',(x,y-0.03,1.78),(seg*0.7,0.03,0.16),(0,0,8*math.cos(i*1.4))); a.box('M_Dark',(x,y-0.03,1.5),(seg*0.5,0.03,0.12),(0,0,8*math.cos(i*1.4)))
    a.finish()

def _limb(a,mat,shoulder,rx,ry,length,r1,r2,hand=True):
    mx=Matrix.Translation(shoulder)@Matrix.Rotation(R(ry),4,'Y')@Matrix.Rotation(R(rx),4,'X')
    a.put(cyl(r1,r2,length,6),mat,mx@Matrix.Translation((0,0,-length/2)))
    if hand:
        p=mx@Vector((0,0,-length-0.03)); a.sp('M_Skin',tuple(p),0.055,1)
    return mx
def _body(a,shirt='M_Shirt',pants='M_Pants',skin='M_Skin',hair='M_Hair'):
    for x in (-0.1,0.1):
        a.cy(pants,(x,0,0.46),0.07,0.088,0.86,7); a.box('M_Shoe',(x,-0.04,0.05),(0.13,0.27,0.1))
    a.cy(pants,(0,0,0.92),0.18,0.19,0.16,8,scale=(1,0.72,1))
    a.cy(shirt,(0,0,1.2),0.185,0.235,0.6,8,scale=(1,0.66,1))
    a.cy(skin,(0,0,1.52),0.055,0.055,0.12,6)
    a.sp(skin,(0,0,1.65),0.125,2,(0.92,1,1.06)); a.sp(hair,(0,0.025,1.7),0.132,2,(0.95,1.0,0.78))
def person(name,pose='down'):
    a=A(name); _body(a)
    L,Rs=(-0.255,0,1.44),(0.255,0,1.44)
    if pose=='down': _limb(a,'M_Shirt',L,0,10,0.6,0.055,0.045); _limb(a,'M_Shirt',Rs,0,-10,0.6,0.055,0.045)
    elif pose=='fist': _limb(a,'M_Shirt',L,0,10,0.6,0.055,0.045); _limb(a,'M_Shirt',Rs,-10,-165,0.6,0.055,0.045)
    elif pose=='sign':
        _limb(a,'M_Shirt',L,-8,160,0.6,0.055,0.045); _limb(a,'M_Shirt',Rs,-8,-160,0.6,0.055,0.045)
        a.cy('M_Wood',(0,-0.12,1.95),0.02,0.02,0.9,5)
        a.box('M_Sign',(0,-0.14,2.3),(0.95,0.04,0.6)); a.box('M_FlagRed',(0,-0.165,2.43),(0.75,0.02,0.12)); a.box('M_Dark',(0,-0.165,2.22),(0.6,0.02,0.09))
    elif pose=='drum':
        _limb(a,'M_Shirt',L,-55,12,0.55,0.055,0.045); _limb(a,'M_Shirt',Rs,-55,-12,0.55,0.055,0.045)
        a.cy('M_DrumWhite',(0,-0.42,1.08),0.3,0.3,0.34,14,(0,90,0))
        for xx in (-0.17,0.17): a.cy('M_FlagRed',(xx,-0.42,1.08),0.315,0.315,0.04,14,(0,90,0))
        a.box('M_Dark',(0,-0.2,1.3),(0.05,0.3,0.05),(30,0,0))
    a.finish()
def officer():
    a=A("police_officer"); _body(a,'M_PoliceNavy','M_PoliceNavy','M_Skin','M_HelmetBlack')
    a.box('M_HelmetBlack',(0,0,1.24),(0.46,0.34,0.5)); a.box('M_HelmetBlack',(0,0,0.95),(0.42,0.3,0.08))
    for x in (-0.1,0.1): a.box('M_HelmetBlack',(x,-0.02,0.2),(0.15,0.3,0.4))
    a.sp('M_HelmetBlack',(0,0.01,1.71),0.175,2,(1,1.05,0.92)); a.box('M_Glass',(0,-0.15,1.66),(0.3,0.06,0.16),(10,0,0))
    _limb(a,'M_PoliceNavy',(-0.255,0,1.44),-60,10,0.55,0.06,0.05); _limb(a,'M_PoliceNavy',(0.255,0,1.44),-30,-8,0.6,0.06,0.05)
    a.box('M_Shield',(-0.05,-0.42,1.05),(0.62,0.05,1.2)); a.box('M_White',(-0.05,-0.45,1.42),(0.5,0.02,0.11)); a.box('M_Metal',(-0.05,-0.43,1.66),(0.62,0.06,0.04))
    a.cy('M_HelmetBlack',(0.3,-0.25,1.1),0.025,0.025,0.6,5,(60,0,0))
    a.finish()

def tree(name,leaf1,leaf2,seed=1):
    rnd=random.Random(seed); a=A(name)
    a.cy('M_Bark',(0,0,1.4),0.27,0.18,2.8,8)
    a.cy('M_Bark',(0.45,0,3.1),0.12,0.07,1.6,6,(0,32,0)); a.cy('M_Bark',(-0.35,0.3,3.0),0.12,0.07,1.5,6,(-25,-30,0))
    blobs=[(0,0,4.3,2.0),(1.25,0.5,3.8,1.45),(-1.15,-0.4,3.75,1.5),(0.35,-1.15,4.0,1.4),(-0.45,1.05,4.15,1.4),(0.2,0.1,5.45,1.35),(1.0,-0.6,4.9,1.0)]
    for i,(x,y,z,r) in enumerate(blobs): a.sp(leaf1 if i%3!=1 else leaf2,(x,y,z),r*rnd.uniform(0.9,1.1),2,(1,1,0.86))
    a.finish()
def _frond(L=4.2,W=1.0,droop=2.3,n=6):
    b=bmesh.new(); prev=None
    for i in range(n+1):
        t=i/n; y=L*t; z=-droop*t*t+0.7*t; w=W*(0.25+0.75*math.sin(math.pi*min(1.0,0.2+t*0.95)))*(1-0.55*t)
        l=b.verts.new((-w/2,y,z-0.1*(1-t))); m=b.verts.new((0,y,z+0.07)); r=b.verts.new((w/2,y,z-0.1*(1-t)))
        if prev: b.faces.new((prev[0],prev[1],m,l)); b.faces.new((prev[1],prev[2],r,m))
        prev=(l,m,r)
    return b
def palm():
    a=A("palm"); rnd=random.Random(5); h=0; x=0
    for i in range(8):
        r=0.34-i*0.02; seg=0.95; x+=0.035*i
        a.cy('M_PalmTrunk' if i%2==0 else 'M_PalmTrunk2',(x,0,h+seg/2),r,r-0.035,seg,8); h+=seg
    top=(x,0,h); a.sp('M_PalmLeaf',top,0.42,1)
    for k in range(3): a.sp('M_Coconut',(x+0.25*math.cos(k*2.1),0.25*math.sin(k*2.1),h-0.3),0.16,1)
    for k in range(12):
        mx=Matrix.Translation(top)@Matrix.Rotation(R(k*30+rnd.uniform(-10,10)),4,'Z')@Matrix.Rotation(R(-8-rnd.uniform(0,22)),4,'X')
        a.put(_frond(rnd.uniform(3.6,4.5),rnd.uniform(0.9,1.15),rnd.uniform(1.8,2.6)),'M_PalmLeaf',mx)
    for k in range(4):
        mx=Matrix.Translation(top)@Matrix.Rotation(R(k*90+45),4,'Z')@Matrix.Rotation(R(35),4,'X')
        a.put(_frond(2.2,0.6,0.6),'M_Leaf2',mx)
    a.finish()
def lamp(name="lamp",double=False):
    a=A(name)
    a.cy('M_Iron',(0,0,0.15),0.3,0.3,0.3,8); a.cy('M_Iron',(0,0,0.6),0.22,0.13,0.6,8)
    a.cy('M_Iron',(0,0,2.55),0.085,0.065,3.6,8); a.cy('M_Iron',(0,0,1.0),0.13,0.13,0.14,8)
    heads=[(0,0)]
    if double:
        a.box('M_Iron',(0,0,4.3),(1.5,0.08,0.08)); a.sp('M_Iron',(0,0,4.45),0.1,1); heads=[(-0.72,0),(0.72,0)]
    for (hx,hy) in heads:
        a.cy('M_Iron',(hx,hy,4.4),0.16,0.2,0.12,6)
        a.cy('M_LampGlow',(hx,hy,4.76),0.19,0.29,0.6,6)
        a.cy('M_Iron',(hx,hy,5.12),0.36,0.08,0.28,6); a.sp('M_Iron',(hx,hy,5.3),0.06,1)
    a.finish()
def bench():
    a=A("bench")
    for i in range(4): a.box('M_Wood',(0,-0.21+i*0.14,0.46),(2.0,0.11,0.05))
    for i in range(3): a.box('M_Wood',(0,0.33+i*0.02,0.66+i*0.15),(2.0,0.05,0.11),(-14,0,0))
    for x in (-0.88,0.88):
        a.box('M_Iron',(x,0.0,0.22),(0.07,0.55,0.44)); a.box('M_Iron',(x,0.36,0.75),(0.07,0.06,0.65),(-14,0,0)); a.box('M_Iron',(x,-0.05,0.62),(0.07,0.5,0.05))
    a.finish()
def paver():
    a=A("paver"); a.cy('M_PathStone2',(0,0,0.02),0.75,0.75,0.04,20); a.cy('M_PlazaPaving',(0,0,0.03),0.5,0.5,0.04,20); a.finish()
def flower_row(name,col):
    a=A(name); rnd=random.Random(hash(name)%1000)
    a.box('M_Hedge',(0,0,0.18),(1.25,0.6,0.36))
    for i in range(7): a.sp(col,(rnd.uniform(-0.55,0.55),rnd.uniform(-0.22,0.22),0.4),rnd.uniform(0.13,0.2),1)
    a.finish()
def _car(a,body,roof,glass='M_Glass'):
    a.box(body,(0,0,0.62),(1.78,4.4,0.56)); a.box(body,(0,-1.55,0.93),(1.74,1.2,0.08),(-6,0,0))
    a.add(pyr(1.6,2.25,0.55,0.78),glass,(0,0.2,1.17))
    a.box(roof,(0,0.22,1.47),(1.28,1.72,0.07))
    a.box('M_Metal',(0,-2.22,0.45),(1.8,0.1,0.18)); a.box('M_Metal',(0,2.22,0.45),(1.8,0.1,0.18))
    a.box('M_LampGlow',(-0.6,-2.21,0.72),(0.32,0.05,0.14)); a.box('M_LampGlow',(0.6,-2.21,0.72),(0.32,0.05,0.14))
    a.box('M_LightRed',(-0.65,2.21,0.72),(0.3,0.05,0.14)); a.box('M_LightRed',(0.65,2.21,0.72),(0.3,0.05,0.14))
    for y in (-1.4,1.4):
        for x in (-0.84,0.84): a.cy('M_Tire',(x,y,0.36),0.36,0.36,0.24,12,(0,90,0)); a.cy('M_Metal',(x*1.08,y,0.36),0.18,0.18,0.04,10,(0,90,0))
def taxi():
    a=A("taxi"); _car(a,'M_TaxiBlack','M_TaxiYellow')
    for sx in (-1,1): a.box('M_TaxiYellow',(sx*0.79,0.2,1.17),(0.06,2.0,0.5),(0,sx*-8,0))
    a.box('M_TaxiYellow',(0,-0.95,1.17),(1.5,0.06,0.5),(-35,0,0)); a.box('M_TaxiYellow',(0,1.33,1.17),(1.5,0.06,0.5),(35,0,0))
    a.box('M_White',(0,0.1,1.6),(0.55,0.16,0.22))
    a.finish()
def patrol():
    a=A("patrol_car"); _car(a,'M_PatrolWhite','M_PatrolWhite')
    a.box('M_PoliceBlue',(0,0,0.66),(1.8,4.42,0.2))
    a.box('M_LightRed',(-0.35,0.15,1.58),(0.6,0.3,0.14)); a.box('M_LightBlue',(0.35,0.15,1.58),(0.6,0.3,0.14))
    a.finish()
def traffic_light():
    a=A("traffic_light")
    a.cy('M_Iron',(0,0,1.9),0.08,0.07,3.8,8); a.box('M_Iron',(0,-1.3,3.75),(0.08,2.6,0.08))
    for (y,z) in [(0,2.6),(-2.4,3.4)]:
        a.box('M_Dark',(0,y,z),(0.32,0.3,0.95))
        a.cy('M_TrafficRed',(0,y-0.16,z+0.28),0.1,0.1,0.04,10,(90,0,0)); a.cy('M_TrafficOff',(0,y-0.16,z),0.1,0.1,0.04,10,(90,0,0)); a.cy('M_TrafficOff',(0,y-0.16,z-0.28),0.1,0.1,0.04,10,(90,0,0))
    a.finish()
def monument():
    a=A("monument")
    a.cy('M_Marble',(0,0,0.15),4.6,4.6,0.3,32); a.box('M_Marble',(0,0,0.5),(7,7,0.4)); a.box('M_Marble',(0,0,0.9),(5.8,5.8,0.4)); a.box('M_Marble',(0,0,1.3),(4.6,4.6,0.4))
    a.box('M_Marble',(0,0,2.6),(3.4,3.4,2.2)); a.box('M_Cornice',(0,0,3.8),(3.9,3.9,0.3)); a.box('M_Cornice',(0,0,1.6),(3.7,3.7,0.2))
    for k in range(4):
        t=R(k*90); a.box('M_Gold',(1.72*math.cos(t),1.72*math.sin(t),2.6),(0.9 if k%2 else 0.06,0.06 if k%2 else 0.9,0.9))
    a.add(pyr(2.3,2.3,8.0,0.62),'M_Marble',(0,0,7.95)); a.add(pyr(1.6,1.6,0.6,0.3),'M_Marble',(0,0,12.25))
    a.cy('M_Marble',(0,0,12.9),0.42,0.42,0.5,10)
    a.cy('M_Marble',(0,0,13.6),0.24,0.32,1.1,8); a.sp('M_Marble',(0,0,14.35),0.2,2)
    _limb(a,'M_Marble',(0.25,0,14.05),0,-150,0.6,0.06,0.05,hand=False); a.cy('M_Marble',(-0.2,0.1,13.7),0.05,0.05,1.5,6,(0,-10,0))
    a.finish()
