# Element TML First-Hit Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor the element combat system so original item/projectile hits rely on TML native stats and `NPC.HitModifiers`, while the element pipeline keeps only element-only semantics such as buildup, reactions, mastery, and coordinated attacks.

**Architecture:** First remove the parallel crit pipeline from original hits and restore `ElementDamage` to standard TML crit behavior. Then shrink `ElementCombatPlayerState` and `ReactionStatSnapshot` so they no longer mirror native player offense/crit/defense, and finally rewire pre-hit and on-hit paths so amplification reactions modify original hit modifiers while transformative reactions remain post-hit derived effects without crit in phase one.

**Tech Stack:** C# 12, .NET 8, tModLoader API, Terraria `DamageClass`, `NPC.HitModifiers`, `NPC.HitInfo`

---

### Task 1: Restore ElementDamage to Native TML Crit Behavior

**Files:**
- Modify: `DamageClasses/Element/ElementDamage.cs`
- Verify: `DamageClasses/Element/ElementCombatSystem.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "UseStandardCritCalcs|SetDefaultStats|ShowStatTooltipLine" DamageClasses/Element/ElementDamage.cs
```

Expected:

- `UseStandardCritCalcs => false` is still present
- `SetDefaultStats` still contains a no-op crit line

**Step 2: Run baseline build**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: baseline build succeeds before the refactor.

**Step 3: Write minimal implementation**

- Remove the empty `SetDefaultStats` override
- Remove `UseStandardCritCalcs => false`
- Keep tooltip behavior only if it still matches the new crit design; otherwise remove the override as well

**Step 4: Run verification**

Run:

```bash
rg -n "UseStandardCritCalcs|SetDefaultStats" DamageClasses/Element/ElementDamage.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected:

- native crit suppression is gone
- build succeeds

**Step 5: Commit**

```bash
git add DamageClasses/Element/ElementDamage.cs
git commit -m "refactor: restore native crit behavior for element damage"
```

### Task 2: Remove the Parallel Crit Pipeline From Original Hits

**Files:**
- Modify: `DamageClasses/Element/ElementCombatSystem.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "ResolveSourceCritPreview|RollCrit|SetCrit\\(|CritDamage \\+=" DamageClasses/Element/ElementCombatSystem.cs
```

Expected:

- original-hit crit preview and manual crit rolling are still present

**Step 2: Write minimal implementation**

- Delete `SourceCritPreview`
- Delete `ResolveSourceCritPreview(...)`
- Delete `RollCrit(...)` in `ElementCombatSystem`
- Make `ModifyOutgoingHit(...)` only:
  - build the planned hit
  - apply element-only source damage modifiers
  - apply amplification reaction multiplier to `modifiers.SourceDamage`
- Do not call `SetCrit()`
- Do not add `CritDamage` there in phase one

**Step 3: Run verification**

Run:

```bash
rg -n "ResolveSourceCritPreview|RollCrit|SetCrit\\(|CritDamage \\+=" DamageClasses/Element/ElementCombatSystem.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected:

- no manual crit flow remains in original-hit logic
- build succeeds

**Step 4: Commit**

```bash
git add DamageClasses/Element/ElementCombatSystem.cs
git commit -m "refactor: remove manual crit flow from original element hits"
```

### Task 3: Shrink ElementCombatPlayerState to Element-Only Data

**Files:**
- Modify: `DamageClasses/Element/Players/ElementCombatPlayerState.cs`
- Modify: `DamageClasses/Element/Data/ElementOffenseStatSet.cs`
- Modify: `DamageClasses/Element/Data/ElementCritStatSet.cs`
- Modify: `DamageClasses/Element/Data/ElementDefenseStatSet.cs`
- Modify: `DamageClasses/Element/Data/CombatContextSnapshot.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "SyncFromPlayer|GetTotalDamage|GetCritChance|endurance|Crit \\{|Defense \\{" DamageClasses/Element/Players/ElementCombatPlayerState.cs DamageClasses/Element/Data
```

Expected:

- player stat mirrors are still present
- crit/defense containers are still treated as snapshot truth

**Step 2: Write minimal implementation**

- Remove `SyncFromPlayer(...)`
- Stop exporting native offense/crit/defense mirrors into snapshot
- Keep only element-only offense fields in `ElementOffenseStatSet`
- Remove or neutralize `ElementCritStatSet` and `ElementDefenseStatSet` from runtime snapshot usage in phase one
- Keep `ElementMastery`, `ElementDamageBonus`, and `CoordinatedAttacks`

**Step 3: Run verification**

Run:

```bash
rg -n "SyncFromPlayer|GetTotalDamage|GetCritChance|endurance" DamageClasses/Element/Players/ElementCombatPlayerState.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected:

- no native player-stat readback remains
- build succeeds

**Step 4: Commit**

```bash
git add DamageClasses/Element/Players/ElementCombatPlayerState.cs DamageClasses/Element/Data/ElementOffenseStatSet.cs DamageClasses/Element/Data/ElementCritStatSet.cs DamageClasses/Element/Data/ElementDefenseStatSet.cs DamageClasses/Element/Data/CombatContextSnapshot.cs
git commit -m "refactor: keep only element-only data in element player state"
```

### Task 4: Rebuild ReactionStatSnapshot as a Reaction-Only Numeric Context

**Files:**
- Modify: `DamageClasses/Element/Data/ReactionDamageModifierSet.cs`
- Modify: `DamageClasses/Element/Data/ReactionStatSnapshot.cs`
- Modify: `DamageClasses/Element/Players/ElementCombatPlayerState.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "CritChanceAdd|CritDamageMult|GlobalModifiers|ByElement|ByReactionType" DamageClasses/Element/Data DamageClasses/Element/Players/ElementCombatPlayerState.cs
```

Expected:

- reaction snapshot still carries original-hit crit buckets and mirrored general damage buckets

**Step 2: Write minimal implementation**

- Remove original-hit crit fields from reaction-only modifiers
- Keep only reaction-side buckets that still matter in phase one:
  - `ElementMult`
  - `ReactionTypeMult`
  - `BasicMult`
  - `MasteryMult`
  - `ExtraMult`
  - optional vulnerability/resistance overrides
- Export only element-only and reaction-only values from `ElementCombatPlayerState.ExportSnapshot()`

**Step 3: Run verification**

Run:

```bash
rg -n "CritChanceAdd|CritDamageMult" DamageClasses/Element/Data DamageClasses/Element/Players/ElementCombatPlayerState.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected:

- original-hit crit buckets are gone from reaction snapshot
- build succeeds

**Step 4: Commit**

```bash
git add DamageClasses/Element/Data/ReactionDamageModifierSet.cs DamageClasses/Element/Data/ReactionStatSnapshot.cs DamageClasses/Element/Players/ElementCombatPlayerState.cs
git commit -m "refactor: reduce reaction snapshot to reaction-only buckets"
```

### Task 5: Rewire Pre-Hit Damage Modification to TML + Element-Only Multipliers

**Files:**
- Modify: `DamageClasses/Element/ElementCombatSystem.cs`
- Review: `Items/Weapon/Element/PrototypeThunderSword.cs`
- Review: `Items/Weapon/Element/PrototypeWindSword.cs`
- Review: `Projectiles/Weapons/PrototypeThunderBolt.cs`
- Review: `Projectiles/Weapons/Element/BurnedGel.cs`
- Review: `Projectiles/Weapons/Element/AdvancedElementOrb.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "ResolveCustomElementDamageMultiplier|AmplificationMultiplier|SourceDamage \\*=" DamageClasses/Element/ElementCombatSystem.cs
```

Expected:

- current pre-hit code is still coupled to the old snapshot shape

**Step 2: Write minimal implementation**

- Keep `ModifyItemHit(...)` / `ModifyProjectileHit(...)`
- Make `ModifyOutgoingHit(...)` only read:
  - the element labels on the hit payload
  - element-only snapshot bonuses
  - amplification reaction multiplier
- Apply these to `modifiers.SourceDamage`
- Do not touch crit logic in this phase

**Step 3: Run verification**

Run:

```bash
rg -n "SetCrit\\(|CritDamage \\+=|RollCrit" DamageClasses/Element/ElementCombatSystem.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected:

- pre-hit path only mutates damage modifiers
- build succeeds

**Step 4: Commit**

```bash
git add DamageClasses/Element/ElementCombatSystem.cs
git commit -m "refactor: apply only element-only pre-hit modifiers to original hits"
```

### Task 6: Remove Manual Crit From ReactionDamageCalculator and Freeze Transformative Crit Off

**Files:**
- Modify: `DamageClasses/Element/Services/ReactionDamageCalculator.cs`
- Modify: `DamageClasses/Element/Reactions/DamageInstancePlan.cs`
- Modify: `DamageClasses/Element/Services/ReactionExecutor.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "RollCrit|IsCrit|CritChance|CritDamage" DamageClasses/Element/Services/ReactionDamageCalculator.cs DamageClasses/Element/Reactions/DamageInstancePlan.cs DamageClasses/Element/Services/ReactionExecutor.cs
```

Expected:

- reaction damage still includes manual crit rolling and crit flags

**Step 2: Write minimal implementation**

- Remove reaction-side `RollCrit(...)`
- Remove `IsCrit` from `DamageInstancePlan` if it is no longer needed
- Make phase-one transformative/prototype reaction damage non-crit by definition
- Keep mastery and reaction-only multiplier calculation
- Keep amplification multiplier output

**Step 3: Run verification**

Run:

```bash
rg -n "RollCrit|IsCrit|CritChance|CritDamage" DamageClasses/Element/Services/ReactionDamageCalculator.cs DamageClasses/Element/Reactions/DamageInstancePlan.cs DamageClasses/Element/Services/ReactionExecutor.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected:

