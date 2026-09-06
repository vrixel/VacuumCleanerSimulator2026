"""Blender (headless) decimation of a downloaded model into a game-ready OBJ (+ .mtl and PNG textures).

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

    # The game simulates its own power cord: drop any cable, plug or wire the artist modelled, by object name
    # first, then by material name on the faces that remain.
    CABLE = ("cable", "cord", "wire", "plug", "cavo", "kabel", "fil_", "plane", "floor", "ground", "dock", "station", "backdrop")
    dropped = [o for o in meshes if any(k in o.name.lower() for k in CABLE)]
    for o in dropped:
        print("dropping object", o.name)
        bpy.data.objects.remove(o, do_unlink=True)
    meshes = [o for o in meshes if o not in dropped]
    for o in meshes:
        bad = {i for i, slot in enumerate(o.material_slots) if slot.material and any(k in slot.material.name.lower() for k in CABLE)}
        if not bad:
            continue
        print("dropping faces of", o.name, "with materials", [o.material_slots[i].material.name for i in bad])
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="DESELECT")
        bpy.ops.object.mode_set(mode="OBJECT")
        for poly in o.data.polygons:
            poly.select = poly.material_index in bad
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.delete(type="FACE")
        bpy.ops.object.mode_set(mode="OBJECT")
    meshes = [o for o in meshes if len(o.data.polygons) > 0]
    if not meshes:
        raise SystemExit("nothing left after dropping cables in " + src)

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

    out_dir = os.path.dirname(os.path.abspath(dst))
    os.makedirs(out_dir, exist_ok=True)
    # glTF materials often route the base colour through mix / vertex-colour nodes; the OBJ and FBX exporters only
    # see an Image Texture wired straight into the Principled base colour. Rewire every material that way.
    for mat in bpy.data.materials:
        if not mat.use_nodes or mat.node_tree is None:
            continue
        nodes = mat.node_tree.nodes
        bsdf = next((n for n in nodes if n.type == "BSDF_PRINCIPLED"), None)
        imgs = [n for n in nodes if n.type == "TEX_IMAGE" and n.image is not None]
        if bsdf is None or not imgs:
            continue
        # prefer the image feeding (directly or not) the base colour; else the first colour image
        pick = None
        for n in imgs:
            nm = (n.image.name + " " + n.label + " " + n.name).lower()
            if "normal" in nm or "rough" in nm or "metal" in nm or "occlusion" in nm or "emissi" in nm:
                continue
            pick = n
            break
        if pick is None:
            continue
        links = mat.node_tree.links
        for l in list(bsdf.inputs["Base Color"].links):
            links.remove(l)
        links.new(pick.outputs["Color"], bsdf.inputs["Base Color"])
    # glTF textures arrive packed in memory; write them as PNG next to the FBX so Unity's importer finds them by name.
    stem = os.path.splitext(os.path.basename(dst))[0]
    n_tex = 0
    for img in list(bpy.data.images):
        if img.users == 0 or img.size[0] == 0:
            continue
        safe = "".join(ch if ch.isalnum() or ch in "-_" else "_" for ch in os.path.splitext(img.name)[0])
        path = os.path.join(out_dir, f"{stem}_{safe}.png")
        img.filepath_raw = path
        img.file_format = "PNG"
        try:
            img.save()
            n_tex += 1
        except Exception as e:  # noqa: BLE001
            print("texture save failed", img.name, e)
    os.chdir(out_dir)
    # OBJ, not FBX: no axis metadata for Unity to reinterpret. Written Y-up, -Z forward, metres, with a .mtl that
    # points at the PNGs next to it; Unity mirrors X on import, which does not matter for a vacuum.
    if dst.lower().endswith(".fbx"):
        dst = dst[:-4] + ".obj"
    bpy.ops.wm.obj_export(filepath=dst, export_selected_objects=False, forward_axis="NEGATIVE_Z", up_axis="Y",
                          export_materials=True, path_mode="RELATIVE", apply_modifiers=True, export_normals=True,
                          export_uv=True, export_triangulated_mesh=True)
    print(f"textures written: {n_tex}")
    print(f"LOWPOLY {os.path.basename(src)}: {before} -> {after} faces, size {size} m -> {dst}")


main()
