# buildings_kit.py — kit de edificios porteños con estilos distintos (italianizante, art nouveau, art déco)
# se suman a los haussmannianos de gen_assets.building para que ninguna cuadra se repita
import math, random, bmesh
import gen_assets as GA
from gen_assets import A, cyl, sph, pyr, R, face_box, face_cy, face_put, tri_prism

def _shops(a,h,rnd,gf):
    awn=rnd.choice(['M_AwningRed','M_AwningGreen','M_AwningRed','M_AwningBlue'])
    for ang in (0,90,180,270):
        for xl in (-3.6,3.6):
            face_box(a,'M_Iron',ang,h,xl,1.75,(3.2,0.1,2.9),0.0)
            face_box(a,'M_ShopGlow',ang,h,xl,1.7,(2.9,0.1,2.65),0.09)
            face_box(a,awn,ang,h,xl,3.35,(3.4,1.5,0.08),0.72)
            face_box(a,'M_White',ang,h,xl,3.07,(3.4,0.05,0.18),1.42)
            face_box(a,'M_ShopSign',ang,h,xl,gf-0.35,(2.4,0.08,0.42),0.05)
        face_box(a,'M_Dark',ang,h,0,1.45,(1.4,0.1,2.8),0.08)
        face_box(a,'M_Cornice',ang,h,0,3.05,(1.8,0.25,0.22),0.12)

def _base(a,w,gf,floors,fh,facade):
    a.box('M_FacadeDark',(0,0,gf/2),(w,w,gf))
    for zz in (1.0,2.0,3.0): a.box('M_Plinth',(0,0,zz),(w+0.04,w+0.04,0.07))
    a.box(facade,(0,0,gf+floors*fh/2),(w-0.3,w-0.3,floors*fh))
    a.box('M_Cornice',(0,0,gf),(w+0.3,w+0.3,0.4))
    for k in range(1,floors): a.box('M_Cornice',(0,0,gf+k*fh-0.2),(w-0.1,w-0.1,0.12))

def _arch(a,ang,half,xl,zc,wd,ht,lit):
    zt=zc+ht/2
    face_box(a,'M_Cornice',ang,half,xl,zc,(wd+0.36,0.12,ht),0.0); face_cy(a,'M_Cornice',ang,half,xl,zt,wd/2+0.18,0.12,0.0,16)
    face_box(a,'M_Dark',ang,half,xl,zc,(wd,0.1,ht),0.05); face_cy(a,'M_Dark',ang,half,xl,zt,wd/2,0.1,0.05,16)
    g='M_WindowLit' if lit else 'M_Window'
    face_box(a,g,ang,half,xl,zc,(wd-0.18,0.1,ht-0.05),0.09); face_cy(a,g,ang,half,xl,zt,wd/2-0.09,0.1,0.09,16)
    face_box(a,'M_Cornice',ang,half,xl,zt+wd/2+0.08,(0.3,0.25,0.36),0.12)

def _balustrade(a,ang,half,x0,x1,z,out):
    L=x1-x0; n=max(2,int(L/0.38))
    face_box(a,'M_Cornice',ang,half,(x0+x1)/2,z,(L,0.6,0.12),out)
    face_box(a,'M_Cornice',ang,half,(x0+x1)/2,z+0.78,(L,0.36,0.1),out)
    for i in range(n+1):
        c,s_=math.cos(R(ang)),math.sin(R(ang)); xl=x0+L*i/n; y=-(half+out)
        a.cy('M_Cornice',(xl*c-y*s_, xl*s_+y*c, z+0.4),0.07,0.1,0.66,6)

def _planter(a,x,y,z,rnd):
    a.box('M_Chimney',(x,y,z+0.25),(1.1,0.6,0.5))
    for k in range(3): a.sp(rnd.choice(['M_Leaf','M_Leaf2','M_Leaf3']),(x+(k-1)*0.35,y,z+0.7),0.38,2,(1,1,0.8))

