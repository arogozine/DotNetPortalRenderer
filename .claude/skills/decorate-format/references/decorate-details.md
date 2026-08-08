# DECORATE format details

Source: [DECORATE format specifications](https://zdoom.org/wiki/DECORATE_format_specifications), [DECORATE](https://zdoom.org/wiki/DECORATE), [Actor states](https://zdoom.org/wiki/Actor_states) (aka the "Loop" page).

## Actor header

```
actor classname [: parentclassname] [replaces replaceclassname] [doomednum]
```

- **classname**: referenced by this name elsewhere. ZDoom accepts a wide range of values, but for compatibility stick to a valid identifier (alphanumeric + underscore, not starting with a digit).
- **parentclassname**: optional parent to inherit attributes from. Defaults to `Actor`.
- **replaces replaceclassname**: optional. Works at a higher level than duplicate doomednums — affects all attempts to spawn the replaced actor on a map. Does NOT affect actors created through other means (e.g. giving an inventory item directly to a player). Player actors are handled differently, so this doesn't work for custom player classes. Skill definitions can specify other replacements, applied before DECORATE replacement.
- **doomednum**: optional editor number distinguishing the actor from other map things. Needed if the actor should be placeable in a map editor. Value is generally arbitrary — avoid clashing with numbers already in use.

An actor definition = properties + flags + state definitions. States can call **action functions** ("code pointers") that make the actor do something when that frame displays — these are the basis of almost all monster/weapon behavior. Instead of a dedicated action function you can also call almost any ACS action special.

Comments: `// to end of line` and `/* ... */` (both C-style). Comments starting with `//$` are reserved by certain editors (Doom Builder, SLADE 3) for special metadata purposes.

## #include

```
#include "<full_path_and_lump_name>"
```

Lets you split DECORATE across files/subfolders — e.g. put actor definitions under an `actors/` subfolder in a PK3, and have the root `decorate.txt` `#include` each one. Also lets you precisely control load order when an actor is referenced in multiple places (avoids load-order errors). May appear anywhere except inside an actor definition.

DECORATE lumps are **cumulative** — multiple DECORATE lumps can load at once without overwriting each other, so adding new actors doesn't require editing existing definitions; just add a new lump/file defining only the new actors.

## Inheritance

Any actor can specify a parent class to inherit its properties, flags, and states from, then override only what changes:

```
actor SuperImp : DoomImp
{
    health 1500
    mass 200
    painchance 10

    States
    {
    Raise:
        stop  // removes the inherited Raise state so this actor can't be resurrected
    }
}
```

Note: a state containing only `stop` effectively "deletes" an inherited state — useful for removing behavior (like Arch-Vile resurrection eligibility) picked up from a parent class.

Within states, `goto label` can jump to a base-class state that's been overridden in the current actor, or to the inherited one if no override exists. Use the scope operator `::` to explicitly jump to a specific ancestor's state: `goto Actor::GenericFreezeDeath` or `goto super::State` (`super` = immediate parent). `goto` is static (compile-time), unlike `A_Jump`, which is dynamic/virtual.

## States block — full detail

```
States
{
Label:
    SPRT ABCD 5 Bright A_SomeFunction
    ...
}
```

### State labels
An identifier followed by `:`. Names a state sequence so it can be entered/checked via that name. Alphanumeric (within reason), not case-sensitive. A state can have multiple labels stacked on separate lines. Most states have no label and simply follow the previous one in sequence.

Standard labels the engine expects for certain actor types:

| Label | Purpose |
|---|---|
| `Spawn` | Displayed on spawn; usually a monster's idle loop too. Also entered by hitscan puffs hitting a non-bleeding actor with `+PUFFONACTORS`. The action function on the very first Spawn frame does NOT run on initial spawn — only once the sequence loops. Use `NoDelay` to force it to run immediately. |
| `Idle` | Alternate idle state when a monster runs out of targets; falls back to `Spawn` if absent. |
| `See` | Walking/chase animation. Required for actors that chase/attack. |
| `Melee` | Close-range attack. Also entered by melee-attack puffs (e.g. `A_CustomPunch`) with `+PUFFONACTORS`. |
| `Missile` | Ranged attack. |
| `Pain` | Reaction to damage. Multiple variants possible per damage type. |
| `Death` | Normal death sequence. Multiple variants per damage type. Also entered by projectiles hitting a wall/actor if `Crash`/`XDeath` aren't defined. |
| `Death.Extreme` (`XDeath`) | Splatter death when health drops below `GibHealth`. Also used by bleeding-actor hits from projectiles/puffs. |
| `Death.Fire` (`Burn`), `Death.Ice` (`Ice`), `Death.Disintegrate` (`Disintegrate`) | Damage-type-specific death sequences. |
| `Raise` | Resurrection sequence (this actor being resurrected). |
| `Heal` | This monster resurrecting another one. |
| `Crash` | Corpse hits the floor. Also used by non-bleeding-actor projectile hits (if no `Crash`) and wall/plane puff hits. |
| `Crash.Extreme` | Splatter crash variant. |
| `Crush` | Actor crushed by a ceiling/door/etc. |
| `Wound` | Damaged but health still above 0 and below `WoundHealth`. |
| `Slam` | Actor with `SKULLFLY` hits another actor. |
| `Greetings`, `Yes`, `No` | Strife dialog system states. |
| `Active`, `Inactive` | Hexen-style switchable decorations. |
| `Bounce`, `Bounce.Floor`, `Bounce.Ceiling`, `Bounce.Wall`, `Bounce.Actor`, `Bounce.Actor.Creature` | `USEBOUNCESTATE` bouncers; partial matches fall back like `Pain` states do. |
| `Ready`, `Select`, `Deselect`, `Fire` | Weapon-specific states (and more for custom inventory items). |

You can also define arbitrary custom state labels and jump to them with `A_Jump`/`goto`.

### State line anatomy
Five components: sprite name, frame letter, duration (tics), action function, and successor (implicit unless flow control overrides it).

```
STUF C 5 Bright A_Look
```
`STUF` = sprite, `C` = frame, `5` = duration, `A_Look` = action function.

- **Successor**: implicitly the next defined state, unless changed with `goto`, `loop`, `wait`, or `stop`.
- **Collapsing frames**: identical sprite/duration/keywords/action function in sequence can be strung together: `STUF ABCD 5 Bright A_Look` = 4 distinct states on one line (up to 256 chars in a frame sequence). Valid frame letters: `A`-`Z`, `[`, `\`, `]` (the latter three require the frame sequence to be quoted, e.g. `"A[B\"`).
- **Duration `-1`** = infinite; actor never leaves that state on its own (but can still be externally moved, e.g. into `Pain`).
- **Random duration**: `random(min,max)` inline, or use `A_SetTics(expr)` for a fully dynamic duration via DECORATE expressions (this uses up the action-function slot, so it can't be combined with another action function on that state).
- **One sprite per line** — different sprites can be mixed across lines within one actor.
- `TNT1 A` = invisible for that state's duration.
- Special sprite/frame names `----` or `####` keep the actor's current sprite/frame (see the sprite wiki page for more).

### State keywords
Placed between duration and the action function call:

- **Bright** — sprite renders fullbright while in this state (ignored in fog).
- **CanRaise** — marks the state as eligible for `A_VileChase` resurrection targeting (normally only `-1`-duration states qualify); also makes the actor eligible for monster respawn if enabled.
- **Fast** / **Slow** — halves/doubles duration under fast-monster or slow-monster skill settings (or `-fast`), unless the actor has `NEVERFAST`.
- **Light("lightname")** — binds a dynamic light (defined separately in GLDEFS) to this state; inherited by derived actors, unlike GLDEFS-only bindings. Has no effect in plain ZDoom software renderer.
- **NoDelay** — forces the action function on this state to run on the actor's very first tic (only meaningful on the first Spawn-sequence state).
- **Offset(x, y)** — sprite offset, used for HUD/weapon sprites. `Offset(0,0)` means "keep previous offset" (Hexen compatibility quirk), not "reset to zero."

### Flow control keywords
- **loop** — jump to the most recently defined state label (loop the current sequence). Don't combine with a `-1` duration state — unnecessary and can cause problems.
- **stop** — end the animation; if the last state's duration is > -1 the actor is removed. A state containing only `stop` effectively deletes an inherited state.
- **wait** — freeze on the last defined state (commonly paired with functions that wait for a timer/event, e.g. `A_WeaponReady`, `A_Raise`).
- **fail** — (custom inventory items) marks that the state sequence failed to succeed.
- **goto label[+offset]** — jump to any state in the current actor, including inherited/base-class states (`goto See`, or `goto Actor::GenericFreezeDeath`, or `goto super::State`). `+offset` skips forward that many frames past the label. `goto` is static (compile-time) — for dynamic/conditional jumps use `A_Jump`-family functions.

Important: no assumptions are made about intent — states are never implicitly created. If no flow control is specified, execution simply continues into the state defined next in the file.

### Variable duration example
```
POSS A 10 A_Look
```
vs.
```
POSS A random(10,20) A_Look
```
or full dynamic control:
```
POSS A 0 A_Look
POSS A 10 A_SetTics((waterlevel + 10) - (accuracy / 10))
```

### Anonymous functions (multi-action states)
Braces in place of a single action function name let you call multiple functions from one state (semicolon required after each statement):
```
TNT1 A 0
{
    A_Chase;
    A_FaceTarget;
}
```
Supports `if`/`else`, `for`, `while`, `do...while`, `foreach` (ZScript only), `return` (optionally returning a `state`, `int`, `bool`, or `float` — all returns in one block must share a type), `break`, and `continue`. Example jump patterns:
```
{ return state("Null"); }        // destroy the actor
{ return state("JumpState"); }   // guaranteed jump
{ return A_Jump(256, "JumpState"); }
{ return state(""); }            // abort without jumping, play out remaining tics
```
`A_Jump`-family functions used as an `if` condition don't actually jump — they just report whether they *would have*, letting you build conditional logic without side effects. Pass a valid state label (or `"Null"`) or the check will always be false.

### Full worked example
```
actor ZombieMan 3004
{
    States
    {
    Spawn:
        POSS AB 10 A_Look
    See:
        POSS AABBCCDD 4 A_Chase
    Missile:
        POSS E 10 A_FaceTarget
        POSS F 8 A_PosAttack
        POSS E 8
        goto See
    Pain:
        POSS G 3
        POSS G 3 A_Pain
        goto See
    Death:
        POSS H 5
        POSS I 5 A_Scream
        POSS J 5 A_Fall
        POSS K 5
        POSS L -1
        stop
    XDeath:
        POSS M 5
        POSS N 5 A_XScream
        POSS O 5 A_Fall
        POSS PQRST 5
        POSS U -1
        stop
    Raise:
        POSS KJIH 5
        goto See
    }
}
```

## Common actor properties (partial reference)

These are set directly in the actor body (no `Default {}` block — that's ZScript-only syntax). Values shown are common defaults inherited from the base `Actor` class where noted; always verify against a parent class's actual definition if precision matters.

| Property | Purpose |
|---|---|
| `Health` | Hit points. |
| `Speed` | Movement speed (map units/tic for monsters; different scale for projectiles). |
| `Radius`, `Height` | Collision cylinder dimensions. |
| `Mass` | Affects knockback/pushback resistance. |
| `Damage` | Damage dealt on contact (projectiles) or via attack functions. |
| `PainChance` | 0–256 chance of entering the Pain state when damaged. |
| `Gravity` | Multiplier on normal gravity (1.0 = normal). |
| `Scale` | Visual scale multiplier. |
| `Alpha`, `RenderStyle` | Transparency/blending (`Normal`, `Translucent`, `Add`, `Fuzzy`, `None`, etc). |
| `SeeSound`, `AttackSound`, `PainSound`, `DeathSound`, `ActiveSound` | Sound lump names for various triggers. |
| `DropItem` | Item(s) spawned when this actor dies. |
| `MeleeRange` | Distance threshold for melee-attack eligibility. |
| `MaxStepHeight` | Max height the actor can step up onto automatically. |
| `Species` | Groups actors for infighting/damage-sharing rules (see `DONTHARMSPECIES` etc. in flags). |
| `DamageType`, `DamageFactor` | Custom damage typing and per-type multipliers. |
| `Obituary` | Death message shown in multiplayer. |
| `Tag` | Display name (e.g., for automap/HUD). |
| `Translation` | Palette translation table for recoloring sprites. |
| `BloodColor`, `BloodType` | Blood color/actor-class override. |
| `FloatSpeed` | Vertical speed for `FLOAT`-flagged actors. |
| `MeleeThreshold`, `MaxTargetRange`, `MinMissileChance` | AI tuning for engagement range/ranged-attack likelihood (these properties supersede the deprecated `LONGMELEERANGE`, `MISSILEEVENMORE` etc. flags). |

For the authoritative, complete, version-by-version property list (including weapon-specific and inventory-specific properties), check the ZDoom Wiki's "Actor properties" page directly, since it is updated as GZDoom adds features — this reference captures the stable, commonly used core rather than every property.

## DECORATE vs. ZScript

- DECORATE is deprecated since GZDoom 2.3.0; ZScript fully supersedes it, exposing every DECORATE property/flag/function plus much more (custom classes, full expression language, virtual functions, structs).
- DECORATE knowledge transfers to ZScript with minimal syntax changes — states, flags, properties, and action-function names are the same or near-identical.
- Flag prefixes are optional in DECORATE, but **required** in ZScript when a flag is defined outside the base `Actor` class (e.g. `+Inventory.INVBAR`, not `+INVBAR`).
- New projects are generally steered toward ZScript; DECORATE remains fully functional for legacy content and simpler mods.
