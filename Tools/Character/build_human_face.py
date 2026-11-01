"""Build reviewed MPFB face targets without changing the proven zero-pose geometry.

Run Blender --background --factory-startup --disable-autoexec --python this.py --
    NEW_PRODUCTION_DIRECTORY BASELINE_PRODUCTION_DIRECTORY
The baseline is read only. Importing these outputs into Unity is a separate step.
"""
from pathlib import Path
import hashlib
import importlib
import json
import math
import shutil
import struct
import sys
import types

import bpy


TARGETS = ("eyeBlinkLeft", "eyeBlinkRight", "jawOpen", "mouthSmileLeft",
           "mouthSmileRight", "browDownLeft", "browDownRight",
           "eyeLookUpLeft", "eyeLookUpRight", "eyeLookDownLeft", "eyeLookDownRight",
           "eyeLookInLeft", "eyeLookInRight", "eyeLookOutLeft", "eyeLookOutRight")
PACK_HASH = "d113107bd7eb59f3af4df6fc0ec29bfcc593f496d0b336aec14f086a80ce7146"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot():
    rigs = [obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE']
    assert len(rigs) == 1
    bones = {bone.name: tuple(value for row in bone.matrix_local for value in row)
             for bone in rigs[0].data.bones}
    result = {}
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        obj.data.calc_loop_triangles()
        result[obj.name] = {
            'vertices': [tuple(vertex.co) for vertex in obj.data.vertices],
            'polygons': [tuple(poly.vertices) for poly in obj.data.polygons],
            'matrix': tuple(value for row in obj.matrix_world for value in row),
            'triangles': len(obj.data.loop_triangles),
            'weights': [sorted((obj.vertex_groups[group.group].name, float(group.weight))
                               for group in vertex.groups
                               if obj.vertex_groups[group.group].name in bones)
                        for vertex in obj.data.vertices],
        }
    return result, bones


def geometry_digest(meshes, bones):
    hashed = hashlib.sha256()
    for name in sorted(meshes):
        mesh = meshes[name]
        hashed.update(name.encode('utf-8'))
        for vertex in mesh['vertices']:
            hashed.update(struct.pack('<3f', *vertex))
        for polygon in mesh['polygons']:
            hashed.update(struct.pack('<I', len(polygon)))
            hashed.update(struct.pack('<' + 'I' * len(polygon), *polygon))
    for name in sorted(bones):
        hashed.update(name.encode('utf-8'))
        hashed.update(struct.pack('<16f', *bones[name]))
    return hashed.hexdigest()


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if len(args) != 2:
        raise RuntimeError('Pass the new production directory and read-only baseline directory')
    production, baseline = (Path(value).resolve() for value in args)
    assert production != baseline and production.is_dir() and baseline.is_dir()
    output = production / 'export'
    assert not output.exists(), 'Use a new export directory; never overwrite a prior result'
    pack = production / 'faceunits01.zip'
    assert digest(pack) == PACK_HASH, 'Unreviewed face target pack'
    source_blend = baseline / 'Rodja-clothed-source.blend'
    source_digest = digest(source_blend)
    # Reading the proven source gives a measurable contract, not an approximate new body.
    bpy.ops.wm.open_mainfile(filepath=str(source_blend))
    expected_meshes, expected_bones = snapshot()
    assert len(expected_meshes) == 7 and len(expected_bones) == 53
    bpy.ops.wm.read_factory_settings(use_empty=True)

    package = baseline / 'source/mpfb2-2.0.18/src'
    assert (package / 'mpfb/services/faceservice.py').is_file()
    for name, path in [('bl_ext', production), ('bl_ext.vkt_local', package)]:
        module = types.ModuleType(name)
        module.__path__ = [str(path)]
        sys.modules[name] = module
    mpfb = importlib.import_module('bl_ext.vkt_local.mpfb')
    bpy.context.preferences.addons.new().module = 'bl_ext.vkt_local.mpfb'
    mpfb.register()
    from bl_ext.vkt_local.mpfb.services import HumanService, TargetService, FaceService, ExportService, ClothesService
    from bl_ext.vkt_local.mpfb.ui.apply_assets.assetlibrary.assetsettingspanel import ASSET_SETTINGS_PROPERTIES

    macro = TargetService.get_default_macro_info_dict()
    macro.update(gender=1., age=.28, height=.5, muscle=.62, weight=.5, proportions=.5)
    macro['race'] = {'caucasian': 1., 'african': 0., 'asian': 0.}
    human = HumanService.create_human(macro_detail_dict=macro)
    human.name = 'RodjaBody'
    rig = HumanService.add_builtin_rig(human, 'game_engine')
    rig.name = 'RodjaRig'
    assets = baseline / 'selected-assets'
    # The isolated user data is deliberately empty. Set this scene's read-only
    # asset search root so official child interpolation can resolve every MHCLO.
    ASSET_SETTINGS_PROPERTIES.set_value('second_root', str(assets), entity_reference=bpy.context.scene)
    HumanService.set_character_skin(str(assets / 'skins/young_caucasian_male/young_caucasian_male.mhmat'), human, skin_type='MAKESKIN')
    for relative, kind in [
        ('clothes/male_casualsuit01/male_casualsuit01.mhclo', 'Clothes'),
        ('clothes/shoes02/shoes02.mhclo', 'Clothes'),
        ('eyes/low-poly/low-poly.mhclo', 'Eyes'),
        ('eyebrows/eyebrow001/eyebrow001.mhclo', 'Eyebrows'),
        ('hair/short02/short02.mhclo', 'Hair'),
        ('teeth/teeth_base/teeth_base.mhclo', 'Teeth'),
    ]:
        HumanService.add_mhclo_asset(str(assets / relative), human, asset_type=kind, subdiv_levels=0)

    # Bake only the chosen macro body. Load official facial deltas while original
    # MakeHuman vertex numbering still exists, before any clothing mask is applied.
    TargetService.bake_targets(human)
    for name in TARGETS:
        path = production / 'faceunits01/targets/faceunits' / (name + '.target')
        assert path.is_file()
        TargetService.load_target(human, str(path), name=name, weight=0.)
    for child in [obj for obj in bpy.context.scene.objects if obj.type == 'MESH' and obj != human]:
        resolved = ClothesService.find_clothes_absolute_path(child)
        assert resolved and Path(resolved).is_file(), 'Unresolved interpolation source: ' + child.name
    FaceService.interpolate_targets(human)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    for obj in meshes:
        # MPFB's copy/bake/apply-delta method preserves real facial targets through
        # the exact same topology masks used by the proven static export.
        ExportService.bake_modifiers_remove_helpers(obj, bake_masks=True, bake_subdiv=False,
                                                   remove_helpers=obj == human, also_proxy=False)
        for modifier in list(obj.modifiers):
            if modifier.type != 'ARMATURE':
                obj.modifiers.remove(modifier)
        for poly in obj.data.polygons:
            poly.use_smooth = True
        if obj.data.shape_keys:
            for key in obj.data.shape_keys.key_blocks:
                key.value = 0.

    actual_meshes, actual_bones = snapshot()
    assert set(actual_meshes) == set(expected_meshes), 'Mesh identity changed'
    assert set(actual_bones) == set(expected_bones), 'Rig identity changed'
    max_delta = 0.
    for name, current in actual_meshes.items():
        old = expected_meshes[name]
        assert current['polygons'] == old['polygons'], 'Topology changed: ' + name
        assert len(current['vertices']) == len(old['vertices'])
        assert current['weights'] == old['weights'], 'Skin weights changed: ' + name
        max_delta = max(max_delta, max((sum((a-b)**2 for a, b in zip(x, y))**.5
                                      for x, y in zip(current['vertices'], old['vertices'])), default=0.))
        assert max(abs(a-b) for a, b in zip(current['matrix'], old['matrix'])) < 1.e-6
    assert max_delta < 2.e-6, 'Zero-pose geometry drift: ' + str(max_delta)
    rest_delta = max(abs(a-b) for name in actual_bones for a, b in zip(actual_bones[name], expected_bones[name]))
    assert rest_delta < 1.e-6, 'Rest rig changed'
    assert digest(source_blend) == source_digest, 'Baseline source was changed'

    keys = {}
    for obj in meshes:
        keys[obj.name] = {}
        if not obj.data.shape_keys:
            continue
        basis = obj.data.shape_keys.key_blocks['Basis']
        for key in obj.data.shape_keys.key_blocks:
            if key.name == 'Basis':
                continue
            lengths = [(vertex.co - basis.data[index].co).length for index, vertex in enumerate(key.data)]
            assert all(math.isfinite(value) for value in lengths)
            keys[obj.name][key.name] = {'movedVertices': sum(value > 1.e-6 for value in lengths),
                                       'maximumDeltaMeters': max(lengths, default=0.)}
    assert all(keys['RodjaBody'].get(name, {}).get('movedVertices', 0) > 0 for name in TARGETS[:7])
    assert all(keys['RodjaBody.low-poly'].get(name, {}).get('movedVertices', 0) > 0 for name in TARGETS[7:]), 'Eyes need real mesh deltas'

    output.mkdir()
    materials = []
    for material in [value for value in bpy.data.materials if value.users > 0]:
        images = [node.image for node in material.node_tree.nodes if node.type == 'TEX_IMAGE' and node.image]
        diffuse = next((image for image in images if 'normal' not in image.name.lower() and '_ao' not in image.name.lower()), None)
        normal = next((image for image in images if 'normal' in image.name.lower()), None)
        alpha = any(value in material.name.lower() for value in ('short02', 'eyebrow'))
        entry = {'name': material.name, 'diffuse': None, 'normal': None, 'alphaClip': alpha}
        material.node_tree.nodes.clear()
        nodes = material.node_tree.nodes
        bsdf = nodes.new('ShaderNodeBsdfPrincipled')
        bsdf.inputs['Roughness'].default_value = .72
        surface = nodes.new('ShaderNodeOutputMaterial')
        material.node_tree.links.new(bsdf.outputs['BSDF'], surface.inputs['Surface'])
        for image, role in [(diffuse, 'diffuse'), (normal, 'normal')]:
            if image is None:
                continue
            source = Path(bpy.path.abspath(image.filepath))
            assert source.is_file()
            destination = output / source.name
            if destination.exists():
                assert destination.read_bytes() == source.read_bytes(), 'Texture collision'
            shutil.copy2(source, destination)
            entry[role] = source.name
            texture = nodes.new('ShaderNodeTexImage')
            texture.image = image
            if role == 'diffuse':
                material.node_tree.links.new(texture.outputs['Color'], bsdf.inputs['Base Color'])
                if alpha:
                    material.node_tree.links.new(texture.outputs['Alpha'], bsdf.inputs['Alpha'])
            else:
                image.colorspace_settings.name = 'Non-Color'
                normal_map = nodes.new('ShaderNodeNormalMap')
                material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
                material.node_tree.links.new(normal_map.outputs['Normal'], bsdf.inputs['Normal'])
        materials.append(entry)

    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    # Non-armature modifiers have already been baked. Applying modifiers during
    # FBX export would discard shape keys, so export the verified mesh data directly.
    bpy.ops.export_scene.fbx(filepath=str(output / 'RodjaSource.fbx'), use_selection=True,
                             object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             bake_anim=False, axis_forward='-Z', axis_up='Y', path_mode='COPY',
                             embed_textures=False, use_mesh_modifiers=False)
    bpy.ops.export_scene.gltf(filepath=str(output / 'RodjaSource.glb'), use_selection=True,
                              export_format='GLB', export_animations=False, export_morph=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(production / 'Rodja-face-source.blend'))
    manifest = dict(generator='MPFB 2.0.18 / Blender 4.5.9', license='CC0-1.0', rig='game_engine',
                    meshes=[dict(name=name, vertices=len(mesh['vertices']), triangles=mesh['triangles'])
                            for name, mesh in actual_meshes.items()],
                    triangles=sum(mesh['triangles'] for mesh in actual_meshes.values()), bones=len(actual_bones),
                    zeroPoseMaximumDeltaMeters=max_delta, restRigMaximumDelta=rest_delta,
                    topologyUnchanged=True, skinWeightsUnchanged=True, targets=keys, materials=materials,
                    baselineBlendSha256=source_digest, baselineGeometrySha256=geometry_digest(expected_meshes, expected_bones),
                    generatedGeometrySha256=geometry_digest(actual_meshes, actual_bones),
                    targetPack=json.loads((production / 'faceunits-source.json').read_text()),
                    fbx_sha256=digest(output / 'RodjaSource.fbx'), glb_sha256=digest(output / 'RodjaSource.glb'))
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    print('VKT_FACE_ASSET ' + json.dumps({key: manifest[key] for key in ['triangles','bones','zeroPoseMaximumDeltaMeters','restRigMaximumDelta','fbx_sha256','glb_sha256']}), flush=True)


if __name__ == '__main__':
    main()
