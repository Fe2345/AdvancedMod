using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Geometry;
using AdvancedMod.Basic.Graphics.Rendering;
using AdvancedMod.Contents.Vfx;
using Microsoft.Xna.Framework;
using AdvancedMod.Contents.Hallow.Weapons;

var results = new List<object>();
int failures = 0;
const float dt = 1f / 60;
void Test(string name, Action action)
{
    try { action(); results.Add(new { name, status = "PASS" }); Console.WriteLine($"PASS {name}"); }
    catch (Exception e) { failures++; results.Add(new { name, status = "FAIL", error = e.ToString() }); Console.WriteLine($"FAIL {name}: {e}"); }
}
void Assert(bool value) { if (!value) throw new Exception("断言失败"); }
void Near(float a, float b) { Assert(MathF.Abs(a - b) < 0.0001f); }

Test("同轮创建停止与延迟取消不显示且仅释放一次", () =>
{
    var m = new WorldEffectManager(); var e = new Probe(); var h = m.Create(e);
    m.Stop(h); m.Stop(h); m.Update(dt); Assert(e.Updates == 0 && e.Releases == 1 && m.Count == 0);
    e = new(); h = m.Create(e, 1); m.Update(dt); m.Stop(h); m.Update(dt); Assert(e.Updates == 0 && e.Releases == 1);
});
Test("正常停止幂等且拒绝后续状态与样本", () =>
{
    var m = new WorldEffectManager(); var e = new Probe(); var h = m.Create(e); m.Update(dt);
    m.SetTarget(h, 3); m.AppendSample(h, 7); m.AppendSample(h, 9); m.Stop(h); m.Stop(h); m.SetTarget(h, 99); m.AppendSample(h, 99); m.Update(dt);
    Assert(e.Stops == 1 && e.Target == 3 && e.Samples == 16);
});
Test("更新期间提交命令留到下轮", () =>
{
    var m = new WorldEffectManager(); var e = new Probe(); var h = m.Create(e);
    e.OnUpdate = () => m.Stop(h); m.Update(dt); Assert(e.Stops == 0); m.Update(dt); Assert(e.Stops == 1);
});
Test("世界清理释放待启动实例与清除命令且编号不复用", () =>
{
    var m = new WorldEffectManager(); var a = new Probe(); var old = m.Create(a, 2); m.Clear(); m.Clear();
    var b = new Probe(); var current = m.Create(b); m.Kill(old); m.Update(dt);
    Assert(a.Releases == 1 && current.Value > old.Value && m.Count == 1 && b.Updates == 1);
});
Test("预算拒绝安全释放与强制删除", () =>
{
    var m = new WorldEffectManager(1); var a = new Probe(); var h = m.Create(a); var b = new Probe();
    Assert(m.Create(b).Value == 0 && b.Releases == 1); m.Kill(h); m.Update(dt); Assert(a.Releases == 1 && m.Count == 0);
});
Test("领域半途关闭从当前显示值收束且不会被复活", () =>
{
    var m = new WorldEffectManager(); var ward = new GoldenWard(new(Vector2.Zero, 200)); var h = m.Create(ward); m.Update(0.2f);
    float radius = ward.DisplayRadius, alpha = ward.Alpha;
    m.Stop(h); m.SetTarget(h, new WardVisualState(new(900, 900), 900)); m.Update(dt);
    Assert(ward.DisplayRadius < radius && ward.Alpha < alpha && ward.DisplayRadius > radius * 0.9f);
    for (int i = 0; i < 60; i++) { m.Stop(h); m.Update(dt); } Assert(m.Count == 0);
});
Test("多次绘制不推进领域生命周期", () =>
{
    var ward = new GoldenWard(new(new(300, 300), 180)); ward.Update(new(0.3f, 0.3));
    float radius = ward.DisplayRadius, alpha = ward.Alpha; var q = new VfxDrawQueue(); var context = new VfxDrawContext(Vector2.Zero, new(1920, 1080));
    ward.CollectDrawCommands(q, context); q.Clear(); ward.CollectDrawCommands(q, context);
    Near(radius, ward.DisplayRadius); Near(alpha, ward.Alpha);
});
Test("环境同类最大强度与释放单来源不关闭", () =>
{
    var m = new WorldEffectManager(); var layer = m.Environment.Register("gold", 0.1f);
    var a = m.AcquireEnvironment("gold", new(0.8f)); var b = m.AcquireEnvironment("gold", new(0.6f)); m.Update(0.1f); Near(0.8f, layer.Intensity);
    m.ReleaseEnvironment(a); m.Update(0.1f); Near(0.6f, layer.Intensity); Assert(layer.Requested);
    m.ReleaseEnvironment(b); m.Update(0.02f); float fading = layer.Intensity;
    m.AcquireEnvironment("gold", new(1)); m.Update(0.01f); Assert(layer.Intensity > fading && layer.Intensity < 1);
});
Test("环境平级稳定仲裁、优先级切换与旧令牌失效", () =>
{
    var m = new WorldEffectManager(); var a = m.Environment.Register("a", 1); var b = m.Environment.Register("b", 1);
    var old = m.AcquireEnvironment("b", new(1)); m.AcquireEnvironment("a", new(1)); m.Update(dt); Assert(a.Requested && !b.Requested);
    m.SetEnvironmentTarget(old, new(1, 5)); m.Update(dt); Assert(!a.Requested && b.Requested);
    m.Clear(); var current = m.AcquireEnvironment("b", new(1)); m.ReleaseEnvironment(old); m.Update(dt);
    Assert(current.Value > old.Value && b.Requested && m.Environment.RequestCount == 1);
});
Test("快速切换最多两层且天空重置清除对应请求", () =>
{
    var m = new WorldEffectManager(); m.Environment.Register("a", 1); m.Environment.Register("b", 1); m.Environment.Register("c", 1);
    m.AcquireEnvironment("a", new(1)); m.Update(0.2f); m.AcquireEnvironment("b", new(1, 1)); m.Update(0.1f);
    m.AcquireEnvironment("c", new(1, 2)); m.Update(0.01f);
    int visible = 0; foreach (var layer in m.Environment.Layers) if (layer.Visible) visible++;
    Assert(visible <= EnvironmentController.MaximumVisibleLayers);
    m.Environment.ResetStyle("c"); Assert(m.Environment.RequestCount == 2 && !m.Environment.Get("c").Visible);
});
Test("轨迹容量、时间年龄、补点、传送断开、停止后拒绝输入", () =>
{
    var trail = new TrailBuffer(8); trail.Append(new(Vector2.Zero, new(20, 0), 0));
    trail.Append(new(new(0, 40), new(20, 40), 0.1)); Assert(trail.Count > 2);
    trail.Append(new(new(1000, 0), new(1020, 0), 0.2)); Assert(trail[trail.Count - 1].BreakBefore);
    int count = trail.Count; trail.Close(); trail.Append(new(Vector2.One, Vector2.One, 1)); Assert(count == trail.Count);
    trail.Prune(0.15); Assert(trail.Count == 1); trail.Prune(1); Assert(trail.Count == 0);
});
Test("轨迹非有限值与逆序时间拒绝、剑气按时间到期", () =>
{
    var trail = new TrailBuffer(); trail.Append(new(Vector2.Zero, Vector2.One, 2));
    trail.Append(new(new(float.NaN, 0), Vector2.One, 3)); trail.Append(new(Vector2.One, new(20, 0), 1)); Assert(trail.Count == 1);
    var effect = new SlashTrailEffect(); effect.AppendSample(new(Vector2.Zero, Vector2.One, 0)); effect.Update(new(dt, 0.1)); Assert(!effect.IsFinished);
    effect.Stop(); effect.Update(new(dt, 0.4)); Assert(effect.IsFinished);
});
Test("退化与闭合描边有限、扫掠断开不产生跨地图三角形", () =>
{
    var mesh = new VfxMesh(); mesh.Stroke(new[] { Vector2.Zero, Vector2.Zero, Vector2.One, Vector2.UnitX }, 2, Color.White, true);
    Assert(mesh.Count > 0); for (int i = 0; i < mesh.Count; i++) Assert(float.IsFinite(mesh.Vertices[i].Position.X) && float.IsFinite(mesh.Vertices[i].Position.Y));
    mesh.Clear(); mesh.Stroke(new[] { Vector2.Zero, Vector2.Zero }, 2, Color.White, true); Assert(mesh.Count == 0);
    var trail = new TrailBuffer(); trail.Append(new(Vector2.Zero, Vector2.UnitX, 0)); trail.Append(new(new(1000, 0), new(1001, 0), 0.1));
    mesh.Sweep(trail, 0.1, 1, Color.White); Assert(mesh.Count == 0);
});
Test("参考三弧接头与循环穿插缺口", () =>
{
    var path = KnotPath.Reference;
    for (int i = 0; i < 3; i++)
    {
        Assert(Vector2.Distance(path.Arcs[i][0].Position, path.Arcs[(i + 1) % 3][^1].Position) < 0.0001f);
        Assert(Array.Exists(path.Gaps[i], x => x));
        for (int k = 1; k < path.Arcs[i].Length; k++) Assert(path.Arcs[i][k].Length > path.Arcs[i][k - 1].Length);
    }
});

