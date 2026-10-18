# defense_kit.py - defensas desbloqueables (cada 3 mandatos sobrevividos): puntero con parrilla, contenedor de basura,
# comisario con megafono, concertina, mesa de requisa, barrera de desvio, drone y tanqueta.
# Frente = Blender -Y (= Unity +Z; en el juego se instancian con yaw 90, mirando a los manifestantes).
# Personajes a escala real (en Unity se escalan como la policia); props a escala de juego.
import math
import gen_assets as GA
from gen_assets import A, R, cyl, sph
from mathutils import Matrix, Vector

def _belly(a,mat,z=1.08,r=0.27):
    a.sp(mat,(0,-0.06,z),r,2,(1.05,0.95,1.0))

def puntero(name='puntero'):
    # gordito del barrio con delantal, gorra y pinza, detras de una parrillita con choris; cajita con billetes
    a=A(name)
    GA._body(a,'M_PunteroShirt','M_Pants','M_Skin','M_Hair')
    _belly(a,'M_PunteroShirt',1.1,0.29)
    a.box('M_Apron',(0,-0.31,0.95),(0.42,0.04,0.62))                       # delantal
    a.box('M_Apron',(0,-0.2,1.33),(0.3,0.04,0.2))
    a.cy('M_FlagRed',(0,0.0,1.79),0.14,0.14,0.08,10)                         # gorra
    a.box('M_FlagRed',(0,-0.15,1.76),(0.24,0.16,0.03))
    a.box('M_Hair',(0,-0.13,1.6),(0.12,0.03,0.025))                          # bigote
    # brazos: uno con la pinza hacia la parrilla, otro saludando
    mx=GA._limb(a,'M_PunteroShirt',(0.255,0,1.44),-70,-10,0.55,0.06,0.05)
    a.box('M_Metal',(0.3,-0.75,1.0),(0.04,0.4,0.03),(30,0,0))
    GA._limb(a,'M_PunteroShirt',(-0.255,0,1.44),-10,150,0.55,0.06,0.05)
    # parrilla adelante
    py=-1.0
    for x in (-0.45,0.45):
        for y in (py-0.25,py+0.25): a.cy('M_Iron',(x,y,0.4),0.025,0.025,0.8,6)
    a.box('M_Iron',(0,py,0.82),(1.0,0.6,0.06))
    a.box('M_Coal',(0,py,0.86),(0.9,0.5,0.03))
    for i in range(7): a.box('M_Iron',(-0.42+i*0.14,py,0.92),(0.02,0.58,0.02))
    for i in range(5): a.cy('M_Chori',(-0.36+i*0.18,py,0.96),0.045,0.045,0.42,8,(90,0,0))
    a.box('M_Iron',(0,py+0.3,1.15),(1.0,0.03,0.5))
    # mesita plegable con la cajita y los billetes
    for x in (0.95,1.45):
        a.box('M_Iron',(x,py+0.2,0.36),(0.03,0.03,0.72),(0,12,0))
    a.box('M_Wood',(1.2,py+0.2,0.74),(0.7,0.5,0.04))
    a.box('M_Dark',(1.2,py+0.2,0.84),(0.34,0.24,0.16))
    for k in range(3): a.box('M_Cash',(1.18+k*0.03,py+0.18,0.94+k*0.012),(0.24,0.12,0.01),(0,0,k*12))
    a.cy('M_Chori',(1.0,py+0.25,0.8),0.05,0.05,0.24,8,(0,90,0))
    a.box('M_Sign',(1.4,py+0.35,0.85),(0.2,0.02,0.24),(-10,0,0))
    return a.finish()

