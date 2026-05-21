# Element Build Stat System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rebuild the current element combat slice into a full weapon-plus-equipment numeric system where reactions, armor, accessories, and coordinated attacks all flow through one stable player-side stat context.

**Architecture:** The work starts by correcting the reaction damage model so amplification reactions truly modify the original hit rather than append a pseudo-multiplier damage packet. Then it adds a dedicated player-side element stat aggregation layer, threads that stat snapshot into hit processing, and only after that implements the first complete content slice: Molten Bone armor and Advanced Element Bottle coordinated attacks.

**Tech Stack:** C# 12, .NET 8, tModLoader API, ReLogic, FNA

---

### Task 1: Freeze the New Reaction Semantics in Code-Level Types

**Files:**
- Modify: `DamageClasses/Element/Definitions/ReactionDefinitions.cs`
- Modify: `DamageClasses/Element/Reactions/ReactionRule.cs`
- Modify: `DamageClasses/Element/Reactions/ReactionResult.cs`
- Modify: `DamageClasses/Element/Reactions/ReactionRuleRegistry.cs`

**Step 1: Write the failing check**

Search for the old assumption that all reactions are treated as appended damage packets.

Run:

```bash
rg -n "BaseDamageFactor - 1f|ExtraDamages|PrimaryDamage|ScaleFromSourceDamage" DamageClasses/Element
```

Expected:

- `ElementCombatSystem` still computes reaction damage by subtracting `1f` from the multiplier
- current result shape still assumes reactions are damage packets first and semantics second

**Step 2: Run build before edits**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: baseline build succeeds before the reaction semantic refactor.

**Step 3: Write minimal implementation**

- Add an explicit semantic split in reaction definitions:
  - amplification reaction
  - transformative reaction
- Extend `ReactionRule` so each rule declares its semantic category
- Extend `ReactionResult` so it can represent:
  - amplification multiplier applied to the original hit
  - optional transformative damage plan
  - optional extra effects that are not the reaction essence itself
- Mark current reactions as:
  - `Vaporize` -> amplification
  - `Overloaded` -> transformative
  - `ElectroCharged` -> transformative
  - `ThunderWind` -> keep temporary prototype classification until it is redesigned later

**Step 4: Run verification**

Run:

```bash
rg -n "BaseDamageFactor - 1f|Amplification|Transformative" DamageClasses/Element
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected:

- new semantic category symbols exist
- build succeeds

**Step 5: Commit**

```bash
git add DamageClasses/Element/Definitions/ReactionDefinitions.cs DamageClasses/Element/Reactions/ReactionRule.cs DamageClasses/Element/Reactions/ReactionResult.cs DamageClasses/Element/Reactions/ReactionRuleRegistry.cs
git commit -m "refactor: define amplification and transformative reaction semantics"
```

### Task 2: Add a Player-Side Element Stat Aggregation Layer

**Files:**
- Create: `DamageClasses/Element/Data/ElementCritStatSet.cs`
- Create: `DamageClasses/Element/Data/ElementOffenseStatSet.cs`
- Create: `DamageClasses/Element/Data/ElementDefenseStatSet.cs`
- Create: `DamageClasses/Element/Data/CoordinatedAttackStatSet.cs`
- Modify: `DamageClasses/Element/Data/ReactionDamageModifierSet.cs`
- Modify: `DamageClasses/Element/Data/ReactionStatSnapshot.cs`
- Modify: `DamageClasses/Element/Data/CombatContextSnapshot.cs`
- Modify: `Systems/Globals/AdvancedPlayer.cs`
- Create: `DamageClasses/Element/Players/ElementCombatPlayerState.cs`

**Step 1: Write the failing check**

Search the current codebase for any real player-side aggregation of element damage, mastery, reaction crit, or coordinated attack state.

Run:

```bash
rg -n "ElementMastery|ElementCrit|CoordinatedAttack|ReactionCrit|ElementDamageBonus|DamageReduction" DamageClasses Systems Items
```

Expected: no real player-side aggregation layer exists yet.

**Step 2: Write minimal implementation**

- Add explicit stat holder objects for:
  - elemental offense
  - elemental crit
  - defense and incoming-damage reduction
  - coordinated attack configuration
- Add a dedicated player-side state object that can:
  - reset every tick
  - receive armor/accessory contributions
  - export a `CombatContextSnapshot`
- Keep `AdvancedPlayer` as the Terraria entry point, but move element stat ownership into a dedicated component object rather than adding dozens of loose fields to `AdvancedPlayer`

**Step 3: Run build to verify it fails where snapshots are still unused**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build may still succeed, but the new types are now available for the next tasks.

**Step 4: Refactor**

Do not wire content items yet. This task is only about introducing a stable aggregation target for equipment stats.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Data DamageClasses/Element/Players/ElementCombatPlayerState.cs Systems/Globals/AdvancedPlayer.cs
git commit -m "feat: add player-side element stat aggregation layer"
```

