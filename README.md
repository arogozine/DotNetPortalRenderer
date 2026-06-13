## Project Overview

**DotNetPortalRenderer** is a .NET 10 software-based portal rendering engine that renders maps from the Doom (ID Tech 1) and Build (Duke Nukem 3D) game engines. It implements a classic portal-based raycasting renderer—a rendering technique that divides 3D space into sectors (rooms) connected by portals (passages between rooms).

The renderer runs in software (no GPU rendering), using SIMD/vectorized operations for performance. It renders to a BGRA framebuffer that gets uploaded to an OpenGL texture each frame.

### AI Use Disclosure

Claude Code, Copilot, and Grok were used for,
- Code Review and optimization recommendation. Such as "Native" methods and pointing me towards the Avx2 instructions.
- Help with algorithms, such as slope calculations.
- Documentation. Except this paragraph.
- Unit test generation.

Majority of the application and the rendering design are handmade.

### Project Structure

The solution contains 7 projects:

1. **RenderingEngine** - Core portal rendering library
   - Implements the `PortalRenderer` class (main rendering loop)
   - Portal-based sector traversal and depth-based rendering
   - Wall, floor, ceiling, and sprite rendering
   - Handles raycasting, texture mapping, and shading
   - Contains math formulas for geometry calculations

2. **SoftwareRenderer** - Cross platform application
   - Entry point with command-line argument parsing (via System.CommandLine)
   - Opens OpenGL window via OpenTK.Windowing.Desktop (GameWindow)
   - Manages the game loop, keyboard input, and framebuffer updates
   - Uses OpenTK (OpenGL) and SkiaSharp for window presentation (uploads rendered buffer as texture quad)

3. **SoftwareRendererModels** - Shared data models
   - Game state models: `FixedGameState`, `RenderableMap`, `PortalPlayerSnapshot`
   - Rendering state: `RenderableSector`, `RenderableWall`, `RenderableSprite`
   - Texture models: `BGRA`, `DoomTexture`, `BuildTexture`, `GameTextureInfo`
   - Enums for rendering options, texture transforms, sector settings

4. **DoomAssetLoader** - Doom WAD file parsing
   - Loads IWAD (base assets) and PWAD (custom content) files
   - Extracts maps, textures, sprites from Doom binary WAD format
   - Supports both classic binary format and UDMF (text-based) maps

5. **BuildAssetLoader** - Duke Nukem 3D GRP file parsing
   - Loads GRP files and extracts maps, textures, sprites
   - Handles palette and lookup table files for color/shading

6. **Tests** - Unit tests (XUnit)
   - Tests for math functions, memory utilities, and asset loading

7. **Benchmark** - Performance benchmarks (BenchmarkDotNet)
   - Benchmarks for rendering operations, vector operations, and math functions

### Rendering Pipeline

The core rendering happens in `PortalEngine` and `GameRenderingThread`:

1. **PortalEngine** (`Engine/PortalEngine.cs`)
   - Manages game state and player position
   - Handles keyboard input for camera movement and rotation
   - Orchestrates game loop startup via `GameRenderingThread`

2. **GameRenderingThread** (`Engine/GameRenderingThread.cs`)
   - Runs renderer on a background thread (long-running task)
   - Synchronizes frame rendering with main UI thread via semaphores
   - Returns BGRA framebuffer pointer for OpenGL texture upload

3. **PortalRenderer** (`Engine/Engine/Renderer/PortalRenderer.cs`)
   - Main rendering implementation
   - **Key method: `DrawScreen()`** - Portal-based depth-first rendering:
     1. Start with player's sector
     2. For each depth level: render all walls in queued sectors
     3. Calculate visible floor/ceiling bounds using raycasting
     4. Render floors and ceilings within visible bounds
     5. Identify portal walls and queue connected sectors for next depth
     6. After all walls: render sprites and transparent textures

### Running

```bash
# Load a Doom map
dotnet run --project SoftwareRenderer -- --map E1M1 --iwad /path/to/doom.wad

# Load a Doom map with custom PWAD
dotnet run --project SoftwareRenderer -- --map E1M1 --iwad /path/to/doom.wad --pwad /path/to/custom.wad

# Load a Duke Nukem 3D map
dotnet run --project SoftwareRenderer -- --map NAME --grp /path/to/duke3d/folder
```