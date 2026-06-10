# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**DotNetPortalRenderer** is a .NET 10 software-based portal rendering engine that renders maps from the Doom (ID Tech 1) and Build (Duke Nukem 3D) game engines. It implements a classic portal-based raycasting renderer—a rendering technique that divides 3D space into sectors (rooms) connected by portals (passages between rooms).

The renderer runs in software (no GPU rendering), using SIMD/vectorized operations for performance. It renders to a BGRA framebuffer that gets uploaded to an OpenGL texture each frame.

## Architecture Overview

### Project Structure

The solution contains 7 projects:

1. **RenderingEngine** - Core portal rendering library
   - Implements the `PortalRenderer` class (main rendering loop)
   - Portal-based sector traversal and depth-based rendering
   - Wall, floor, ceiling, and sprite rendering
   - Handles raycasting, texture mapping, and shading
   - Contains math formulas for geometry calculations

2. **SoftwareRenderer** - Windows application (WinExe)
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

### Key Rendering Concepts

**Portal-Based Rendering**: The engine divides levels into sectors (rooms). Walls can be portals that connect to adjacent sectors. The renderer uses a depth-based approach:
- Depth 0: player's sector
- Depth 1: sectors visible through portals from player's sector
- Depth 2+: sectors visible through nested portals
- Rendering stops at `EngineConstants.MaxRenderDepth` (2048)

**Raycasting & Y-Coordinates**: 
- For each screen column (X), cast a ray and find wall intersections
- Use plane intersection math to calculate screen Y coordinates for ceiling/floor
- Clamp to render window (shrinks as you look toward edges)

**Texture Rendering**:
- Two code paths: power-of-two textures (bit shift optimizations) vs. odd-sized
- Textures support transforms: `FlipX`, `FlipY`, `Rotated` (combined via enum flags)
- Textures cached with palette and shade combinations
- Build engine: shading via 32-level lookup table; Doom: software shading

**Sprites**: Rendered after all walls (depth sorting). Types: wall-aligned, floor-aligned, rotating sprites.

### Memory Management

Uses custom memory pooling (`AlignedMemoryPool`, `DynamicAlignedMemoryPool`) to:
- Avoid allocations during hot rendering paths
- Maintain cache locality (memory is 64-byte aligned)
- Pre-allocate buffers for common render structures (render status, texture positions, distances)

**Memory buckets** (enum `MemoryPoolBucket`) include:
- Render column status per pixel
- Wall start/end Y positions
- Texture positions and increments
- Angle cache, distance buffer, skybox data

## Command Reference

### Building

```bash
# Debug build
dotnet build DotNetPortalRenderer.sln -c Debug

# Release build (with AOT enabled for SoftwareRenderer)
dotnet build DotNetPortalRenderer.sln -c Release
```

### Running

```bash
# Load a Doom map
dotnet run --project SoftwareRenderer -- --map E1M1 --iwad /path/to/doom.wad

# Load a Doom map with custom PWAD
dotnet run --project SoftwareRenderer -- --map E1M1 --iwad /path/to/doom.wad --pwad /path/to/custom.wad

# Load a Duke Nukem 3D map
dotnet run --project SoftwareRenderer -- --map NAME --grp /path/to/duke3d/folder
```

### Testing

```bash
# Run all tests
dotnet test DotNetPortalRenderer.sln

# Run specific test file
dotnet test Tests/Tests.csproj

# Run single test by name
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~MathFormulaUnitTests"

# Run with detailed output
dotnet test DotNetPortalRenderer.sln -v d
```

### Benchmarking

```bash
# Run all benchmarks
dotnet run --project Benchmark -c Release

# Run specific benchmark class
dotnet run --project Benchmark -c Release -- --filter "*VectorRotate*"
```

### Code Quality

```bash
# Check for code analysis violations (warnings treated as errors)
dotnet build DotNetPortalRenderer.sln /p:TreatWarningsAsErrors=true

# Format code (if using dotnet format)
dotnet format DotNetPortalRenderer.sln
```

## Important Code Patterns

### SIMD & Vectorization

- `System.Runtime.Intrinsics` for Vector128/Vector256 operations
- Min/max operations use `Vector128.MinNative()` / `Vector128.MaxNative()`
- Used in `MathFormulas.GetMinMaxValue()` for performance-critical paths

