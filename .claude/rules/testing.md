---
paths: 
  - 'Tests/**/*'
---

# Testing Rules

- If needed, add "unsafe" keyword to the class - avoid "unsafe" on methods.
- Naming: ClassNameTests.MethodName_ShortDescriptionOfBehaviorBeingTested
- Directory structure: group tests by SourceProject/Functionality
- Use self-documenting code, concise comments.
- For SIMD code with scalar fallback, run unit tests with three configurations,
	- With "DOTNET_EnableHWIntrinsic=0"
	- With "DOTNET_EnableAVX2=0"
	- Normally