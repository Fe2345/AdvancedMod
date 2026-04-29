# Element Combat Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the first working scaffold of the new element combat subsystem under `DamageClasses/Element`, replace the old `DoT` damage class with `ElementDamage`, and establish the main data flow objects needed for later hit processing.

**Architecture:** The implementation starts by migrating the damage class and creating the new `DamageClasses/Element` namespace tree. Then it adds the core immutable data objects, builder/plan objects, and service shells for the new pipeline. Runtime hooks into items, projectiles, players, and NPCs stay shallow in this pass; the goal is a compilable backbone, not a full reaction system yet.

**Tech Stack:** C# 12, .NET 8, tModLoader API, ReLogic, FNA

---

### Task 1: Replace `DoT` DamageClass With `ElementDamage`

**Files:**
- Create: `DamageClasses/Element/ElementDamage.cs`
- Modify: `Items/Weapon/DoT/WindFlower.cs`
- Modify: `Projectiles/Weapons/DoT/WindFlowerProj.cs`
- Modify: `Localization/en-US.hjson`
- Modify: `Localization/zh-Hans.hjson`
- Delete or stop using: `DamageClasses/DoT.cs`

**Step 1: Write the failing check**

Search for remaining direct usage of the old damage class.

Run:

```bash
rg -n "DamageClasses\\.DoT|ModContent\\.GetInstance<global::AdvancedMod\\.DamageClasses\\.DoT>" .
```

Expected: matches in current item, projectile, localization, and old class file.

**Step 2: Verify current state before edits**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: current baseline build result recorded before migration.

**Step 3: Write minimal implementation**

- Add `DamageClasses/Element/ElementDamage.cs`
- Move the behavior from `DamageClasses/DoT.cs` into `ElementDamage`
- Update `WindFlower` and `WindFlowerProj` to use `ElementDamage`
- Rename localization keys from `DamageClasses.DoT.DisplayName` to `DamageClasses.ElementDamage.DisplayName`
- Remove `DamageClasses/DoT.cs` if no longer referenced

**Step 4: Run verification**

Run:

```bash
rg -n "DamageClasses\\.DoT|ModContent\\.GetInstance<global::AdvancedMod\\.DamageClasses\\.DoT>" .
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected:
- `rg` returns no live references
- build succeeds

**Step 5: Commit**

```bash
git add DamageClasses/Element/ElementDamage.cs Items/Weapon/DoT/WindFlower.cs Projectiles/Weapons/DoT/WindFlowerProj.cs Localization/en-US.hjson Localization/zh-Hans.hjson DamageClasses/DoT.cs
git commit -m "refactor: replace dot damage class with element damage"
```

### Task 2: Create Core Element Domain Types

**Files:**
- Create: `DamageClasses/Element/Definitions/ElementType.cs`
- Create: `DamageClasses/Element/Definitions/ReactionType.cs`
- Create: `DamageClasses/Element/Definitions/AttackSourceKind.cs`
- Create: `DamageClasses/Element/Definitions/AttackFlags.cs`
- Create: `DamageClasses/Element/Definitions/ElementApplicationFlags.cs`
- Create: `DamageClasses/Element/Definitions/ReactionConsumeMode.cs`
- Create: `DamageClasses/Element/Definitions/ReactionDamageModel.cs`

**Step 1: Write the failing check**

Create the minimal compile target in the next task assuming these types exist.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: future references to these types will fail until the definitions exist.

**Step 2: Write minimal implementation**

Add the basic enums and flags only. Do not add behavior yet.

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the new definition types present.

**Step 4: Refactor**

Keep names aligned with `docs/ELEMENT_COMBAT_SUBSYSTEM.md`. Avoid adding extra enum values not yet used by the plan.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Definitions
git commit -m "feat: add core element combat definitions"
```

### Task 3: Add Input-Side Core Objects

**Files:**
- Create: `DamageClasses/Element/Data/AttackElementSeed.cs`
- Create: `DamageClasses/Element/Data/AttackSourceIntent.cs`
- Create: `DamageClasses/Element/Data/ElementApplication.cs`
- Create: `DamageClasses/Element/Data/AttackPayload.cs`
- Create: `DamageClasses/Element/Data/CombatContextSnapshot.cs`
- Create: `DamageClasses/Element/Data/ReactionStatSnapshot.cs`

**Step 1: Write the failing check**

Reference these types from a new pipeline shell in Task 4 and confirm the build fails because they do not yet exist.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing input/domain data types.

**Step 2: Write minimal implementation**

Add immutable or init-only shells with the fields already agreed in the design doc:

- `AttackSourceIntent`
- `AttackElementSeed`
- `ElementApplication`
- `AttackPayload`
- `CombatContextSnapshot`
- `ReactionStatSnapshot`

