# Casa Rosada v2 — detailed stylized model + Blender preview renders
import bpy, bmesh, math, random
from mathutils import Matrix, Vector, Euler
import gen_assets as GA
from gen_assets import A, cube, cyl, sph, pyr, R, face_box, face_cy

P='M_CR_Wall'; PD='M_CR_Dark'; W='M_CR_Trim'; GR='M_CR_Roof'
H0=5.4; H1=10.2; H2=14.0

def face_add(a,tb,mat,ang,half,xl,z,out=0.0,pitch=0.0):
    c,s=math.cos(R(ang)),math.sin(R(ang)); y=-(half+out)
    a.add(tb,mat,(xl*c-y*s, xl*s+y*c, z),(pitch,0,ang))
def tri(w,h,d):
    b=bmesh.new()
    v=[b.verts.new(p) for p in [(-w/2,-d/2,0),(w/2,-d/2,0),(0,-d/2,h),(-w/2,d/2,0),(w/2,d/2,0),(0,d/2,h)]]
    b.faces.new((v[0],v[1],v[2])); b.faces.new((v[5],v[4],v[3]))
    b.faces.new((v[0],v[3],v[4],v[1])); b.faces.new((v[1],v[4],v[5],v[2])); b.faces.new((v[2],v[5],v[3],v[0]))
    bmesh.ops.recalc_face_normals(b,faces=list(b.faces)); return b

def halfdisc(r,hs,d,seg=14):
    b=bmesh.new(); fr=[];bk=[]
    for i in range(seg+1):
        t=math.pi*i/seg; x=-math.cos(t)*r; z=math.sin(t)*r*hs
        fr.append(b.verts.new((x,-d/2,z))); bk.append(b.verts.new((x,d/2,z)))
    b.faces.new(fr[::-1]); b.faces.new(bk)
    for i in range(seg): b.faces.new((fr[i],fr[i+1],bk[i+1],bk[i]))
    b.faces.new((fr[0],bk[0],bk[-1],fr[-1]))
    bmesh.ops.recalc_face_normals(b,faces=list(b.faces)); return b
def arch_window(a,ang,half,xl,zb,w,hr,lit=False):
    za=zb+hr
    face_box(a,W,ang,half,xl,zb+hr/2,(w+0.42,0.16,hr),0.0)
    face_cy(a,W,ang,half,xl,za,w/2+0.21,0.16,0.0,20)
    g='M_WindowLit' if lit else 'M_Window'
    face_box(a,g,ang,half,xl,zb+hr/2-0.02,(w,0.1,hr-0.04),0.1)
    face_cy(a,g,ang,half,xl,za,w/2,0.1,0.1,20)
    face_box(a,W,ang,half,xl,za+w/2+0.12,(0.32,0.3,0.42),0.16)
    face_box(a,W,ang,half,xl,zb-0.08,(w+0.5,0.34,0.12),0.14)
def rect_window(a,ang,half,xl,zc,w,h,lit=False,ped=None,balc=False):
    face_box(a,W,ang,half,xl,zc,(w+0.4,0.16,h+0.4),0.0)
    face_box(a,'M_WindowLit' if lit else 'M_Window',ang,half,xl,zc,(w,0.1,h),0.1)
    face_box(a,W,ang,half,xl,zc-h/2-0.25,(w+0.55,0.3,0.12),0.12)
    top=zc+h/2+0.2
    if ped=='tri':
        face_box(a,W,ang,half,xl,top+0.08,(w+0.8,0.4,0.16),0.18)
        face_add(a,tri(w+0.9,0.65,0.36),W,ang,half,xl,top+0.16,0.18)
    elif ped=='seg':
        face_box(a,W,ang,half,xl,top+0.08,(w+0.8,0.4,0.16),0.18)
        face_add(a,halfdisc((w+0.8)/2,0.45,0.36),W,ang,half,xl,top+0.16,0.18)
    elif ped=='cap':
        face_box(a,W,ang,half,xl,top+0.1,(w+0.7,0.42,0.2),0.18)
    if balc:
        zb=zc-h/2-0.3
        face_box(a,W,ang,half,xl,zb,(w+1.0,0.8,0.16),0.4)
        face_box(a,W,ang,half,xl,zb+0.72,(w+1.0,0.12,0.1),0.74)
        n=6
        for i in range(n+1): face_add(a,cyl(0.05,0.07,0.6,6),W,ang,half,xl-(w+0.9)/2+(w+0.9)*i/n,zb+0.38,0.74)
