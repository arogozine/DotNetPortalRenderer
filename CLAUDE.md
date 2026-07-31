# CLAUDE.md

## Project Overview

**DotNetPortalRenderer** is a .NET 10 software-based portal rendering engine that renders maps from the Doom (ID Tech 1) and Build (Duke Nukem 3D) game engines.
It implements a classic portal-based raycasting renderer.

## Architecture Overview

### Project Structure

1. **RenderingEngine** - Core portal rendering library
2. **SoftwareRenderer** - Entry point. Cross platform application.
3. **SoftwareRendererModels** - Shared data models
4. **DoomAssetLoader** - Doom WAD file parsing
5. **BuildAssetLoader** - Duke Nukem 3D GRP file parsing
6. **Tooling** - General-purpose utilities (memory pooling, sorting, async logging)
7. **Tests** - Unit tests (XUnit)
8. **Benchmark** - Performance benchmarks (BenchmarkDotNet)

### Rendering

Main rendering implementation is in (`Engine/Engine/Renderer/PortalRenderer.cs`).

### Memory Management

Uses custom memory pooling for SIMD aligned loads.

Uses object pooling to avoid GC.

## Command Reference

### Building

```bash
dotnet build DotNetPortalRenderer.sln -c Debug
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
```

### Benchmarking

```bash
# Run all benchmarks
dotnet run --project Benchmark -c Release
```

## Guidelines & Standards

### Guidelines

- **Self-documenting code**: Avoid redundant comments.
- **Use vectorized/SIMD code**: Use Vector intrinsics with scalar fallback.
- **Maximum optimization**: Prefer performance over safety.
- **Immutability**: Prefer readonly structs, sealed records and classes, etc.
- **LLM-assisted code**: Add "AI Assisted" comment to written or modified code.

### Standards

- No unused parameters, assignments, and declarations
- Prefer native casts (`float.ConvertToIntegerNative<int>`, `Vector.ConvertToInt32Native`, etc).
- Naming: Append "Ptr" for any pointer.
- Naming: Append "Ref" for any ref pointer.
- Naming: Append "V" for any vector.
- Use `Unsafe.SkipInit` for structs.
- Prefer static local functions.
- Avoid primary constructors

## Prohibited Behavior

- GIT: Commit, Push, Stage, and Unstage