### Unsafe Pointers

- Rendering uses `unsafe` extensively for direct memory access
- BGRA buffer is `void*` for direct pixel manipulation
- Texture data accessed via `fixed` statements and pointer arithmetic
- Memory pools return typed `void*` pointers cast to `Span<T>`

### Extensions

- Extension types (C# 12 feature) on `Vector2`, `TextureRenderingOptions`, `RenderColumnStatus`, etc.
- Provides readable properties and methods (e.g., `renderStatus.IsFinished`, `options.IsSkybox`)

### Attributes

- `[SkipLocalsInit]` module attribute: skips zero-initialization of local variables (performance)
- `[InternalsVisibleTo("Tests", "Benchmark")]`: allows test/benchmark projects to access internal types

## Code Guidelines

From `.github/copilot-instructions.md`:

- **Render accuracy & optimization required**: Core rendering must be accurate and optimized
- **Use vectorized/SIMD code** where appropriate for performance-critical functions
- **Prefer good locality**: Memory access patterns matter for cache efficiency
- **Self-documenting code**: Avoid redundant comments; code should be clear
- **LLM-assisted code**: Add "AI Assisted" comment to any AI-written sections

## Coding Standards

From `.editorconfig`:
- No primary constructors (`csharp_style_prefer_primary_constructors = false`)
- Prefer static local functions
- Unused parameters, assignments, and declarations are warnings
- Explicit casts for numeric conversions (required)

## Key Classes and Methods

### Core Rendering

- `PortalRenderer.DrawScreen(PortalPlayerSnapshot)` - Main render loop
- `PortalRenderer.DrawScreenStep()` - Render one depth level
- `WallRenderer.DrawBasicWall()`, `DrawPortalWall()` - Wall rendering
- `FloorAndCeilingRenderer.RenderFloorVector()`, `RenderCeilingVector()` - Plane rendering
- `SpriteRenderer.DrawSprite()` - Sprite rendering

### Helper Classes

- `WallHelper` - Wall filtering, sorting, and bunch calculation
- `SpriteHelper` - Sprite filtering by sector/distance
- `RenderWindowHelper` - Tracks render bounds (shrinking window as depth increases)
- `WallCalculations`, `TextureTransformHelper` - Math and transformations

### Asset Loading

- `GameLoader.LoadFixedGameState()` - Entry point for asset loading
- `WadReader`, `GrpReader` - Asset file parsing
- `GameRenderStateLoader` - Converts loaded assets to renderable format

## Building and Publishing

### Development

- Use Visual Studio 2022 (solution configured for it)
- Projects target .NET 10
- Recommended: `AllowUnsafeBlocks=true` for all projects (already set)

### Release/AOT

- `SoftwareRenderer` has `PublishAot=true` for native compilation
- `PublishTrimmed=true` to reduce binary size
- `CheckForOverflowUnderflow=false` for performance (no overflow checks)
- `JsonSerializerIsReflectionEnabledByDefault=false` (AOT-friendly)

## Debugging Tips

- Breakpoint in `PortalRenderer.DrawFrame()` to pause rendering
- Check `RenderWindowHelper` state to understand visible bounds
- Memory pool buckets can be inspected to verify data (e.g., render status, texture coordinates)
- OpenGL debug output enabled via `EnableDebugOutput()` in `OnLoad()` (conditional on `DEBUG` via `[Conditional("DEBUG")]`)

## Performance Considerations

1. **Memory pooling**: Hot path allocations avoided via pre-allocated buckets
2. **SIMD**: Vector operations for min/max calculations
3. **Cache locality**: 64-byte aligned buffers for typical cache line size
4. **Branching**: Separate rendering for power-of-two vs. odd textures to avoid branches
5. **Pointer arithmetic**: Direct memory access faster than array indexing in hot loops

## Testing Coverage

- `MathFormulaUnitTests` - Geometry and plane intersection math
- `AlignedMemoryPoolTests` - Memory pool functionality
- `SharedHelpersTests` - Utility function tests

Good candidates for new tests: sprite rotation, texture transforms, slope calculations.