- reaction path no longer rolls crit
- build succeeds

**Step 4: Commit**

```bash
git add DamageClasses/Element/Services/ReactionDamageCalculator.cs DamageClasses/Element/Reactions/DamageInstancePlan.cs DamageClasses/Element/Services/ReactionExecutor.cs
git commit -m "refactor: remove crit logic from phase-one reaction damage"
```

### Task 7: Tighten Equipment Contribution Boundaries

**Files:**
- Modify: `Items/Armor/Element/MoltenBoneHelmet.cs`
- Modify: `Items/Armor/Element/MoltenBoneBreastplate.cs`
- Modify: `Items/Armor/Element/MoltenBoneGreaves.cs`
- Modify: `Items/Accessory/Element/AdvancedElementBottleBase.cs`
- Review: `DamageClasses/Element/Interfaces/IElementStatProvider.cs`
- Review: `DamageClasses/Element/Interfaces/IElementArmorSetProvider.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "GetDamage\\(|GetCritChance\\(|endurance|AddElementDamageBonus|AddElementMastery|CoordinatedAttacks" Items/Armor/Element Items/Accessory/Element
```

Expected:

- current equipment effects still mix native fields and element-only state without an explicit boundary contract

**Step 2: Write minimal implementation**

- Ensure each effect follows one rule:
  - native-supported stat -> write only `player`
  - element-only stat -> write only `state`
- Validate:
  - molten helmet -> mastery only in `state`
  - molten chestplate -> generic damage only in `player`
  - molten greaves -> element crit only in `player`
  - molten set bonus -> element damage and endurance only in `player`
  - advanced element bottle -> element-only bonus and coordinated attack only in `state`

**Step 3: Run verification**

Run:

```bash
rg -n "GetDamage\\(|GetCritChance\\(|endurance|AddElementDamageBonus|AddElementMastery|CoordinatedAttacks" Items/Armor/Element Items/Accessory/Element
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected:

- equipment responsibility split is easy to inspect
- build succeeds

**Step 4: Commit**

```bash
git add Items/Armor/Element Items/Accessory/Element
git commit -m "refactor: separate native and element-only equipment contributions"
```

### Task 8: Run Structure Checks and Prepare the Next-Phase Handoff

**Files:**
- Modify: `docs/plans/2026-05-10-element-tml-first-hit-refactor-design.md`
- Modify: `docs/plans/2026-05-10-element-tml-first-hit-refactor-implementation.md`

**Step 1: Write the failing check**

Run:

```bash
rg -n "RollCrit|SetCrit\\(|SyncFromPlayer|GetTotalDamage\\(|GetCritChance\\(" DamageClasses/Element
```

Expected:

- any remaining matches should only exist in places intentionally kept for later phases

**Step 2: Write minimal implementation**

- Update the design/plan docs with any implementation drift discovered during the refactor
- Add a short “phase one complete / phase two deferred” note covering:
  - transformative crit deferred
  - derived attack layer still pending

**Step 3: Run final verification**

Run:

```bash
rg -n "RollCrit|SetCrit\\(|SyncFromPlayer" DamageClasses/Element
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
git status --short
```

Expected:

- no forbidden manual original-hit crit pipeline remains
- build succeeds
- worktree changes are limited to the planned refactor

**Step 4: Commit**

```bash
git add docs/plans/2026-05-10-element-tml-first-hit-refactor-design.md docs/plans/2026-05-10-element-tml-first-hit-refactor-implementation.md DamageClasses/Element Items/Armor/Element Items/Accessory/Element
git commit -m "refactor: align element hit flow with tml native stat handling"
```

## Phase One Completion Note

- 已完成：原始命中恢复 TML 原生暴击与原生 player 数值聚合，元素系统只在 pre-hit 阶段通过 `NPC.HitModifiers.SourceDamage` 追加元素专属倍率和增幅反应倍率。
- 已完成：`ElementCombatPlayerState` 不再回读原生增伤、暴击、减伤；snapshot 只保存元素专属上下文。
- 已完成：阶段一反应派生伤害不再手动暴击，`DamageInstancePlan` 不再携带 `IsCrit`。
- 已确认：熔骨护甲与高级元素瓶的边界为原生字段写 `player`，元素精通、分元素倍率、协同攻击写 `state`。
- 暂缓：剧变反应暴击、反应暴伤体系、正式 `DerivedAttack` 层和更多反应异化效果。
