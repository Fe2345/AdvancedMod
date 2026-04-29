# Thunder Wind Prototype Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a playable in-game vertical slice for the new element system with thunder/wind elements, one shared reaction, three test weapons, two test accessories, and NPC element/reaction text feedback.

**Architecture:** This slice keeps the rules intentionally narrow: two elements, one reaction, fixed element gain/consume values, and fixed reaction damage scaling from source hit damage. The work proceeds from low-level definitions and state containers, into a minimal `ProcessHit` chain, then into concrete Terraria content and finally lightweight NPC text display for validation.

**Tech Stack:** C# 12, .NET 8, tModLoader API, ReLogic, FNA

---

### Task 1: Consolidate Definition Files and Add XML Comments to Existing Core Types

**Files:**
- Create: `DamageClasses/Element/Definitions/AttackDefinitions.cs`
- Create: `DamageClasses/Element/Definitions/ElementDefinitions.cs`
- Create: `DamageClasses/Element/Definitions/ReactionDefinitions.cs`
- Delete: `DamageClasses/Element/Definitions/AttackFlags.cs`
- Delete: `DamageClasses/Element/Definitions/AttackSourceKind.cs`
- Delete: `DamageClasses/Element/Definitions/ElementApplicationFlags.cs`
- Delete: `DamageClasses/Element/Definitions/ElementType.cs`
- Delete: `DamageClasses/Element/Definitions/ReactionConsumeMode.cs`
- Delete: `DamageClasses/Element/Definitions/ReactionDamageModel.cs`
- Delete: `DamageClasses/Element/Definitions/ReactionType.cs`
- Modify: `DamageClasses/Element/ElementDamage.cs`
- Modify: `DamageClasses/Element/Data/AttackElementSeed.cs`
- Modify: `DamageClasses/Element/Data/AttackPayload.cs`
- Modify: `DamageClasses/Element/Data/AttackSourceIntent.cs`
- Modify: `DamageClasses/Element/Data/CombatContextSnapshot.cs`
- Modify: `DamageClasses/Element/Data/ElementApplication.cs`
- Modify: `DamageClasses/Element/Data/ReactionDamageModifierSet.cs`
- Modify: `DamageClasses/Element/Data/ReactionStatSnapshot.cs`

**Step 1: Write the failing check**

Run:

```bash
rg -n "enum AttackSourceKind|enum ElementType|enum ReactionType" DamageClasses/Element/Definitions
```

Expected: the enums are still split into multiple files.

**Step 2: Run build before edits**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: baseline build succeeds before the refactor.

**Step 3: Write minimal implementation**

- Merge attack-related definitions into `AttackDefinitions.cs`
- Merge element-related definitions into `ElementDefinitions.cs`
- Merge reaction-related definitions into `ReactionDefinitions.cs`
- Add XML summary comments to the definition file contents and all current core data objects
- Do not change behavior in this task

**Step 4: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds with the consolidated definition layout.

**Step 5: Commit**

```bash
git add DamageClasses/Element/Definitions DamageClasses/Element/ElementDamage.cs DamageClasses/Element/Data
git commit -m "refactor: consolidate element definitions and add xml docs"
```

### Task 2: Define Thunder/Wind Prototype Enums and Reaction Rule Shell

**Files:**
- Modify: `DamageClasses/Element/Definitions/ElementDefinitions.cs`
- Modify: `DamageClasses/Element/Definitions/ReactionDefinitions.cs`
- Create: `DamageClasses/Element/Reactions/ReactionRule.cs`

**Step 1: Write the failing check**

Create a compile reference that expects explicit thunder/wind and thunder-wind reaction symbols.

Run:

```bash
rg -n "Thunder|Wind|ThunderWind" DamageClasses/Element
```

Expected: no such prototype symbols yet.

**Step 2: Write minimal implementation**

- Add `Thunder` and `Wind` to the element enum
- Add one prototype reaction enum value for the shared thunder-wind reaction
- Add `ReactionRule` with only the fields needed for this prototype:
  - reaction type
  - trigger element
  - target element
  - fixed consume values
  - base damage factor
  - damage output element

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add DamageClasses/Element/Definitions DamageClasses/Element/Reactions/ReactionRule.cs
git commit -m "feat: add thunder wind prototype definitions"
```

### Task 3: Add Minimal NPC Element State Container for Thunder/Wind

**Files:**
- Create: `DamageClasses/Element/State/ElementState.cs`
- Create: `DamageClasses/Element/State/ElementComponent.cs`
- Create: `DamageClasses/Element/Adapters/NpcElementAdapter.cs`
- Modify: `Systems/Globals/AdvancedGlobelNPC.cs`

**Step 1: Write the failing check**

Add a compile reference from `AdvancedGlobelNPC` to `NpcElementAdapter`.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failure until the adapter and state types exist.

**Step 2: Write minimal implementation**

- `ElementState` only needs current gauge for now
- `ElementComponent` only needs thunder and wind storage plus last reaction text/tick placeholders
- `NpcElementAdapter` should expose or own the component
- `AdvancedGlobelNPC` should delegate to the adapter rather than embed new state directly

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add DamageClasses/Element/State DamageClasses/Element/Adapters/NpcElementAdapter.cs Systems/Globals/AdvancedGlobelNPC.cs
git commit -m "feat: add minimal npc element state container"
```

