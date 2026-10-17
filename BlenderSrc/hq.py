# High-quality pipeline: bevels, smooth shading, box UVs, procedural tileable textures (+normal maps)
import bpy, bmesh, math, os
import numpy as np
import gen_assets as GA
from gen_assets import A, R
TEX=GA.ROOT+"Assets/Textures/"
os.makedirs(TEX,exist_ok=True)

# ---------- texture synthesis ----------
def _noise(n,seed,lo,hi,aniso=(1.0,1.0)):
    rng=np.random.default_rng(seed); w=rng.standard_normal((n,n))
    f=np.fft.fftfreq(n)*n; FX,FY=np.meshgrid(f,f); r=np.sqrt((FX*aniso[0])**2+(FY*aniso[1])**2)
    filt=((r>=lo)&(r<=hi))/np.maximum(r,1)
    o=np.real(np.fft.ifft2(np.fft.fft2(w)*filt)); return (o-o.mean())/(o.std()+1e-9)
def _tiles(n,cols,rows,mortar,stagger,seed,var=0.1,bevel=0.0):
    rng=np.random.default_rng(seed); y,x=np.mgrid[0:n,0:n].astype(float)
    th=n/rows; tw=n/cols; row=(y//th).astype(int)
    xo=(x+((row%2)*tw/2 if stagger else 0))%n; col=(xo//tw).astype(int)
    fx=xo%tw; fy=y%th
    edge=np.minimum(np.minimum(fx,tw-fx),np.minimum(fy,th-fy))
    tv=rng.uniform(-var,var,(rows+1,cols+1))[row,col]
    h=np.clip(edge/max(mortar,1e-3),0,1)
    if bevel>0: h=np.clip(edge/bevel,0,1)
    return h,tv,fy/th
def _save(name,arr,normal=False):
    n=arr.shape[0]
    if arr.ndim==2: rgba=np.dstack([arr,arr,arr,np.ones_like(arr)])
    else: rgba=np.dstack([arr,np.ones((n,n))])
    rgba=np.clip(rgba,0,1).astype(np.float32)
    img=bpy.data.images.get(name) or bpy.data.images.new(name,n,n,alpha=False)
    if img.size[0]!=n: img.scale(n,n)
    if normal: img.colorspace_settings.name='Non-Color'
    img.pixels.foreach_set(rgba.ravel()); img.filepath_raw=TEX+name+".png"; img.file_format='PNG'; img.save()
def _normal(h,s):
    dx=np.roll(h,-1,1)-np.roll(h,1,1); dy=np.roll(h,-1,0)-np.roll(h,1,0)
    nx,ny,nz=-dx*s,-dy*s,np.ones_like(h); l=np.sqrt(nx*nx+ny*ny+nz*nz)
    return np.dstack([nx/l*0.5+0.5,ny/l*0.5+0.5,nz/l*0.5+0.5])
def make_textures(n=512):
    out={}
    # plaster
    a=0.9+0.035*_noise(n,1,2,40)+0.02*_noise(n,2,40,256); h=0.5*_noise(n,3,8,128)
    _save("T_plaster",a); _save("T_plaster_n",_normal(h,0.6),True)
    # rusticated stone blocks
    h,tv,_=_tiles(n,4,8,6,True,4,0.06,bevel=10); a=0.86+tv+0.04*_noise(n,5,4,128); a=np.where(h<0.25,0.62,a)
    _save("T_stone",a); _save("T_stone_n",_normal(h*3+0.3*_noise(n,6,16,200),2.5),True)
    # roof tiles / copper sheets
    h,tv,fy=_tiles(n,8,12,2,True,7,0.04); a=0.84+tv+0.1*fy+0.025*_noise(n,8,2,24); a=np.where(h<0.4,0.7,a)
    _save("T_roof",a); _save("T_roof_n",_normal(h*0.6+fy*1.2,2.0),True)
    # plaza paving (large warm slabs)
    h,tv,_=_tiles(n,6,6,3,False,9,0.06); a=0.88+tv+0.04*_noise(n,10,3,64)+0.025*_noise(n,11,64,256); a=np.where(h<0.5,0.7,a)
    _save("T_paving",a); _save("T_paving_n",_normal(h+0.2*_noise(n,12,30,256),1.6),True)
    # small sidewalk tiles
    h,tv,_=_tiles(n,10,10,2,False,13,0.05); a=0.86+tv+0.04*_noise(n,14,3,64); a=np.where(h<0.5,0.68,a)
    _save("T_sidewalk",a); _save("T_sidewalk_n",_normal(h,1.4),True)
    # asphalt
    a=0.82+0.06*_noise(n,15,2,24)+0.07*_noise(n,16,120,256); h=_noise(n,17,100,256)
    _save("T_asphalt",a); _save("T_asphalt_n",_normal(h,0.8),True)
    # grass
    g1=_noise(n,18,2,16); g2=_noise(n,19,60,256); a=0.82+0.1*g1+0.12*g2; h=g2
    _save("T_grass",a); _save("T_grass_n",_normal(h,1.2),True)
    # foliage (clumpy leaves)
    l1=_noise(n,20,6,40); l2=_noise(n,21,40,160); cl=np.tanh(1.6*(l1+0.5*l2)); a=0.78+0.2*cl; h=cl
    _save("T_leaves",a); _save("T_leaves_n",_normal(h,3.0),True)
    # bark
    b=_noise(n,22,2,128,aniso=(1.0,0.12)); a=0.8+0.12*b; _save("T_bark",a); _save("T_bark_n",_normal(b,2.0),True)
    # wood planks
    h,tv,_=_tiles(n,1,8,2,False,23,0.08); w=_noise(n,24,2,128,aniso=(0.1,1.0)); a=0.82+tv+0.07*w; a=np.where(h<0.5,0.55,a)
    _save("T_wood",a); _save("T_wood_n",_normal(h+0.3*w,1.5),True)
    # marble
    m=_noise(n,25,2,32); a=0.93+0.04*m+0.03*np.tanh(4*_noise(n,26,8,64)); _save("T_marble",a); _save("T_marble_n",_normal(m*0.2,0.5),True)
    return "textures ok"

# ---------- material table: name -> (texture, meters per repeat) ----------
TEXMAP={}
def load_texmap():
    TEXMAP.clear()
    for ln in open(GA.ROOT+"BlenderSrc/palette.txt"):
        p=ln.split()
        if len(p)>=5: TEXMAP[p[0]]=(p[3],float(p[4]))
load_texmap()

def M_hq(name):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    c=GA.PAL.get(name,(1,0,1)); cl=(GA.lin(c[0]),GA.lin(c[1]),GA.lin(c[2]),1)
    m.diffuse_color=cl
    if m.node_tree is None:
        try: m.use_nodes=True
        except Exception: pass
    nt=m.node_tree; nodes=nt.nodes; links=nt.links
    b=nodes.get("Principled BSDF")
    if b is None: return m
    b.inputs["Base Color"].default_value=cl
    try: b.inputs["Roughness"].default_value=0.25 if ('Glass' in name or 'Window' in name) else 0.8
    except Exception: pass
    if name in TEXMAP and "tx" not in nodes:
        tex,scale=TEXMAP[name]
        tc=nodes.new("ShaderNodeTexCoord"); mp=nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value=(1/scale,1/scale,1/scale)
        links.new(tc.outputs["UV"],mp.inputs["Vector"])
        ti=nodes.new("ShaderNodeTexImage"); ti.name="tx"; ti.image=bpy.data.images.load(TEX+tex+".png",check_existing=True)
        links.new(mp.outputs["Vector"],ti.inputs["Vector"])
        mix=nodes.new("ShaderNodeMix"); mix.data_type='RGBA'; mix.blend_type='MULTIPLY'; mix.inputs["Factor"].default_value=1.0
        mix.inputs[6].default_value=cl; links.new(ti.outputs["Color"],mix.inputs[7]); links.new(mix.outputs[2],b.inputs["Base Color"])
        try:
            tn=nodes.new("ShaderNodeTexImage"); tn.image=bpy.data.images.load(TEX+tex+"_n.png",check_existing=True); tn.image.colorspace_settings.name='Non-Color'
            links.new(mp.outputs["Vector"],tn.inputs["Vector"]); nm=nodes.new("ShaderNodeNormalMap"); links.new(tn.outputs["Color"],nm.inputs["Color"]); links.new(nm.outputs["Normal"],b.inputs["Normal"])
        except Exception: pass
    return m
GA.M=M_hq

# ---------- geometry upgrades ----------
_orig_box=A.box
def box_hq(s,mat,loc,size,rot=(0,0,0)):
    b=GA.cube(*size); mn=min(size)
    if mn>=0.18 and getattr(s,'bevel',True):
        off=min(0.06,mn*0.18)
        bmesh.ops.bevel(b,geom=list(b.edges),offset=off,offset_type='OFFSET',segments=1,profile=0.5,affect='EDGES')
    s.add(b,mat,loc,rot)
A.box=box_hq

def _finish_hq(s,export=True,flip=False,smooth_angle=38.0):
    bm=s.bm
    if flip: bmesh.ops.rotate(bm,cent=(0,0,0),matrix=GA.Matrix.Rotation(R(180),3,'Z'),verts=bm.verts)
    bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=0.0005)
    ng=[f for f in bm.faces if len(f.verts)>4]
    if ng: bmesh.ops.triangulate(bm,faces=ng)
    bm.normal_update()
    uv=bm.loops.layers.uv.verify()
    for f in bm.faces:
        f.smooth=True
        n=f.normal; ax=abs(n.x),abs(n.y),abs(n.z)
        for l in f.loops:
            co=l.vert.co
            if ax[2]>=ax[0] and ax[2]>=ax[1]: l[uv].uv=(co.x,co.y)
            elif ax[0]>=ax[1]: l[uv].uv=(co.y,co.z)
            else: l[uv].uv=(co.x,co.z)
    lim=R(smooth_angle)
    for e in bm.edges:
        if len(e.link_faces)!=2: e.smooth=False; continue
        try: e.smooth = e.calc_face_angle(0.0) < lim
        except Exception: e.smooth=False
    me=bpy.data.meshes.new(s.name); bm.to_mesh(me); bm.free()
    for m in s.mats: me.materials.append(GA.M(m))
    o=bpy.data.objects.new(s.name,me); bpy.context.scene.collection.objects.link(o)
    if export:
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
        bpy.ops.export_scene.fbx(filepath=GA.OUT+s.name+".fbx",use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=True,object_types={'MESH'},mesh_smooth_type='EDGE',use_mesh_modifiers=True,use_tspace=True)
    return o
A.finish=_finish_hq
