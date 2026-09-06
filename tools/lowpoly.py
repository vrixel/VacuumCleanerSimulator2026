"""Blender (headless) decimation of a downloaded model into a game-ready FBX.

    "D:\Program Files\Blender\blender-4.5.13-windows-x64\blender.exe" -b --python tools\lowpoly.py -- <in.glb|.gltf|.fbx|.obj> <out.fbx> [target_faces=4000] [size_m=0.6]

Imports the file, joins all meshes, applies a Decimate (collapse) modifier to reach the target face count, scales the
whole model so its largest horizontal extent is size_m and its base sits on y = 0 (Unity convention: origin on the
floor, +z forward is left to the caller), then exports FBX with the textures copied next to it. Prints the face
counts before and after.
"""
import os
import sys

import bpy


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) < 2:
        raise SystemExit("usage: blender -b --python lowpoly.py -- in out.fbx [target_faces] [size_m]")
    src, dst = argv[0], argv[1]
    target = int(argv[2]) if len(argv) > 2 else 4000
    size = float(argv[3]) if len(argv) > 3 else 0.6

    bpy.ops.wm.read_factory_settings(use_empty=True)
    ext = os.path.splitext(src)[1].lower()
    if ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=src)
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=src)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=src)
    else:
        raise SystemExit("unsupported input " + ext)

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        raise SystemExit("no mesh in " + src)
    before = sum(len(o.data.polygons) for o in meshes)

    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.quads_convert_to_tris()
    bpy.ops.object.mode_set(mode="OBJECT")
    faces = len(obj.data.polygons)
    if faces > target:
        mod = obj.modifiers.new("Decimate", "DECIMATE")
        mod.ratio = target / faces
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    after = len(obj.data.polygons)

    # normalise: largest horizontal extent = size, base on the ground, centred
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    horiz = max(max(xs) - min(xs), max(ys) - min(ys))
    s = size / horiz if horiz > 1e-6 else 1.0
    obj.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    obj.location = (-(max(xs) + min(xs)) / 2, -(max(ys) + min(ys)) / 2, -min(zs))
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    os.makedirs(os.path.dirname(os.path.abspath(dst)), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=dst, use_selection=False, path_mode="COPY", embed_textures=False,
                             mesh_smooth_type="FACE", apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y")
    print(f"LOWPOLY {os.path.basename(src)}: {before} -> {after} faces, size {size} m -> {dst}")


main()