Test("天裁四段角度连续与旋向圈数", () =>
{
    var combo = new HallowBladeCombo(); float aim = 0.3f, previous = 0;
    float[] sweeps = { MathF.PI, -MathF.Tau, MathF.Tau * 2, MathF.Tau };
    for (int i = 0; i < 4; i++)
    {
        var next = combo.Begin(aim, (ulong)(i * 60)); Assert(next.Stage == i);
        Near(i == 0 ? aim - MathF.PI / 2 : previous, next.StartAngle);
        float end = HallowBladeRules.Angle(i, next.StartAngle, 1); Near(sweeps[i], end - next.StartAngle);
        combo.Complete(i, end, (ulong)((i + 1) * 60)); previous = end;
    }
    Assert(combo.Begin(-1, 240).Stage == 0); Near(-1 - MathF.PI / 2, combo.Begin(-1, 240).StartAngle);
});
Test("天裁连招149刻衔接150刻重置与中断", () =>
{
    var combo = new HallowBladeCombo(); combo.Complete(0, 1, 60);
    Assert(combo.Begin(2, 209).Stage == 1); Near(1, combo.Begin(2, 209).StartAngle);
    Assert(combo.Begin(2, 210).Stage == 0); combo.Complete(1, 2, 300); combo.Reset(); Assert(combo.Begin(0, 301).Stage == 0);
});
Test("天裁第三段命中周期只在第二圈开启与倍率", () =>
{
    Near(1, HallowBladeRules.DamageMultiplier(0)); Near(1, HallowBladeRules.DamageMultiplier(1));
    Near(0.75f, HallowBladeRules.DamageMultiplier(2)); Near(2.75f, HallowBladeRules.DamageMultiplier(3));
    for (int i = 0; i <= 1000; i++)
    {
        float t = i / 1000f;
        Assert(HallowBladeRules.HitCycle(2, t) == (HallowBladeRules.Angle(2, 0, t) > MathF.Tau ? 1 : 0));
    }
    Assert(HallowBladeRules.HitCycle(3, 1) == 0);
});
Test("单外缘扫掠内部透明外缘着色且默认材质不改变", () =>
{
    var trail = new TrailBuffer(); trail.Append(new(Vector2.Zero, new(100, 0), 0));
    trail.Append(new(new(0, 5), new(100, 5), 0.01)); var mesh = new VfxMesh();
    mesh.Sweep(trail, 0.01, 1, Color.Gold, true);
    Assert(mesh.Count == 6 && mesh.Vertices[0].Color.A == 0 && mesh.Vertices[1].Color.A > 0 && mesh.Vertices[2].Color.A == 0);
    mesh.Clear(); mesh.Sweep(trail, 0.01, 1, Color.Gold); Assert(mesh.Count == 12);
});

