# heli_kit.py - helipuerto detras de la Casa Rosada y helicoptero presidencial (estilo Sikorsky S-76, blanco y azul).
# Frente = Blender -Y (= Unity +Z). Rotor principal aparte (heli_rotor) para que gire en el juego.
import math
import gen_assets as GA
from gen_assets import A, R

def helipad(name='helipad', r=9.5, H=0.35):
    a=A(name)
    a.cy('M_RoofFlat',(0,0,H/2),r,r,H,40)
    a.cy('M_Curb',(0,0,H/2-0.02),r+0.35,r+0.35,H-0.04,40)            # cordon de piedra
    z=H
    a.cy('M_RoadYellow',(0,0,z+0.01),r-0.6,r-0.6,0.02,40)              # anillo amarillo
    a.cy('M_RoofFlat',(0,0,z+0.012),r-1.05,r-1.05,0.022,40)
    a.cy('M_White',(0,0,z+0.014),6.2,6.2,0.024,40)                       # circulo blanco
    a.cy('M_RoofFlat',(0,0,z+0.016),5.7,5.7,0.026,40)
    for x in (-1.8,1.8): a.box('M_White',(x,0,z+0.03),(0.9,6.0,0.03))  # H
    a.box('M_White',(0,0,z+0.03),(2.7,0.9,0.03))
    for k in range(16):                                                   # balizas del borde
        t=R(k*22.5); a.cy('M_LampGlow',((r-0.25)*math.cos(t),(r-0.25)*math.sin(t),z+0.1),0.13,0.11,0.2,8)
    # manga de viento
    wx,wy=r+2.5,r*0.55
    a.cy('M_Curb',(wx,wy,0.15),0.45,0.45,0.3,10)
    a.cy('M_Iron',(wx,wy,2.6),0.07,0.06,5.0,8)
    for i,(mat,r1,r2) in enumerate((('M_AwningRed',0.42,0.36),('M_White',0.36,0.3),('M_AwningRed',0.3,0.24),('M_White',0.24,0.19))):
        a.cy(mat,(wx-0.45-i*0.62,wy,4.75-i*0.07),r1,r2,0.6,10,(0,-94,0))
    return a.finish()

def _ell(a,mat,c,r):  # elipsoide suave
    a.sp(mat,c,1.0,3,r)

