import bpy, numpy as np, struct, re, time
t0=time.time()
dg=bpy.context.evaluated_depsgraph_get()
import sys, os
MODE=os.environ.get('SKIN_MODE','body')
EXCLUDE=os.environ.get('SKIN_EXCLUDE')   # objects the female exterior leaves out
OUTNAME=os.environ.get('SKIN_OUT')
if MODE=='body':
    V=0.005; R=3; ER=2; mn=np.array([-0.60,-0.02,-0.22],np.float32); mx=np.array([0.60,1.80,0.22],np.float32); OUT='/private/tmp/za/skinfield.bin'
elif MODE=='head':
    V=0.0025; R=2; ER=1; mn=np.array([-0.14,1.42,-0.15],np.float32); mx=np.array([0.14,1.78,0.14],np.float32); OUT='/private/tmp/za/skinhead.bin'
else:   # left hand and wrist, mirrored for the right
    V=0.002; R=2; ER=0; mn=np.array([0.17,0.64,-0.14],np.float32); mx=np.array([0.38,0.90,0.10],np.float32); OUT='/private/tmp/za/skinhand.bin'
dims=np.ceil((mx-mn)/V).astype(int)+1
occ=np.zeros(dims,bool)
def mark(p):
    i=np.floor((p-mn)/V+0.5).astype(int)
    ok=np.all((i>=0)&(i<dims),axis=1); i=i[ok]
    occ[i[:,0],i[:,1],i[:,2]]=True
n_obj=0
for o in bpy.data.objects:
    if o.type not in('MESH',): continue
    coll=o.users_collection[0].name if o.users_collection else ''
    if coll[:2] not in ('1:','4:','7:','8:','6:'): continue
    if re.search(r'\.[gjt]$',o.name): continue
    if EXCLUDE and re.search(EXCLUDE,o.name): continue
    if re.search(r'bursa|fascia|sheath|nodes?\b|node\)|Nucleus|nucleus|tract|sulcus|gyrus',o.name): continue
    eo=o.evaluated_get(dg); me=eo.to_mesh()
    if len(me.vertices)<3: eo.to_mesh_clear(); continue
    M=np.array(eo.matrix_world)
    co=np.empty(len(me.vertices)*3,np.float32); me.vertices.foreach_get('co',co); co=co.reshape(-1,3)
    w=(co@M[:3,:3].T)+M[:3,3]
    me.calc_loop_triangles()
    tri=np.empty(len(me.loop_triangles)*3,np.int32); me.loop_triangles.foreach_get('vertices',tri); tri=tri.reshape(-1,3)
    pts=[w]
    if len(tri):
        a,b,c=w[tri[:,0]],w[tri[:,1]],w[tri[:,2]]
        pts+= [(a+b+c)/3,(a+b)/2,(b+c)/2,(a+c)/2]
    P=np.concatenate(pts).astype(np.float32)
    # convert Blender (x,y,z) -> unity (x,z,y)
    P=P[:,[0,2,1]]
    mark(P); n_obj+=1
    eo.to_mesh_clear()
print("MARKED",n_obj,occ.sum(),time.time()-t0)

def dilate1(a,axis):
    b=a.copy()
    sl=[slice(None)]*3
    s1=tuple(slice(1,None) if k==axis else slice(None) for k in range(3)); s0=tuple(slice(None,-1) if k==axis else slice(None) for k in range(3))
    b[s1]|=a[s0]; b[s0]|=a[s1]
    return b
def dilate(a,r):
    # approx-spherical: alternate axis steps and diagonal pairs
    for _ in range(r):
        a=dilate1(dilate1(dilate1(a,0),1),2)
    return a
def erode(a,r): return ~dilate(~a,r)

d=dilate(occ,R)
# fill interior: flood outside from the border
outside=np.zeros_like(d); outside[0,:,:]=~d[0,:,:]; outside[-1,:,:]=~d[-1,:,:]; outside[:,0,:]=~d[:,0,:]; outside[:,-1,:]=~d[:,-1,:]; outside[:,:,0]=~d[:,:,0]; outside[:,:,-1]=~d[:,:,-1]
free=~d
for it in range(4000):
    n=dilate1(dilate1(dilate1(outside,0),1),2)&free
    if (n==outside).all(): break
    outside=n
solid=~outside
print("FILLED",solid.sum(),it,time.time()-t0)
skin=erode(solid,ER)      # ends ~ 2 voxels (10mm) beyond the innermost tissue, then smoothing pulls it in a little
f=skin.astype(np.float32)
def blur(a,s,axis):
    k=int(3*s); x=np.arange(-k,k+1); g=np.exp(-x*x/(2*s*s)); g/=g.sum()
    pad=[(0,0)]*3; pad[axis]=(k,k); p=np.pad(a,pad,mode='edge')
    out=np.zeros_like(a)
    for j,w in enumerate(g):
        sl=[slice(None)]*3; sl[axis]=slice(j,j+a.shape[axis]); out+=w*p[tuple(sl)]
    return out
for _ in range(1 if MODE=='hand' else 2):
    for ax in range(3): f=blur(f,1.6,ax)
field=(0.5-f).astype(np.float32)          # negative inside
print("FIELD",field.min(),field.max(),time.time()-t0)
with open(OUTNAME or OUT,'wb') as fh:
    fh.write(struct.pack('<3i3f f',*dims,*mn,V))
    fh.write(field.tobytes(order='C'))
print("DONE")
