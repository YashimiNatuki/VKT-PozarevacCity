from pathlib import Path
import bpy, sys, types, importlib, json, shutil, hashlib
from mathutils import Vector

# Run Blender with --python build_human.py -- <isolated-production-directory>.
# The directory must contain the reviewed MPFB source and selected CC0 assets.
if '--' not in sys.argv or len(sys.argv) <= sys.argv.index('--') + 1:
    raise RuntimeError('Pass the isolated production directory after --')
ROOT = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
if not (ROOT / 'source/mpfb2-2.0.18/src/mpfb').is_dir():
    raise RuntimeError('Reviewed MPFB 2.0.18 source missing')
ASSETS = ROOT / 'selected-assets'
OUT = ROOT / 'export'
OUT.mkdir(exist_ok=True)
for name, path in [('bl_ext', ROOT), ('bl_ext.vkt_local', ROOT / 'source/mpfb2-2.0.18/src')]:
    module = types.ModuleType(name)
    module.__path__ = [str(path)]
    sys.modules[name] = module
mpfb = importlib.import_module('bl_ext.vkt_local.mpfb')
bpy.context.preferences.addons.new().module = 'bl_ext.vkt_local.mpfb'
mpfb.register()
from bl_ext.vkt_local.mpfb.services import HumanService, TargetService

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
macro = TargetService.get_default_macro_info_dict()
macro.update(gender=1., age=.28, height=.5, muscle=.62, weight=.5, proportions=.5)
macro['race'] = {'caucasian': 1., 'african': 0., 'asian': 0.}
human = HumanService.create_human(macro_detail_dict=macro)
human.name = 'RodjaBody'
rig = HumanService.add_builtin_rig(human, 'game_engine')
rig.name = 'RodjaRig'
HumanService.set_character_skin(str(ASSETS / 'skins/young_caucasian_male/young_caucasian_male.mhmat'), human, skin_type='MAKESKIN')
parts = [
 ('clothes/male_casualsuit01/male_casualsuit01.mhclo', 'Clothes'),
 ('clothes/shoes02/shoes02.mhclo', 'Clothes'),
 ('eyes/low-poly/low-poly.mhclo', 'Eyes'),
 ('eyebrows/eyebrow001/eyebrow001.mhclo', 'Eyebrows'),
 ('hair/short02/short02.mhclo', 'Hair'),
 ('teeth/teeth_base/teeth_base.mhclo', 'Teeth')]
for relative, kind in parts:
    HumanService.add_mhclo_asset(str(ASSETS / relative), human, asset_type=kind, subdiv_levels=0)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
manifest = {'generator': 'MPFB 2.0.18 / Blender 4.5.9', 'rig': 'game_engine', 'license': 'CC0-1.0', 'materials': [], 'meshes': []}
for obj in meshes:
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    # Bake the chosen body shape and clothing masks; retain skinning weights.
    if obj.data.shape_keys:
        bpy.ops.object.shape_key_remove(all=True, apply_mix=True)
    for mod in list(obj.modifiers):
        if mod.type == 'MASK':
            bpy.ops.object.modifier_apply(modifier=mod.name)
        elif mod.type not in {'ARMATURE'}:
            obj.modifiers.remove(mod)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    obj.data.calc_loop_triangles()
    manifest['meshes'].append({'name': obj.name, 'vertices': len(obj.data.vertices), 'triangles': len(obj.data.loop_triangles)})

# Resolve the source images into a small portable PBR material set.
for material in [m for m in bpy.data.materials if m.users > 0]:
    images = [n.image for n in material.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image]
    diffuse = next((im for im in images if 'normal' not in im.name.lower() and '_ao' not in im.name.lower()), None)
    normal = next((im for im in images if 'normal' in im.name.lower()), None)
    alpha = any(x in material.name.lower() for x in ('short02', 'eyebrow'))
    entry = {'name': material.name, 'diffuse': None, 'normal': None, 'alphaClip': alpha}
    material.node_tree.nodes.clear()
    nodes = material.node_tree.nodes
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = .72
    output = nodes.new('ShaderNodeOutputMaterial')
    material.node_tree.links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    for im, role in [(diffuse, 'diffuse'), (normal, 'normal')]:
        if im is None:
            continue
        source = Path(bpy.path.abspath(im.filepath))
        if not source.is_file():
            raise RuntimeError('Missing texture: ' + str(source))
        dest = OUT / source.name
        if dest.exists() and dest.read_bytes() != source.read_bytes():
            raise RuntimeError('Texture name collision: ' + source.name)
        shutil.copy2(source, dest)
        entry[role] = source.name
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = im
        if role == 'diffuse':
            material.node_tree.links.new(texture.outputs['Color'], bsdf.inputs['Base Color'])
            if alpha:
                material.node_tree.links.new(texture.outputs['Alpha'], bsdf.inputs['Alpha'])
        else:
            im.colorspace_settings.name = 'Non-Color'
            normal_node = nodes.new('ShaderNodeNormalMap')
            material.node_tree.links.new(texture.outputs['Color'], normal_node.inputs['Color'])
            material.node_tree.links.new(normal_node.outputs['Normal'], bsdf.inputs['Normal'])
    manifest['materials'].append(entry)

triangles = sum(m['triangles'] for m in manifest['meshes'])
assert 10000 <= triangles <= 80000, triangles
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
for obj in meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(OUT / 'RodjaSource.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=False, use_mesh_modifiers=True)
bpy.ops.export_scene.gltf(filepath=str(OUT / 'RodjaSource.glb'), use_selection=True, export_format='GLB', export_animations=False)
manifest['triangles'] = triangles
manifest['bones'] = len(rig.data.bones)
manifest['fbx_sha256'] = hashlib.sha256((OUT / 'RodjaSource.fbx').read_bytes()).hexdigest()
(OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Rodja-clothed-source.blend'))

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.render.resolution_x = 720
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.world.color = (.13, .13, .13)
for location, power, size in [((3,-4,4), 650, 4), ((-3,-2,2.5), 400, 3), ((0,3,3), 700, 3)]:
    bpy.ops.object.light_add(type='AREA', location=location)
    light = bpy.context.object
    light.data.energy = power
    light.data.shape = 'DISK'
    light.data.size = size
    light.rotation_euler = (Vector((0,0,1)) - light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(0,-3.9,1.05))
camera = bpy.context.object
camera.rotation_euler = (Vector((0,0,.95)) - camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.lens = 50
scene.camera = camera
scene.render.filepath = str(ROOT / 'human-front.png')
bpy.ops.render.render(write_still=True)
camera.location = (.2,-1.1,1.65)
camera.rotation_euler = (Vector((0,0,1.62)) - camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath = str(ROOT / 'human-face.png')
bpy.ops.render.render(write_still=True)
print('VKT_ASSET', json.dumps(manifest), flush=True)
