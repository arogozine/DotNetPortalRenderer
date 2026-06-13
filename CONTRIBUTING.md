# Contributing to DotNetPortalRenderer

Thank you for your interest in contributing to DotNetPortalRenderer! We welcome contributions from everyone, especially bug reports and fixes.

## How to Contribute

### Submitting Pull Requests

1. **Target the `InDev` branch** - All pull requests must be submitted to the `InDev` branch, not `main`.
2. **Fork and create a branch** - Create a feature branch from `InDev` for your changes.
3. **Write clear commit messages** - Describe what your changes do and why.
4. **Submit your PR** - Include a clear description of the changes and any relevant issue numbers.

### Types of Contributions Welcome

- **Bug Reports & Fixes** - These are especially appreciated
- **Performance Improvements** - Optimizations aligned with the project's rendering focus
- **Documentation** - Improvements to clarity and completeness
- **Test Coverage** - Additional unit tests and benchmarks
- **Feature Enhancements** - Aligned with the portal rendering engine's scope

## Code Guidelines

- Use self-documenting code. Avoid redundant comments.
- For core rendering functions, use vectorized/SIMD code where appropriate.
- Prefer algorithms that offer good locality.
- No unused parameters, assignments, or declarations.
- Prefer native casts (`float.ConvertToIntegerNative<int>`, `Vector.ConvertToInt32Native`, etc).
- Naming conventions:
  - Append "Ptr" for any pointer
  - Append "Ref" for any ref pointer
  - Append "V" for any vector
- Use `Unsafe.SkipInit` for structs
- Prefer static local functions
- Avoid primary constructors

## Attribution Requirements

**All code contributions must include proper attribution:**

- If you're using code from external sources (StackOverflow, GitHub, etc.), you must include attribution comments with a link or reference.
- If code is not original, clearly document the source.
- The project maintains ATTRIBUTION files in asset loader directories - include relevant attributions there if applicable.

## AI-Generated Code Disclosure

**AI use must be disclosed in pull requests:**

1. **Add "AI Assisted" or "Copilot Assisted" comments** to any code written or significantly modified with AI assistance
2. **Disclose AI tools used** in the PR description (e.g., "Generated using GitHub Copilot", "Enhanced with Claude")
3. **Reviewer responsibility** - You, the developer, are responsible for:
   - Thoroughly reviewing all AI-generated code
   - Understanding the generated code before submitting the PR
   - Ensuring the code meets project standards and performs correctly
   - Testing the code to verify it works as intended

### Why This Matters

This ensures code quality and maintains transparency about the tools used in development.

## Getting Started

1. Read [CLAUDE.md](CLAUDE.md) for the project architecture and command reference
2. Check [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for community standards
3. Review existing code to understand the project's style and approach
4. Build and test your changes with `dotnet build` and `dotnet test`

## Questions?

Feel free to open an issue if you have questions about contributing. We're happy to help!
