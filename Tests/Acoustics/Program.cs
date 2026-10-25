using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AdvancedMod.Contents.Acoustics;
using AdvancedMod.Basic.Graphics;
using Microsoft.Xna.Framework;

var results = new List<object>();
int failures = 0;
void Assert(bool condition) { if (!condition) throw new Exception("断言失败"); }
void Near(float a, float b) => Assert(MathF.Abs(a - b) < 0.0001f);
void Test(string name, Action run)
{
    try { run(); results.Add(new { name, status = "PASS" }); Console.WriteLine($"PASS {name}"); }
    catch (Exception e) { failures++; results.Add(new { name, status = "FAIL", error = e.Message }); }
}
Test("等幅同相相量与预算", () => { var f = SoundRules.Fuse(1, 1, 0, 0); Near(f.Amplitude, 2); Near(f.Coherence, 1); Near(f.Energy, 1.8f); });
Test("等幅反相转移为附着预算", () => { var f = SoundRules.Fuse(1, 1, 0, MathF.PI); Near(f.Amplitude, 0); Near(f.Coherence, 0); Near(f.Energy * SoundRules.DamageFactor(f.Coherence), 0.36f); Near(f.Energy * SoundRules.ElementFactor(f.Coherence), 3.6f); });
Test("正交相位连续过渡", () => { var f = SoundRules.Fuse(1, 1, 0, MathF.PI / 2); Near(f.Coherence, 0.5f); Near(f.Phase, MathF.PI / 4); });
Test("不等幅反相保留残差", () => { var f = SoundRules.Fuse(1, 0.25f, 0, MathF.PI); Near(f.Amplitude, 0.5f); Near(f.Coherence, 1f / 9); });
Test("交换次序不改变伤害附着", () => { var a = SoundRules.Fuse(0.7f, 0.3f, 0.2f, 1.1f); var b = SoundRules.Fuse(0.3f, 0.7f, 1.1f, 0.2f); Near(a.Coherence, b.Coherence); Near(a.Phase, b.Phase); });
Test("相消后再融合不能恢复满振幅", () => { var f = SoundRules.Fuse(1, 1, 0, 0, 0, 0); Near(f.Amplitude, 0); Near(f.Coherence, 0); });
Test("平移不改变会合相位", () => { Near(SoundRules.PhaseAt(new(20, 50), new(4, 50), Vector2.UnitX, 0, 32), MathF.PI); });
Test("镜面反射转向且两次可逆", () => { var n = Vector2.Normalize(new Vector2(1, 1)); var d = SoundRules.Reflect(Vector2.UnitX, n); Near(d.X, 0); Near(d.Y, -1); Near(SoundRules.Reflect(d, n).X, 1); });
Test("正对碰的角度舍入误差不会生成垂直合成方向", () => { var b = new Vector2(MathF.Cos(MathF.PI), MathF.Sin(MathF.PI)); var d = SoundRules.FusionDirection(Vector2.UnitX, b, 0.8f, 0.8f); Near(d.X, 1); Near(d.Y, 0); });
Test("有向包络分离与边缘相交", () => { Assert(SoundRules.Overlap(Vector2.Zero, Vector2.UnitX, new(64), new(128, 0), Vector2.UnitX, new(64))); Assert(!SoundRules.Overlap(Vector2.Zero, Vector2.UnitX, new(64), new(129, 0), Vector2.UnitX, new(64))); });
Test("旋转包络不会漏判角部", () => { Assert(SoundRules.Overlap(Vector2.Zero, Vector2.Normalize(new(1, 1)), new(64), new(80, 0), Vector2.UnitX, new(2))); });
Test("生命周期内衰减与反射损耗有界", () => { float e = 1; for (int i = 0; i < SoundRules.Lifetime; i++) e *= SoundRules.Decay; Assert(e > 0 && e < 0.3f); Assert(MathF.Pow(SoundRules.ReflectionRetention, 4) < 1); });
Test("瞬时效果结束和波包强制清理", () => { var m = new WorldEffectManager(); var pose = new SoundPose(Vector2.Zero, Vector2.UnitX, 0, 32, 1, 1, true); m.Create(new SoundVisual(pose, true)); m.Update(1); Assert(m.Count == 0); var h = m.Create(new SoundVisual(pose)); m.Update(0.1f); m.Kill(h); m.Update(0.1f); Assert(m.Count == 0); });
string path = args.Length > 0 ? args[0] : "sound-offline.json";
Test("实体无OnKill失活时视觉心跳到期清理", () => { var m = new WorldEffectManager(); var pose = new SoundPose(Vector2.Zero, Vector2.UnitX, 0, 32, 1, 1, false); var h = m.Create(new SoundVisual(pose)); for (int i = 0; i < 20; i++) { m.SetTarget(h, pose); m.Update(1f / 60); Assert(m.Count == 1); } m.Update(SoundRules.VisualLeaseSeconds); Assert(m.Count == 0); });
File.WriteAllText(path, JsonSerializer.Serialize(new { results, failures }, new JsonSerializerOptions { WriteIndented = true }));
return failures == 0 ? 0 : 1;