For `ReactionStatSnapshot`, include the reduced multiplier-bucket model now documented, not the older split-bucket version.

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Refactor**

Make field naming consistent across `Intent`, `Payload`, and later calculator contexts.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Data
git commit -m "feat: add element combat input data objects"
```

### Task 4: Add Input Modifier Pipeline Shell

**Files:**
- Create: `DamageClasses/Element/Input/AttackInputResolveContext.cs`
- Create: `DamageClasses/Element/Input/IAttackInputModifier.cs`
- Create: `DamageClasses/Element/Input/AttackInputBuilder.cs`
- Create: `DamageClasses/Element/Input/InputAugmentPlan.cs`
- Create: `DamageClasses/Element/Input/AttackInputModifierPipeline.cs`
- Create: `DamageClasses/Element/Input/PayloadBuilder.cs`

**Step 1: Write the failing check**

Create the service shells first and confirm the build fails because the interfaces and contexts do not exist yet.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing input pipeline types.

**Step 2: Write minimal implementation**

- Add the context, interface, builder, plan, pipeline, and payload builder shells
- Keep behavior minimal: the pipeline may simply copy `AttackSourceIntent` into `InputAugmentPlan` and `AttackPayload`
- Add trace/debug list fields but do not wire UI yet

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Refactor**

Ensure the input builder exposes only additive or replacing methods agreed in the design doc.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Input
git commit -m "feat: add input augmentation pipeline scaffold"
```

### Task 5: Add Target State and Apply/Consume Plans

**Files:**
- Create: `DamageClasses/Element/State/ElementState.cs`
- Create: `DamageClasses/Element/State/DotState.cs`
- Create: `DamageClasses/Element/State/ApplyCooldownState.cs`
- Create: `DamageClasses/Element/State/ElementComponent.cs`
- Create: `DamageClasses/Element/State/TemporaryElementStateView.cs`
- Create: `DamageClasses/Element/Plans/ElementApplyEntry.cs`
- Create: `DamageClasses/Element/Plans/ElementApplyPlan.cs`
- Create: `DamageClasses/Element/Plans/ElementConsumptionEntry.cs`
- Create: `DamageClasses/Element/Plans/ElementConsumptionPlan.cs`

**Step 1: Write the failing check**

Reference these types from the service shells in Task 6 and confirm the build fails because they do not yet exist.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing state and plan types.

**Step 2: Write minimal implementation**

Add state holders and simple plan objects only. Do not implement full rules yet.

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Refactor**

Keep all mutable state inside state containers and builders; plan objects should stay immutable or init-only.

**Step 5: Commit**

```bash
git add DamageClasses/Element/State DamageClasses/Element/Plans
git commit -m "feat: add element state and plan objects"
```

### Task 6: Add Core Reaction and Damage Calculation Shells

**Files:**
- Create: `DamageClasses/Element/Reactions/ReactionRule.cs`
- Create: `DamageClasses/Element/Reactions/ReactionCandidate.cs`
- Create: `DamageClasses/Element/Reactions/ReactionSelectionResult.cs`
- Create: `DamageClasses/Element/Reactions/ReactionTransformContext.cs`
- Create: `DamageClasses/Element/Reactions/IReactionOutcomeModifier.cs`
- Create: `DamageClasses/Element/Reactions/ReactionTransformBuilder.cs`
- Create: `DamageClasses/Element/Reactions/ReactionTransformPlan.cs`
- Create: `DamageClasses/Element/Reactions/ReactionDamageContext.cs`
- Create: `DamageClasses/Element/Reactions/DamageInstancePlan.cs`
- Create: `DamageClasses/Element/Reactions/ReactionResult.cs`

**Step 1: Write the failing check**

Reference these types from the calculator services in the next step and verify missing-type compile failures.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing reaction and result types.

**Step 2: Write minimal implementation**

Add the shells only, with field names matching the final design doc:

- reduced multiplier-bucket model in reaction damage stats
- support for transformed reaction type
- support for consumption overrides and extra effect plans

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Refactor**

