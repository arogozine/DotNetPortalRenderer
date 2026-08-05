# Duke Nukem 3D CON Format — Summary

Source: [EDuke32 Wiki: Scripting](https://wiki.eduke32.com/wiki/Scripting), [Con FAQ 4.2](https://wiki.eduke32.com/wiki/Confaq42)

## File Structure

CON scripts are plain text, split across three top-level files, chained via `include`:

- **DEFS.CON** — defines tile/sprite constants (e.g. `PIGCOP` = 2000), sound names, misc definitions.
- **USER.CON** — weapon strengths, health/ammo values, level names, quotes, episode/skill names.
- **GAME.CON** — the bulk of actor code; compiled first, `#include`s the other two.

Mods typically add their own `.con` files pulled in via `include <filename>`.

## Lexical Rules

- Comments: `//` to end of line (space required after `//`), or `/* ... */` block comments.
- Blocks grouped with `{ }` (like C). `nullop` is a no-op alternative to empty `{}`.
- Whitespace-delimited tokens; no semicolons.
- Identifiers (labels, gamevars, actions, moves, AIs) are case-sensitive and cannot start with a digit.
- No strict typing — most values are integers or predefined string constants.

## Top-Level Primitives (definition blocks)

| Primitive | Syntax | Purpose |
|---|---|---|
| `define` | `define <name> <value>` | Named integer constant |
| `action` | `action <name> <startframe> <frames> <viewtype> <incvalue> <delay>` | Names an animation sequence |
| `move` | `move <name> <horizontal> <vertical> [<directions>]` | Names a velocity/direction |
| `ai` | `ai <name> <action> <speed> <type>` | Names an AI behavior (composable, e.g. `randomangle dodgebullet`) |
| `actor` ... `enda` | `actor <name> <strength> <action> <speed> <ai> { code } enda` | Defines a sprite's behavior (built-in actors only pre-1.4) |
| `useractor` | `useractor <enemy\|notenemy\|enemystayput> <name> <strength> { code }` | Defines a new custom actor (1.4+) |
| `state` ... `ends` | `state <name> { code } ends` | Named, reusable code block; invoked later via `state <name>` |
| `onevent` ... `endevent` | `onevent EVENT_X { code } endevent` | EDuke32-only: hook into engine event points (additive — can be defined multiple times) |

## Control Flow

- `if<condition> <action> [else <action>]` — no braces needed for single statements; braces required for multi-statement branches (and to disambiguate trailing `else`).
- `ifvar`/gamevar comparison ops for EDuke32 gamevar conditions.
- `break` exits the current state/actor code early.

## Command Categories (see [full command list](https://wiki.eduke32.com/wiki/Full_command_list))

- **Player state**: `addphealth`, `addammo`, `addweapon`, `addinventory`, `addkills`, `ifphealthl`, `ifpinventory`, `ifp`
- **Actor state**: `strength`/`addstrength`, `cstat`/`cstator`, `spritepal`, `sizeat`/`sizeto`, `count`/`ifcount`, `clipdist`
- **Spawning/effects**: `spawn`, `debris`, `guts`, `lotsofglass`, `money`, `mail`, `paper`, `hitradius`
- **Sound**: `sound`, `soundonce`, `stopsound`, `globalsound`, `definesound`
- **Sensing (`if...`)**: `ifcansee`, `ifactor`, `ifdead`, `ifsquished`, `ifhitweapon`, `ifrnd`, `ifpdistl/g`, `ifinwater`, `ifoutside`, etc.
- **Flow/meta**: `include`, `killit`, `cactor`, `resetcount`, `resetactioncount`, `quote`/`definequote`

## EDuke32 Extensions (beyond vanilla CON)

- **Gamevars**: `gamevar <name> <initial> <flags>` — flags: `0` = global, `1` = per-player, `2` = per-actor. Manipulated with arithmetic commands (`addvar`, etc.) and read/set via `getactor`/`setactor`, `getplayer`/`setplayer`, `getprojectile`/`setprojectile`.
- **Structure access**: direct get/set on `actor` (sprite), `player`, `tsprite` (render-only sprite override), `input` structures — exposes fields not reachable via classic primitives.
- **Events**: `onevent EVENT_NAME { ... } endevent` — engine calls these at defined hook points (e.g. `EVENT_RESETINVENTORY`). Additive across files.
- **Custom projectiles**: `defineprojectile <tilenum> <function> <value>`, read/write via `getprojectile`/`setprojectile`.
- **Screen drawing**: `rotatesprite`, `screentext` for HUD/UI rendering.
- **String manipulation**: quote copy/concatenation/print commands.

## Notes for Parser Design

- Grammar is essentially: a flat sequence of top-level declarations (`define`, `action`, `move`, `ai`, `actor`/`useractor`, `state`, `onevent`, `include`), each either single-line or delimited by a block terminator (`enda`, `ends`, `endevent`) with `{ }` used internally for conditional grouping.
- Most "commands" inside actor/state bodies are simple `keyword arg1 arg2 ...` lines — a big flat command table (name → arity/arg types) covers most of the language; only `if*` commands need special control-flow handling (peek next statement/block as the true-branch, optional `else`).
- `include` requires resolving and inlining/parsing referenced files relative to the source file.
- Case sensitivity + no reserved-word list beyond the primitives themselves means identifier collision with command names is a user error, not a parser error, to detect.