def balus(a,ang,half,x0,x1,z,out=0.25,urns=True,step=0.42,mid=True):
    L=x1-x0; n=max(2,int(L/step)); xm=(x0+x1)/2
    face_box(a,W,ang,half,xm,z+0.08,(L,0.44,0.16),out)
    face_box(a,W,ang,half,xm,z+0.95,(L,0.5,0.14),out)
    for i in range(n+1): face_add(a,cyl(0.08,0.115,0.7,6),W,ang,half,x0+L*i/n,z+0.51,out)
    posts=[x0,x1]+([x0+L*k/3 for k in (1,2)] if (mid and L>6) else [])
    for xl in posts:
        face_box(a,W,ang,half,xl,z+0.55,(0.5,0.52,1.1),out)
        if urns:
            face_box(a,P,ang,half,xl,z+1.4,(0.42,0.42,0.6),out)
            face_add(a,pyr(0.46,0.46,1.0,0.05),W,ang,half,xl,z+2.2,out); face_add(a,sph(0.09,1),'M_Gold',ang,half,xl,z+2.75,out)
def dentils(a,ang,half,x0,x1,z,out=0.1):
    n=int((x1-x0)/0.5)
    for i in range(n): face_box(a,W,ang,half,x0+0.25+i*(x1-x0)/n,z,(0.22,0.26,0.24),out)
def pilaster(a,ang,half,xl,z0,z1,w=0.55):
    face_box(a,W,ang,half,xl,(z0+z1)/2,(w,0.18,z1-z0),0.06)
    face_box(a,W,ang,half,xl,z1-0.15,(w+0.25,0.3,0.3),0.1); face_box(a,W,ang,half,xl,z0+0.15,(w+0.2,0.28,0.3),0.08)

def block(a,x0,x1,hy,top,mez=False):
    cx=(x0+x1)/2; wx=x1-x0; d=2*hy
    a.box(P,(cx,0,(0.6+H0)/2),(wx,d,H0-0.6))
    for zz in (1.45,2.3,3.15,4.0,4.85): a.box(PD,(cx,0,zz),(wx+0.05,d+0.05,0.09))
    a.box(W,(cx,0,H0+0.05),(wx+0.4,d+0.4,0.4))
    a.box(P,(cx,0,(H0+0.25+top)/2),(wx,d,top-H0-0.25))
    a.box(W,(cx,0,H1),(wx+0.2,d+0.2,0.22))
    if mez: a.box(W,(cx,0,H2),(wx+0.2,d+0.2,0.2))
    a.box(W,(cx,0,top-0.2),(wx+0.3,d+0.3,0.3)); a.box(W,(cx,0,top+0.2),(wx+1.0,d+1.0,0.42))
    return cx,wx,d

def bays_face(a,ang,half,centers,top,rnd,peds=('tri','seg'),mez=False,ground=True,balc=True):
    for i,xl in enumerate(centers):
        if ground: arch_window(a,ang,half,xl,1.7,1.3,1.9,lit=rnd.random()<0.05)
        rect_window(a,ang,half,xl,7.55,1.4,2.7,lit=rnd.random()<0.08,ped=peds[i%len(peds)],balc=balc)
        rect_window(a,ang,half,xl,12.0,1.2,1.9,lit=rnd.random()<0.12,ped='cap')
        if mez: rect_window(a,ang,half,xl,15.0,0.9,0.8,lit=False,ped=None)

def bell_roof(a,cx,cy_,bw,bd,z,tiers=((1.1,0.93),(1.0,0.88),(0.9,0.82),(0.8,0.74),(0.6,0.6))):
    for (hh,f) in tiers:
        a.add(pyr(bw,bd,hh,f),GR,(cx,cy_,z+hh/2)); bw*=f; bd*=f; z+=hh
    a.box(GR,(cx,cy_,z+0.05),(bw,bd,0.1))
    for sx in (-1,1):
        a.box('M_Iron',(cx,cy_+sx*bd/2,z+0.35),(bw,0.05,0.06)); a.box('M_Iron',(cx+sx*bw/2,cy_,z+0.35),(0.05,bd,0.06))
    return z