def contenedor(name='contenedor'):
    # contenedor verde de residuos (CABA) con tapa, ruedas y manijas; tamano de juego
    a=A(name); W,D,H=2.0,1.3,1.45
    a.box('M_DumpGreen',(0,0,0.28+H/2),(W,D,H))
    a.box('M_DumpGreen',(0,0,0.28+H+0.06),(W+0.08,D+0.08,0.12))
    a.box('M_DumpGreen',(0,0.05,0.28+H+0.2),(W+0.04,D+0.02,0.1),(-8,0,0))   # tapa entreabierta
    for z in (0.6,1.1): a.box('M_Dark',(0,-D/2-0.02,0.28+z),(W*0.9,0.03,0.05))
    for x in (-W/2+0.25,W/2-0.25):
        for y in (-D/2+0.2,D/2-0.2):
            a.cy('M_Tire',(x,y,0.14),0.14,0.14,0.12,10,(0,90,0)); a.box('M_Iron',(x,y,0.26),(0.08,0.08,0.1))
    for x in (-W/2-0.06,W/2+0.06): a.box('M_Iron',(x,0,0.28+H*0.75),(0.06,D*0.7,0.06))
    a.box('M_White',(0,-D/2-0.02,0.28+H*0.42),(0.6,0.02,0.3))                 # calco
    a.box('M_DumpGreen',(0,-D/2-0.035,0.28+H*0.42),(0.32,0.02,0.16))
    return a.finish()

def comisario(name='comisario'):
    # comisario panzon con gorra de visera dorada, bigotazo y megafono
    a=A(name)
    GA._body(a,'M_PoliceNavy','M_PoliceNavy','M_Skin','M_Hair')
    _belly(a,'M_PoliceNavy',1.08,0.31)
    a.box('M_Gold',(0,-0.31,1.1),(0.06,0.04,0.36))                            # botones
    a.cy('M_Dark',(0,-0.04,0.9),0.31,0.31,0.08,14)                            # cinturon
    a.box('M_Gold',(0,-0.33,0.9),(0.12,0.03,0.08))
    for x in (-0.24,0.24): a.box('M_Gold',(x,0,1.47),(0.14,0.3,0.03))         # charreteras
    a.cy('M_PoliceNavy',(0,0.0,1.8),0.16,0.17,0.12,12)                        # gorra
    a.cy('M_PoliceNavy',(0,0.0,1.87),0.2,0.2,0.05,12)
    a.box('M_Dark',(0,-0.17,1.76),(0.26,0.18,0.03),(-12,0,0))                 # visera
    a.box('M_Gold',(0,-0.16,1.79),(0.26,0.02,0.03))
    a.sp('M_Gold',(0,-0.19,1.84),0.035,1)
    a.box('M_Hair',(0,-0.13,1.6),(0.2,0.05,0.045))                            # bigotazo
    for x in (-0.11,0.11): a.box('M_Hair',(x,-0.12,1.57),(0.06,0.04,0.06),(0,x*120,0))
    # brazo derecho al megafono, izquierdo senalando adelante
    GA._limb(a,'M_PoliceNavy',(0.255,0,1.44),-95,-25,0.42,0.065,0.055)
    a.cy('M_White',(0.12,-0.48,1.62),0.07,0.2,0.42,12,(90,0,0))
    a.cy('M_FlagRed',(0.12,-0.7,1.62),0.21,0.21,0.04,12,(90,0,0))
    a.box('M_Dark',(0.2,-0.38,1.5),(0.05,0.08,0.16))
    GA._limb(a,'M_PoliceNavy',(-0.255,0,1.44),-80,30,0.6,0.065,0.055)
    return a.finish()

def concertina(name='concertina',L=4.4):
    # rollos de alambre de puas sobre caballetes de madera; a lo largo del eje X
    a=A(name); n=16; r=0.42; seg=12
    for i in range(n):
        x=-L/2+L*i/(n-1)
        for k in range(seg):
            t=2*math.pi*k/seg; t2=2*math.pi*(k+1)/seg
            y=r*math.cos(t); z=0.5+r*math.sin(t)
            y2=r*math.cos(t2); z2=0.5+r*math.sin(t2)
            ln=math.hypot(y2-y,z2-z)
            ang=math.degrees(math.atan2(z2-z,y2-y))
            a.box('M_Metal',((x),(y+y2)/2,(z+z2)/2),(0.03,ln*1.05,0.03),(-ang,8,0))
            if k%3==0: a.box('M_Metal',(x,y,z),(0.08,0.02,0.02),(0,0,45))     # puas
    for x in (-L/2-0.15,0,L/2+0.15):
        for s in (-1,1): a.box('M_Wood',(x,s*0.1,0.45),(0.07,0.07,1.05),(s*28,0,0))
        a.box('M_Wood',(x,0,0.62),(0.06,0.5,0.06))
    for y in (-0.38,0.38): a.box('M_Iron',(0,y,0.15),(L+0.3,0.03,0.03))
    return a.finish()

