# DECORATE / ZScript actor flags

Source: [ZDoom Wiki — Actor flags](https://zdoom.org/wiki/Actor_flags). This is a categorized digest — consult the live wiki page for exact version-gating (some flags are "New from X.Y.Z") and any flags added after this reference was written.

All flags default to `false`/unset on the base `Actor` class. Set with `+FLAGNAME`, clear with `-FLAGNAME`, in the actor body (DECORATE) or the `Default { }` block (ZScript). In ZScript, flags defined outside the base `Actor` class need their defining-class prefix, e.g. `+Inventory.INVBAR` — this prefix is optional in DECORATE. At runtime in ZScript, flags are also accessible as boolean fields: `bNOCLIP = true;`.

## Flag combos (shorthand properties)
- **`Monster`** — enables the flag set needed for a standard monster: `SHOOTABLE`, `COUNTKILL`, `SOLID`, `CANPUSHWALLS`, `CANUSEWALLS`, `ACTIVATEMCROSS`, `PASSMOBJ`, `ISMONSTER`, plus more.
- **`Projectile`** — enables the flag set for a standard projectile: `NOBLOCKMAP`, `NOGRAVITY`, `DROPOFF`, `MISSILE`, `ACTIVATEIMPACT`, `ACTIVATEPCROSS`, `NOTELEPORT`.
- **`BounceType`** property is a shorthand for combinations of the bounce flags below — don't mix the property with manually-set bounce flags.

## General / physics
`SOLID`, `SHOOTABLE`, `FLOAT` (needs `NOGRAVITY` too), `NOGRAVITY`, `PUSHABLE`, `NOBLOOD` region aside — `DROPOFF` (can walk off ledges), `SLIDESONWALLS`, `WINDTHRUST`, `CANNOTPUSH`, `NOTELEPORT`, `NODROPOFF`, `NOCLIP`, `NOSECTOR` (not linked into sector — avoid), `FLOORCLIP`, `SPAWNCEILING`, `FLOORHUGGER`/`CEILINGHUGGER` (for projectiles), `CANPASS`, `SOLID`-vs-`NOBLOCKMAP` interplay for collision.

## Rendering / appearance
`FLATSPRITE`, `ROLLSPRITE`, `ROLLCENTER`, `SPRITEANGLE`, `XFLIP`/`YFLIP`, `XFLIPGROUNDONLY`... (flipping variants), `VISIBILITYPULSE`, `NOINTERPOLATION`, `ADDITIVEPOISON`... — full appearance group covers billboarding (`FORCEXYBILLBOARD`, `FORCEYBILLBOARD`, `ROLLSPRITE`), fullbright (`BRIGHT`), invisibility (`INVISIBLE`, `NOBLOOD`, `NOBLOODDECALS`), shadows (`CASTSPRITESHADOW`, `NOSHADOW`), stealth (`STEALTH`), mirror rendering (`NOMIRROR`, `MIRRORONLY`), and pixel aspect (`SQUAREPIXELS`, `STRETCHPIXELS`).

## Monster AI
`AMBUSH` (deaf — needs LoS after waking), `AVOIDMELEE`, `BOSS` (immune to several instakill effects, plays sounds at full volume), `DONTMORPH`, `DORMANT`, `FRIENDLY`, `JUMPDOWN`, `LOOKALLAROUND`, `NORESPAWN`, `NOSPLASHALERT`, `NOTARGETSWITCH`, `NOVERTICALMELEERANGE`, `NOTHRESHOLD`, `NOICEDEATH`... (see also `LOOKALLAROUND`, `QUICKTORETALIATE`, `STAYMORPHED`, `NOFEAR`, `HUNTSPECIES`, `IGNOREPLAYERPUFF`, `NOFRIENDLYFIRE`, `PUFFONACTORS`), `INCOMBAT`, `STANDSTILL`, `NOFORWARDFALL`, `NOBLOCKMONST`, `NOTAUTOAIMED`, `NOTARGET`/`NOTARGETABLE`/`INVISIBLETARGET`, `NOTELEFRAG`, `TELESTOMP`, `NOINFIGHTSPECIES`, `NOKILLSCRIPTS`/`EXTREMEDEATH`(-related), `THRUACTORS`/`THRUSPECIES`, `RESSURECT`... — this is the largest category (100+ flags); consult the live wiki for the exact name/behavior of any AI-tuning flag not listed above rather than guessing.

## Damage / defense
`INVULNERABLE`, `BUDDHA` (indestructible last hit point), `NODAMAGE`, `REFLECTIVE` (+ `DEFLECT`/`FORCERADIUSDMG`/`MIRRORREFLECT`/`SEEKERMISSILE`-interactions), `NORADIUSDMG`, `DONTHARMCLASS`, `DONTHARMSPECIES`, `DONTHURTSPECIES` (deprecated alias), `ALLOWPAIN`, `FORCEPAIN`, `PAINLESS`, `SHADOW`/`SHADOWBLOCK`/`DOSHADOWBLOCK`, `NOFRICTION`, `FIRERESIST` (deprecated → use `DamageFactor "Fire", 0.5`), `TELEFRAG`/`NOTELEFRAG`.

## Projectile-specific
`MISSILE`, `RIPPER` (rips through victims), `NORIPPERDAMAGE`, `SEEKERMISSILE`, `THRUGHOST`, `THRUSPECIES`, `SKYEXPLODE`, `NOEXPLODEFLOOR`, `STRIFEDAMAGE`, `EXPLOCOUNT`, `FORCERADIUSDMG`, `FOILINVUL`, `FOILBUDDHA`, `CANTLEAVEFLOORPIC`, `EXTREMEDEATH`/`NOEXTREMEDEATH`, `DONTREFLECT`, `NOTELESTOMP`, `SPAWNSOUNDSOURCE`, `NOFORWARDFALL`, `SLIDESONWALLS` (shared with general), `STEPMISSILE`.

## Bounce (paired with `BounceType` property, don't combine both)
`BOUNCEONWALLS`, `BOUNCEONFLOORS`, `BOUNCEONCEILINGS`, `BOUNCEONACTORS`, `BOUNCEAUTOOFF`, `BOUNCEAUTOOFFFLOORONLY`, `BOUNCELIKEHERETIC`, `CANBOUNCEWATER`, `NOWALLBOUNCESND`, `NOBOUNCESOUND`, `MBFBOUNCER`, `USEBOUNCESTATE`.

## Puff / blood
`ISPUFF`, `PUFFGETSOWNER`, `PUFFONACTORS`, `ALWAYSPUFF`, `SPAWNBLOOD`, `USEALPHA`, `NODECAL`, `SHATTERWHITEBLOOD`, `THRUACTORS`, `THRUGHOST`.

## Item / pickup counting
`DROPPED`, `CORPSE`, `COUNTITEM`, `COUNTKILL`, `COUNTSECRET`, `NOTDMATCH`, `NOTAUTOAIMED`, `NOLIFTDROP`, `NOTONAUTOMAP`, `DMFLAGSSPAWNFILTER`.

## Inventory-only flags (need `Inventory.` prefix in ZScript)
`QUIET`, `AUTOACTIVATE`, `UNDROPPABLE`, `PERSISTENTPOWER`, `INVBAR`, `KEEPDEPLETED`, `INTERHUBSTRIP`, `ALWAYSPICKUP`, `FANCYPICKUPSOUND`, `NOATTENPICKUPSOUND`, `BIGPOWERUP`, `ALWAYSRESPAWN` (note: separate from the monster `ALWAYSRESPAWN` flag — items need `Inventory.ALWAYSRESPAWN` in ZScript), `IGNORESKILL`, `NEVERRESPAWN`, `PERSISTENTPOWER`, `ADDITIVETIME` (stacks powerup duration instead of overwriting), `UNTOSSABLE`, `NOSCREENFLASH`, `TRANSFER`.

## Weapon-only flags
`NOAUTOFIRE`, `READYSNDHALF`, `NOBOB`, `AXEBLOOD`, `NOALERT`, `AMMO_OPTIONAL`, `ALT_AMMO_OPTIONAL`, `AMMO_CHECKBOTH`, `PRIMARY_USES_BOTH`, `WIMPY_WEAPON`, `POWERED_UP`, `STAFF2_KICKBACK`, `EXPLOSIVE`, `MELEEWEAPON`, `WEAPON_BOT_EXPLOSIVE`/`WEAPON_BOT_MELEE`, `CHEATNOTWEAPON`, `NOAUTOSWITCHTO`, `NOAUTOSWITCHFROM` (approx names — verify exact spelling on wiki), `NOAUTOAIM`.

## Player-class flags
`NOSKIN`, `CROUCHABLEMORPH`, `MAKEFOOTSTEPS` (dev-only), plus morph-related flags on `PowerMorph`.

## Boss event triggers (map-specific, matched to classic IWAD levels)
`MAP07BOSS1`/`MAP07BOSS2`, `E1M8BOSS`, `E2M8BOSS`, `E3M8BOSS`, `E4M6BOSS`, `E4M8BOSS`.

## Deprecated (documented for legacy reference only — use the listed replacement)
| Deprecated flag | Replacement |
|---|---|
| `LOWGRAVITY` | `Gravity 0.125` |
| `QUARTERGRAVITY` | `Gravity 0.25` |
| `LONGMELEERANGE` | `MeleeThreshold 196` |
| (long missile range) | `MaxTargetRange 896` |
| `HIGHERMPROB` | `MinMissileChance 160` |
| `FIRERESIST` | `DamageFactor "Fire", 0.5` |
| `DONTHURTSPECIES` | `DONTHARMCLASS` (name was misleading) |
| `FIREDAMAGE` | `DamageType "Fire"` |
| `ICEDAMAGE` | `DamageType "Ice"` |
| `MISSILEEVENMORE` | `MissileChanceMult 0.125` |
| `MISSILEMORE` | `MissileChanceMult 0.5` |
| `HERETICBOUNCE` | `BounceType "Heretic"` (or `BOUNCEONFLOORS` + `BOUNCELIKEHERETIC`) |
| `HEXENBOUNCE` | `BounceType "Hexen"` |
| `DOOMBOUNCE` | `BounceType "Doom"` |
| `FASTER` / `FASTMELEE` | use the `Fast` state keyword instead |

## Internal-only flags
A set of flags (`RUNNING`, `UNMORPHED`, `FLY`, `ONMOBJ`, `IGNOREMAPARGS`, `HASCRASHED`, `WARNBOT`, `FRIGHTENED`, `PAINSCALED`, `NOSKINCOLOR`, `DORMANT`-adjacent, etc.) are used internally by GZDoom and exposed to ZScript but cannot be set directly in an actor definition — informational only.

---

When the user needs the precise wording/behavior of a specific flag not spelled out here in full, treat this file as a map of categories rather than a verbatim copy — fetch or recall the exact ZDoom Wiki "Actor flags" entry for that flag name before asserting fine behavioral details, since GZDoom adds new flags across versions.
