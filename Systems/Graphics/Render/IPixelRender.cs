using Microsoft.Xna.Framework.Graphics;
using AdvancedMod.Utils.Render;

namespace AdvancedMod.Systems.Graphics.Render
{
    //像素化Render的接口，弹幕只需要实现这个接口就可以让渲染系统联动起来进行渲染
    public interface IPixelRender
    {
        /// <summary>
        /// The layer to render the primitive(s) to.
        /// </summary>
        BasicDrawLayer LayerToRenderTo => BasicDrawLayer.BeforeProjectiles;

        /// <summary>
        /// Render primitives that use pixelation here.
        /// </summary>
        void RenderPixelatedPrimitives(SpriteBatch spriteBatch, BasicDrawLayer layer);
    }
}