def casa_rosada_v2():
    a=A("casa_rosada"); rnd=random.Random(11)
    WG=7.4; PE=7+WG; PW=10.0          # alas de 2 vanos (antes 3) -> fachada más compacta, se escala más grande en el juego
    a.box(PD,(0,0,0.3),(2*(PE+PW)+1.5,23.5,0.6))
    # ---- wings ----
    for (x0,x1) in ((-PE,-7),(7,PE)):
        cx,wx,d=block(a,x0,x1,10,14.4)
        cs=[x0+wx*(i+0.5)/2 for i in range(2)]
        bays_face(a,0,10,cs,14.4,rnd); bays_face(a,180,10,[-c for c in cs],14.4,rnd,balc=False)
        for xb in (x0+wx/2,): pilaster(a,0,10,xb,H0+0.3,14.1); pilaster(a,180,10,-xb,H0+0.3,14.1)
        dentils(a,0,10,x0,x1,14.0,0.18); dentils(a,180,10,-x1,-x0,14.0,0.18)
        balus(a,0,10,x0+0.4,x1-0.4,14.45,0.1); balus(a,180,10,-x1+0.4,-x0-0.4,14.45,0.1,urns=False)
        a.add(pyr(wx-0.6,d-2.6,2.0,0.5),GR,(cx,0,14.45+1.0))
        a.box(GR,(cx,0,16.5),((wx-0.6)*0.5,(d-2.6)*0.5,0.1))
        a.box(W,(cx,0,16.75),((wx-0.6)*0.42,1.4,0.5)); a.add(pyr((wx-0.6)*0.42,1.4,0.6,0.08),'M_Glass',(cx,0,17.3))
        for ang in (0,180):
            for xl in ((cs[0]+cs[1])/2,):
                xx=xl if ang==0 else -xl
                face_box(a,W,ang,8.0,xx,15.5,(1.0,1.0,1.1),0); face_box(a,'M_Window',ang,8.0,xx,15.45,(0.55,0.1,0.7),0.47)
                face_add(a,pyr(1.2,1.1,0.55,0.1),GR,ang,8.0,xx,16.3,0)
    # ---- pavilions ----
    for sx in (-1,1):
        x0,x1=(-PE-PW,-PE) if sx<0 else (PE,PE+PW)
        cx,wx,d=block(a,x0,x1,11,16.4,mez=True)
        cs=[x0+wx*(i+0.5)/3 for i in range(3)]
        bays_face(a,0,11,cs,16.4,rnd,peds=('seg','tri','seg'),mez=True)
        bays_face(a,180,11,[-c for c in cs],16.4,rnd,mez=True,balc=False)
        ys=[-11+22*(i+0.5)/5 for i in range(5)]
        endang=270 if sx<0 else 90
        bays_face(a,endang,PE+PW,[(-y if sx<0 else y) for y in ys],16.4,rnd,peds=('tri','seg','tri','seg','tri'),mez=True)
        for (qx,qy) in ((x0,-11),(x1,-11),(x0,11),(x1,11)):
            a.box(W,(qx,qy,(H0+16.2)/2),(0.8,0.8,16.2-H0))
            for k in range(6): a.box(PD,(qx,qy,0.9+k*0.8),(0.95 if k%2 else 0.75,0.95 if k%2 else 0.75,0.55))
        dentils(a,0,11,x0,x1,16.0,0.18); dentils(a,180,11,-x1,-x0,16.0,0.18); dentils(a,endang,PE+PW,-11,11,16.0,0.18)
        z=bell_roof(a,cx,0,wx+0.6,d+0.6,16.62)
        # oculus dormers on the roof
        for (ang,half,xls) in ((0,10.65,[cx-2.7,cx+2.7]),(180,10.65,[-cx-2.7,-cx+2.7]),(endang,PE+PW-0.35,[-5,0,5])):
            for xl in xls:
                face_box(a,GR,ang,half-0.6,xl,17.7,(1.5,1.2,1.6),0)
                face_add(a,pyr(1.6,1.3,0.7,0.1),GR,ang,half-0.6,xl,18.85,0)
                face_cy(a,W,ang,half-0.6,xl,17.75,0.55,0.12,0.6,20); face_cy(a,'M_Window',ang,half-0.6,xl,17.75,0.4,0.12,0.7,20)
        # lantern
        a.cy(W,(cx,0,z+0.2),1.5,1.5,0.4,8)
        a.cy(W,(cx,0,z+1.1),1.1,1.1,1.4,8); a.cy('M_Window',(cx,0,z+1.1),1.13,1.13,0.8,8)
        a.cy(W,(cx,0,z+1.85),1.3,1.3,0.15,8)
        a.cy(GR,(cx,0,z+2.5),1.3,0.12,1.3,8); a.cy('M_Iron',(cx,0,z+3.6),0.05,0.02,1.2,5); a.sp('M_Gold',(cx,0,z+3.25),0.18,1)
    # ---- central block ----
    cx,wx,d=block(a,-7,7,11,17.8,mez=True)
    # grand portal
    face_box(a,W,0,11,0,3.1,(5.6,0.2,5.0),-0.04); face_cy(a,W,0,11,0,5.6,2.8,0.2,-0.04,24)
    face_box(a,'M_Dark',0,11,0,3.1,(4.4,0.3,5.0),0.12); face_cy(a,'M_Dark',0,11,0,5.6,2.2,0.3,0.12,32)
    face_box(a,W,0,11,0,8.0,(0.7,0.4,1.0),0.12)
    for xs in (-1,1):
        for x in (3.55*xs,4.75*xs):
            a.box(W,(x,-11.95,1.35),(1.0,1.0,1.5)); a.box(W,(x,-11.95,2.2),(1.15,1.15,0.2))
            a.cy(W,(x,-11.95,5.3),0.34,0.29,6.0,14); a.box(W,(x,-11.95,8.45),(0.9,0.9,0.3)); a.box(W,(x,-11.95,8.2),(0.7,0.7,0.25))
        a.box(W,(xs*4.15,-11.55,1.2),(2.6,1.4,1.2))
    a.box(W,(0,-11.7,8.95),(11.6,1.9,0.7)); a.box(W,(0,-11.7,9.4),(12.2,2.2,0.22))
    balus(a,0,12.6,-5.8,5.8,9.5,0.0); 
    for xs in (-1,1):
        c=cyl(0.08,0.115,0.7,6)
        for k in range(4): a.add(cyl(0.08,0.115,0.7,6),W,(xs*5.95,-11.0-k*0.45,10.0))
        a.box(W,(xs*5.95,-11.8,10.45),(0.44,1.8,0.14))
    for x in (-2.7,0,2.7): arch_window(a,0,11,x,10.6,1.5,2.6,lit=x==0)
    for x in (-4.1,-1.35,1.35,4.1): pilaster(a,0,11,x,10.0,14.6,0.45)
    for x in (-5.8,5.8):
        arch_window(a,0,11,x,1.7,1.0,1.9); rect_window(a,0,11,x,12.0,1.0,1.9,ped='cap')
    for x in (-3.2,0,3.2): rect_window(a,0,11,x,16.0,1.0,0.9)
    dentils(a,0,11,-7,7,17.6,0.18); dentils(a,180,11,-7,7,17.6,0.18)
    bays_face(a,180,11,[-4.2,0,4.2],17.8,rnd,mez=True,balc=False)
    # attic + pediment + coat of arms
    a.box(P,(0,-1.0,19.3),(12.4,19.2,2.6)); a.box(W,(0,-1.0,20.7),(12.9,19.7,0.3))
    for xs in (-1,1): a.box(W,(xs*6.0,-10.4,19.3),(0.7,0.4,2.6))
    a.add(tri(13.4,2.8,1.0),W,(0,-10.25,20.85)); a.add(tri(11.8,2.2,1.05),P,(0,-10.3,21.0))
    a.cy('M_Gold',(0,-10.72,19.35),1.35,1.35,0.12,20,(90,0,0),(1,1,1.0))
    a.sp('M_Celeste',(0,-10.82,19.35),1.0,2,(0.85,0.12,1.1))
    a.box('M_White',(0,-11.0,19.2),(1.5,0.06,0.75)); a.sp('M_SunYellow',(0,-11.05,20.15),0.32,1,(1,0.3,1))
    for xs in (-1,1): a.sp('M_Gold',(xs*6.4,-10.4,21.0),0.3,1); a.cy('M_Gold',(xs*6.4,-10.4,20.75),0.18,0.25,0.3,8)
    a.add(pyr(12.0,17.0,2.6,0.5),GR,(0,0.5,20.85+1.3))
    # flag
    a.cy(W,(0,0.5,26.2),0.11,0.06,8.0,8); a.sp('M_Gold',(0,0.5,30.3),0.18,1)
    GA._wave(a,['M_Celeste','M_White','M_Celeste'],0.1,0.5,30.0,4.2,2.4,6,12,sun=True)
    # entrance steps & lamps
    for k,(dy,wz) in enumerate(((3.0,7.2),(2.4,6.4),(1.8,5.6))): a.box(W,(0,-11.0-dy/2,0.1+k*0.2),(wz,dy,0.2))
    for xs in (-1,1):
        a.cy('M_Iron',(xs*6.6,-13.2,1.8),0.08,0.06,3.0,8); a.cy('M_Iron',(xs*6.6,-13.2,0.35),0.25,0.18,0.5,8)
        a.cy('M_LampGlow',(xs*6.6,-13.2,3.6),0.2,0.28,0.55,6); a.cy('M_Iron',(xs*6.6,-13.2,3.98),0.34,0.06,0.25,6)
    return a.finish()

