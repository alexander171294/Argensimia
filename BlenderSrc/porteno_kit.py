# porteno_kit.py - edificios eclecticos portenos (referencia: esquinas tipo Otto Wulff / Av. de Mayo)
# Basamento oscuro de piedra con atlantes, cuerpo de piedra clara con pilastras y bow-windows,
# mansarda de pizarra con lucarnas, torre redonda en la ochava y dos agujas con linternas de cobre.
import math, random
import gen_assets as GA
from gen_assets import A, pyr, R, face_box, face_cy

STONE='M_StoneDark'

def _atlas(a,ang,half,xl,z0,ht,out=0.25):
    # atlante estilizado: pedestal, piernas, torso, cabeza y brazos sosteniendo la cornisa
    face_box(a,STONE,ang,half,xl,z0+0.35,(0.9,0.7,0.7),out)
    face_box(a,STONE,ang,half,xl,z0+0.7+ht*0.2,(0.55,0.5,ht*0.4),out+0.05)
    face_box(a,STONE,ang,half,xl,z0+0.7+ht*0.55,(0.75,0.6,ht*0.32),out+0.1)
    face_cy(a,STONE,ang,half,xl,z0+0.7+ht*0.78,0.24,0.45,out+0.12,10)
    for s in (-1,1): face_box(a,STONE,ang,half,xl+s*0.42,z0+0.7+ht*0.82,(0.2,0.35,ht*0.35),out+0.08)
    face_box(a,STONE,ang,half,xl,z0+0.7+ht+0.1,(1.1,0.75,0.3),out)

def _arch_win(a,ang,half,xl,zc,wd,ht,glass='M_Window',frame='M_Cornice',out=0.0):
    zt=zc+ht/2
    face_box(a,frame,ang,half,xl,zc,(wd+0.34,0.12,ht),out); face_cy(a,frame,ang,half,xl,zt,wd/2+0.17,0.12,out,14)
    face_box(a,'M_Dark',ang,half,xl,zc,(wd,0.1,ht),out+0.05); face_cy(a,'M_Dark',ang,half,xl,zt,wd/2,0.1,out+0.05,14)
    face_box(a,glass,ang,half,xl,zc,(wd-0.16,0.1,ht-0.05),out+0.09); face_cy(a,glass,ang,half,xl,zt,wd/2-0.08,0.1,out+0.09,14)

def _rect_win(a,ang,half,xl,zc,wd,ht,out=0.0,ped=False):
    face_box(a,'M_Cornice',ang,half,xl,zc,(wd+0.36,0.12,ht+0.3),out)
    face_box(a,'M_Dark',ang,half,xl,zc,(wd,0.1,ht),out+0.05)
    face_box(a,'M_Window',ang,half,xl,zc,(wd-0.16,0.1,ht-0.1),out+0.09)
    face_box(a,'M_Cornice',ang,half,xl,zc+ht/2+0.3,(wd+0.6,0.3,0.18),out+0.05)
    if ped: face_box(a,'M_Cornice',ang,half,xl,zc+ht/2+0.62,(wd*0.5,0.22,0.42),out+0.05)

def _balcony(a,ang,half,xl,z,wd,dep=0.55):
    face_box(a,'M_Cornice',ang,half,xl,z,(wd,dep*2,0.16),dep*0.3)
    # baranda de hierro calada: pasamanos + barrotes (no un panel lleno)
    face_box(a,'M_Iron',ang,half,xl,z+0.9,(wd,0.05,0.06),dep*1.25)
    face_box(a,'M_Iron',ang,half,xl,z+0.2,(wd,0.05,0.05),dep*1.25)
    n=6
    for i in range(n+1): face_box(a,'M_Iron',ang,half,xl-wd/2+wd*i/n,z+0.55,(0.035,0.035,0.7),dep*1.25)

