# plaza_map.py — lee el mapa dibujado (plaza_map.svg) y lo lleva a coordenadas de mundo
import re, math, site, sys
if site.getusersitepackages() not in sys.path: sys.path.append(site.getusersitepackages())
ROOT="/Users/alexander/git/argensimia/Argensimia/BlenderSrc/"
PX,PZ=40.0,26.0
QUAD=[(45,25),(945,25),(945,610),(45,610)]   # TL,TR,BR,BL del mapa limpio (plaza_design_v3) = borde de la plaza
LAWN_FILL="#738F2D"; LANE="#B25757"

def _bil(s,t):
    (a,b,c,d)=QUAD
    x=(1-s)*(1-t)*a[0]+s*(1-t)*b[0]+s*t*c[0]+(1-s)*t*d[0]
    y=(1-s)*(1-t)*a[1]+s*(1-t)*b[1]+s*t*c[1]+(1-s)*t*d[1]
    return x,y
def to_world(px,py):
    # inversa bilineal (Newton). Vista del dibujo = cámara mirando al sur: derecha = oeste (Casa Rosada), arriba = sur
    s,t=0.5,0.5
    for _ in range(30):
        x,y=_bil(s,t); e=1e-4
        x1,y1=_bil(s+e,t); x2,y2=_bil(s,t+e)
        J=((x1-x)/e,(x2-x)/e,(y1-y)/e,(y2-y)/e); det=J[0]*J[3]-J[1]*J[2]
        dx,dy=px-x,py-y
        s+=( J[3]*dx-J[1]*dy)/det; t+=(-J[2]*dx+J[0]*dy)/det
    return (PX-2*PX*s, -PZ+2*PZ*t)

def _tok(d): return re.findall(r'[MLHVCZmlhvcz]|-?\d*\.?\d+(?:e-?\d+)?',d)
def parse_path(d,steps=14):
    tk=_tok(d); i=0; cmd=None; cur=(0,0); start=(0,0); subs=[]; pts=[]
    def num():
        nonlocal i; v=float(tk[i]); i+=1; return v
    while i<len(tk):
        if re.match(r'[A-Za-z]',tk[i]): cmd=tk[i]; i+=1
        if cmd=='M':
            if pts: subs.append(pts)
            cur=(num(),num()); start=cur; pts=[cur]; cmd='L'
        elif cmd=='L': cur=(num(),num()); pts.append(cur)
        elif cmd=='H': cur=(num(),cur[1]); pts.append(cur)
        elif cmd=='V': cur=(cur[0],num()); pts.append(cur)
        elif cmd=='C':
            p1=(num(),num()); p2=(num(),num()); p3=(num(),num()); p0=cur
            for k in range(1,steps+1):
                t=k/steps; u=1-t
                pts.append((u**3*p0[0]+3*u*u*t*p1[0]+3*u*t*t*p2[0]+t**3*p3[0], u**3*p0[1]+3*u*u*t*p1[1]+3*u*t*t*p2[1]+t**3*p3[1]))
            cur=p3
        elif cmd in 'Zz':
            cur=start
            if pts: subs.append(pts); pts=[]
        else: i+=1
    if pts: subs.append(pts)
    return subs

def load():
    txt=open(ROOT+'plaza_map.svg').read()
    lawns=[];lanes=[];reinf=[]
    for m in re.finditer(r'<path([^>]*)/>',txt):
        at=m.group(1); d=re.search(r'd="([^"]+)"',at).group(1)
        fill=re.search(r'fill="([^"]+)"',at); stroke=re.search(r'stroke="([^"]+)"',at); sw=re.search(r'stroke-width="([^"]+)"',at)
        if fill and fill.group(1).upper()==LAWN_FILL.upper():
            for sp in parse_path(d): lawns.append([to_world(*p) for p in sp])
        elif stroke and stroke.group(1).upper()==LANE.upper():
            pts=[to_world(*p) for sp in parse_path(d,24) for p in sp]
            (lanes if float(sw.group(1))>=8 else reinf).append(pts)
    return lawns,lanes,reinf
