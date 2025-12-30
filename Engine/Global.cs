global using XyzTuple = (float X, float Y, float Z);

// Various compiler attributes
global using System.Diagnostics;
global using System.Runtime.CompilerServices;
global using System.Diagnostics.CodeAnalysis;
global using System.Runtime.InteropServices;

// Structs usually get zero-ed out on initialization which slightly reduces performance.
// This attribute allows us to skip doing that.
// No equivalent exists for allocating small arrays though.
[module: SkipLocalsInit]
[assembly: InternalsVisibleTo("Tests")]
[assembly: InternalsVisibleTo("Benchmark")]
