# Contributing to DotNetPortalRenderer

I welcome contributions from everyone, especially bug fixes and more features.

## How to Contribute

### Submitting Pull Requests

1. **Target the `InDev` branch** - All pull requests must be submitted to the `InDev` branch, not `main`.
2. **Fork and create a branch** - Create a feature branch from `InDev` for your changes.
3. **Write clear commit messages** - Describe what your changes do and why.
4. **Submit your PR** - Provide a summary of changes.

### Types of Contributions Welcome

- **Bug Reports & Fixes** - These are especially appreciated
- **WAD and GRP parsing enhancements** - These are a can of worms. I don't like worms.
- **Performance Improvements** - Optimizations aligned with the project's rendering focus
- **Documentation** - Improvements to clarity and completeness
- **Test Coverage** - Additional unit tests and benchmarks

## Code Guidelines

- Use self-documenting code. Avoid redundant comments.
- For core rendering functions, use vectorized/SIMD code where appropriate.

## Attribution Requirements

**All code contributions must include proper attribution**

If code is not original, clearly document the source in an ATTRIBUTION.md file

## AI-Generated Code Disclosure

**AI use must be disclosed in pull requests**

Please review and understand the code you've generated.

## Getting Started

1. Read [CLAUDE.md](CLAUDE.md) for the project architecture and command reference
2. Check [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for community standards
3. Review existing code to understand the project's style and approach
4. Build and test your changes with `dotnet build` and `dotnet test`

## Addendum

This project is maintained by one person for fun.

If you do submit a PR, I may not get back to you for a bit.