def italianate(name,floors,seed,facade='M_Facade2',w=14.0):
    """italianizante porteño: ventanas en arco, balcones con balaustrada, terraza con parapeto, macetas y tanque"""
    rnd=random.Random(seed); a=A(name); h=w/2; fh=3.4; gf=4.2; top=gf+floors*fh
    _base(a,w,gf,floors,fh,facade)
    for cx in (-1,1):
        for cz in (-1,1):
            for k in range(floors*3):
                a.box('M_Cornice',(cx*(h-0.3),cz*(h-0.3),gf+0.55+k*fh/3),(0.85 if k%2 else 0.65,0.85 if k%2 else 0.65,fh/3-0.12))
    cols=[-4.4,-1.47,1.47,4.4]
    for ang in (0,90,180,270):
        for k in range(floors):
            z=gf+k*fh+1.5
            for xl in cols:
                _arch(a,ang,h-0.15,xl,z,1.15,1.9 if k<floors-1 else 1.5,rnd.random()<0.08)
                if k==0: _balustrade(a,ang,h-0.15,xl-0.95,xl+0.95,z-1.25,0.32)
                elif k%2==0: face_box(a,'M_Iron',ang,h-0.15,xl,z-0.85,(1.4,0.04,0.45),0.38); face_box(a,'M_Cornice',ang,h-0.15,xl,z-1.12,(1.5,0.45,0.08),0.2)
        nm=int((w-0.6)/0.6)
        for j in range(nm): face_box(a,'M_Cornice',ang,h-0.15,-(w-0.6)/2+0.3+j*(w-0.6)/nm,top-0.35,(0.16,0.5,0.32),0.14)
        _balustrade(a,ang,h,-h+0.4,h-0.4,top+0.35,-0.25)
    a.box('M_Cornice',(0,0,top),(w+0.6,w+0.6,0.4))
    a.box('M_RoofFlat',(0,0,top+0.25),(w-0.4,w-0.4,0.1))
    for (x,y) in [(-4.2,-4.6),(-1.5,-4.6),(1.4,-4.6),(4.3,-4.6),(-4.6,1.5),(4.6,-1.2)]:
        if rnd.random()<0.8: _planter(a,x,y,top+0.3,rnd)
    tx,ty=rnd.choice([(3.2,3.0),(-3.0,3.2)])
    for lx in (-0.7,0.7):
        for ly in (-0.7,0.7): a.box('M_Iron',(tx+lx,ty+ly,top+1.0),(0.12,0.12,1.5))
    a.cy('M_Metal',(tx,ty,top+2.5),1.1,1.1,1.6,16); a.cy('M_Metal',(tx,ty,top+3.35),1.15,0.2,0.3,16)
    a.box(facade,(-3.5,2.8,top+1.4),(3.2,3.6,2.4)); a.box('M_Cornice',(-3.5,2.8,top+2.65),(3.5,3.9,0.2))
    _shops(a,h,rnd,gf)
    return a.finish()