def _body(a,w,floors,gf,f1,fh,facade,cols,rnd,corner_skip=None,bays=True):
    h=w/2; zb=gf+f1; top=zb+floors*fh
    a.box(STONE,(0,0,(gf+f1)/2),(w,w,gf+f1))
    for k in range(int(gf/0.6)): a.box(STONE,(0,0,0.3+k*0.6),(w+0.08,w+0.08,0.06))
    a.box(STONE,(0,0,gf),(w+0.3,w+0.3,0.35))
    a.box(facade,(0,0,zb+floors*fh/2),(w-0.3,w-0.3,floors*fh))
    a.box(STONE,(0,0,zb),(w+0.6,w+0.6,0.45)); a.box(STONE,(0,0,zb+0.32),(w+0.3,w+0.3,0.2))
    for k in range(1,floors): a.box('M_Cornice',(0,0,zb+k*fh-0.15),(w-0.1,w-0.1,0.12))
    a.box('M_Cornice',(0,0,top-0.15),(w+0.35,w+0.35,0.35)); a.box('M_Cornice',(0,0,top+0.2),(w+0.9,w+0.9,0.35))
    sk=lambda ang,xl: bool(corner_skip and corner_skip(ang,xl))
    mid=cols[len(cols)//2] if len(cols)%2==1 else None
    awn=rnd.choice(['M_AwningRed','M_AwningGreen','M_AwningRed','M_AwningBlue'])
    for ang in (0,90,180,270):
        for xl in cols:
            if sk(ang,xl): continue
            _arch_win(a,ang,h,xl,gf*0.45,2.2,gf*0.62,'M_ShopGlow',STONE,0.0)
            face_box(a,awn,ang,h,xl,gf*0.8,(2.5,1.2,0.08),0.55)
            _arch_win(a,ang,h,xl,gf+f1*0.45,1.3,f1*0.55,'M_Window',STONE,0.02)
            for k in range(floors):
                z=zb+k*fh+1.55
                if bays and xl==mid and 0<k<floors-1: continue
                if k==floors-1: _arch_win(a,ang,h-0.15,xl,z,1.15,1.7)
                else:
                    _rect_win(a,ang,h-0.15,xl,z,1.15,1.95,0.0,ped=(k==0))
                    if k in (0,2) or rnd.random()<0.25: _balcony(a,ang,h-0.15,xl,z-1.2,1.6)
        xs=sorted(cols)
        for j in range(len(xs)-1):
            if sk(ang,xs[j]) or sk(ang,xs[j+1]): continue
            xm=(xs[j]+xs[j+1])/2
            _atlas(a,ang,h,xm,gf+0.2,f1-1.2)
            face_box(a,'M_Cornice',ang,h-0.15,xm,zb+floors*fh/2,(0.42,0.3,floors*fh),0.0)
        nm=int((w-0.6)/0.7)
        for j in range(nm): face_box(a,'M_Cornice',ang,h-0.15,-(w-0.6)/2+0.35+j*(w-0.6)/nm,top-0.45,(0.16,0.5,0.3),0.14)
        if bays and mid is not None and not sk(ang,mid):
            z0=zb+fh; z1=zb+(floors-1)*fh
            face_box(a,facade,ang,h,mid,(z0+z1)/2,(2.6,1.6,z1-z0),0.55)
            face_box(a,'M_Cornice',ang,h,mid,z0-0.1,(3.0,2.0,0.28),0.4)
            face_box(a,'M_Cornice',ang,h,mid,z1+0.12,(3.0,2.0,0.28),0.4)
            for k in range(1,floors-1):
                z=zb+k*fh+1.55
                _arch_win(a,ang,h+1.35,mid,z,1.25,1.8,'M_Window','M_Cornice')
                face_box(a,'M_Cornice',ang,h,mid,zb+k*fh-0.15,(2.8,1.75,0.12),0.5)
    return top

def _mansard(a,w,top,MH,dormers,skip=None):
    h=w/2
    a.add(pyr(w-0.3,w-0.3,MH,0.6),'M_Slate',(0,0,top+0.37+MH/2))
    for ang in (0,90,180,270):
        for xl in dormers:
            if skip and skip(ang,xl): continue
            face_box(a,'M_Cornice',ang,h-0.2,xl,top+1.9,(1.3,1.1,1.8),-0.7)
            face_box(a,'M_Window',ang,h-0.2,xl,top+1.85,(0.7,0.1,1.15),-0.12)
            face_cy(a,'M_Cornice',ang,h-0.2,xl,top+2.95,0.68,1.1,-0.65,14)
            face_cy(a,'M_Window',ang,h-0.2,xl,top+3.9,0.28,0.2,-1.15,12)

def _spire(a,x,y,zb,facade,sz=3.4,H=7.5):
    a.box('M_Cornice',(x,y,zb+0.15),(sz+0.5,sz+0.5,0.3))
    a.box(facade,(x,y,zb+1.6),(sz,sz,2.6))
    for ang in (0,90,180,270):
        c,s_=math.cos(R(ang)),math.sin(R(ang)); yy=-(sz/2+0.02)
        a.box('M_Window',(x-yy*s_, y+yy*c, zb+1.7),(0.75,0.1,1.6),(0,0,ang))
    a.box('M_Cornice',(x,y,zb+3.0),(sz+0.4,sz+0.4,0.3))
    a.add(pyr(sz+0.1,sz+0.1,H,0.12),'M_Slate',(x,y,zb+3.15+H/2))
    zl=zb+3.15+H
    a.cy('M_RoofGreen',(x,y,zl+0.6),0.55,0.55,1.2,8)
    for i in range(8):
        t=2*math.pi*(i+0.5)/8; a.box('M_Dark',(x+math.cos(t)*0.56,y+math.sin(t)*0.56,zl+0.65),(0.22,0.06,0.7),(0,0,math.degrees(t)+90))
    a.cy('M_RoofGreen',(x,y,zl+1.6),0.62,0.05,1.0,8)
    a.cy('M_Gold',(x,y,zl+2.5),0.05,0.02,1.2,6); a.sp('M_Gold',(x,y,zl+2.2),0.12,1)

def otto(name='building_otto',floors=6,seed=11,w=18.0,facade='M_Facade3'):
    # esquina con ochava redonda en (+X,-Y) de Blender = frente-izquierda en Unity (el FBX invierte X), igual que building_corner
    rnd=random.Random(seed); a=A(name); h=w/2; gf=4.6; f1=4.0; fh=3.3
    cols=[-6.6,-3.6,-0.6,2.4,5.4]
    def skip(ang,xl): return (ang==0 and xl>h-4.6) or (ang==90 and xl<-h+4.6)
    top=_body(a,w,floors,gf,f1,fh,facade,cols,rnd,skip,bays=True)
    Rr=3.9; cx,cy=h-2.6,-h+2.6; zb=gf+f1
    a.cy(STONE,(cx,cy,(gf+f1)/2),Rr+0.1,Rr+0.1,gf+f1,40)
    a.cy(STONE,(cx,cy,zb),Rr+0.5,Rr+0.5,0.45,40)
    a.cy(facade,(cx,cy,zb+floors*fh/2),Rr-0.05,Rr-0.05,floors*fh,40)
    for k in range(1,floors): a.cy('M_Cornice',(cx,cy,zb+k*fh-0.15),Rr+0.12,Rr+0.12,0.14,40)
    for i in range(7):
        t=math.radians(270+i*15); deg=math.degrees(t)+90
        for k in range(floors):
            z=zb+k*fh+1.55
            a.box('M_Cornice',(cx+math.cos(t)*(Rr+0.02),cy+math.sin(t)*(Rr+0.02),z),(1.35,0.14,2.3),(0,0,deg))
            a.box('M_Window',(cx+math.cos(t)*(Rr+0.08),cy+math.sin(t)*(Rr+0.08),z-0.05),(0.85,0.1,1.95),(0,0,deg))
        x4,y4=cx+math.cos(t)*(Rr+0.12),cy+math.sin(t)*(Rr+0.12)
        if i%2==0: a.box('M_ShopGlow',(x4,y4,1.9),(1.6,0.1,2.8),(0,0,deg))
        else: a.box(STONE,(x4,y4,gf+f1*0.5),(0.6,0.5,f1*0.8),(0,0,deg))
    for zbal in (zb+fh-0.15, zb+3*fh-0.15, zb+(floors-1)*fh-0.15):
        a.cy('M_Cornice',(cx,cy,zbal),Rr+0.8,Rr+0.8,0.16,40)
        for i in range(24):
            t=math.radians(240+i*6.5); x,y=cx+math.cos(t)*(Rr+0.75),cy+math.sin(t)*(Rr+0.75)
            a.box('M_Iron',(x,y,zbal+0.9),(1.0,0.05,0.06),(0,0,math.degrees(t)+90))
            a.box('M_Iron',(x,y,zbal+0.5),(0.04,0.04,0.8),(0,0,math.degrees(t)+90))
    a.cy('M_Cornice',(cx,cy,top+0.2),Rr+0.6,Rr+0.6,0.35,40)
    a.cy(facade,(cx,cy,top+1.6),Rr-0.6,Rr-0.6,2.5,32)
    for i in range(10):
        t=2*math.pi*i/10; a.box('M_Window',(cx+math.cos(t)*(Rr-0.55),cy+math.sin(t)*(Rr-0.55),top+1.6),(0.7,0.1,1.5),(0,0,math.degrees(t)+90))
    a.cy('M_Cornice',(cx,cy,top+2.9),Rr-0.35,Rr-0.35,0.25,32)
    a.sp('M_Slate',(cx,cy,top+3.0),Rr-0.5,3,(1,1,1.45))
    zl=top+3.0+(Rr-0.5)*1.4
    a.cy('M_RoofGreen',(cx,cy,zl+0.5),0.8,0.8,1.2,8); a.cy('M_RoofGreen',(cx,cy,zl+1.5),0.85,0.05,1.0,8)
    a.cy('M_Gold',(cx,cy,zl+2.5),0.06,0.02,1.4,6)
    _mansard(a,w,top,4.2,[-3.6,-0.6,2.4,5.4],skip)
    _spire(a,h-2.4,-h+8.4,top+0.4,facade)
    _spire(a,h-8.4,-h+2.4,top+0.4,facade)
    return a.finish()

def porteno(name,floors,seed,w=14.0,facade='M_Facade3',spire=True):
    # medianera eclectica portena: mismo lenguaje que la esquina (basamento con atlantes, bow-window, mansarda)
    rnd=random.Random(seed); a=A(name); h=w/2; gf=4.4; f1=3.8; fh=3.3
    cols=[-4.8,-1.6,1.6,4.8] if seed%2==0 else [-5.0,-2.5,0.0,2.5,5.0]
    top=_body(a,w,floors,gf,f1,fh,facade,cols,rnd,None,bays=len(cols)%2==1)
    _mansard(a,w,top,3.8,[-3.2,0.0,3.2])
    if spire:
        x,y=0,-h+2.2
        a.box('M_Cornice',(x,y,top+0.4),(3.6,3.6,0.3))
        a.box(facade,(x,y,top+1.7),(3.0,3.0,2.4))
        a.box('M_Window',(x,y-1.52,top+1.8),(0.8,0.1,1.5))
        a.sp('M_Slate',(x,y,top+2.9),1.75,2,(1,1,1.5))
        a.cy('M_RoofGreen',(x,y,top+5.6),0.45,0.45,0.9,8); a.cy('M_Gold',(x,y,top+6.6),0.05,0.02,1.0,6)
    return a.finish()


class _S:
    # proxy de A que escala muebles al tamano de la gente del juego (personas x1.45): posiciones respecto de un pivote
    # y tamanos. sy/syl permiten no estirar a lo largo (la barra mantiene su largo marcado en escena).
    def __init__(s,a,px,py,z,S=1.45,sy=None,syl=None):
        s.a,s.px,s.py,s.z,s.S=a,px,py,z,S; s.sy=S if sy is None else sy; s.syl=S if syl is None else syl
    def P(s,l): return (s.px+(l[0]-s.px)*s.S, s.py+(l[1]-s.py)*s.sy, s.z+(l[2]-s.z)*s.S)
    def box(s,m,l,sz,rot=(0,0,0)): s.a.box(m,s.P(l),(sz[0]*s.S,sz[1]*s.syl,sz[2]*s.S),rot)
    def cy(s,m,l,r1,r2,h,seg=12,rot=(0,0,0)): s.a.cy(m,s.P(l),r1*s.S,r2*s.S,h*s.S,seg,rot)
    def sp(s,m,l,r,sub=1,scale=(1,1,1)): s.a.sp(m,s.P(l),r*s.S,sub,scale)

TERRACE_CAM=(-4.94,-13.43,221.0,15.0)   # x, y, rumbo (grados, plano XY de Blender) e inclinacion hacia abajo: apunta a la Casa Rosada

def terrace(name='building_terrace',w=14.0,ext=8.0,seed=61,facade='M_Facade3',floors=4):
    # esquina bajo la camara: edificio porteno con roof-top en voladizo sobre la calle hacia la plaza (Blender -Y = frente).
    # Solo lo marcado en escena: barra sobre el lado oeste terminando antes del fotografo, una mesa de madera con silla
    # del lado de la plaza y el tripode del camarografo apuntando hacia abajo a la Casa Rosada.
    rnd=random.Random(seed); a=A(name); h=w/2; gf=4.4; f1=3.8; fh=3.3
    top=_body(a,w,floors,gf,f1,fh,facade,[-4.8,-1.6,1.6,4.8],rnd,None,bays=False)
    z=top+0.38
    y0,y1=-h-ext,h
    yc=(y0+y1)/2; L=y1-y0
    a.box('M_Cornice',(0,-h-ext/2,z-0.35),(w+0.6,ext,0.5))
    a.box(STONE,(0,-h-ext/2,z-0.7),(w+0.2,ext-0.4,0.25))
    for x in (-h+0.6,-h/3,h/3,h-0.6):
        for k in range(3):
            d=ext*(0.75-k*0.22)
            a.box(STONE,(x,-h-d/2,z-1.0-k*0.9),(0.5,d,0.8))
    a.box('M_PathStone2',(0,yc,z-0.05),(w+0.4,L+0.2,0.12))
    def rail(x0,y0_,x1,y1_):
        dx,dy=x1-x0,y1_-y0_; Ln=math.hypot(dx,dy); ang=math.degrees(math.atan2(dy,dx))
        cx,cy_=(x0+x1)/2,(y0_+y1_)/2
        a.box('M_Iron',(cx,cy_,z+1.5),(Ln,0.09,0.09),(0,0,ang))
        a.box('M_Iron',(cx,cy_,z+0.2),(Ln,0.06,0.05),(0,0,ang))
        n=max(2,int(Ln/0.35))
        for i in range(n+1):
            t=i/n; a.box('M_Iron',(x0+dx*t,y0_+dy*t,z+0.85),(0.05,0.05,1.3))
    hw=w/2+0.1
    rail(-hw,y0+0.1,hw,y0+0.1); rail(-hw,y0+0.1,-hw,y1-0.1); rail(hw,y0+0.1,hw,y1-0.1); rail(-hw,y1-0.1,hw,y1-0.1)
    # barra: lado oeste, de y=-5.1 a -12.4 (termina justo antes del fotografo), con su decorado
    by0,by1=-12.4,-5.1; bl=by1-by0; bc=(by0+by1)/2
    real=a; a=_S(real,-7.0,bc,z,1.45,sy=1.0,syl=1.0)
    a.box('M_Chimney',(-6.25,bc,z+1.0),(0.45,bl,2.0))                   # contrabarra
    for lvl in (0.85,1.45):
        a.box('M_Wood',(-5.95,bc,z+lvl-0.05),(0.3,bl-0.2,0.05))
        for k in range(int((bl-0.4)/0.32)):
            a.cy(rnd.choice(['M_Gold','M_AwningGreen','M_White','M_AwningRed','M_Dark','M_Gold']),(-5.95,by0+0.3+k*0.32,z+lvl+0.15),0.06,0.06,rnd.uniform(0.25,0.4),6)
    a.box('M_Dark',(-4.65,bc,z+0.55),(0.75,bl,1.1))                      # mostrador
    a.box('M_Wood',(-4.6,bc,z+1.14),(1.0,bl+0.2,0.08))                  # tapa de madera
    for k in range(int(bl/0.6)): a.box('M_Wood',(-4.27,by0+0.3+k*0.6,z+0.55),(0.04,0.4,0.9))   # frente de tablas
    for k in range(5):
        y=by0+0.8+k*(bl-1.6)/4
        a.cy('M_Iron',(-3.65,y,z+0.38),0.035,0.035,0.75,6); a.cy('M_Wood',(-3.65,y,z+0.77),0.21,0.21,0.07,10)
    for y in (by0+1.2,bc,by1-1.2): a.cy('M_White',(-4.6,y,z+1.25),0.05,0.06,0.16,8)   # vasos/canillas
    # pergola de hierro sobre la barra con enredadera y guirnalda de luces
    px0,px1=-6.6,-3.2
    for x in (px0,px1):
        for y in (by0,by1): a.box('M_Iron',(x,y,z+1.4),(0.12,0.12,2.8))
    for y in (by0,by1): a.box('M_Iron',((px0+px1)/2,y,z+2.85),(px1-px0+0.3,0.12,0.14))
    for k in range(7):
        y=by0+bl*k/6; a.box('M_Iron',((px0+px1)/2,y,z+2.95),(px1-px0+0.5,0.07,0.08))
    for x in (px0,px1): a.box('M_Iron',(x,bc,z+2.95),(0.08,bl+0.4,0.1))
    for i in range(16):
        a.sp(rnd.choice(['M_Leaf','M_Leaf2','M_Leaf3']),(rnd.uniform(px0,px1),rnd.uniform(by0,by1),z+3.1+rnd.uniform(-0.1,0.2)),rnd.uniform(0.4,0.6),1,(1.3,1.3,0.45))
    for i in range(7): a.sp('M_FlowerPurple',(rnd.uniform(px0,px1),rnd.uniform(by0,by1),z+3.25),0.16,1)
    for k in range(4): a.sp('M_Leaf2',(px1+0.12,by1,z+0.4+k*0.6),0.3,1)
    n=16
    for i in range(n+1):
        t=i/n; y=by0+bl*t; zz=z+2.75-math.sin(t*math.pi)*0.35
        a.sp('M_LampGlow',(px1+0.05,y,zz),0.07,1)
        if i<n: a.box('M_Iron',(px1+0.05,y+bl/n/2,zz),(0.02,bl/n,0.02))
    # macetones en las puntas de la barra
    for y in (by1+0.8,):
        a.box('M_Chimney',(-6.2,y,z+0.3),(0.9,0.9,0.6))
        for k in range(3): a.sp(rnd.choice(['M_Leaf','M_Leaf2']),(-6.2+rnd.uniform(-0.2,0.2),y+rnd.uniform(-0.2,0.2),z+0.85+k*0.25),0.42,2,(1,1,0.9))
        a.sp('M_FlowerPink',(-6.2,y,z+1.45),0.22,1)
    a=real
    # mesa de madera (vista desde arriba) con una silla del lado de la plaza
    tx,ty=2.97,-8.84
    real=a; a=_S(real,tx,ty,z,1.45)
    a.box('M_Wood',(tx,ty,z+0.76),(1.4,1.4,0.07))
    for sx in (-1,1):
        for sy in (-1,1): a.box('M_Iron',(tx+sx*0.6,ty+sy*0.6,z+0.37),(0.06,0.06,0.74))
    cy0=ty-1.05
    a.box('M_Wood',(tx,cy0,z+0.46),(0.5,0.5,0.06))
    for sx in (-1,1):
        for sy in (-1,1): a.box('M_Iron',(tx+sx*0.21,cy0+sy*0.21,z+0.22),(0.04,0.04,0.45))
    a.box('M_Wood',(tx,cy0-0.24,z+0.75),(0.5,0.05,0.55))
    a=real
    # tripode con camara de TV apuntando hacia abajo, a la Casa Rosada
    cx,cy_,yaw,tilt=TERRACE_CAM
    real=a; a=_S(real,cx,cy_,z,1.45)
    for k in range(3):
        t=math.radians(yaw+60+k*120); a.box('M_Iron',(cx+math.cos(t)*0.25,cy_+math.sin(t)*0.25,z+0.72),(0.035,0.035,1.5),(math.sin(t)*10,-math.cos(t)*10,0))
    d=(math.cos(math.radians(yaw))*math.cos(math.radians(tilt)),math.sin(math.radians(yaw))*math.cos(math.radians(tilt)),-math.sin(math.radians(tilt)))
    zc=z+1.55
    a.box('M_Dark',(cx,cy_,zc),(0.65,0.36,0.38),(0,tilt,yaw))
    a.cy('M_Dark',(cx+d[0]*0.45,cy_+d[1]*0.45,zc+d[2]*0.45),0.13,0.15,0.3,10,(0,90+tilt,yaw))
    a.box('M_Gold',(cx,cy_,zc+0.24),(0.14,0.08,0.06),(0,tilt,yaw))
    a.box('M_Dark',(cx-d[0]*0.4,cy_-d[1]*0.4,zc+0.12),(0.12,0.28,0.2),(0,0,yaw))
    a=real
    return a.finish()

def medianera(name,depth,height,thick=0.14):
    # muro ciego de medianera a medida (sin escalar en Unity, para que la textura quede en metros)
    a=A(name)
    a.box('M_Facade3',(0,0,height/2),(thick,depth,height))
    a.box('M_Cornice',(0,0,height-0.12),(thick+0.06,depth+0.06,0.24))
    for k in range(1,int(height/3.2)+1): a.box('M_Facade3',(0,0,k*3.2),(thick+0.03,depth,0.08))   # lineas de entrepiso
    return a.finish()


def valla_l(name='valla_l',w=2.4,H=2.8,foot=1.3):
    # valla policial antimultitud en L (referencia: vallas de chapa y malla con pie del lado de los manifestantes).
    # Panel en el plano y=0, pie hacia Blender -Y (frente = manifestantes), puntales atras (+Y). Escala de la gente del juego.
    a=A(name); hw=w/2
    for x in (-hw+0.05,hw-0.05): a.box('M_Rust',(x,0,H/2),(0.1,0.1,H))
    for z in (0.08,H*0.5,H-0.05): a.box('M_Rust',(0,0,z),(w,0.1,0.1))
    a.box('M_SteelDark',(0,0,H*0.25+0.04),(w-0.12,0.04,H*0.5-0.1))          # chapa antideslizante abajo
    for i in range(1,4): a.box('M_Rust',(0,-0.03,H*0.5*i/4),(w-0.12,0.02,0.03))
    n=15
    for i in range(n+1): a.box('M_Iron',(-hw+0.1+(w-0.2)*i/n,0,H*0.75),(0.03,0.03,H*0.5-0.1))   # malla arriba
    for z in (H*0.62,H*0.87): a.box('M_Iron',(0,0,z),(w-0.12,0.03,0.03))
    # pie en L del lado de los manifestantes (se paran arriba y no pueden empujar la valla)
    a.box('M_SteelDark',(0,-foot/2,0.035),(w,foot,0.07))
    a.box('M_Rust',(0,-foot+0.03,0.07),(w,0.06,0.12))
    for x in (-hw+0.06,0,hw-0.06): a.box('M_Rust',(x,-foot/2,0.08),(0.06,foot,0.1))
    for x in (-hw+0.15,hw-0.15):
        for k in range(4): a.box('M_Rust',(x,-0.12-k*0.1,0.25-k*0.05),(0.06,0.1,0.4-k*0.1))   # cartela
    # puntales traseros del lado de la policia
    for x in (-hw+0.25,hw-0.25):
        a.box('M_Rust',(x,0.55,H*0.42),(0.08,0.08,math.hypot(1.1,H*0.84)),(math.degrees(math.atan2(1.1,H*0.84)),0,0))
        a.box('M_Rust',(x,1.05,0.04),(0.25,0.35,0.08))
    return a.finish()

VARIANTS=[('building_pt_a',6,51,True),('building_pt_b',5,52,False),('building_pt_c',7,53,True)]
def build_all():
    out={}
    o=otto(); out[o.name]=len(o.data.polygons)
    for (n,fl,sd,sp) in VARIANTS:
        o=porteno(n,fl,sd,spire=sp); out[n]=len(o.data.polygons)
    o=terrace(); out[o.name]=len(o.data.polygons)
    return out
