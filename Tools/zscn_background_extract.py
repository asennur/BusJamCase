"""Read the approved preview without saving the Blender source."""
import bpy, json, os, sys
from mathutils import Matrix, Vector

out = sys.argv[sys.argv.index('--') + 1]
os.makedirs(out, exist_ok=True)
c = Matrix(((-1,0,0,0),(0,0,1,0),(0,-1,0,0),(0,0,0,1)))
collection = bpy.data.collections['Onizleme_Yerlesim']
result = {'source': bpy.data.filepath, 'conversion': '(-X, Z, -Y)', 'objects': [], 'cameras': [], 'scene_objects': []}
for obj in bpy.data.objects:
    world = c @ obj.matrix_world @ c.inverted()
    pos, rot, scale = world.decompose()
    item = {'name': obj.name, 'data': obj.data.name if obj.data else '', 'type': obj.type,
            'position': list(pos), 'quaternion_xyzw': list(rot), 'scale': list(scale),
            'matrix': [list(row) for row in world]}
    item['quaternion_xyzw'] = [rot.x, rot.y, rot.z, rot.w]
    if obj.type == 'MESH':
        item['bounds'] = [list(c @ (obj.matrix_world @ Vector(v))) for v in obj.bound_box]
        if obj.name == 'Env_Tree_A':
            result['known_vertices'] = [list(c.to_3x3() @ (obj.matrix_world.to_3x3() @ v.co)) for v in obj.data.vertices]
    if obj.type == 'CAMERA':
        item['camera'] = {'lens': obj.data.lens, 'angle_y': obj.data.angle_y, 'type': obj.data.type,
                          'ortho_scale': obj.data.ortho_scale, 'sensor_width': obj.data.sensor_width}
        result['cameras'].append(item)
    if obj in collection.all_objects[:]:
        result['objects'].append(item)
    result['scene_objects'].append(item)
with open(os.path.join(out, 'EnvironmentKit-layout.json'), 'w', encoding='utf-8') as f:
    json.dump(result, f, indent=2)
print('Extracted', len(result['objects']), 'preview objects and', len(result['cameras']), 'cameras; source not saved.')