def requisa(name='requisa'):
    # mesa plegable con lo secuestrado: palos, pasamontanas, tubos de mortero, gomeras y mochilas
    a=A(name)
    a.box('M_Wood',(0,0,0.9),(2.2,0.95,0.06))
    for x in (-1.0,1.0):
        for s in (-1,1): a.box('M_Iron',(x,s*0.25,0.45),(0.04,0.04,0.95),(s*20,0,0))
    a.box('M_White',(0,0,0.94),(2.1,0.9,0.01))
    for i in range(4): a.cy('M_Wood',(-0.6+i*0.08,-0.1+i*0.06,1.0),0.03,0.03,1.4,6,(0,90,8*i))
    for i in range(3): a.cy('M_Plinth',(0.45+i*0.18,0.15,1.02),0.07,0.07,0.75,8,(90,0,-10+i*12))
    for i in range(3): a.sp('M_HelmetBlack',(0.2+i*0.22,-0.25,1.0),0.1,1,(1,1,0.6))
    a.box('M_Wood',(-0.35,0.28,0.97),(0.06,0.25,0.03),(0,0,30)); a.box('M_Wood',(-0.35,0.28,0.97),(0.06,0.25,0.03),(0,0,-30))
    # mochilas en el piso al costado de la mesa
    for k,(x,mat) in enumerate(((1.45,'M_FlagRed'),(1.75,'M_Celeste'),(1.6,'M_Dark'))):
        a.box(mat,(x,-0.2+k*0.3,0.25),(0.35,0.25,0.5),(0,0,k*25)); a.box(mat,(x,-0.2+k*0.3,0.52),(0.3,0.22,0.08),(0,0,k*25))
    # bolsas de evidencia
    for i in range(3): a.box('M_Sign',(-1.5,-0.3+i*0.3,0.18),(0.36,0.26,0.36),(0,0,i*15))
    return a.finish()

def desvio(name='desvio'):
    # barrera de desvio a rayas naranja y blanca con flecha, y conos
    a=A(name); W=3.2
    for x in (-W/2+0.15,W/2-0.15):
        for s in (-1,1): a.box('M_Iron',(x,s*0.18,0.6),(0.06,0.06,1.25),(s*16,0,0))
    for z in (0.85,1.2):
        n=8
        for i in range(n):
            x=-W/2+(i+0.5)*W/n
            a.box('M_ConeOrange' if i%2==0 else 'M_White',(x,-0.02,z),(W/n,0.05,0.24),(0,32,0))
    a.box('M_Dark',(0,-0.06,1.52),(0.9,0.04,0.32))                            # cartel de flecha
    a.box('M_RoadYellow',(-0.08,-0.09,1.52),(0.5,0.03,0.1))
    a.box('M_RoadYellow',(0.22,-0.09,1.52),(0.24,0.03,0.24),(0,45,0))
    for (x,y) in ((-1.9,-0.9),(-0.7,-1.2),(0.6,-1.2),(1.9,-0.9),(2.4,0.2)):
        a.box('M_Dark',(x,y,0.03),(0.5,0.5,0.06))
        a.cy('M_ConeOrange',(x,y,0.4),0.21,0.04,0.72,10)
        a.cy('M_White',(x,y,0.45),0.15,0.12,0.12,10)
    return a.finish()

def drone(name='drone'):
    # cuadricoptero policial: cuerpo, 4 brazos con motores (las helices las agrega Unity para que giren),
    # camara colgante y balizas roja y azul
    a=A(name)
    a.sp('M_PoliceNavy',(0,0,0),0.55,2,(1.1,1.3,0.45))
    a.sp('M_White',(0,0,0.12),0.45,2,(1.0,1.2,0.35))
    a.box('M_White',(0,-0.2,-0.12),(0.5,0.6,0.12))
    for k in range(4):
        t=math.radians(45+90*k); x,y=1.15*math.cos(t),1.15*math.sin(t)
        a.box('M_Dark',(x/2,y/2,0.05),(0.14,1.2,0.08),(0,0,math.degrees(t)-90))
        a.cy('M_Dark',(x,y,0.12),0.13,0.13,0.22,10)
        a.cy('M_Iron',(x,y,-0.25),0.03,0.03,0.5,6)
    a.sp('M_Dark',(0,-0.45,-0.25),0.18,2)
    a.sp('M_Glass',(0,-0.6,-0.27),0.09,1)
    a.sp('M_LightRed',(-0.25,0.3,0.25),0.08,1); a.sp('M_LightBlue',(0.25,0.3,0.25),0.08,1)
    for x in (-0.35,0.35): a.box('M_Iron',(x,0,-0.45),(0.05,0.9,0.04))
    return a.finish()