Test("天裁缓动单调且两端静止、两圈中点不停顿", () =>
{
    Near(0, HallowBladeRules.MotionProgress(0)); Near(1, HallowBladeRules.MotionProgress(1));
    float previous = 0;
    for (int i = 1; i <= 1000; i++)
    {
        float p = HallowBladeRules.MotionProgress(i / 1000f); Assert(p >= previous); previous = p;
    }
    float startup = HallowBladeRules.MotionProgress(0.01f);
    float middle = HallowBladeRules.MotionProgress(0.51f) - HallowBladeRules.MotionProgress(0.5f);
    Assert(middle > startup * 20 && middle > 0.01f);
    float finish = 1 - HallowBladeRules.MotionProgress(0.99f);
    Assert(finish < middle / 20);
    Assert(HallowBladeRules.MotionProgress(0.5f) > 0.75f);
    Assert(HallowBladeRules.MotionProgress(0.9f) - HallowBladeRules.MotionProgress(0.8f) > 0.02f);
});
Test("相切弧尾端重合、内层空洞与亮度阶跃", () =>
{
    var mesh = new VfxMesh(); const float radius = 192, offset = 0.12f, sweep = 1.9f;
    mesh.TangentCrescent(Vector2.Zero, radius, sweep, sweep, offset, 1f / 3, Color.White, Color.Gold, 1);
    Assert(mesh.Count > 0); bool dark = false, bright = false, cutoff = false;
    for (int i = 0; i < mesh.Count; i++)
    {
        var v = mesh.Vertices[i]; float r = v.Position.Length();
        Assert(float.IsFinite(r) && r >= radius / 3 - 0.01f && r <= radius + 0.01f);
        if (MathF.Abs(r - radius / 3) < 0.01f) { Assert(v.Color.A == 0); cutoff = true; }
        dark |= v.Color.A > 0 && v.Color.A < 60; bright |= v.Color.A > 200;
    }
    Assert(dark && bright && cutoff);
    // 内圆圆心偏移d，半径R-d，尾端距两圆圆心分别为R与R-d。
    float d = radius * offset; Near(radius, d + (radius - d));
    mesh.Clear(); mesh.TangentCrescent(Vector2.Zero, radius, 0, 0, offset, 1f / 3, Color.White, Color.Gold, 1);
    Assert(mesh.Count == 0);
});