def helicopter(name='helicopter'):
    # Caricaturesco (cabina panzona, cola corta) pero detallado: se ve de cerca en el menu.
    a=A(name)
    W,N,G='M_White','M_PoliceNavy','M_Glass'
    # --- fuselaje
    _ell(a,W,(0,-0.4,2.05),(1.45,3.6,1.35))                     # cabina
    _ell(a,W,(0,-0.2,1.4),(1.36,3.4,0.8))                        # panza
    _ell(a,W,(0,-3.35,1.75),(1.0,1.3,0.95))                      # trompa
    a.box(W,(0,0.0,1.05),(2.1,4.4,0.5))
    _ell(a,N,(0,-0.45,1.5),(1.475,3.62,0.24))                    # franja azul
    _ell(a,N,(0,-3.3,1.45),(1.02,1.31,0.22))
    _ell(a,'M_Gold',(0,-0.42,1.82),(1.468,3.615,0.05))           # filete dorado
    # --- parabrisas con marco
    _ell(a,G,(0,-3.1,2.4),(1.12,1.38,0.82))
    a.box(N,(0,-3.85,2.45),(0.1,0.12,1.15),(-38,0,0))            # parante central
    _ell(a,W,(0,-2.35,2.95),(1.0,0.55,0.18))                     # techo sobre el parabrisas
    a.sp('M_Dark',(0,-4.42,1.95),0.12,2)                          # nariz / radar
    for sx in (-1,1): a.cy('M_Metal',(sx*0.35,-4.55,1.7),0.025,0.02,0.6,6,(-90,0,0))   # pitots
    a.cy('M_Dark',(0,-3.9,0.95),0.22,0.18,0.28,12)               # reflector bajo la trompa
    a.sp('M_LampGlow',(0,-3.9,0.8),0.16,2)
    # --- ventanas redondeadas y puerta
    for sx in (-1,1):
        x=sx*1.36
        for y in (-1.5,-0.45,0.6):
            xx=x*(1.0 if y>-1 else 0.985)
            a.cy(W,(xx,y,2.2),0.46,0.46,0.12,20,(0,90,0))          # marco
            a.cy(G,(xx+sx*0.03,y,2.2),0.38,0.38,0.12,20,(0,90,0))
        a.box('M_Metal',(sx*1.44,-0.45,2.82),(0.06,2.8,0.06))     # riel de la puerta corrediza
        a.box(N,(sx*1.43,-2.45,1.95),(0.04,0.9,1.25))              # puerta del piloto
        a.box(G,(sx*1.45,-2.45,2.25),(0.04,0.62,0.5))
        a.box('M_Metal',(sx*1.47,-2.1,1.85),(0.05,0.18,0.05))      # manija
        # escudo nacional en la puerta (ovalo celeste y blanco con sol)
        a.cy('M_Celeste',(sx*1.47,1.45,1.95),0.34,0.34,0.04,18,(0,90,0))
        a.box(W,(sx*1.49,1.45,1.82),(0.03,0.6,0.22))
        a.sp('M_SunYellow',(sx*1.5,1.45,2.24),0.11,1)
        a.box('M_Dark',(sx*0.95,-1.2,0.8),(0.5,0.6,0.25))          # escalon
    # --- capot de turbinas
    a.box(W,(0,0.15,3.05),(1.7,3.2,0.82))
    _ell(a,W,(0,-1.45,3.0),(0.85,0.55,0.4))
    _ell(a,W,(0,1.75,3.0),(0.85,0.6,0.4))
    for sx in (-1,1):
        for k in range(4): a.box('M_Dark',(sx*0.86,-0.9+k*0.18,3.1),(0.03,0.08,0.42))   # rejillas de toma
        a.cy('M_Dark',(sx*0.55,2.05,3.2),0.2,0.24,0.6,12,(-70,0,sx*18))                 # escapes
        a.cy('M_StoneDark',(sx*0.6,2.3,3.25),0.16,0.16,0.05,12,(-70,0,sx*18))
    a.box(N,(0,0.15,3.47),(1.2,2.6,0.06))
    a.cy('M_Metal',(0,0.25,3.55),0.42,0.36,0.25,16)               # base del mastil
    a.cy('M_Dark',(0,0.25,3.78),0.17,0.15,0.4,12)
    a.sp('M_LightRed',(0,1.6,3.5),0.13,2)                          # balizas
    a.sp('M_LightRed',(0,0.2,0.75),0.12,2)
    a.box('M_Dark',(0,2.2,3.55),(0.04,0.4,0.35),(-30,0,0))         # antena
    # --- cola
    a.cy(W,(0,5.4,2.4),0.72,0.3,5.4,18,(-90,0,0))
    a.cy(N,(0,5.3,2.12),0.5,0.2,5.0,14,(-90,0,0))
    a.box('M_Dark',(0,5.0,2.95),(0.04,0.3,0.35),(-30,0,0))         # antena
    a.box(W,(0,8.15,3.35),(0.2,1.45,2.3),(-24,0,0))               # deriva
    a.box(N,(0,8.6,4.35),(0.22,1.0,0.5),(-24,0,0))
    a.box(W,(0,7.95,1.75),(0.16,0.8,0.8),(28,0,0))                # aleta ventral
    a.sp('M_Metal',(0,8.2,1.3),0.1,1)                              # patin de cola
    for sx in (-1,1):                                              # bandera en la deriva
        for i,mat in enumerate(('M_Celeste','M_White','M_Celeste')):
            a.box(mat,(sx*0.11,8.05+i*0.11,3.6-i*0.26),(0.02,0.95,0.26),(-24,0,0))
        a.sp('M_SunYellow',(sx*0.12,8.16,3.34),0.1,1)
    a.box(W,(0,7.3,2.45),(3.0,0.85,0.12))                         # estabilizador
    for sx in (-1,1):
        a.box(N,(sx*1.5,7.35,2.6),(0.1,0.75,0.6))                 # placas de los extremos
        a.sp('M_LightRed' if sx<0 else 'M_NavGreen',(sx*1.56,7.0,2.6),0.08,1)
    # rotor de cola (lado derecho) con caja de engranajes
    a.sp('M_Metal',(0.2,8.55,3.75),0.22,2)
    a.cy('M_Dark',(0.38,8.55,3.75),0.12,0.12,0.25,10,(0,90,0))
    for ang in (0,90):
        a.box('M_Dark',(0.5,8.55,3.75),(0.04,0.22,2.2),(ang,0,0))
        a.box('M_RoadYellow',(0.5,8.55,3.75),(0.045,0.23,0.25),(ang,0,0))
    # --- patines caricaturescos
    for sx in (-1,1):
        x=sx*1.4
        a.cy('M_Metal',(x,-0.3,0.14),0.1,0.1,5.4,12,(-90,0,0))
        a.cy('M_Metal',(x,-3.25,0.32),0.1,0.1,0.62,12,(54,0,0))     # punta curvada hacia arriba
        a.sp('M_Metal',(x,-3.0,0.14),0.1,2)
        a.sp('M_Metal',(x,-3.5,0.5),0.1,2)
        a.sp('M_Metal',(x,2.4,0.14),0.1,1)
        for y in (-1.8,1.2):
            a.cy('M_Metal',(sx*1.15,y,0.55),0.08,0.08,1.0,10,(0,sx*-30,0))
            a.box('M_Dark',(x,y,0.14),(0.24,0.3,0.24))               # abrazaderas
    return a.finish()

def heli_rotor(name='heli_rotor', L=7.0):
    a=A(name)
    a.cy('M_Metal',(0,0,0.05),0.5,0.45,0.1,20)                     # plato oscilante
    a.cy('M_Dark',(0,0,0.25),0.36,0.3,0.3,16)
    a.cy('M_Metal',(0,0,0.48),0.16,0.08,0.22,12)
    a.sp('M_Dark',(0,0,0.6),0.09,1)
    for k in range(4):
        t=k*90; c,s=math.cos(R(t)),math.sin(R(t))
        a.box('M_Metal',(c*0.55,s*0.55,0.25),(0.55,0.24,0.16),(0,0,t))            # mordaza
        a.cy('M_Metal',(c*0.33+s*0.12,s*0.33-c*0.12,0.12),0.03,0.03,0.25,6)      # varilla de paso
        a.box('M_Dark',(c*(L*0.35+0.6),s*(L*0.35+0.6),0.24),(L*0.7,0.46,0.07),(0,0,t))
        a.box('M_Dark',(c*(L*0.85+0.6),s*(L*0.85+0.6),0.22),(L*0.3,0.38,0.06),(0,0,t))
        a.box('M_RoadYellow',(c*(L+0.42),s*(L+0.42),0.22),(0.4,0.39,0.065),(0,0,t))   # punta amarilla
    return a.finish()

def build_all():
    out={}
    for f in (helipad,helicopter,heli_rotor):
        o=f(); out[o.name]=len(o.data.polygons)
    return out