def nouveau(name,floors,seed,facade='M_Facade3',w=14.0):
    """art nouveau: bow-window central, ventanas de dintel curvo, mansarda con cúpulas de zinc en las esquinas"""
    rnd=random.Random(seed); a=A(name); h=w/2; fh=3.3; gf=4.2; top=gf+floors*fh
    _base(a,w,gf,floors,fh,facade)
    cols=[-5.2,-2.6,0.0,2.6,5.2]
    for ang in (0,90,180,270):
        bay=ang in (0,180)
        for k in range(floors):
            z=gf+k*fh+1.5
            for i_,xl in enumerate(cols):
                if bay and abs(xl)<3 and 0<k<floors-1: continue
                _arch(a,ang,h-0.15,xl,z,1.05,1.85,rnd.random()<0.08)
                face_box(a,'M_Iron',ang,h-0.15,xl,z-0.95,(1.3,0.04,0.45),0.4)
                face_cy(a,'M_Iron',ang,h-0.15,xl,z-1.15,0.66,0.6,0.42,12)
        if bay:
            z0=gf+fh; z1=gf+(floors-1)*fh
            face_box(a,facade,ang,h,0,(z0+z1)/2,(6.0,1.6,z1-z0),0.6)
            face_box(a,'M_Cornice',ang,h,0,z0-0.1,(6.4,2.0,0.3),0.4)
            face_box(a,'M_Cornice',ang,h,0,z1+0.1,(6.4,2.0,0.3),0.4)
            _balustrade(a,ang,h,-3.1,3.1,z1+0.25,0.75)
            for k in range(1,floors-1):
                z=gf+k*fh+1.5
                for xl in (-1.6,0.0,1.6): _arch(a,ang,h+1.45,xl,z,1.0,1.8,rnd.random()<0.1)
                face_box(a,'M_Cornice',ang,h,0,gf+k*fh-0.2,(6.2,1.8,0.14),0.5)
        nm=int((w-0.6)/0.62)
        for j in range(nm): face_box(a,'M_Cornice',ang,h-0.15,-(w-0.6)/2+0.31+j*(w-0.6)/nm,top-0.32,(0.16,0.42,0.3),0.12)
    a.box('M_Cornice',(0,0,top-0.1),(w+0.3,w+0.3,0.3)); a.box('M_Cornice',(0,0,top+0.2),(w+0.8,w+0.8,0.35))
    MH=3.8; MT=0.76
    a.add(pyr(w-0.2,w-0.2,MH,MT),'M_Slate',(0,0,top+0.37+MH/2)); a.add(pyr(MT*(w-0.2),MT*(w-0.2),1.0,0.25),'M_Roof',(0,0,top+0.37+MH+0.5))
    for ang in (0,90,180,270):
        for xl in (-2.6,2.6):
            face_box(a,'M_Cornice',ang,h-0.1,xl,top+1.9,(1.25,1.0,1.7),-0.55)
            face_box(a,'M_Window',ang,h-0.1,xl,top+1.85,(0.7,0.1,1.15),0.0)
            face_cy(a,'M_Slate',ang,h-0.1,xl,top+2.85,0.66,1.0,-0.5,14)
    for (cx,cy_) in ((-h+1.6,-h+1.6),(h-1.6,-h+1.6)):
        a.cy(facade,(cx,cy_,top+1.6),1.6,1.6,3.0,16); a.cy('M_Cornice',(cx,cy_,top+3.2),1.75,1.75,0.22,16)
        for kk in range(6):
            t=R(kk*60); a.box('M_Window',(cx+math.cos(t)*1.62,cy_+math.sin(t)*1.62,top+1.7),(0.5,0.1,1.3),(0,0,kk*60+90))
        a.sp('M_Zinc',(cx,cy_,top+3.3),1.65,2,(1,1,1.35)); a.cy('M_Iron',(cx,cy_,top+5.9),0.06,0.02,1.4,6); a.sp('M_Gold',(cx,cy_,top+5.4),0.14,1)
    _shops(a,h,rnd,gf)
    return a.finish()

