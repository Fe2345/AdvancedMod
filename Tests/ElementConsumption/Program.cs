using System;
using AdvancedMod.Basic.Global;

var item = new CombatGlobalItem();
var projectile = new CombatGlobalProjectile();
if (item.ElementConsumptionMultiplier != 1f || projectile.ElementConsumptionMultiplier != 1f ||
    item.ReactionCooldownMultiplier != 1f || projectile.ReactionCooldownMultiplier != 1f)
    throw new Exception("修正默认值错误");
#if ZERO_LITERAL
item.ElementConsumptionMultiplier = 0f;
#elif ZERO_CONSTANT
const float half = 0.5f;
projectile.ElementConsumptionMultiplier = half - half;
#elif ZERO_DEFAULT
projectile.ElementConsumptionMultiplier = default(float);
#else
item.ElementConsumptionMultiplier = 0.5f;
projectile.ElementConsumptionMultiplier = 2f;
item.ReactionCooldownMultiplier = 0f;
new Unrelated().ElementConsumptionMultiplier = 0f;
if (new CombatGlobalItem().ElementConsumptionMultiplier != 1f ||
    new CombatGlobalProjectile().ElementConsumptionMultiplier != 1f)
    throw new Exception("不同实例的修正发生共享");
Console.WriteLine("通过：默认值、实例独立性及无关字段不误报。");
#endif

class Unrelated { public float ElementConsumptionMultiplier; }

// 只替代编译这些 Global 所需的外部类型，不启动 Terraria 或伪造命中验收。
namespace Terraria.ModLoader
{
    public class GlobalItem { public virtual bool InstancePerEntity => false; }
    public class GlobalProjectile { public virtual bool InstancePerEntity => false; }
}
namespace AdvancedMod.Basic.Calculate.Tml
{
    public class AttackProfile { public static AttackProfile Unknown { get; } = new(); }
}