def _emission():
    for ln in open(GA.ROOT+"BlenderSrc/palette.txt"):
        p=ln.split()
        if len(p)>=3 and float(p[2])>0 and p[0] in bpy.data.materials:
            m=bpy.data.materials[p[0]]; b=m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
            if b:
                h=p[1].lstrip('#'); c=[GA.lin(int(h[i:i+2],16)/255) for i in (0,2,4)]
                try: b.inputs["Emission Color"].default_value=(*c,1); b.inputs["Emission Strength"].default_value=float(p[2])*0.5
                except Exception: pass

def preview(obj,out_prefix):
    sc=bpy.context.scene
    for eng in ('BLENDER_EEVEE_NEXT','BLENDER_EEVEE'):
        try: sc.render.engine=eng; break
        except Exception: pass
    try: sc.view_settings.view_transform='Standard'
    except Exception: pass
    sc.render.resolution_x=1600; sc.render.resolution_y=900; sc.render.resolution_percentage=100
    w=sc.world or bpy.data.worlds.new("W"); sc.world=w
    try:
        bg=w.node_tree.nodes.get("Background"); bg.inputs[0].default_value=(0.95,0.72,0.55,1); bg.inputs[1].default_value=1.1
    except Exception: w.color=(0.85,0.62,0.45)
    gm=bpy.data.meshes.new("gp"); b=bmesh.new(); bmesh.ops.create_grid(b,x_segments=1,y_segments=1,size=70); b.to_mesh(gm)
    g=bpy.data.objects.new("ground_preview",gm); sc.collection.objects.link(g); gm.materials.append(GA.M('M_PlazaPaving'))
    ld=bpy.data.lights.new("sun",'SUN'); ld.energy=5.0; ld.color=(1.0,0.78,0.55)
    try: ld.angle=R(2)
    except Exception: pass
    lo=bpy.data.objects.new("sun",ld); sc.collection.objects.link(lo)
    lo.rotation_euler=Vector((0.6,0.7,-0.42)).to_track_quat('-Z','Y').to_euler()
    cd=bpy.data.cameras.new("cam"); cd.sensor_fit='VERTICAL'; cd.lens=12/math.tan(R(15))
    co=bpy.data.objects.new("cam",cd); sc.collection.objects.link(co); sc.camera=co
    shots={"game":(Vector((0.576,0.117,-0.809)),105,Vector((0,0,6))),
           "hero":(Vector((0.42,0.82,-0.38)),92,Vector((0,0,8))),
           "front":(Vector((0.05,1.0,-0.22)),95,Vector((0,0,10)))}
    out=[]
    for k,(dv,dist,tgt) in shots.items():
        dv=dv.normalized(); co.location=tgt-dv*dist; co.rotation_euler=dv.to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=out_prefix+k+".png"
        bpy.ops.render.render(write_still=True); out.append(sc.render.filepath)
    return out
