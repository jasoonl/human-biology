import bpy, json, struct, array
dg = bpy.context.evaluated_depsgraph_get()
index=[]; blob=bytearray()
skip_coll=("Reference lines","Cross section","Bonus")
for o in bpy.data.objects:
    if o.type not in ('MESH','CURVE'): continue
    coll=o.users_collection[0].name if o.users_collection else ''
    if coll.startswith(skip_coll): continue
    try:
        eo=o.evaluated_get(dg); me=eo.to_mesh()
    except Exception as e:
        continue
    me.calc_loop_triangles()
    if len(me.vertices)<3 or len(me.loop_triangles)<1:
        eo.to_mesh_clear(); continue
    M=eo.matrix_world
    verts=array.array('f')
    for v in me.vertices:
        p=M@v.co
        verts.extend((p.x,p.z,p.y))          # Blender (x,y,z) -> Unity (x,z,y): left=+X, up=Y, front=-Z
    tris=array.array('i')
    for t in me.loop_triangles:
        a,b,c=t.vertices
        tris.extend((a,c,b))                  # axis swap flips handedness, so flip winding
    index.append({"name":o.name,"coll":coll,"v":len(me.vertices),"t":len(me.loop_triangles),"off":len(blob)})
    blob+=verts.tobytes(); blob+=tris.tobytes()
    eo.to_mesh_clear()
open('/private/tmp/za/zana.bin','wb').write(blob)
json.dump(index,open('/private/tmp/za/zana_index.json','w'))
print("EXPORTED",len(index),len(blob))
