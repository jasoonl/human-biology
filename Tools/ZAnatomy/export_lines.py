import bpy, json, re
from mathutils import Vector
out=[]
def bez(p0,h0,h1,p1,n):
    pts=[]
    for i in range(n+1):
        t=i/n; u=1-t
        pts.append(u*u*u*p0+3*u*u*t*h0+3*u*t*t*h1+t*t*t*p1)
    return pts
for o in bpy.data.objects:
    if o.type!='CURVE': continue
    coll=o.users_collection[0].name if o.users_collection else ''
    if coll[:2] not in ('5:','7:','8:'): continue
    cu=o.data; M=o.matrix_world
    bd=cu.bevel_depth if cu.bevel_depth>0 else 0.001
    lines=[]
    for sp in cu.splines:
        pts=[];rad=[]
        if sp.type=='BEZIER':
            bp=sp.bezier_points
            for i in range(len(bp)-1):
                seg=bez(bp[i].co,bp[i].handle_right,bp[i+1].handle_left,bp[i+1].co,4)
                for k,v in enumerate(seg):
                    if i>0 and k==0: continue
                    t=k/4; r=bp[i].radius*(1-t)+bp[i+1].radius*t
                    pts.append(v); rad.append(r)
        else:
            for p in sp.points:
                pts.append(Vector(p.co[:3])); rad.append(p.radius)
        if len(pts)<2: continue
        w=[M@p for p in pts]
        lines.append({"p":[round(c,5) for q in w for c in (q.x,q.z,q.y)],"r":[round(x,3) for x in rad]})
    if lines:
        out.append({"name":o.name,"coll":coll,"bevel":round(bd,5),"lines":lines})
json.dump(out,open('/private/tmp/za/zana_lines.json','w'))
print("LINES",len(out),sum(len(o['lines']) for o in out))
