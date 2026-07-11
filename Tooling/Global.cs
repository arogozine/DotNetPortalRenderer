// Various compiler attributes
global using System.Diagnostics;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;

[module: SkipLocalsInit]
[assembly: InternalsVisibleTo("Tests")]
[assembly: InternalsVisibleTo("Benchmark")]