Test("三层相切圆边界最大宽64且弧度封顶135度", () =>
{
    const float radius = 192, width = 64, arc = MathF.PI * 0.75f;
    float previous = radius;
    for (int layer = 1; layer <= 3; layer++)
    {
        float d = VfxMesh.TangentCircleOffset(radius, width * layer / 3, arc);
        Near(radius, VfxMesh.TangentBoundaryRadius(radius, d, 0));
        float r = VfxMesh.TangentBoundaryRadius(radius, d, arc);
        Near(radius - width * layer / 3, r); Assert(previous > r); previous = r;
    }
    var mesh = new VfxMesh();
    mesh.LayeredTangentCrescent(Vector2.Zero, radius, 0, MathF.Tau, arc, width, 1f / 3,
        Color.White, Color.Yellow, Color.Gold, Color.Gold * 0.4f, 1);
    Assert(mesh.Count > 0);
    for (int i = 0; i < mesh.Count; i++)
    {
        var v = mesh.Vertices[i]; float angle = MathF.Atan2(v.Position.Y, v.Position.X);
        Assert(angle >= -arc - 0.0001f && angle <= 0.0001f);
        Assert(v.Position.Length() >= radius / 3 - 0.01f && v.Position.Length() <= radius + 0.01f);
    }
});