### Task 3: Thread CombatContextSnapshot Through the Hit Pipeline

**Files:**
- Modify: `DamageClasses/Element/Input/AttackInputResolveContext.cs`
- Modify: `DamageClasses/Element/Input/AttackInputModifierPipeline.cs`
- Modify: `DamageClasses/Element/Input/PayloadBuilder.cs`
- Modify: `DamageClasses/Element/Data/AttackPayload.cs`
- Modify: `DamageClasses/Element/ElementCombatSystem.cs`

**Step 1: Write the failing check**

Search for where `CombatContextSnapshot` is created or populated during real hits.

Run:

```bash
rg -n "new CombatContextSnapshot|ContextSnapshot =" DamageClasses/Element
```

Expected: payload context is still empty default data.

**Step 2: Write minimal implementation**

- Capture a real `CombatContextSnapshot` from the source player before payload creation
- Thread it through the input pipeline into `AttackPayload`
- Ensure projectile and item hits both receive the same source-player stat snapshot

**Step 3: Run verification**

Run:

```bash
rg -n "ContextSnapshot =" DamageClasses/Element
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected:

- real context assignment exists in the pipeline
- build succeeds

**Step 4: Commit**

```bash
git add DamageClasses/Element/Input DamageClasses/Element/Data/AttackPayload.cs DamageClasses/Element/ElementCombatSystem.cs
git commit -m "feat: thread player combat snapshot through element payloads"
```

### Task 4: Replace Appended Pseudo-Multipliers With a Real Damage Calculation Stage

**Files:**
- Create: `DamageClasses/Element/Services/ReactionDamageCalculator.cs`
- Create: `DamageClasses/Element/Data/ReactionDamageContext.cs`
- Modify: `DamageClasses/Element/ElementCombatSystem.cs`
- Modify: `DamageClasses/Element/Services/ReactionExecutor.cs`

**Step 1: Write the failing check**

Search for the current appended-damage implementation.

Run:

```bash
rg -n "BaseDamageFactor - 1f|SimpleStrikeNPC\\(" DamageClasses/Element
```

Expected:

- original hit damage is still being followed by an appended reaction strike
- no dedicated reaction damage calculator exists

**Step 2: Write minimal implementation**

- Add `ReactionDamageCalculator`
- For amplification reactions:
  - compute the modified original-hit multiplier from the reaction rule and current stat snapshot
  - return an amplification result, not an appended reaction damage packet
- For transformative reactions:
  - compute a standalone reaction damage result using the rule and stat snapshot
  - keep extra effects separate from the reaction essence
- Make `ElementCombatSystem` produce calculator input and `ReactionResult`
- Make `ReactionExecutor` respect the semantic category:
  - amplification path should not append a fake primary strike
  - transformative path may perform a real reaction strike

**Step 3: Run verification**

Run:

```bash
rg -n "BaseDamageFactor - 1f" DamageClasses/Element
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected:

- the old `- 1f` pseudo-amplification logic is gone
- build succeeds

**Step 4: Refactor**