def tanqueta(name='tanqueta'):
    # vehiculo antidisturbios caricaturesco: casco blindado, 6 ruedas, ventanas tipo ranura, torreta con canon de espuma.
    # Largo a lo largo de Blender -Y (frente); en el juego ocupa 2 slots.
    a=A(name); L,W=7.6,2.9
    for y in (-2.5,0,2.5):
        for x in (-W/2,W/2):
            a.cy('M_Tire',(x,y,0.62),0.62,0.62,0.5,16,(0,90,0)); a.cy('M_Metal',(x*1.08,y,0.62),0.3,0.3,0.12,12,(0,90,0))
    a.box('M_PoliceNavy',(0,0,1.45),(W-0.1,L,1.3))
    a.box('M_SteelDark',(0,0,0.85),(W-0.3,L+0.2,0.35))
    a.box('M_PoliceNavy',(0,0.4,2.45),(W-0.45,L-1.6,0.75))
    a.box('M_PoliceNavy',(0,-L/2+0.15,1.95),(W-0.5,0.9,0.85),(-35,0,0))      # trompa en angulo
    a.box('M_White',(0,0,1.7),(W-0.06,L-0.6,0.22))                           # franja
    for x in (-W/2+0.18,W/2-0.18):
        for y in (-1.6,-0.4,0.8,2.0): a.box('M_Glass',(x*1.03,y,2.5),(0.05,0.55,0.18))
    a.box('M_Glass',(0,-L/2+0.65,2.65),(W-0.9,0.06,0.3),(-25,0,0))
    a.box('M_SteelDark',(0,-L/2-0.1,0.95),(W+0.1,0.3,0.45))                   # paragolpes
    for x in (-0.9,0.9): a.box('M_LampGlow',(x,-L/2-0.27,1.3),(0.35,0.04,0.2))
    for x in (-0.5,0.5): a.box('M_LightRed' if x<0 else 'M_LightBlue',(x,-1.6,2.92),(0.5,0.3,0.18))
    for y in (-2.0,-1.0,0,1.0,2.0):
        for x in (-W/2-0.02,W/2+0.02): a.box('M_SteelDark',(x,y,1.45),(0.05,0.08,1.1))
    a.box('M_Iron',(0,L/2+0.05,1.2),(0.8,0.1,0.9))                            # puerta trasera
    return a.finish()

def foam_turret(name='tanqueta_torreta'):
    # torreta giratoria aparte (el canon apunta en Unity)
    a=A(name)
    a.cy('M_SteelDark',(0,0,0.2),0.75,0.7,0.4,16)
    a.sp('M_PoliceNavy',(0,0,0.45),0.6,2,(1,1,0.55))
    a.cy('M_Metal',(0,-0.9,0.55),0.16,0.2,1.4,12,(90,0,0))
    a.cy('M_White',(0,-1.6,0.55),0.24,0.24,0.18,12,(90,0,0))
    a.cy('M_Dark',(0,-1.7,0.55),0.15,0.15,0.04,12,(90,0,0))
    return a.finish()


