# CON Parser — Summary

Overview of how Duke Nukem 3D `.CON` scripts get from raw text to a typed `Command` tree in `BuildAssetLoader.Con`.

## 1. Build Engine .CON Format

CON is Duke Nukem 3D's actor-scripting language (see `CON_FORMAT_SUMMARY.md` for the full grammar notes). Key points:

- Plain text, split across `DEFS.CON` / `USER.CON` / `GAME.CON`, chained via `include`.
- Whitespace-delimited tokens, no semicolons; `//` and `/* */` comments.
- Top-level declarations: `define`, `action`, `move`, `ai`, `actor`/`useractor { }`, `state { }`, `onevent { }`.
- Inside a body, most statements are flat `keyword arg1 arg2 ...` lines; `if*` commands take a true-branch (one statement or a `{ }` block) and an optional `else` branch.
- Several commands are **context-sensitive**: `move`, `ai`, and `state` each have a distinct *declaration* form (top-level, outside any body) and *invocation* form (referencing an already-declared name from inside a body), distinguished only by where they appear — not by syntax.
- Real shipped CON (`CONFiles/`) exercises a few quirks not obvious from the wiki docs — see [§4](#4-contreebuilder) for how the builder handles them.

## 2. Commands

The typed AST node types live in `Commands/Commands*.cs`, one file per category (Actor, Audio, Conditionals, Flow, Gamevar, MetaSettings, Player, Preprocessor, Screen, Sectors, Spawning, StructureAccess), plus shared base shapes in `Commands.cs`.

- **`CommandList`** (`CommandList.cs`) — enum of every recognized keyword (~300 entries), one per CON command. A few names collide with C# keywords (`else`, `switch`, `break`, ...) and carry a `[Description("...")]` attribute holding the real CON spelling.
- **`Command`** — abstract record base for every AST node; `record Command(CommandList Start)`.
- **`Structure`** — block-scoped command closed by an explicit keyword (`enda`, `ends`, `endevent`); carries a mutable `Body` list. Used by `actor`/`useractor`, `onevent`/`appendevent`, `state`/`defstate`/`prependstate`/`appendstate`, `eventloadactor`, `switch`.
- **`ConditionalStructure`** — an `if*` command; has `Body` (true-branch) and `ElseBody` (optional false-branch, `null` if absent).
- **`LoopStructure`** — a `whilevar*` command; has `Body`.
- Everything else is a flat, immutable record with typed fields matching that command's arguments (e.g. `SpawnCommand(string TileNumber)`, `HitradiusCommand(string Radius, string Damage1, ...)`).
- Enum-driven families collapse related keywords into one record via a lookup table: `GamevarOperator`/`GamevarOperatorLookup` (`setvar`/`addvar`/... → `VarOpCommand`), `GamevarCondition`/`GamevarConditionLookup` (`ifvare`/`ifvarg`/... → `IfVarCommand`), and the `varvar`/`ifvarvar*` siblings that compare two gamevars instead of a gamevar and a constant.

## 3. Token Parser

**`ConParser.Parse(ReadOnlySpan<char>)`** (`ConParser.cs`) turns raw CON source into a flat `List<ConToken>`.

- **`ConToken`** (`ConToken.cs`) — base record with a `ConTokenType` (`Command`, `Value`, `BlockStart`, `BlockEnd`, `NewLine`); `BlockStart`/`BlockEnd`/`NewLine` are shared singleton instances.
- **`CommandToken`** — wraps a recognized `CommandList` value. A word becomes a `CommandToken` only if it *exactly* matches a known keyword; produced via a `FrozenDictionary<string, CommandList>` alternate lookup for allocation-free span matching.
- **`ValueToken`** — everything else: identifiers, numbers, defines, quoted-ish free text.
- `{` / `}` become `BlockStart`/`BlockEnd`. `//` and `/* */` comments are skipped entirely and collapse to a single `NewLine` marker (only emitted when a real newline was consumed) — comment text itself never reaches the token stream.
- The parser is purely lexical: it has no notion of statements, bodies, or nesting. That structure is built by `ConTreeBuilder`.

## 4. ConTreeBuilder

**`ConTreeBuilder.Build(IReadOnlyList<ConToken>)`** (`ConTreeBuilder*.cs`) walks the flat token stream and produces the top-level `List<Command>` tree, recursively populating `Body`/`ElseBody` on structures. `NewLine` tokens are filtered out up front since they carry no structural meaning.

**Architecture**

- **`ConTreeCursor`** (`ConTreeCursor.cs`) — a position-tracking wrapper over the token list with typed reads (`ReadValue`, `ReadInt`, `TryReadValue`/`TryReadInt` for optional trailing args) and `CountContiguousValues()`. Since a value token can never start a new statement (only a `CommandToken` can), the run of consecutive value tokens after a keyword is *exactly* that command's full argument list — which resolves variable-arity commands (optional/trailing args, variadic lists like `ifp`'s OR'd flags or `music`'s track list) without per-command lookahead hacks.
- **`ConTreeBuilder.cs`** — the core dispatcher (`ParseCommand`) and structural parsers: `actor`/`useractor`, `onevent`/`appendevent`, `state` family, `switch`/`case`/`default`, `whilevar*` loops, and the context-sensitive `move`/`ai`/`action` dispatch (an `insideBody` flag threaded through the recursion decides declaration vs. invocation).
- **`ConTreeBuilder.Conditionals.cs`** — builds the `ConditionalStructure` leaf (condition args only) for every `if*` command; body/else attachment happens in the core file.
- **`ConTreeBuilder.{Actor,Audio,Flow,Gamevar,MetaSettings,Player,Preprocessor,Screen,Sectors,Spawning,StructureAccess}.cs`** — one flat-command mapping table per category, mirroring the `Commands/Commands.*.cs` split. Each is a `CommandList → Command?` switch tried in sequence by `ParseFlatCommand`.

**Notable real-world quirks it accounts for** (found by running the builder against the shipped `CONFiles/DEFS.CON`, `USER.CON`, `GAME.CON`):

- **Dangling `else` crosses brace boundaries.** A trailing `else` binds to the *innermost still-open* conditional — even if that conditional's true-branch was a `{ }` block that already closed. `ParseStatements`/`ParseBranch` track a `pending` conditional across loop iterations (including brace-skip iterations) and use `FindPendingTail` to drill into the last-attached branch, so chains like `ifcount 48 body else { ifcount 32 body } else ifcount 16 { ... }` attach the second `else` to `ifcount 32`, not `ifcount 48`.
- **Decorative, unowned `{ }` wrappers.** Some `state` bodies wrap their entire contents in a `{ }` pair with no owning `if`/`while`/`case`. `ParseStatements` treats a stray `BlockStart`/`BlockEnd` as a no-op skip rather than an error.
- **Glued structure-access tokens.** `getactor[id].member`/`setactor[id].member`-style syntax has no whitespace before `[`, so the tokenizer never emits a `CommandToken` for it — the whole thing (plus indices/members) is one `ValueToken`. `ParseStructureAccessFallback` recognizes this from the raw token text (`prefix[id].member`) when a value appears where a command was expected.
- **Declaration vs. invocation ambiguity.** `move`/`ai`/`state` each have a data-full declaration form (top-level) and a bare-name invocation form (inside a body); resolved purely by the `insideBody` context flag, not by syntax.

**Validation**: `Tests/BuildAssetLoader/ConParser/ConTreeBuilderTests.cs` parses the full `DEFS.CON`/`USER.CON`/`GAME.CON` corpus end-to-end plus targeted cases for the brace-crossing `else` and `move` context disambiguation.
