# 2026-10-30：原 Godot Shader 的真实 OpenGL 基准截图。
extends SceneTree
func _initialize():
 call_deferred("run")
func run():
 root.size=Vector2i(1280,900)
 var bg=ColorRect.new()
 bg.color=Color(22.0/255,24.0/255,35.0/255,1)
 bg.size=Vector2(1280,900)
 root.add_child(bg)
 var scene=load("res://cosmic.tscn").instantiate()
 root.add_child(scene)
 scene.position=Vector2(640,450)
 for layer in scene.get_children():
  layer.material.set_shader_parameter("derive_progress",1)
  layer.material.set_shader_parameter("progress",.3)
 for i in range(30): await process_frame
 await RenderingServer.frame_post_draw
 root.get_texture().get_image().save_png(ProjectSettings.globalize_path("res://../../docs/testing/idyllic/godot-original.png"))
 quit()