def hidrante(level=2):
    # camion hidrante blindado (NV2) y de asalto (NV3). Mismas medidas que police_truck (largo en Y, frente -Y)
    # para que el canon de Unity y el slot doble coincidan. Sin canon: lo agrega Unity (gira).
    a=A('hidrante_l%d'%level)
    B,N,S,M='M_PoliceBlue','M_PoliceNavy','M_SteelDark','M_Metal'
    # chasis y ruedas grandes (4 ejes en NV3)
    a.box('M_Dark',(0,0.2,0.8),(2.4,7.6,0.65))
    axles=(-2.6,-0.9,1.0,2.7) if level>=3 else (-2.5,1.0,2.6)
    for y in axles:
        for x in (-1.22,1.22):
            a.cy('M_Tire',(x,y,0.62),0.62,0.62,0.46,14,(0,90,0)); a.cy(M,(x*1.12,y,0.62),0.28,0.28,0.08,10,(0,90,0))
    # caja blindada con chanfles
    a.box(N,(0,1.1,2.05),(2.66,5.2,2.6))
    a.box(N,(0,1.1,3.42),(2.3,5.0,0.22))
    for x in (-1.2,1.2): a.box(N,(x,1.1,3.3),(0.32,5.1,0.3),(0,x*25,0))
    # cabina blindada en cuna
    a.box(N,(0,-2.6,1.75),(2.66,2.3,2.0))
    a.box(N,(0,-3.35,2.55),(2.5,0.9,0.7),(-38,0,0))
    a.box('M_Glass',(0,-3.62,2.45),(2.0,0.08,0.45),(-38,0,0))
    for k in range(5): a.box(S,(-0.8+k*0.4,-3.7,2.45),(0.06,0.1,0.55),(-38,0,0))           # rejilla del parabrisas
    for x in (-1.34,1.34):
        a.box('M_Glass',(x,-2.6,2.25),(0.05,1.0,0.45))
        for k in range(3): a.box(S,(x*1.02,-2.95+k*0.35,2.25),(0.05,0.06,0.5))
    # placas laterales con remaches y franja amarilla/negra
    for x in (-1.36,1.36):
        a.box(S,(x,1.1,1.75),(0.1,5.0,1.25))
        for k in range(10): a.box('M_RoadYellow' if k%2==0 else 'M_Dark',(x*1.01,-1.15+k*0.5,1.05),(0.08,0.5,0.22),(0,0,0))
        for k in range(8): a.sp(M,(x*1.03,-1.4+k*0.65,2.3),0.05,1)
        a.box('M_White',(x*1.005,1.1,2.75),(0.06,4.8,0.18))
    # paragolpes con defensa (bull bar); en NV3 una pala en cuna
    a.box(S,(0,-3.85,0.95),(2.7,0.35,0.55))
    for x in (-0.9,0,0.9): a.box(M,(x,-4.05,1.35),(0.12,0.12,0.9))
    a.box(M,(0,-4.05,1.75),(2.2,0.12,0.12))
    if level>=3:
        a.box(S,(0,-4.35,0.75),(2.9,0.25,1.1),(-30,0,0))
        for k in range(5): a.box('M_RoadYellow' if k%2==0 else 'M_Dark',(-1.1+k*0.55,-4.5,0.75),(0.5,0.05,0.9),(-30,0,25))
    for x in (-0.95,0.95): a.box('M_LampGlow',(x,-3.75,1.3),(0.35,0.06,0.22))
    # tanque de agua atras
    a.cy(S,(0,2.6,2.2),1.05,1.05,2.4,16,(90,0,0))
    a.cy(M,(0,3.82,2.2),0.6,0.6,0.08,16,(90,0,0))
    # balizas largas y techo
    a.box('M_LightRed',(-0.55,-2.2,2.86),(0.9,0.35,0.18)); a.box('M_LightBlue',(0.55,-2.2,2.86),(0.9,0.35,0.18))
    a.box(S,(0,-2.2,2.78),(2.2,0.4,0.06))
    if level>=3:
        # jaula sobre el techo, pasamanos, segunda barra de luces atras, escalera
        for x in (-1.0,1.0):
            a.box(M,(x,1.1,4.15),(0.09,4.6,0.09))
            for k in range(5): a.box(M,(x,-1.1+k*1.1,3.8),(0.09,0.09,0.7))
        for k in range(5): a.box(M,(0,-1.1+k*1.1,4.15),(2.0,0.08,0.08))
        a.box('M_LightRed',(-0.55,3.5,3.62),(0.9,0.3,0.16)); a.box('M_LightBlue',(0.55,3.5,3.62),(0.9,0.3,0.16))
        for k in range(6): a.box(M,(0.9,3.95,0.9+k*0.42),(0.5,0.06,0.06))
        for x in (-1.36,1.36): a.box(S,(x*1.02,-2.6,1.2),(0.1,2.2,0.9))   # puertas blindadas
    return a.finish()

def build_all():
    out={}
    for f in (puntero,contenedor,comisario,concertina,requisa,desvio,drone,tanqueta,foam_turret,lambda: hidrante(2),lambda: hidrante(3)):
        o=f(); out[o.name]=len(o.data.polygons)
    return out