def deco(name,floors,seed,facade='M_Facade3',w=14.0):
    """art déco: pilastras verticales continuas, escalonamiento en los últimos pisos, coronamiento con mástil"""
    rnd=random.Random(seed); a=A(name); h=w/2; fh=3.2; gf=4.4; low=floors-2; top=gf+low*fh
    _base(a,w,gf,low,fh,facade)
    cols=[-5.0,-2.5,0.0,2.5,5.0]
    for ang in (0,90,180,270):
        for k in range(low):
            z=gf+k*fh+1.55
            for xl in cols:
                face_box(a,'M_Dark',ang,h-0.15,xl,z,(1.5,0.1,2.3),0.04)
                face_box(a,'M_WindowLit' if rnd.random()<0.08 else 'M_Window',ang,h-0.15,xl,z,(1.32,0.1,2.15),0.08)
                for m in (-0.22,0.22): face_box(a,'M_Dark',ang,h-0.15,xl+m,z,(0.05,0.1,2.15),0.11)
                face_box(a,'M_Cornice',ang,h-0.15,xl,z-1.3,(1.6,0.12,0.3),0.06)
        for xl in (-6.25,-3.75,-1.25,1.25,3.75,6.25):
            face_box(a,'M_Cornice',ang,h-0.15,xl,gf+low*fh/2+0.4,(0.5,0.32,low*fh+0.8),0.0)
        face_box(a,'M_Cornice',ang,h-0.15,0,top+0.2,(w,0.5,0.5),0.0)
    a.box('M_Cornice',(0,0,top+0.45),(w+0.2,w+0.2,0.1))
    w2=w-3.6; h2=w2/2
    a.box(facade,(0,0,top+fh),(w2,w2,2*fh))
    for ang in (0,90,180,270):
        for k in range(2):
            z=top+k*fh+1.55
            for xl in (-3.6,-1.2,1.2,3.6):
                face_box(a,'M_Dark',ang,h2,xl,z,(1.4,0.1,2.2),0.03); face_box(a,'M_Window',ang,h2,xl,z,(1.22,0.1,2.05),0.07)
        _balustrade(a,ang,h,-h+0.4,h-0.4,top+0.5,-0.25)
        for xl in (-2.4,0.0,2.4): face_box(a,'M_Cornice',ang,h2,xl,top+fh,(0.42,0.3,2*fh),0.0)
    t2=top+2*fh
    a.box('M_Cornice',(0,0,t2+0.15),(w2+0.4,w2+0.4,0.3))
    a.box(facade,(0,0,t2+1.2),(w2-3.4,w2-3.4,1.8)); a.box('M_Cornice',(0,0,t2+2.15),(w2-3.0,w2-3.0,0.2))
    a.box(facade,(0,0,t2+2.8),(2.6,2.6,1.2)); a.box('M_Cornice',(0,0,t2+3.45),(2.9,2.9,0.15))
    a.cy('M_Iron',(0,0,t2+5.0),0.06,0.04,3.0,6)
    _shops(a,h,rnd,gf)
    return a.finish()


