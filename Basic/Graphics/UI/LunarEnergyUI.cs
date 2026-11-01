using System;
using System.Collections.Generic;
using AdvancedMod.Basic.Global;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;

namespace AdvancedMod.Basic.Graphics.UI;

// 月曜度专用界面；不引入通用 UI 管理设施。
[Autoload(Side = ModSide.Client)]
public sealed class LunarEnergyUI : ModSystem
{
    internal const float IconSize = 48f;
    internal const float PanelWidth = 780f, PanelHeight = 320f, Gap = 12f;
    // 与本机原版 UpdateMinimapAnchors 对齐。额外留出所有原版边框及缩放按钮空间。
    internal const float MapTop = 90f, MapRight = 52f, MapSize = 240f, MapFrameClearance = 48f;
    internal LunarState State { get; private set; }
    private UserInterface userInterface;
    private GameTime lastTime = new();
    internal static bool Visible => !Main.gameMenu && !Main.hideUI && !Main.mapFullscreen &&
        Main.LocalPlayer.GetModPlayer<PlayerSpecialState>().LunarRate > 0f;

    public override void Load()
    {
        State = new LunarState();
        userInterface = new UserInterface();
        userInterface.SetState(State);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        lastTime = gameTime;
        State.Layout();
        if (Visible)
            userInterface.Update(gameTime);
        else
            userInterface.ResetLasts();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0) return;
        layers.Insert(index, new LegacyGameInterfaceLayer("AdvancedMod: Lunar Energy", () =>
        {
            if (Visible)
            {
                State.Layout();
                userInterface.Draw(Main.spriteBatch, lastTime);
            }
            return true;
        }, InterfaceScaleType.UI));
    }

    public override void OnWorldUnload() => State?.ClosePanel();
    public override void Unload()
    {
        var state = State;
        Main.QueueMainThreadAction(() => state?.DisposeTextures());
        State = null;
        userInterface = null;
    }

    internal sealed class LunarState : UIState
    {
        internal MoonIcon Icon { get; private set; }
        internal UIPanel Panel { get; private set; }
        internal bool PanelOpen { get; private set; }
        private const float Padding = 20f, SkillSize = 64f, SkillTop = 42f;
        private readonly List<SkillButton> skills = new();
        private UIText energyText, costText, passiveText, activeText;
        // 仅 UI 原型使用：必须替换为实际技能模块的消耗变量，不能据此结算。
        private static readonly int[] PreviewCosts = { 60, 120, 180, 240 };
        private static readonly int[] SkillItems = { ItemID.FragmentSolar, ItemID.FragmentVortex, ItemID.FragmentNebula, ItemID.FragmentStardust };

        public override void OnInitialize()
        {
            Icon = new MoonIcon();
            Icon.Width.Set(IconSize, 0f);
            Icon.Height.Set(IconSize, 0f);
            Icon.OnLeftClick += (_, _) => TogglePanel();
            Panel = new UIPanel
            {
                BackgroundColor = new Color(16, 22, 44) * 0.96f,
                BorderColor = new Color(155, 170, 175)
            };
            Panel.Width.Set(PanelWidth, 0f);
            Panel.Height.Set(PanelHeight, 0f);
            Panel.SetPadding(Padding);
            energyText = AddText(0f, .85f);
            costText = AddText(150f, 1f);
            passiveText = AddText(194f, 1f);
            activeText = AddText(238f, 1f);
            foreach (LunarSkill skill in Enum.GetValues<LunarSkill>())
            {
                var button = new SkillButton(skill, SkillItems[(int)skill]);
                button.Width.Set(SkillSize, 0f);
                button.Height.Set(SkillSize, 0f);
                button.Top.Set(SkillTop, 0f);
                button.OnLeftClick += (_, _) =>
                {
                    Main.LocalPlayer.GetModPlayer<PlayerSpecialState>().SelectedLunarSkill = skill;
                    RefreshText();
                };
                skills.Add(button);
                Panel.Append(button);
            }
            Append(Icon);
        }

        private UIText AddText(float top, float scale)
        {
            var text = new UIText("", scale) { IgnoresMouseInteraction = true, IsWrapped = true };
            text.Top.Set(top, 0f);
            text.Width.Set(0f, 1f);
            text.Height.Set(40f, 0f);
            Panel.Append(text);
            return text;
        }

        private void RefreshText()
        {
            var player = Main.LocalPlayer.GetModPlayer<PlayerSpecialState>();
            energyText.SetText(Language.GetTextValue("Mods.AdvancedMod.LunarUI.Energy", player.LunarRate.ToString("0.##"), PlayerSpecialState.MAX_LUNAR_RATE));
            costText.SetText(Language.GetTextValue("Mods.AdvancedMod.LunarUI.Cost", PreviewCosts[(int)player.SelectedLunarSkill]));
            string prefix = $"Mods.AdvancedMod.LunarSkill.{player.SelectedLunarSkill}";
            passiveText.SetText(Language.GetTextValue("Mods.AdvancedMod.LunarUI.Passive", Language.GetTextValue(prefix + ".Passive")));
            activeText.SetText(Language.GetTextValue("Mods.AdvancedMod.LunarUI.Active", Language.GetTextValue(prefix + ".Active")));
        }

        internal void DisposeTextures()
        {
            Icon?.DisposeTexture();
            foreach (var skill in skills) skill.DisposeTexture();
        }

        internal void TogglePanel()
        {
            if (PanelOpen) ClosePanel();
            else { PanelOpen = true; Append(Panel); Layout(); }
        }
        internal void ClosePanel() { PanelOpen = false; Panel?.Remove(); }

        internal void Layout()
        {
            if (Icon == null) return;
            float width = PlayerInput.RealScreenWidth / Main.UIScale;
            float height = PlayerInput.RealScreenHeight / Main.UIScale;
            // 小地图关闭或切换为叠加模式时保持锚点，不因模式切换跳动。
            float x = width - MapRight - (MapSize + MapFrameClearance) * Main.MapScale - Gap - IconSize;
            float y = MapTop + MapSize * Main.MapScale / 2f - IconSize / 2f;
            Icon.Left.Set(Math.Max(Gap, x), 0f);
            Icon.Top.Set(y, 0f);
            // 面板右侧与图标对齐，向左下展开；屏幕不足时限制宽度，保留内容高度。
            Panel.Width.Set(Math.Min(PanelWidth, Math.Max(IconSize, width - Gap * 2f)), 0f);
            Panel.Left.Set(Math.Max(Gap, x + IconSize - Panel.Width.Pixels), 0f);
            Panel.Top.Set(Math.Max(Gap, Math.Min(y + IconSize + Gap, height - PanelHeight - Gap)), 0f);
            Panel.Height.Set(PanelHeight, 0f);
            float contentWidth = Panel.Width.Pixels - Padding * 2f;
            for (int i = 0; i < skills.Count; i++)
                skills[i].Left.Set(contentWidth * (i + .5f) / skills.Count - SkillSize / 2f, 0f);
            Recalculate();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            RefreshText();
            // 仅占用实际控件区域；不能用覆盖全屏的 UIState.IsMouseHovering。
            if (Icon.IsMouseHovering || (PanelOpen && Panel.IsMouseHovering))
                Main.LocalPlayer.mouseInterface = true;
        }
    }

    // 四种碎片使用原版素材；圆形底板和命中区域独立于物品的透明边界。
    private sealed class SkillButton : UIElement
    {
        private readonly LunarSkill skill;
        private readonly int itemType;
        private Texture2D background;
        public SkillButton(LunarSkill skill, int itemType) { this.skill = skill; this.itemType = itemType; }
        internal void DisposeTexture() { background?.Dispose(); background = null; }
        public override bool ContainsPoint(Vector2 point)
        {
            var bounds = GetDimensions();
            return Vector2.DistanceSquared(point, bounds.Center()) <= bounds.Width * bounds.Width / 4f;
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (background == null)
            {
                var pixels = new Color[MoonIcon.TextureSize * MoonIcon.TextureSize];
                MoonIcon.RenderPixels(pixels, 0f);
                background = new Texture2D(Main.instance.GraphicsDevice, MoonIcon.TextureSize, MoonIcon.TextureSize);
                background.SetData(pixels);
            }
            var bounds = GetDimensions();
            bool selected = Main.LocalPlayer.GetModPlayer<PlayerSpecialState>().SelectedLunarSkill == skill;
            spriteBatch.Draw(background, bounds.ToRectangle(), selected ? new Color(255, 232, 155) : IsMouseHovering ? Color.White : new Color(155, 169, 185));
            Main.instance.LoadItem(itemType);
            var texture = TextureAssets.Item[itemType].Value;
            const float itemSize = 34f;
            float scale = itemSize / Math.Max(texture.Width, texture.Height);
            spriteBatch.Draw(texture, bounds.Center(), null, Color.White, 0f, texture.Size() / 2f, scale, SpriteEffects.None, 0f);
            string name = Language.GetTextValue($"Mods.AdvancedMod.LunarSkill.{skill}.DisplayName");
            Terraria.Utils.DrawBorderString(spriteBatch, name, new Vector2(bounds.Center().X, bounds.Y + bounds.Height + 8f), selected ? new Color(255, 232, 155) : Color.Silver, .85f, .5f);
        }
    }

    internal sealed class MoonIcon : UIElement
    {
        internal const int TextureSize = 96;
        private Texture2D texture;
        private readonly Color[] pixels = new Color[TextureSize * TextureSize];
        private float renderedRate = -1f;
        internal float DisplayedFraction => renderedRate;
        // 数学坐标 -3π/4：屏幕 Y 轴向下，故可见月牙从左下开始。
        internal static readonly Vector2 Axis = new(-MathF.Sqrt(0.5f), MathF.Sqrt(0.5f));

        internal static bool IsLit(Vector2 point, float fraction)
        {
            if (fraction <= 0f || point.LengthSquared() > 1f) return false;
            float along = Vector2.Dot(point, Axis);
            float across = point.X * Axis.Y - point.Y * Axis.X;
            return along >= (1f - 2f * fraction) * MathF.Sqrt(Math.Max(0f, 1f - across * across));
        }

        public override bool ContainsPoint(Vector2 point) =>
            Vector2.DistanceSquared(point, GetDimensions().Center()) <= IconSize * IconSize / 4f;

        internal void DisposeTexture() { texture?.Dispose(); texture = null; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            float fraction = Main.LocalPlayer.GetModPlayer<PlayerSpecialState>().LunarRate / PlayerSpecialState.MAX_LUNAR_RATE;
            if (texture == null)
                texture = new Texture2D(Main.instance.GraphicsDevice, TextureSize, TextureSize);
            if (fraction != renderedRate)
            {
                RenderPixels(pixels, fraction);
                texture.SetData(pixels);
                renderedRate = fraction;
            }
            spriteBatch.Draw(texture, GetDimensions().ToRectangle(), Color.White);
        }

        // 参考图的银色外圈、古铜内圈；几何绘制保留独立透明背景，不复制中心靶纹。
        internal static void RenderPixels(Color[] target, float fraction)
        {
            const float outer = 46f, moonRadius = 32f;
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                var p = new Vector2(x + 0.5f - TextureSize / 2f, y + 0.5f - TextureSize / 2f);
                float r = p.Length();
                Color color;
                float bevel = Math.Clamp(0.6f + (-p.X - p.Y) / 160f, 0.25f, 1f);
                if (r > outer) color = Color.Transparent;
                else if (r > 44f) color = new Color(39, 43, 43);
                else if (r > 40f) color = Color.Lerp(new Color(98, 112, 111), new Color(223, 229, 216), bevel);
                else if (r > 38f) color = new Color(56, 58, 51);
                else if (r > 34f) color = Color.Lerp(new Color(83, 57, 38), new Color(174, 144, 94), bevel);
                else if (r > moonRadius) color = new Color(157, 181, 181);
                else
                {
                    float cloud = (MathF.Sin(p.X * .15f + p.Y * .07f) + 1f) * .5f;
                    color = Color.Lerp(new Color(9, 17, 38), new Color(40, 32, 76), cloud);
                    uint hash = unchecked((uint)(x * 374761393 + y * 668265263));
                    hash = (hash ^ (hash >> 13)) * 1274126177u;
                    if (hash % 173u == 0) color = new Color(146, 172, 205);
                    if (IsLit(p / moonRadius, fraction))
                        color = Color.Lerp(new Color(144, 186, 201), new Color(232, 244, 228), .35f + .55f * bevel);
                }
                target[y * TextureSize + x] = color;
            }
        }
    }
}
