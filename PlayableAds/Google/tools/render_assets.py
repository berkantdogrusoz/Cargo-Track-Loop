import math
import os
import sys

import bpy
from mathutils import Vector


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUTPUT = os.path.join(ROOT, "PlayableAds", "Google", "assets")

ASSETS = [
    (
        "anubis",
        "Assets/Art/tripo_convert_d47b84d0-cf5a-4452-a58e-47ad933360e2.fbm/egyptian+jackal+3d+model.fbx",
        "Assets/Art/tripo_convert_d47b84d0-cf5a-4452-a58e-47ad933360e2.fbm/egyptian+jackal+3d+model.fbm/egyptian+jackal+3d+model_basecolor.jpg",
        -64.0,
    ),
    (
        "pyramid",
        "Assets/Art/kaya/piramid/pyramid+3d+model.fbx",
        "Assets/Art/kaya/piramid/pyramid+3d+model.fbm/pyramid+3d+model_basecolor.jpg",
        -28.0,
    ),
    (
        "cactus",
        "Assets/Art/kaya/kaktüs/cactus+3d+model.fbx",
        "Assets/Art/kaya/kaktüs/cactus+3d+model.fbm/cactus+3d+model_basecolor.jpg",
        20.0,
    ),
    (
        "bones",
        "Assets/Art/kaya/kemik iskelet/bone+skull+3d+model.fbx",
        "Assets/Art/kaya/kemik iskelet/bone+skull+3d+model.fbm/bone+skull+3d+model_basecolor.jpg",
        -14.0,
    ),
    (
        "rocks",
        "Assets/Art/kaya/rock+pile+3d+model.fbx",
        "Assets/Art/kaya/rock+pile+3d+model.fbm/rock+pile+3d+model_basecolor.jpg",
        24.0,
    ),
]


def resolve_case_insensitive(relative_path):
    current = ROOT
    for part in relative_path.replace("\\", "/").split("/"):
        exact = os.path.join(current, part)
        if os.path.exists(exact):
            current = exact
            continue
        match = next((name for name in os.listdir(current) if name.casefold() == part.casefold()), None)
        if match is None:
            raise FileNotFoundError(relative_path)
        current = os.path.join(current, match)
    return current


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def import_fbx(path):
    try:
        bpy.ops.import_scene.fbx(filepath=path)
    except AttributeError:
        bpy.ops.wm.fbx_import(filepath=path)


def mesh_bounds(objects):
    points = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        raise RuntimeError("Imported FBX contains no mesh")
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def apply_texture(objects, texture_path):
    image = bpy.data.images.load(texture_path, check_existing=True)
    material = bpy.data.materials.new(name="PlayableAssetMaterial")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    for node in list(nodes):
        nodes.remove(node)
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.interpolation = "Linear"
    shader.inputs["Roughness"].default_value = 0.72
    shader.inputs["Metallic"].default_value = 0.0
    links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    links.new(texture.outputs["Alpha"], shader.inputs["Alpha"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    for obj in objects:
        if obj.type != "MESH":
            continue
        obj.data.materials.clear()
        obj.data.materials.append(material)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def setup_render(objects, output_path, yaw_degrees):
    roots = [obj for obj in objects if obj.parent is None]
    pivot = bpy.data.objects.new("RenderPivot", None)
    bpy.context.collection.objects.link(pivot)
    for obj in roots:
        obj.parent = pivot
    pivot.rotation_euler[2] = math.radians(yaw_degrees)

    minimum, maximum = mesh_bounds(objects)
    center = (minimum + maximum) * 0.5
    pivot.location -= Vector((center.x, center.y, minimum.z))
    bpy.context.view_layer.update()
    minimum, maximum = mesh_bounds(objects)
    size = maximum - minimum
    max_size = max(size.x, size.z, size.y * 0.82)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = output_path
    scene.render.resolution_percentage = 100
    scene.view_settings.look = "AgX - Medium High Contrast"

    camera_data = bpy.data.cameras.new("Camera")
    camera = bpy.data.objects.new("Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max_size * 1.22
    target = Vector((0.0, 0.0, size.z * 0.48))
    camera.location = Vector((0.0, -max_size * 3.2, size.z * 1.55))
    look_at(camera, target)

    world = bpy.data.worlds.new("World") if scene.world is None else scene.world
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.72, 0.76, 0.82, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.48

    key_data = bpy.data.lights.new("Key", type="AREA")
    key_data.energy = 620
    key_data.shape = "DISK"
    key_data.size = max_size * 3.0
    key = bpy.data.objects.new("Key", key_data)
    bpy.context.collection.objects.link(key)
    key.location = Vector((-max_size * 2.0, -max_size * 2.2, max_size * 3.4))
    look_at(key, target)

    fill_data = bpy.data.lights.new("Fill", type="AREA")
    fill_data.energy = 230
    fill_data.size = max_size * 2.5
    fill = bpy.data.objects.new("Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = Vector((max_size * 2.2, -max_size * 1.0, max_size * 1.6))
    look_at(fill, target)

    bpy.ops.render.render(write_still=True)


def main():
    os.makedirs(OUTPUT, exist_ok=True)
    for name, model_relative, texture_relative, yaw in ASSETS:
        clear_scene()
        model_path = resolve_case_insensitive(model_relative)
        texture_path = resolve_case_insensitive(texture_relative)
        import_fbx(model_path)
        imported = [obj for obj in bpy.context.scene.objects if obj.type in {"MESH", "EMPTY", "ARMATURE"}]
        apply_texture(imported, texture_path)
        setup_render(imported, os.path.join(OUTPUT, name + ".png"), yaw)
        print("Rendered", name)


if __name__ == "__main__":
    main()
