import bpy, json, os, sys
from mathutils import Matrix

out = sys.argv[sys.argv.index('--') + 1]
os.makedirs(out, exist_ok=True)
# FBX export uses -Z forward / Y up. Unity reflects the resulting Z axis.
conversion = Matrix(((-1,0,0,0),(0,0,1,0),(0,-1,0,0),(0,0,0,1)))
for name in ('EnvironmentKit', 'MenuKit', 'OtobusKutlama', 'Yolcu'):
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath('ArtSource/' + name + '.blend'))
    collection = bpy.data.collections.get('Onizleme_Yerlesim')
    objects = list(collection.all_objects) if collection else list(bpy.data.objects)
    result = {'source': name, 'collection': collection.name if collection else None, 'objects': []}
    result['export_mesh_bounds'] = {}
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            points = [conversion @ (obj.matrix_world @ __import__('mathutils').Vector(v)) for v in obj.bound_box]
            result['export_mesh_bounds'][obj.name] = {'min':[min(v[i] for v in points) for i in range(3)],
                                                      'max':[max(v[i] for v in points) for i in range(3)]}
            if obj.name == 'Env_Tree_A':
                result['known_mesh_vertices'] = [list(conversion.to_3x3() @ (obj.matrix_world.to_3x3() @ v.co)) for v in obj.data.vertices]
    for obj in objects:
        world = conversion @ obj.matrix_world @ conversion.inverted()
        pos, rot, scale = world.decompose()
        item = {'name':obj.name,'type':obj.type,'data':obj.data.name if obj.data else None,
                'blender_world':[list(row) for row in obj.matrix_world],
                'unity_candidate_world':[list(row) for row in world],
                'position':list(pos),'quaternion_xyzw':[rot.x,rot.y,rot.z,rot.w],'scale':list(scale)}
        if obj.type == 'MESH':
            item['blender_bounds'] = [list(obj.matrix_world @ __import__('mathutils').Vector(v)) for v in obj.bound_box]
        if obj.type == 'CAMERA':
            item['camera']={'lens':obj.data.lens,'sensor_width':obj.data.sensor_width,'sensor_fit':obj.data.sensor_fit,
                            'angle_y':obj.data.angle_y,'clip_start':obj.data.clip_start,'clip_end':obj.data.clip_end}
        if obj.type == 'ARMATURE':
            item['bones'] = [{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local)} for b in obj.data.bones]
        result['objects'].append(item)
    with open(os.path.join(out,name+'-layout.json'),'w',encoding='utf-8') as f: json.dump(result,f,indent=2)
