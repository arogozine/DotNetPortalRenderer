.NET Solution Overview
- BuildAssetLoader: Loads Build Engine (Duke Nukem 3D) Assets (Maps and Textures).
- DoomAssetLoader: Loads Doom Engine (ID Tech 1) Maps and Textures.
- RenderingEngine: Portal-Based Sector (Room) Software Renderer lib.
- SoftwareRenderer: Main executable. Creates Window for rendering. Uses Silk.NET and SkiaSharp.
- SoftwareRendererModels: Models that hold loaded data, game data, and render state.
- Tests: Unit Tests.
- Benchmark: Various benchmarks.

Requirements
- Renderer must be accurate and optimized
- For core rendering functions, use vectorized/SIMD code where appropriate. Prefer algorithms that offer good locality.

Code Guidelines
- Use self-documenting code. Avoid redundant comments.
- For any LLM written or assisted code, add "Copilot Assisted" comment.