### Task 4: Build the Minimal `ProcessHit` Chain

**Files:**
- Create: `DamageClasses/Element/Input/AttackInputResolveContext.cs`
- Create: `DamageClasses/Element/Input/IAttackInputModifier.cs`
- Create: `DamageClasses/Element/Input/AttackInputBuilder.cs`
- Create: `DamageClasses/Element/Input/InputAugmentPlan.cs`
- Create: `DamageClasses/Element/Input/AttackInputModifierPipeline.cs`
- Create: `DamageClasses/Element/Input/PayloadBuilder.cs`
- Create: `DamageClasses/Element/Reactions/ReactionResult.cs`
- Create: `DamageClasses/Element/ElementCombatSystem.cs`

**Step 1: Write the failing check**

Create `ElementCombatSystem` with references to input objects that do not yet exist.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing input pipeline types.

**Step 2: Write minimal implementation**

Implement only what the prototype needs:

- build payload from an intent
- optionally let input modifiers replace the element
- apply `+1` element gauge
- check the single thunder-wind reaction rule
- consume `1` from both sides when reaction happens
- compute reaction damage as `payload.BaseDamage * 1.5f`

Do not implement:

- multiple reaction competition
- DOT
- generic effect spawning
- full trace/event model

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add DamageClasses/Element/Input DamageClasses/Element/Reactions/ReactionResult.cs DamageClasses/Element/ElementCombatSystem.cs
git commit -m "feat: add minimal element combat process chain"
```

### Task 5: Add Prototype Reaction Output Modification

**Files:**
- Create: `DamageClasses/Element/Reactions/ReactionTransformContext.cs`
- Create: `DamageClasses/Element/Reactions/IReactionOutcomeModifier.cs`
- Create: `DamageClasses/Element/Reactions/ReactionTransformBuilder.cs`
- Create: `DamageClasses/Element/Reactions/ReactionTransformPlan.cs`
- Create: `DamageClasses/Element/Reactions/DamageInstancePlan.cs`

**Step 1: Write the failing check**

Wire `ElementCombatSystem` to optionally pass reaction results through a transform stage.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failure until transform objects exist.

**Step 2: Write minimal implementation**

- support one optional extra damage plan
- support one optional override element for the extra damage
- keep the default thunder reaction result intact
- no generic modifier registry yet beyond what this prototype requires

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add DamageClasses/Element/Reactions
git commit -m "feat: add prototype reaction transform support"
```

### Task 6: Add Three Prototype Weapons

**Files:**
- Create: `Items/Weapon/Melee/PrototypeThunderSword.cs`
- Create: `Items/Weapon/Melee/PrototypeWindSword.cs`
- Create: `Items/Weapon/Magic/PrototypeThunderStaff.cs`
- Create: `Projectiles/Weapons/PrototypeThunderBolt.cs`
- Modify: `Localization/en-US.hjson`
- Modify: `Localization/zh-Hans.hjson`

**Step 1: Write the failing check**

Add the weapon and projectile file references with placeholder classes and confirm the build fails until they are implemented correctly.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures for incomplete content classes.

**Step 2: Write minimal implementation**

- thunder sword: melee hit produces thunder intent
- wind sword: melee hit produces wind intent
- thunder staff: shoots a simple straight projectile using existing vanilla visuals
- projectile hit produces thunder intent

