# Overloaded Formal Chain Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the first formal reaction chain slice for the element system by migrating thunder-wind to the new pipeline, adding pyro and overloaded, and wiring the combustible ejector into that shared flow.

**Architecture:** The implementation keeps the existing input-side pipeline and replaces the prototype in-method reaction logic with explicit apply-plan, temporary-state, candidate collection, validation, resolution, and execution layers. The first formal slice supports only thunder-wind and overloaded, but the structure must be stable enough to keep adding reactions and weapons without rewriting `ElementCombatSystem`.

**Tech Stack:** C# 12, .NET 8, tModLoader API, ReLogic, FNA

---

### Task 1: Add Formal Rule and Reaction Text Registries

**Files:**
- Modify: `DamageClasses/Element/Definitions/ElementDefinitions.cs`
- Modify: `DamageClasses/Element/Definitions/ReactionDefinitions.cs`
- Create: `DamageClasses/Element/Reactions/ReactionRuleRegistry.cs`
- Create: `DamageClasses/Element/Reactions/ReactionTextResolver.cs`

**Step 1: Write the failing check**

Search for the current hard-coded prototype reaction text and the missing overloaded symbols.

Run:

```bash
rg -n "\"TW\"|Overloaded|Pyro|ReactionRuleRegistry|ReactionTextResolver" DamageClasses/Element
```

Expected:

- `"TW"` is still hard-coded in `ElementCombatSystem`
- `Overloaded`, `Pyro`, `ReactionRuleRegistry`, and `ReactionTextResolver` do not yet exist

**Step 2: Run build before edits**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: baseline build succeeds before the formal-chain refactor starts.

**Step 3: Write minimal implementation**

- Add `Pyro` to `ElementType`
- Add `Overloaded` to `ReactionType`
- Add a `Priority` and `IgnoreOrder` field to `ReactionRule`
- Create `ReactionRuleRegistry` with two rules:
  - `ThunderWind`
  - `Overloaded`
- Create `ReactionTextResolver` that returns:
  - `ReactionType.ThunderWind -> 雷风`
  - `ReactionType.Overloaded -> 超载`

**Step 4: Run verification**

Run:

```bash
rg -n "\"TW\"|Overloaded|Pyro|ReactionRuleRegistry|ReactionTextResolver" DamageClasses/Element
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected:

- new registry and text resolver symbols exist
- build succeeds

**Step 5: Commit**

```bash
git add DamageClasses/Element/Definitions/ElementDefinitions.cs DamageClasses/Element/Definitions/ReactionDefinitions.cs DamageClasses/Element/Reactions/ReactionRule.cs DamageClasses/Element/Reactions/ReactionRuleRegistry.cs DamageClasses/Element/Reactions/ReactionTextResolver.cs
git commit -m "feat: add formal reaction registry and text resolver"
```

### Task 2: Add Apply Plan, Temporary State View, and Candidate Types

**Files:**
- Create: `DamageClasses/Element/Plans/ElementApplyEntry.cs`
- Create: `DamageClasses/Element/Plans/ElementApplyPlan.cs`
- Create: `DamageClasses/Element/State/TemporaryElementStateView.cs`
- Create: `DamageClasses/Element/Reactions/ReactionCandidate.cs`
- Create: `DamageClasses/Element/Reactions/ReactionValidationResult.cs`

**Step 1: Write the failing check**

Reference the new plan and candidate types from `ElementCombatSystem`, then verify the build fails because the types do not exist yet.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing apply-plan, temporary-view, candidate, and validation-result types.

**Step 2: Write minimal implementation**

- Add `ElementApplyEntry`
- Add `ElementApplyPlan`
- Add `TemporaryElementStateView` with:
  - projected gauge calculation
  - current gauge lookup
- Add `ReactionCandidate`
- Add `ReactionValidationResult`

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the new types present.

**Step 4: Refactor**

Keep object fields aligned with the approved design doc. Do not add DOT, event, or trace fields in this task.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Plans DamageClasses/Element/State/TemporaryElementStateView.cs DamageClasses/Element/Reactions/ReactionCandidate.cs DamageClasses/Element/Reactions/ReactionValidationResult.cs
git commit -m "feat: add formal apply plan and reaction candidate types"
```

### Task 3: Implement Candidate Collection, Validation, and Resolution

**Files:**
- Create: `DamageClasses/Element/Services/ReactionCandidateCollector.cs`
- Create: `DamageClasses/Element/Services/ReactionValidator.cs`
- Create: `DamageClasses/Element/Services/ReactionPriorityResolver.cs`

**Step 1: Write the failing check**

Create references from `ElementCombatSystem` to the three services and verify the build fails because the services do not yet exist.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on the missing collection, validation, and resolution services.

**Step 2: Write minimal implementation**

- `ReactionCandidateCollector`
  - iterate payload applications
  - query `ReactionRuleRegistry`
  - emit possible candidates
- `ReactionValidator`
  - support `ReactionConsumeMode.Fixed` only
  - validate against the projected gauges in `TemporaryElementStateView`
- `ReactionPriorityResolver`
  - choose the highest `Priority`
  - break ties by registry order

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the three services compiled.

**Step 4: Refactor**

