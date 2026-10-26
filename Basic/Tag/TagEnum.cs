using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedMod.Basic.Tag
{
    public enum TagEnum
    {
        /// <summary>
        /// 元素标签
        /// </summary>
        Pyro, //火[1]
        Hydro, //水[2]
        Electro,//雷[3]
        Cryo,//冰[4]
        Chlorophyte,//叶绿[5]
        Anemo,//风[6]
        /// <summary>
        /// 元素异化标签
        /// 元素异化指的是这次伤害如果触发了反应，那么可以使反应异化，而不是已经触发了；此时如果下面的反应异化标签没开启，那么依旧不吃擢升类加成。
        /// </summary>
        Hallow,//神圣
        Lunar,//月曜
        Origin,//始源
        /// <summary>
        /// 反应标签
        /// </summary>
        HydroElectro, //[1]
        HydroAnemo, //[2]
        PyroCryo,//[3]

        /// <summary>
        /// 反应异化标签
        /// </summary>
        Hallowed,//[1]
        Lunacy,//[2]
        Originated,//[3]
    }
}