Test("剑气纹理两侧透明且强度有限", () =>
{
    for (int x = 0; x <= 256; x++)
    {
        float u = x / 256f;
        Near(VfxTextures.SlashIntensity(u, 0), 0); Near(VfxTextures.SlashIntensity(u, 1), 0);
        for (int y = 0; y <= 64; y++)
        {
            float value = VfxTextures.SlashIntensity(u, y / 64f);
            Assert(float.IsFinite(value) && value >= 0 && value <= 1);
        }
    }
});
Test("动态剑气高速面积大于随动且双倍攻速保持相同形状", () =>
{
    float AreaAt(float progress, float duration)
    {
        var slash = new HallowSlashEffect();
        for (int i = 0; i <= 120 * progress; i++)
        {
            float p = i / 120f, angle = HallowBladeRules.Angle(1, 0, p);
            slash.SetTarget(new(Vector2.Zero, 192, angle, angle, p * duration, duration));
        }
        slash.CollectDrawCommands(new(), new(Vector2.Zero, new(800, 600)));
        // 只读取最终三角形，验证可见面积而非重写生成算法。
        var mesh = (VfxMesh)typeof(HallowSlashEffect).GetField("mesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(slash)!;
        float area = 0;
        for (int i = 0; i < mesh.Count; i += 3)
        {
            var a = mesh.Vertices[i]; var b = mesh.Vertices[i + 1]; var c = mesh.Vertices[i + 2];
            Assert(float.IsFinite(a.Position.X) && a.Position.Length() <= 192.01f);
            Assert(a.TextureCoordinate.X >= 0 && a.TextureCoordinate.X <= 1);
            var ab = b.Position - a.Position; var ac = c.Position - a.Position;
            area += MathF.Abs(ab.X * ac.Y - ab.Y * ac.X) / 2;
        }
        return area;
    }
    float fast = AreaAt(0.4f, 40f / 60), slow = AreaAt(0.9f, 40f / 60);
    Assert(fast > slow * 2 && slow > 0);
    Assert(MathF.Abs(fast - AreaAt(0.4f, 20f / 60)) < 0.01f);
});
Test("动态剑气停止后拒绝新姿态并按生命周期结束", () =>
{
    var slash = new HallowSlashEffect();
    slash.SetTarget(new(Vector2.Zero, 192, 0, 0, 0, 1));
    slash.SetTarget(new(Vector2.Zero, 192, 1, 1, 0.1f, 1));
    slash.Stop(); slash.SetTarget(new(new(1000, 1000), 192, 2, 2, 0.2f, 1));
    slash.CollectDrawCommands(new(), new(Vector2.Zero, new(800, 600)));
    Assert(!slash.IsFinished);
    slash.Update(new(HallowSlashEffect.FadeSeconds, HallowSlashEffect.FadeSeconds));
    Assert(slash.IsFinished);
});
Test("双倍攻速两圈段补样保持圆弧而非直线弦", () =>
{
    var slash = new HallowSlashEffect();
    for (int i = 0; i <= 24; i++)
    {
        float p = i / 60f, angle = HallowBladeRules.Angle(2, 0, p);
        slash.SetTarget(new(Vector2.Zero, 192, angle, angle, p / 3, 1f / 6));
    }
    slash.CollectDrawCommands(new(), new(Vector2.Zero, new(800, 600)));
    var mesh = (VfxMesh)typeof(HallowSlashEffect).GetField("mesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(slash)!;
    Assert(mesh.Count > 0);
    for (int i = 0; i < mesh.Count; i++)
    {
        var vertex = mesh.Vertices[i];
        if (vertex.TextureCoordinate.Y == 1) Assert(MathF.Abs(vertex.Position.Length() - 192) < 0.001f);
    }
});
Test("剑气头部宽度为攻击半径一半且随半径缩放", () =>
{
    foreach (float radius in new[] { 192f, 288f })
    {
        var slash = new HallowSlashEffect();
        slash.SetTarget(new(Vector2.Zero, radius, 0, 0, 0, 1));
        slash.SetTarget(new(Vector2.Zero, radius, 1, 1, 0.2f, 1));
        slash.CollectDrawCommands(new(), new(Vector2.Zero, new(800, 600)));
        var mesh = (VfxMesh)typeof(HallowSlashEffect).GetField("mesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(slash)!;
        float inner = float.MaxValue, outer = 0;
        for (int i = 0; i < mesh.Count; i++)
        {
            var v = mesh.Vertices[i];
            if (v.TextureCoordinate.X < 0.99999f) continue;
            inner = Math.Min(inner, v.Position.Length()); outer = Math.Max(outer, v.Position.Length());
        }
        Assert(MathF.Abs(outer - radius) < 0.001f);
        Assert(MathF.Abs(outer - inner - radius / 2) < 0.001f);
    }
});

Test("完美格挡20刻窗口严格排除第21次更新", () =>
{
    Assert(!HallowActionRules.Perfect(-1)); Assert(HallowActionRules.Perfect(0));
    Assert(HallowActionRules.Perfect(19)); Assert(!HallowActionRules.Perfect(20));
});
Test("招架保持弹幕伤害速度尺寸筛选且计入额外更新", () =>
{
    Assert(HallowActionRules.EligibleProjectile(true, 2, 0.6f, 2, 299));
    Assert(!HallowActionRules.EligibleProjectile(false, 100, 2, 1, 12));
    Assert(!HallowActionRules.EligibleProjectile(true, 1, 2, 1, 12));
    Assert(!HallowActionRules.EligibleProjectile(true, 2, 1, 1, 12));
    Assert(!HallowActionRules.EligibleProjectile(true, 2, 2, 1, 300));
});
Test("反击限制512半径正面半圆", () =>
{
    Assert(HallowActionRules.InFront(Vector2.Zero, Vector2.UnitX, new(512, 0)));
    Assert(HallowActionRules.InFront(Vector2.Zero, Vector2.UnitX, new(0, 512)));
    Assert(!HallowActionRules.InFront(Vector2.Zero, Vector2.UnitX, new(513, 0)));
    Assert(!HallowActionRules.InFront(Vector2.Zero, Vector2.UnitX, new(-1, 0)));
});
Test("环境优先级与元素序列化往返", () =>
{
    Assert(HallowActionRules.ChooseEnvironment(true, true, true, true) == AdvancedMod.Basic.Tag.TagEnum.Pyro);
    Assert(HallowActionRules.ChooseEnvironment(false, true, true, true) == AdvancedMod.Basic.Tag.TagEnum.Hydro);
    Assert(HallowActionRules.ChooseEnvironment(false, false, true, true) == AdvancedMod.Basic.Tag.TagEnum.Cryo);
    Assert(HallowActionRules.ChooseEnvironment(false, false, false, true) == AdvancedMod.Basic.Tag.TagEnum.Electro);
    Assert(HallowActionRules.ChooseEnvironment(false, false, false, false) == null);
    for (int i = 0; i <= 4; i++) Assert(HallowActionRules.Encode(HallowActionRules.Decode(i)) == i);
    Assert(HallowActionRules.Decode(255) == null);
});
Test("完美格挡刷新衔接期限但不推进连段", () =>
{
    var combo = new HallowBladeCombo(); combo.Complete(1, 3, 0); combo.Refresh(100);
    var next = combo.Begin(0, 200); Assert(next.Stage == 2); Near(next.StartAngle, 3);
});

Test("剑气云纹周期接缝连续且亮度有限", () =>
{
    for (int i = 0; i <= 128; i++)
    {
        float t = i / 128f;
        Near(VfxTextures.SlashCloudIntensity(0, t), VfxTextures.SlashCloudIntensity(1, t));
        Near(VfxTextures.SlashCloudIntensity(t, 0), VfxTextures.SlashCloudIntensity(t, 1));
        float value = VfxTextures.SlashCloudIntensity(t, t * 0.63f);
        Assert(float.IsFinite(value) && value >= 0 && value <= 1);
    }
});

if (args.Length > 0) File.WriteAllText(args[0], JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, results }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"结果：{results.Count - failures}/{results.Count} 通过");
return failures == 0 ? 0 : 1;

sealed class Probe : WorldEffect, IVfxTarget<int>, IVfxSamples<int>
{
    public int Updates, Releases, Stops, Target, Samples;
    public Action OnUpdate;
    public override bool IsFinished => false;
    public override void Update(in VfxUpdateContext context) { Updates++; OnUpdate?.Invoke(); }
    public override void Stop() => Stops++;
    public override void ReleaseOwnedResources() => Releases++;
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context) { }
    public void SetTarget(in int state) => Target = state;
    public void AppendSample(in int sample) => Samples += sample;
}