Remove fields that duplicate information already held in payload or plans.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Reactions
git commit -m "feat: add reaction and damage result shells"
```

### Task 7: Add Service Shells and Main System Orchestrator

**Files:**
- Create: `DamageClasses/Element/Services/ElementApplyService.cs`
- Create: `DamageClasses/Element/Services/DotService.cs`
- Create: `DamageClasses/Element/Services/ReactionCandidateCollector.cs`
- Create: `DamageClasses/Element/Services/ReactionValidator.cs`
- Create: `DamageClasses/Element/Services/ReactionPriorityResolver.cs`
- Create: `DamageClasses/Element/Services/ReactionOutcomeModifierPipeline.cs`
- Create: `DamageClasses/Element/Services/ReactionConsumptionCalculator.cs`
- Create: `DamageClasses/Element/Services/ReactionDamageCalculator.cs`
- Create: `DamageClasses/Element/Services/ReactionResultBuilder.cs`
- Create: `DamageClasses/Element/Services/ReactionExecutor.cs`
- Create: `DamageClasses/Element/ElementCombatSystem.cs`

**Step 1: Write the failing check**

Create the orchestrator with references to all services and verify the build fails because the service files do not yet exist.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing services.

**Step 2: Write minimal implementation**

- Add service shells with method signatures only
- Wire `ElementCombatSystem.ProcessHit(...)`
- Keep first pass behavior no-op or pass-through where logic is not implemented yet
- Ensure `ReactionDamageCalculator` consumes the new reduced multiplier-bucket structure

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Refactor**

Keep orchestration in `ElementCombatSystem`; do not let calculators reach into Terraria hooks directly.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Services DamageClasses/Element/ElementCombatSystem.cs
git commit -m "feat: add element combat service scaffold"
```

### Task 8: Add Trace and Event Infrastructure

**Files:**
- Create: `DamageClasses/Element/Tracing/CombatTrace.cs`
- Create: `DamageClasses/Element/Tracing/CombatTraceEntry.cs`
- Create: `DamageClasses/Element/Tracing/CombatEvent.cs`
- Create: `DamageClasses/Element/Tracing/ElementAppliedEvent.cs`
- Create: `DamageClasses/Element/Tracing/ReactionTriggeredEvent.cs`
- Create: `DamageClasses/Element/Tracing/ReactionTransformedEvent.cs`
- Create: `DamageClasses/Element/Tracing/ReactionDamageDealtEvent.cs`
- Create: `DamageClasses/Element/Tracing/ResourceGrantedEvent.cs`
- Create: `DamageClasses/Element/Tracing/HitProcessResult.cs`

**Step 1: Write the failing check**

Reference the trace and event types from `ReactionExecutor` and `ElementCombatSystem`, then verify the build fails because they do not exist yet.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing trace/event types.

**Step 2: Write minimal implementation**

Add immutable event and trace objects only. Do not add UI integration yet.

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Refactor**

Keep events semantic and stable. Do not expose internal candidate arrays in public event types.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Tracing
git commit -m "feat: add element combat trace and event models"
```

### Task 9: Add Terraria Adapter Shells

**Files:**
- Modify: `Systems/Globals/AdvancedPlayer.cs`
- Modify: `Systems/Globals/AdvancedGlobelNPC.cs`
- Modify: `Systems/Globals/AdvancedGlobelItem.cs`
- Create: `DamageClasses/Element/Adapters/PlayerElementAdapter.cs`
- Create: `DamageClasses/Element/Adapters/ItemElementAdapter.cs`
- Create: `DamageClasses/Element/Adapters/ProjectileElementAdapter.cs`
- Create: `DamageClasses/Element/Adapters/NpcElementAdapter.cs`

**Step 1: Write the failing check**

Reference the adapter types from the existing global classes and verify compile failures until the adapter classes exist.

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing adapters.

**Step 2: Write minimal implementation**

- Keep existing globals thin
- Add adapter shells and expose only the minimal state accessors needed now
- Do not migrate old DoT behavior into the new adapters in this pass

**Step 3: Run verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Refactor**

Ensure global Terraria hooks delegate into adapters or new services instead of embedding new combat logic inline.

**Step 5: Commit**

```bash
git add Systems/Globals/AdvancedPlayer.cs Systems/Globals/AdvancedGlobelNPC.cs Systems/Globals/AdvancedGlobelItem.cs DamageClasses/Element/Adapters
git commit -m "feat: add terraria adapter scaffolds for element combat"
```

### Task 10: Final Verification Pass

**Files:**
- Review only: `docs/ELEMENT_COMBAT_SUBSYSTEM.md`
- Review only: `docs/plans/2026-04-26-element-combat-implementation.md`

**Step 1: Run project-wide verification**

Run:

```bash
pwsh.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the full scaffold in place.

**Step 2: Run reference checks**

Run:

```bash
rg -n "DamageClasses\\.DoT|ModContent\\.GetInstance<global::AdvancedMod\\.DamageClasses\\.DoT>" .
rg -n "namespace AdvancedMod\\.DamageClasses\\.Element" DamageClasses/Element
```

Expected:
- no old `DoT` damage class references remain
- all new files live under the expected namespace tree

**Step 3: Manual review**

Confirm:
- no compatibility logic for the old system was added
- new files live under `DamageClasses/Element`
- reduced multiplier-bucket model is the one actually encoded in types

**Step 4: Commit**

```bash
git add .
git commit -m "feat: scaffold new element combat subsystem"
```