Keep `ThunderWind` working even if it temporarily stays on the old-style branch. Do not redesign that prototype reaction in this task.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Services/ReactionDamageCalculator.cs DamageClasses/Element/Data/ReactionDamageContext.cs DamageClasses/Element/ElementCombatSystem.cs DamageClasses/Element/Services/ReactionExecutor.cs
git commit -m "refactor: add real reaction damage calculation stage"
```

### Task 5: Add Equipment Contribution APIs for Armor, Accessories, and Set Bonuses

**Files:**
- Create: `DamageClasses/Element/Interfaces/IElementStatProvider.cs`
- Create: `DamageClasses/Element/Interfaces/IElementArmorSetProvider.cs`
- Modify: `Systems/Globals/AdvancedPlayer.cs`
- Modify: `DamageClasses/Element/Players/ElementCombatPlayerState.cs`

**Step 1: Write the failing check**

Search for a unified way for content items to contribute element stats.

Run:

```bash
rg -n "UpdateAccessory|UpdateEquip|IsArmorSet|UpdateArmorSet|ElementCombatPlayerState|IElementStatProvider" Items DamageClasses Systems
```

Expected: item hooks exist, but no unified contribution interface exists yet.

**Step 2: Write minimal implementation**

- Add an interface for single-item stat contribution
- Add an interface or helper for armor set contribution
- Make player-side state collect contributions from equipped items each tick
- Keep the contribution API narrow:
  - additive stat mutation only
  - no direct damage execution
  - no direct NPC writes

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the contribution APIs in place.

**Step 4: Commit**

```bash
git add DamageClasses/Element/Interfaces Systems/Globals/AdvancedPlayer.cs DamageClasses/Element/Players/ElementCombatPlayerState.cs
git commit -m "feat: add equipment contribution interfaces for element stats"
```

### Task 6: Implement the First Full Armor Build Slice: Molten Bone Set

**Files:**
- Create: `Items/Armor/Element/MoltenBoneHelmet.cs`
- Create: `Items/Armor/Element/MoltenBoneChestplate.cs`
- Create: `Items/Armor/Element/MoltenBoneGreaves.cs`
- Modify: `Localization/zh-Hans.hjson`
- Modify: `Localization/en-US.hjson`

**Step 1: Write the failing check**

Search for the molten bone armor pieces.

Run:

```bash
rg -n "MoltenBone|熔骨" Items Localization
```

Expected: the first full element armor set does not exist yet.

**Step 2: Write minimal implementation**

- Add the three armor pieces with the document stats:
  - helmet: defense 8, +50 element mastery
  - chestplate: defense 9, +7% all-class damage
  - greaves: defense 8, +7% element crit rate
- Add the set bonus:
  - +10% element damage
  - -8% incoming damage
- Use the equipment contribution APIs from Task 5

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds and the armor set compiles cleanly.

**Step 4: Commit**

```bash
git add Items/Armor/Element Localization/zh-Hans.hjson Localization/en-US.hjson
git commit -m "feat: add molten bone elemental armor set"
```

### Task 7: Implement the First Accessory Build Slice: Advanced Element Bottle

**Files:**
- Create: `Items/Accessory/Element/AdvancedElementBottleBase.cs`
- Create: `Items/Accessory/Element/AdvancedElectroBottle.cs`
- Create: `Items/Accessory/Element/AdvancedPyroBottle.cs`
- Create: `Projectiles/Accessory/Element/ElementBottleOrb.cs`
- Modify: `Localization/zh-Hans.hjson`
- Modify: `Localization/en-US.hjson`

**Step 1: Write the failing check**

Search for an existing generalized coordinated-attack accessory.

Run:

```bash
rg -n "ElementBottle|CoordinatedAttack|协同攻击" Items Projectiles Localization
```

Expected: no generalized accessory-based coordinated attack implementation exists.

**Step 2: Write minimal implementation**

- Add a reusable base class for element bottle accessories
- Add at least two concrete variants first:
  - electro
  - pyro
- Add a coordinated attack orb projectile that:
  - travels quickly
  - carries the matching element
  - deals 10 elemental damage
  - applies buildup `(4)`
  - cannot penetrate tiles or enemies
- The accessory effect should:
  - grant +15% matching element damage
  - schedule the coordinated orb on hit through the player-side coordinated-attack state

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the first coordinated-attack accessories.

**Step 4: Commit**

```bash
git add Items/Accessory/Element Projectiles/Accessory/Element Localization/zh-Hans.hjson Localization/en-US.hjson
git commit -m "feat: add advanced element bottle accessories"
```

### Task 8: Add a Unified On-Hit Derived Attack Hook for Coordinated Attacks and Future Reaction Mutations

**Files:**
- Create: `DamageClasses/Element/Derived/DerivedAttackRequest.cs`
- Create: `DamageClasses/Element/Derived/DerivedAttackDispatcher.cs`
- Modify: `DamageClasses/Element/Services/ReactionExecutor.cs`
- Modify: `DamageClasses/Element/ElementCombatSystem.cs`

**Step 1: Write the failing check**

Search for a unified derived-attack dispatch path.

Run:

```bash
rg -n "DerivedAttack|CoordinatedAttack|DerivedEffect" DamageClasses/Element
```

Expected: no dispatcher exists for post-hit derived attacks.

**Step 2: Write minimal implementation**

- Add a neutral derived-attack request model
- Add a dispatcher that can spawn or schedule derived attacks after a successful hit
- Use it for coordinated attack orbs first
- Keep the design compatible with future:
  - reaction mutation accessories
  - extra attack segments
  - chain lightning

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds and coordinated attacks no longer require ad-hoc item logic.

**Step 4: Commit**

```bash
git add DamageClasses/Element/Derived DamageClasses/Element/Services/ReactionExecutor.cs DamageClasses/Element/ElementCombatSystem.cs
git commit -m "feat: add derived attack dispatch path"
```

### Task 9: Expand the Rule Table for Hydro and Dendro Readiness Without Shipping Full Content Yet

**Files:**
- Modify: `DamageClasses/Element/Definitions/ElementDefinitions.cs`
- Modify: `DamageClasses/Element/Definitions/ReactionDefinitions.cs`
- Modify: `DamageClasses/Element/Reactions/ReactionRuleRegistry.cs`
- Modify: `DamageClasses/Element/Reactions/ReactionTextResolver.cs`
- Modify: `DamageClasses/Element/State/ElementComponent.cs`
- Modify: `DamageClasses/Element/State/TemporaryElementStateView.cs`
- Modify: `DamageClasses/Element/Services/ReactionExecutor.cs`

**Step 1: Write the failing check**

Search for hydro and dendro support in runtime state and rule definitions.

Run:

```bash
rg -n "Hydro|Dendro|ElectroCharged|Vaporize" DamageClasses/Element
```

Expected: only design-facing placeholders exist, not runtime-ready definitions.

**Step 2: Write minimal implementation**

- Add runtime state slots for `Hydro` and `Dendro`
- Add rule and text placeholders for:
  - `ElectroCharged`
  - `Vaporize`
- Do not implement their full visual/chain behaviors in this task
- Only prepare the runtime shape so the next content phase does not require another structural refactor

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with hydro and dendro runtime readiness.

**Step 4: Commit**

```bash
git add DamageClasses/Element/Definitions DamageClasses/Element/Reactions DamageClasses/Element/State DamageClasses/Element/Services/ReactionExecutor.cs
git commit -m "refactor: prepare runtime shape for hydro and dendro reactions"
```

### Task 10: Final Verification and Manual Checklist

**Files:**
- Verify only

**Step 1: Run build verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 2: Manual in-game verification checklist**

Verify all of the following:

1. `可燃物发射器` still applies `Pyro(5)`.
2. `超载` still triggers correctly and shows `超载`.
3. amplification reactions no longer fake their damage by appending `倍率 - 1` packets.
4. `熔骨套装` bonuses appear in combat behavior:
   - element damage increase
   - incoming damage reduction
   - mastery increase
   - element crit increase
5. an equipped advanced element bottle spawns the coordinated orb on hit.
6. the coordinated orb carries the correct element and buildup.
7. old prototype accessories still compile and do not break unrelated content.

**Step 3: Commit**

```bash
git add .
git commit -m "feat: establish element build stat framework"
```
