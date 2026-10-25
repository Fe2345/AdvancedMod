// 2026-10-25：逐玩家、冻结前事件、卸下、死亡重生和世界边界的适配器测试。
using AdvancedMod.Basic.Effects;
using Terraria;

int passed = 0;
void Check(string name, bool ok) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
void Hit(EffectPlayer p) => p.OnHitNPC(new NPC(), default, 1);
var first = new EffectPlayer(); var second = new EffectPlayer();
first.Initialize(); second.Initialize();
Check("首次更新为空", first.Stacks<Enabled>() == 0);
first.ResetEffects(); first.Add<Enabled>(3); Hit(first);
Check("冻结前事件不使用残留状态", first.Player.Hits == 0);
first.PostUpdateEquips(); Hit(first);
Check("一次应用且命中使用真实层数及正确玩家", first.Player.Applied == 1 && first.Player.Hits == 3 && second.Player.Hits == 0);
second.ResetEffects(); second.PostUpdateEquips(); Hit(second);
Check("关闭效果及其他玩家不收到事件", second.Player.Applied == 0 && second.Player.Hits == 0);
first.ResetEffects(); first.PostUpdateEquips(); Hit(first);
Check("卸下立即消除新刻贡献", first.Stacks<Enabled>() == 0 && first.Player.Hits == 3);
first.ResetEffects(); first.Add<Enabled>(); first.PostUpdateEquips(); first.Player.dead = true; first.UpdateDead(); Hit(first);
Check("死亡清空", first.Stacks<Enabled>() == 0 && first.Player.Hits == 3);
first.Player.dead = false; first.OnRespawn(); Check("重生不恢复旧层数", first.Stacks<Enabled>() == 0);
first.ResetEffects(); first.Add<Enabled>(); first.PostUpdateEquips(); first.OnEnterWorld();
Check("世界边界清空", first.Stacks<Enabled>() == 0);
first.ResetEffects(); first.Add<Enabled>(); first.PostUpdateEquips(); first.PlayerDisconnect();
Check("断开清空", first.Stacks<Enabled>() == 0);
Console.WriteLine($"PASS {passed}/{passed}（tML 替身，不是实机）");
