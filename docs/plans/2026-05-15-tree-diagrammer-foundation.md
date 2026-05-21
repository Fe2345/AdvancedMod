# Tree Diagrammer Foundation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让树状图设计者 Boss 骨架恢复基础可编译状态，并先落地护佑探测器的防御加成。

**Architecture:** 本阶段只处理 Boss 骨架基础行为，不实现激光、电弧、闪电珠等弹幕细节。Boss 主体每帧根据存活的护佑探测器刷新防御值，避免防御值被重复叠加；冲刺电弧只修正当前编译错误，不扩展行为。

**Tech Stack:** .NET 8.0、C#、tModLoader NPC AI、Windows PowerShell `dotnet build`。

---

### Task 1: 编译 RED 检查

**Files:**
- Read: `NPCs/Boss/TreeDiagrammer.cs`
- Read: `Utils/Specials.cs`

**Step 1: Run build to verify current failure**

Run:

```powershell
dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly
```

Expected: FAIL，至少包含 `ApplyDefenseFromGuardProbes` 缺失和 `Vector2.ToVector2()` 误用。

### Task 2: Boss 基础骨架修复

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Step 1: Implement guard probe defense refresh**

Add constants for base defense and guard bonus, and implement `ApplyDefenseFromGuardProbes()`:

```csharp
private void ApplyDefenseFromGuardProbes()
{
    int guardProbeCount = 0;

    foreach (NPC n in Main.ActiveNPCs)
    {
        if (n.type == ModContent.NPCType<TmlProbe>() &&
            n.active &&
            (int)n.ai[0] == NPC.whoAmI &&
            (ProbeMode)(int)n.ai[1] == ProbeMode.Guard)
        {
            guardProbeCount++;
        }
    }

    NPC.defense = BaseDefense + guardProbeCount * GuardProbeDefenseBonus;
}
```

**Step 2: Fix DashArc vector rotation**

Remove the invalid `.ToVector2()` calls after `RotatedBy(...)`.

### Task 3: 项目级编译阻塞修复

**Files:**
- Modify: `Utils/Specials.cs`

**Step 1: Align final boss flag name**

Replace `AdvancedWorld.TheEnd` with `AdvancedWorld.downedTheEnd`.

### Task 4: GREEN 验证

**Files:**
- Verify: whole project

**Step 1: Run build**

Run:

```powershell
dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly
```

Expected: PASS with 0 errors.
