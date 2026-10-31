# 2026-10-30：使用 Godot 原生生成器导出原预设的纹理、曲线和原始基准画面。
extends SceneTree
func _initialize():
 call_deferred("run")
func run():
 var scene = load("res://cosmic.tscn").instantiate()
 root.add_child(scene)
 var target = ProjectSettings.globalize_path("res://../../Assets/Idyllic/")
 var exported = {}
 for layer in scene.get_children():
  var mat = layer.material
  mat.set_shader_parameter("derive_progress", 1)
  mat.set_shader_parameter("progress", 0.3)
  for key in ["base_mask", "distort_texture", "mesh_wide_mask", "color_gradient", "custom_a", "custom_b", "custom_c", "custom_d"]:
   var tex = mat.get_shader_parameter(key)
   if tex == null: continue
   var id = tex.resource_path.get_slice("::", 1)
   if exported.has(id): continue
   if tex is NoiseTexture2D:
    if tex.get_image() == null: await tex.changed
   print("Export ", id, " ", tex.get_class())
   var img = tex.get_image()
   if img == null and (tex is GradientTexture1D or tex is GradientTexture2D):
    var width = tex.width
    var height = 1 if tex is GradientTexture1D else tex.height
    img = Image.create(width, height, false, Image.FORMAT_RGBA8)
    for y in range(height):
     for x in range(width):
      var t = float(x) / max(1, width - 1) if height == 1 else float(y) / max(1, height - 1)
      img.set_pixel(x, y, tex.gradient.sample(t))
   if img == null:
    push_error("Missing image " + id)
    quit(1)
    return
   img.convert(Image.FORMAT_RGBA8)
   img.save_png(target + id + ".png")
   var bytes = FileAccess.get_file_as_bytes(target + id + ".png")
   var file = FileAccess.open(target + id + ".png.bin", FileAccess.WRITE)
   file.store_buffer(bytes)
   exported[id] = true
 print("EXPORTED ", exported.size())
 quit()