def rotunda(name,floors=7,seed=7,r=7.2,facade='M_Facade3'):
    """torre de esquina circular: cuerpo cilíndrico con balcones corridos, tambor con ventanas y cúpula de zinc con linterna"""
    rnd=random.Random(seed); a=A(name); fh=3.3; gf=4.4; top=gf+floors*fh; N=18
    def ring_pos(rad,t): return (rad*math.cos(t),rad*math.sin(t))
    a.cy('M_FacadeDark',(0,0,gf/2),r+0.1,r+0.1,gf,48)
    for zz in (1.0,2.0,3.0): a.cy('M_Plinth',(0,0,zz),r+0.14,r+0.14,0.07,48)
    a.cy(facade,(0,0,gf+floors*fh/2),r-0.1,r-0.1,floors*fh,48)
    a.cy('M_Cornice',(0,0,gf),r+0.35,r+0.35,0.4,48)
    for k in range(1,floors): a.cy('M_Cornice',(0,0,gf+k*fh-0.2),r+0.05,r+0.05,0.13,48)
    for i in range(N):
        t=2*math.pi*(i+0.5)/N; deg=math.degrees(t)+90
        for k in range(floors):
            z=gf+k*fh+1.5; x,y=ring_pos(r-0.05,t)
            a.box('M_Cornice',(x,y,z),(1.4,0.16,2.4),(0,0,deg))
            x2,y2=ring_pos(r+0.02,t); a.box('M_Dark',(x2,y2,z-0.05),(1.0,0.12,2.1),(0,0,deg))
            x3,y3=ring_pos(r+0.07,t); a.box('M_WindowLit' if rnd.random()<0.08 else 'M_Window',(x3,y3,z-0.05),(0.8,0.1,1.95),(0,0,deg))
            x4,y4=ring_pos(r+0.12,t); a.box('M_Cornice',(x4,y4,z+1.35),(1.6,0.3,0.16),(0,0,deg))
        # arcadas comerciales en planta baja
        x,y=ring_pos(r+0.12,t)
        a.box('M_Iron',(x,y,1.75),(1.9,0.1,2.9),(0,0,deg)); x5,y5=ring_pos(r+0.2,t); a.box('M_ShopGlow',(x5,y5,1.7),(1.65,0.08,2.65),(0,0,deg))
        if i%2==0:
            x6,y6=ring_pos(r+0.85,t); a.box(rnd.choice(['M_AwningRed','M_AwningRed','M_AwningGreen']),(x6,y6,3.35),(2.0,1.4,0.08),(18,0,deg))
    # balcones corridos en piso 1 y último
    for zb in (gf+fh-0.15, gf+(floors-1)*fh-0.15):
        a.cy('M_Cornice',(0,0,zb),r+0.85,r+0.85,0.16,48)
        for i in range(48):
            t=2*math.pi*i/48; x,y=ring_pos(r+0.8,t)
            a.box('M_Iron',(x,y,zb+0.5),(0.95,0.05,0.06),(0,0,math.degrees(t)+90))
            a.cy('M_Iron',(x,y,zb+0.3),0.025,0.025,0.5,4)
    # coronamiento: cornisa con ménsulas, tambor, cúpula de zinc, linterna y aguja
    a.cy('M_Cornice',(0,0,top-0.1),r+0.25,r+0.25,0.3,48); a.cy('M_Cornice',(0,0,top+0.25),r+0.7,r+0.7,0.4,48)
    for i in range(40):
        t=2*math.pi*i/40; x,y=ring_pos(r+0.05,t); a.box('M_Cornice',(x,y,top-0.4),(0.16,0.45,0.3),(0,0,math.degrees(t)+90))
    rd=r*0.72; a.cy(facade,(0,0,top+1.7),rd,rd,2.9,40); a.cy('M_Cornice',(0,0,top+3.25),rd+0.25,rd+0.25,0.25,40)
    for i in range(12):
        t=2*math.pi*(i+0.5)/12; x,y=ring_pos(rd+0.03,t); a.box('M_Window',(x,y,top+1.7),(0.75,0.1,1.7),(0,0,math.degrees(t)+90))
        x,y=ring_pos(rd+0.15,t+math.pi/12); a.box('M_Cornice',(x,y,top+1.7),(0.35,0.3,2.7),(0,0,math.degrees(t)+105))
    a.sp('M_Zinc',(0,0,top+3.35),rd+0.1,3,(1,1,1.05))
    for i in range(12):
        t=2*math.pi*i/12; x,y=ring_pos(rd*0.62,t); a.box('M_Cornice',(x,y,top+3.35+rd*0.78),(0.14,0.3,rd*0.9),(math.degrees(0),-42,math.degrees(t)))
    zl=top+3.35+rd*1.05
    a.cy('M_Cornice',(0,0,zl),1.3,1.3,0.2,16); a.cy(facade,(0,0,zl+1.0),1.0,1.0,1.8,16)
    for i in range(8):
        t=2*math.pi*i/8; x,y=ring_pos(1.02,t); a.box('M_Window',(x,y,zl+1.0),(0.45,0.08,1.2),(0,0,math.degrees(t)+90))
    a.cy('M_Cornice',(0,0,zl+2.0),1.2,1.2,0.18,16); a.sp('M_Zinc',(0,0,zl+2.1),1.05,2,(1,1,0.9))
    a.cy('M_Gold',(0,0,zl+3.6),0.08,0.02,2.2,6); a.sp('M_Gold',(0,0,zl+3.2),0.2,1)
    return a.finish()

VARIANTS=[('building_it_a',italianate,4,21),('building_it_b',italianate,5,22),('building_it_c',italianate,3,23),
          ('building_nv_a',nouveau,5,31),('building_nv_b',nouveau,6,32),
          ('building_dc_a',deco,7,41),('building_dc_b',deco,6,42)]
def build_all():
    out={}
    for (n,f,fl,sd) in VARIANTS:
        o=f(n,fl,sd); out[n]=len(o.data.polygons)
    return out
