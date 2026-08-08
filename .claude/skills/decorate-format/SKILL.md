---
name: doom-decorate-format
description: Reference for the DOOM/ZDoom DECORATE actor-scripting language — actor definitions, inheritance, doomednum, state syntax (sprite/frame/duration/action function), flow control (loop/stop/wait/goto), flags (+FLAG/-FLAG), actor properties, and #include usage. Use this whenever the user is writing, reading, debugging, or asking about DECORATE lumps/scripts, custom DOOM actors (monsters, weapons, decorations, projectiles, inventory items), GZDoom/ZDoom modding in the DECORATE format, or converting/porting old DeHackEd patches into DECORATE. Also useful when the user asks how DECORATE relates to ZScript (its successor) or wants a DECORATE actor scaffolded from scratch. Note: DECORATE is deprecated in favor of ZScript, but is still fully supported — mention this if the user is starting a new project.
---

# DOOM DECORATE Format

DECORATE is the text-based actor-definition language used by ZDoom-family Doom source ports (ZDoom, GZDoom, Zandronum, etc.) to define monsters, weapons, projectiles, decorations, and inventory items without editing the executable. It has been **deprecated since GZDoom 2.3.0 in favor of ZScript** — all DECORATE features still work identically, and DECORATE syntax carries over to ZScript with minimal changes, but new projects should generally prefer ZScript. Still explain/write DECORATE whenever that's what the user is working with (many mods, tutorials, and legacy projects use it).

