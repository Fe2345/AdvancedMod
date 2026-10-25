#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using AdvancedMod.Basic.Graphics.Rendering;

namespace AdvancedMod.Basic.Graphics;

public sealed class WorldVfxSystem : ModSystem
{
    public WorldEffectManager Manager { get; } = new();
    private readonly VfxDrawQueue queue = new();
    private readonly List<string> skies = new();
    private readonly HashSet<string> activated = new();
    private BasicEffect? material;
    public const float TickSeconds = 1f / 60;

    public void RegisterSky(string key, float transitionSeconds, Func<EnvironmentController.Layer, CustomSky> factory)
    {
        if (Main.dedServ) return;
        var layer = Manager.Environment.Register(key, transitionSeconds);
        SkyManager.Instance[key] = factory(layer);
        skies.Add(key);
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused) return;
        Manager.Update(TickSeconds);
        foreach (string key in skies)
        {
            var layer = Manager.Environment.Get(key);
            if (layer.Visible && activated.Add(key)) SkyManager.Instance.Activate(key, Vector2.Zero);
            if (!layer.Visible && activated.Remove(key)) SkyManager.Instance.Deactivate(key);
        }
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        queue.Clear();
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Vector2 screen = new(Main.screenWidth, Main.screenHeight), view = screen / zoom;
        var context = new VfxDrawContext(Main.screenPosition + (screen - view) * 0.5f, view);
        try
        {
            Manager.Collect(queue, context);
            material ??= new BasicEffect(Main.instance.GraphicsDevice);
            queue.Draw(material);
        }
        finally { queue.Clear(); }
    }
    private void Clear()
    {
        Manager.Clear(); queue.Clear();
        if (!Main.dedServ) foreach (string key in skies) SkyManager.Instance[key].Reset();
        activated.Clear();
    }
    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();
    public override void Unload()
    {
        Clear();
        // SkyManager 随模组重载统一重建，本模组仅复位自己注册的天空。
        skies.Clear();
        var ownedMaterial = material; material = null;
        if (!Main.dedServ) Main.QueueMainThreadAction(() => { ownedMaterial?.Dispose(); VfxDrawQueue.Unload(); VfxTextures.Unload(); });
    }
}
