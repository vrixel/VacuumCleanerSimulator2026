"""Blender (headless) decimation of a downloaded model into a game-ready OBJ (+ .mtl and PNG textures).

    "D:\Program Files\Blender\blender-4.5.13-windows-x64\blender.exe" -b --python tools\lowpoly.py -- <in.glb|.gltf|.fbx|.obj> <out.obj> [target_faces=4000] [size_m=0.6] [strip_tubes=0] [side_drop=0] [dense_tubes=0]

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
    # optional: also drop tube-like loose parts (cables fused into the body): surface per length below this
    # fraction of the model size. 0 = off. Philips AquaTrio needs about 0.08.
    strip_tubes = float(argv[4]) if len(argv) > 4 else 0.0
    # optional: drop small tube-like parts hanging beside the body (a cord looped on a hook, its plug): parts whose
    # centre sits farther than this fraction of the model size from the vertical axis, above the base. 0 = off.
    side_drop = float(argv[5]) if len(argv) > 5 else 0.0
    # optional: drop densely tessellated small parts (cable loops modelled as fine tubes, hanging on the body):
    # faces per unit of surface, surface in units of the model size squared. Shell panels sit around 15000-26000
    # on the Philips, its handle grip at 40000, its cord loops above 58000. 0 = off.
    dense_tubes = float(argv[6]) if len(argv) > 6 else 0.0

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

    # Cables fused into the body mesh have no name: they are long, thin ribbons or tubes with almost no surface
    # per unit of length. Split every mesh into loose parts and drop those (and dust-sized fragments).
    import bmesh
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.separate(type="LOOSE")
    bpy.ops.object.mode_set(mode="OBJECT")
    parts = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    H = max(max(o.dimensions) for o in parts)
    from mathutils import Vector
    def wbox(o):
        pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
        return (Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))),
                Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts))))
    lo = Vector((1e9,) * 3); hi = Vector((-1e9,) * 3)
    for o in parts:
        mn, mx = wbox(o)
        lo = Vector((min(lo.x, mn.x), min(lo.y, mn.y), min(lo.z, mn.z))); hi = Vector((max(hi.x, mx.x), max(hi.y, mx.y), max(hi.z, mx.z)))
    axis_x, axis_y = (lo.x + hi.x) / 2, (lo.y + hi.y) / 2
    M = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)   # the whole model, not the largest part
    dropped_parts = 0
    for o in parts:
        d = o.dimensions
        L = (d.x * d.x + d.y * d.y + d.z * d.z) ** 0.5
        bm = bmesh.new()
        bm.from_mesh(o.data)
        area = sum(f.calc_area() for f in bm.faces)
        bm.free()
        ratio = area / max(L, 1e-9)
        thin = L > 0.1 * H and ratio < 0.0024 * H
        tube = strip_tubes > 0 and L > 0.1 * H and ratio < strip_tubes * H and len(o.data.polygons) <= 40   # cable segments are few-face tubes
        dust = L < 0.012 * H
        side = False
        if side_drop > 0:
            mn, mx = wbox(o)
            c = (mn + mx) / 2
            off = ((c.x - axis_x) ** 2 + (c.y - axis_y) ** 2) ** 0.5
            # beside the axis, above the head, small and not a tall structural piece (a rail, a tank wall)
            side = off > side_drop * M and c.z > lo.z + 0.25 * M and len(o.data.polygons) < 300 and max(mx - mn) < 0.3 * M
        dense = False
        if dense_tubes > 0 and L > 0.05 * M and L < 0.3 * M and len(o.data.polygons) < 500 and area > 1e-9:
            dense = len(o.data.polygons) * M * M / area > dense_tubes
        if thin or dust or tube or side or dense:
            if (tube or side or dense) and not thin:
                kind = "side" if side else ("dense" if dense else "tube")
                print(f"dropping {kind} part {o.name} L={L:.3f} area/L={ratio:.4f} faces={len(o.data.polygons)} density={len(o.data.polygons) * M * M / max(area, 1e-9):.0f}")
            bpy.data.objects.remove(o, do_unlink=True)
            dropped_parts += 1
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    print(f"dropped {dropped_parts} thin or dust parts of {len(parts)}")

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
