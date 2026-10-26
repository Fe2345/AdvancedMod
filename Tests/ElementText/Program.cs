// 2026-10-26：验证颜色选择、枚举兼容、32 像素边界与 150 刻生命周期。
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Element.Presentation;
using AdvancedMod.Basic.Tag;

int passed = 0;
void Test(string name, Action action) { action(); Console.WriteLine("PASS " + name); passed++; }
void Check(bool ok) { if (!ok) throw new Exception("断言失败"); }
TagSet Tags(params TagEnum[] tags) => new(tags.Select(t => new TagEntry(false, t)));
Test("六种元素具有独立颜色", () => Check(Enumerable.Range(0, 6).Select(i => ElementTextStyle.Rgb((TagEnum)i)).Distinct().Count() == 6));
Test("火为深红", () => { var c = ElementTextStyle.Rgb(TagEnum.Pyro); Check(c.R < 200 && c.R > c.G * 3 && c.R > c.B * 3); });
Test("非元素标签无颜色", () => Check(ElementTextStyle.SelectElement(Tags(TagEnum.Hallow)) is null));
Test("多元素取首标签", () => Check(ElementTextStyle.SelectElement(Tags(TagEnum.Hallow, TagEnum.Hydro, TagEnum.Pyro)) == TagEnum.Hydro));
Test("反应继承触发元素", () => Check(ElementTextStyle.SelectElement(Tags(TagEnum.Cryo), TagEnum.PyroCryo) == TagEnum.Cryo));
Test("静置反应三种回落", () => Check(ElementTextStyle.SelectElement(TagSet.Empty, TagEnum.HydroElectro) == TagEnum.Electro && ElementTextStyle.SelectElement(TagSet.Empty, TagEnum.HydroAnemo) == TagEnum.Hydro && ElementTextStyle.SelectElement(TagSet.Empty, TagEnum.PyroCryo) == TagEnum.Pyro));
Test("名称取本地化键", () => Check(ElementTextStyle.ReactionKey(TagEnum.PyroCryo) == "Mods.AdvancedMod.Reactions.PyroCryo"));
Test("重命名不变更网络数值及槽位", () => Check((int)TagEnum.PyroCryo == 11 && ElementSlots.ReactionFromTag(TagEnum.PyroCryo) == 3));
Test("排斥覆盖所有反应而非仅同名", () => { var l = new ReactionTextLayout(); l.Add(0, 0, .5f); Check(!l.CanPlace(0, 0) && !l.CanPlace(31.99f, 0) && l.CanPlace(32, 0)); });
Test("排斥采用圆形而非方形", () => { var l = new ReactionTextLayout(); l.Add(0, 0, .5f); Check(l.CanPlace(24, 24) && !l.CanPlace(20, 20)); });
Test("倾角上下限与中点", () => { var l = new ReactionTextLayout(); Check(Math.Abs(l.Add(0, 0, 0).Rotation + MathF.PI / 18) < .00001f); Check(l.Add(40, 0, .5f).Rotation == 0); Check(Math.Abs(l.Add(80, 0, 1).Rotation - MathF.PI / 18) < .00001f); });
Test("150刻前仍排斥，期满可重用", () => { var l = new ReactionTextLayout(); var a = l.Add(0, 0, .5f); for (int i = 0; i < 149; i++) a.RemainingTicks--; Check(a.RemainingTicks == 1 && !l.CanPlace(0, 0)); a.RemainingTicks--; Check(l.CanPlace(0, 0)); });
Test("原版池清理释放位置", () => { var l = new ReactionTextLayout(); var a = l.Add(0, 0, .5f); l.Remove(a); Check(l.CanPlace(0, 0)); });
Test("世界切换清理", () => { var l = new ReactionTextLayout(); l.Add(0, 0, .5f); l.Clear(); Check(l.CanPlace(0, 0)); });
Console.WriteLine($"PASS {passed}/{passed}");