Reuse vanilla or already-present textures where possible. No new custom art in this task.

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add Items/Weapon/Melee/PrototypeThunderSword.cs Items/Weapon/Melee/PrototypeWindSword.cs Items/Weapon/Magic/PrototypeThunderStaff.cs Projectiles/Weapons/PrototypeThunderBolt.cs Localization/en-US.hjson Localization/zh-Hans.hjson
git commit -m "feat: add thunder wind prototype weapons"
```

### Task 7: Add Two Prototype Accessories

**Files:**
- Create: `Items/Accessory/PrototypeWindEnchantAccessory.cs`
- Create: `Items/Accessory/PrototypeReactionWindBurstAccessory.cs`
- Modify: `Systems/Globals/AdvancedPlayer.cs`
- Modify: `Localization/en-US.hjson`
- Modify: `Localization/zh-Hans.hjson`

**Step 1: Write the failing check**

Reference two new player flags in `AdvancedPlayer` and confirm the build fails until the accessory content exists.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing accessory content and player flags.

**Step 2: Write minimal implementation**

- accessory 1 sets a player flag that forces all attack input to wind
- accessory 2 sets a player flag that appends one extra `10000` wind damage instance on every successful reaction

Keep the logic centralized in the element system rather than inside item classes.

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add Items/Accessory/PrototypeWindEnchantAccessory.cs Items/Accessory/PrototypeReactionWindBurstAccessory.cs Systems/Globals/AdvancedPlayer.cs Localization/en-US.hjson Localization/zh-Hans.hjson
git commit -m "feat: add prototype element test accessories"
```

### Task 8: Connect Terraria Hit Hooks to the Prototype System

**Files:**
- Create: `DamageClasses/Element/Adapters/PlayerElementAdapter.cs`
- Create: `DamageClasses/Element/Adapters/ItemElementAdapter.cs`
- Create: `DamageClasses/Element/Adapters/ProjectileElementAdapter.cs`
- Modify: `Systems/Globals/AdvancedGlobelItem.cs`
- Modify: `Projectiles/Weapons/PrototypeThunderBolt.cs`
- Modify: `Items/Weapon/Melee/PrototypeThunderSword.cs`
- Modify: `Items/Weapon/Melee/PrototypeWindSword.cs`

**Step 1: Write the failing check**

Add hook calls to the adapter layer and confirm compile failure until the adapter files exist and signatures match.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failures on missing adapters.

**Step 2: Write minimal implementation**

- melee weapons call into the system on direct hit
- projectile calls into the system on projectile hit
- player flags from accessories are readable by the combat system

Do not generalize to every item in the mod. Only connect the prototype content.

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add DamageClasses/Element/Adapters Systems/Globals/AdvancedGlobelItem.cs Projectiles/Weapons/PrototypeThunderBolt.cs Items/Weapon/Melee/PrototypeThunderSword.cs Items/Weapon/Melee/PrototypeWindSword.cs
git commit -m "feat: connect prototype weapons to element combat hooks"
```

### Task 9: Add NPC Element Text and Reaction Text Display

**Files:**
- Modify: `Systems/Globals/AdvancedGlobelNPC.cs`
- Modify: `DamageClasses/Element/State/ElementComponent.cs`

**Step 1: Write the failing check**

Add placeholder draw calls that reference reaction text state fields and confirm compile failure until the state fields exist.

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: compile failure until the state fields and draw path are wired.

**Step 2: Write minimal implementation**

- draw `T:x W:y` above active NPCs with stored element amounts
- when a reaction occurs, store a short-lived reaction text marker
- draw the marker for a short duration above the NPC

No icons, no textures, no panel UI in this task.

**Step 3: Run verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 4: Commit**

```bash
git add Systems/Globals/AdvancedGlobelNPC.cs DamageClasses/Element/State/ElementComponent.cs
git commit -m "feat: add npc element and reaction text display"
```

### Task 10: Final Prototype Verification

**Files:**
- Review only: `docs/plans/2026-04-27-thunder-wind-prototype-design.md`
- Review only: `docs/plans/2026-04-27-thunder-wind-prototype-implementation.md`

**Step 1: Run build verification**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug"
```

Expected: build succeeds.

**Step 2: Run reference checks**

Run:

```bash
rg -n "Thunder|Wind|ThunderWind" DamageClasses/Element Items Projectiles Systems
rg -n "T:|W:|Reaction|雷风|TW" DamageClasses/Element Systems
```

Expected:
- prototype thunder/wind flow is represented in code
- NPC text display strings or their construction logic are present

**Step 3: Manual in-game test checklist**

Verify in game:

1. Wind sword adds wind gauge to NPC.
2. Thunder sword triggers reaction on winded NPC and consumes both sides by 1.
3. Thunder staff projectile triggers the same reaction.
4. Wind-enchant accessory forces thunder weapons to apply wind instead.
5. Reaction-burst accessory appends one extra 10000 wind damage hit after a successful reaction.
6. NPC displays element text and a short reaction text when the reaction happens.

**Step 4: Commit**

```bash
git add .
git commit -m "feat: add thunder wind prototype vertical slice"
```