Primary source: [ZDoom Wiki — DECORATE format specifications](https://zdoom.org/wiki/DECORATE_format_specifications), [Actor states](https://zdoom.org/wiki/Actor_states), [Actor flags](https://zdoom.org/wiki/Actor_flags).

## Core actor definition syntax

```
actor classname [: parentclassname] [replaces replaceclassname] [doomednum]
{
  // properties, flags, states
}
```

- **classname** — identifier other definitions/code reference this actor by. Should be a valid identifier (alphanumeric + underscore, not starting with a digit) for best compatibility.
- **parentclassname** (optional) — the class this actor inherits properties/flags/states from. Defaults to `Actor` if omitted. Inheriting lets you override only what changes (see `references/decorate-details.md` for inheritance notes).
- **replaces replaceclassname** (optional) — makes this actor replace another wherever it would normally spawn on a map (higher-level than reusing a doomednum; doesn't affect direct-give inventory or player classes).
- **doomednum** (optional) — the editor number used to place the actor in a map editor. Omit for actors never placed directly (e.g., pure projectiles, pickups given by other code).

An actor body has three kinds of content: **properties** (`Health 20`), **flags** (`+SHOOTABLE`, `-SOLID`), and a **States** block. Comments use C-style `//` and `/* */`.

Use `#include "path/lump"` (outside any actor definition) to split DECORATE across files — common practice is one file per actor/category under an `actors/` folder, included from a root `decorate.txt`.

For full detail (constants, expressions, replaces edge cases, doomednum conflicts) read `references/decorate-details.md`.

## States block

```
States
{
Spawn:
    STUF AB 10 A_Look
    STUF C 4 Bright A_Chase
    loop
Death:
    STUF D 5 A_Scream
    STUF E 5 A_Fall
    STUF F -1
    stop
}
```

Each state line is: **sprite name** (4 chars) — **frame letter(s)** (A-Z, `[`, `\`, `]`) — **duration in tics** (`-1` = infinite; `random(min,max)` for randomized) — optional **keywords** — optional **action function**.

- Consecutive identical states can be collapsed by stringing frame letters: `STUF ABCD 5 Bright A_Look` defines 4 states in one line.
- A line is limited to one sprite; a frame sequence can be up to 256 characters.
- `TNT1 A` = invisible (no sprite) for that state's duration.
- **State labels** (`Spawn:`, `Death:`, etc.) name a sequence so it can be jumped to. Standard labels the engine looks for: `Spawn`, `See`, `Melee`, `Missile`, `Pain`, `Death`, `XDeath`, `Crash`, `Raise`, `Heal`, `Wound`, `Ready`/`Select`/`Deselect`/`Fire` (weapons), and more — full list in the reference doc.
- **State keywords**: `Bright` (fullbright), `Fast`/`Slow` (duration scaling), `CanRaise` (Arch-Vile-resurrectable), `NoDelay` (run action function on the very first tic), `Light("name")`, `Offset(x,y)`.
- **Flow control**: implicit fallthrough to the next state/line, or explicit `loop` (jump to start of current label), `stop` (end animation / remove actor), `wait` (freeze on last state), `fail` (custom inventory use failure), `goto label[+offset]` (jump anywhere, including `goto ParentClass::State` for inherited states).

Full state-label list, anonymous function blocks (`{ ... }` with `if`/`else`/`for`/`while`/`return`), and dynamic-light binding are in `references/decorate-details.md`.

## Flags

Flags toggle boolean behavior and live in the actor body (no `Default {}` block needed in DECORATE — that's ZScript). Set with `+FLAGNAME`, clear with `-FLAGNAME`. There are hundreds of flags grouped by purpose: general (SOLID, SHOOTABLE, NOGRAVITY, FLOAT), monster AI (AMBUSH, FRIENDLY, JUSTHIT, NOINFIGHTING), projectile (MISSILE, RIPPER, SEEKERMISSILE, EXPLOCOUNT), bounce (BOUNCEONWALLS/FLOORS/CEILINGS/ACTORS), appearance/sound (BRIGHT, INVISIBLE, NOBLOOD, FULLVOLDEATH), inventory-only (INVBAR, UNDROPPABLE, ADDITIVETIME — require the `Inventory.` prefix in ZScript, optional in DECORATE), and weapon-only (NOAUTOFIRE, WIMPY_WEAPON, MELEEWEAPON).

Two important shorthand combos:
- `Monster` — a property that turns on the flag combo needed to make an actor a standard monster (SHOOTABLE, COUNTKILL, SOLID, CANPUSHWALLS, CANUSEWALLS, ACTIVATEMCROSS, PASSMOBJ, etc.).
- `Projectile` — the flag combo for a standard projectile (NOBLOCKMAP, NOGRAVITY, DROPOFF, MISSILE, ACTIVATEIMPACT, ACTIVATEPCROSS, NOTELEPORT).

The complete categorized flag list (300+ flags with descriptions) is in `references/decorate-flags.md` — consult it rather than guessing a flag name/behavior.

## Common actor properties

A non-exhaustive but high-frequency set (all set as `PropertyName value` in the actor body):

`Health`, `Speed`, `Radius`, `Height`, `Mass`, `Damage`, `PainChance`, `Gravity`, `Scale`, `Alpha`, `RenderStyle`, `SeeSound`/`AttackSound`/`PainSound`/`DeathSound`/`ActiveSound`, `DropItem`, `MeleeRange`, `MaxStepHeight`, `Species`, `DamageType`, `DamageFactor`, `Obituary`, `Tag`, `Translation`, `BloodColor`/`BloodType`, `MonsterFallingDamage`-related properties, `FloatSpeed`, `MeleeThreshold`, `MaxTargetRange`, `MinMissileChance`.

For the full property reference and exact defaults inherited from the base `Actor` class, see `references/decorate-details.md`.

## When helping the user

1. If they're starting something new and don't have a strong reason to stick with DECORATE, mention ZScript is the modern successor (same concepts, more power) — but proceed in DECORATE if that's what they want or what their project already uses.
2. When writing a new actor, scaffold: header line → properties → flags → States block with at minimum a `Spawn` label (and `Death` for anything shootable).
3. When debugging, check in this order: sprite/frame naming (4-char sprite + valid frame letter matching actual lump names), state flow (missing `loop`/`stop`/`goto` causing fallthrough into the wrong label), flag combinations (e.g. forgetting `+SHOOTABLE` or the `Monster` property), and doomednum conflicts with other loaded actors.
4. Pull from `references/decorate-flags.md` and `references/decorate-details.md` for anything beyond this summary — don't guess at obscure flag/property behavior since GZDoom has many version-gated and edge-case flags.