Do not move any real write-back logic into these services. They are read-only computation layers.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Services/ReactionCandidateCollector.cs DamageClasses/Element/Services/ReactionValidator.cs DamageClasses/Element/Services/ReactionPriorityResolver.cs
git commit -m "feat: add formal reaction selection services"
```

### Task 4: Implement Unified Reaction Execution and Refactor ElementCombatSystem

**Files:**
- Create: `DamageClasses/Element/Services/ReactionExecutor.cs`
- Modify: `DamageClasses/Element/Reactions/ReactionResult.cs`
- Modify: `DamageClasses/Element/State/ElementComponent.cs`
- Modify: `DamageClasses/Element/ElementCombatSystem.cs`

**Step 1: Write the failing check**

Replace the prototype inline logic in `ElementCombatSystem` with references to `ElementApplyPlan`, `TemporaryElementStateView`, the selection services, and `ReactionExecutor`, then verify the build fails until `ReactionExecutor` and the new result shape exist.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing executor methods or outdated `ReactionResult` shape.

**Step 2: Write minimal implementation**

- Expand `ReactionResult` so it can carry:
  - selected reaction type
  - consume amounts
  - primary damage
  - extra damages
  - display text
  - overload knockback placeholder or value
- Implement `ReactionExecutor` as the only write-back entry:
  - apply gauge from `ElementApplyPlan`
  - consume gauge from the selected validation result
  - strike reaction damage
  - apply extra damage
  - write `LastReactionText`
  - show `CombatText`
  - apply overload knockback
- Refactor `ElementCombatSystem` into orchestration only

**Step 3: Run verification**

Run:

```bash
rg -n "\"TW\"" DamageClasses/Element
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected:

- no remaining hard-coded `"TW"` in element combat logic
- build succeeds

**Step 4: Refactor**

Do not add `ReactionOutcomeModifierPipeline` in this task. Keep the executor minimal and stable.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Services/ReactionExecutor.cs DamageClasses/Element/Reactions/ReactionResult.cs DamageClasses/Element/State/ElementComponent.cs DamageClasses/Element/ElementCombatSystem.cs
git commit -m "refactor: move element combat to formal execution pipeline"
```

### Task 5: Add Pyro Weapon Input and Overloaded Behavior

**Files:**
- Modify: `Items/Weapon/Element/CombustibleEjector.cs`
- Modify: `Projectiles/Weapons/Element/BurnedGel.cs`
- Modify: `Localization/zh-Hans.hjson`
- Modify: `Localization/en-US.hjson`

**Step 1: Write the failing check**

Search the current combustible ejector and burned gel files for missing usable defaults and missing pyro-element hit integration.

Run:

```bash
rg -n "CombustibleEjector|BurnedGel|ElementCombatSystem|Pyro" Items/Weapon/Element Projectiles/Weapons/Element Localization
```

Expected:

- `CombustibleEjector` only has a placeholder default
- `BurnedGel` is not yet wired as a pyro formal-chain hit source
- localization entries for the new weapon are missing or incomplete

**Step 2: Run build before edits**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: baseline formal-chain build succeeds before adding the new weapon slice.

**Step 3: Write minimal implementation**

- Make `CombustibleEjector` a usable launcher-style weapon:
  - damage `20`
  - use time `30`
  - consume gel
  - shoot `BurnedGel`
- Wire `BurnedGel` to call the formal element combat entry with `Pyro(5)`
- Add localization for `CombustibleEjector` and any missing projectile display text

**Step 4: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the new weapon wired.

**Step 5: Commit**

```bash
git add Items/Weapon/Element/CombustibleEjector.cs Projectiles/Weapons/Element/BurnedGel.cs Localization/zh-Hans.hjson Localization/en-US.hjson
git commit -m "feat: add combustible ejector pyro input slice"
```

### Task 6: Verify In-Game Slice and Preserve Prototype Compatibility

**Files:**
- Verify only:
  - `Items/Weapon/Element/PrototypeThunderSword.cs`
  - `Items/Weapon/Element/PrototypeWindSword.cs`
  - `Items/Weapon/Magic/PrototypeThunderStaff.cs`
  - `Projectiles/Weapons/PrototypeThunderBolt.cs`
  - `Items/Accessory/PrototypeWindEnchantAccessory.cs`
  - `Items/Accessory/PrototypeReactionWindBurstAccessory.cs`

**Step 1: Write the failing check**

Search for prototype direct-reaction assumptions that may have been invalidated by the formal-chain refactor.

Run:

```bash
rg -n "PrototypeForceWindElement|PrototypeReactionExtraWindBurst|ProcessItemHit|ProcessProjectileHit" Items Projectiles DamageClasses/Element Systems
```

Expected: prototype sources still point at the shared entry points and can now be verified against the refactored pipeline.

**Step 2: Run full verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds after the full refactor and new weapon integration.

**Step 3: Manual in-game verification checklist**

Verify all of the following:

1. `PrototypeWindSword` can apply wind gauge.
2. `PrototypeThunderSword` can react with stored wind and show `雷风`.
3. `PrototypeThunderStaff` projectile can also trigger `雷风`.
4. `PrototypeWindEnchantAccessory` still forces prototype element input to wind.
5. `PrototypeReactionWindBurstAccessory` still appends its extra reaction damage.
6. `可燃物发射器` can apply `Pyro(5)`.
7. hitting a target with stored thunder can trigger `超载`.
8. `超载` displays the correct text and applies visible knockback.

**Step 4: Commit**

```bash
git add .
git commit -m "feat: add overloaded to formal reaction chain"
```
