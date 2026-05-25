using RenderingEngine.Models;
//using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SoftwareRenderer
{
    internal partial class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            /*
            RootCommand rootCommand = new("Software Renderer");


            var iwadOption = new Option<string>("--iwad")
            {
                Description = "Doom IWAD"
            };

            var pwadOption = new Option<string>("--pwad")
            {
                Description = "Doom PWAD"
            };

            var grpOption = new Option<string>("--grp")
            {
                Description = "Duke GRP"
            };

            var mapOption = new Option<string>("--map")
            {
                Description = "Map Name"
            };

            var paletteOption = new Option<string>("--palette")
            {
                Description = "Enable verbose output"
            };
            */

            // rootCommand.Options.Add

            if (!TryParseArgs(args, out var parsedArgs))
            {
                PrintUsage();
                return;
            }

            try
            {
                using var skiaWindow = SoftwareRendererWindow.CreateNew(parsedArgs);
                skiaWindow.Run();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static bool TryParseArgs(Span<string> args,
            [NotNullWhen(true)] out Arguments? parsed)
        {
            parsed = null;

            string? iwad = null;
            string? pwad = null;
            string? map = null;
            string? palette = null;
            string? grp = null;

            foreach (var raw in args)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string key;
                string value;

                // Support forms: --key=value, -key=value, /key:value, /key=value, key=value
                if (raw.StartsWith("--") || raw.StartsWith('-'))
                {
                    var trimmed = raw.TrimStart('-');
                    var idx = trimmed.IndexOf('=');
                    if (idx >= 0)
                    {
                        key = trimmed[..idx];
                        value = trimmed[(idx + 1)..];
                    }
                    else
                    {
                        // treat as flag without value (skip)
                        continue;
                    }
                }
                else if (raw.StartsWith('/'))
                {
                    var trimmed = raw[1..];
                    var idx = trimmed.IndexOf(':');
                    if (idx >= 0)
                    {
                        key = trimmed[..idx];
                        value = trimmed[(idx + 1)..];
                    }
                    else
                    {
                        idx = trimmed.IndexOf('=');
                        if (idx >= 0)
                        {
                            key = trimmed[..idx];
                            value = trimmed[(idx + 1)..];
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    // allow plain key=value
                    var idx = raw.IndexOf('=');
                    if (idx >= 0)
                    {
                        key = raw[..idx];
                        value = raw[(idx + 1)..];
                    }
                    else
                    {
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(key))
                    continue;

                key = key.Trim().ToLowerInvariant();
                value = value.Trim().Trim('"');

                switch (key)
                {
                    case "iwad":
                        iwad = value;
                        break;
                    case "pwad":
                        pwad = value;
                        break;
                    case "map":
                        map = value;
                        break;
                    case "palette":
                        palette = value;
                        break;
                    case "grp":
                        grp = value;
                        break;
                }
            }

            // Mutually exclusive: (iwad/pwad) vs (palette/grp)
            var usingPaletteGrp = !string.IsNullOrEmpty(palette) || !string.IsNullOrEmpty(grp);
            var usingWads = !string.IsNullOrEmpty(iwad) || !string.IsNullOrEmpty(pwad);
            if (usingPaletteGrp && usingWads)
            {
                Console.Error.WriteLine("Cannot mix iwad/pwad with palette/grp. Choose one set of arguments.");
                return false;
            }

            // Validate required file paths for wad mode
            if (!usingPaletteGrp)
            {
                if (string.IsNullOrEmpty(iwad))
                {
                    Console.Error.WriteLine("Missing required argument: iwad");
                    return false;
                }
                if (!File.Exists(iwad))
                {
                    Console.Error.WriteLine($"iwad file not found: {iwad}");
                    return false;
                }

                if (!string.IsNullOrEmpty(pwad) && !File.Exists(pwad))
                {
                    Console.Error.WriteLine($"pwad file not found: {pwad}");
                    return false;
                }
            }
            else
            {
                // palette/grp mode: both palette and grp must be provided
                if (string.IsNullOrEmpty(palette) || string.IsNullOrEmpty(grp))
                {
                    Console.Error.WriteLine("When using palette/grp mode both --palette and --grp must be provided.");
                    return false;
                }

                if (!File.Exists(palette))
                {
                    Console.Error.WriteLine($"palette file not found: {palette}");
                    return false;
                }
                if (!File.Exists(grp))
                {
                    Console.Error.WriteLine($"grp file not found: {grp}");
                    return false;
                }
            }

            // Validate map: short string, 1..32 chars, only letters, digits, underscore or hyphen
            if (string.IsNullOrEmpty(map))
            {
                Console.Error.WriteLine("Missing required argument: map");
                return false;
            }

            var mapRegex = MapRegex();
            if (!mapRegex.IsMatch(map))
            {
                Console.Error.WriteLine("Invalid map value. Must be 1..32 characters and contain only letters, digits, '_' or '-'.");
                return false;
            }

            parsed = new Arguments
            {
                IWad = iwad,
                PWad = pwad,
                Map = map,
                Palette = palette,
                Grp = grp,
            };

            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage examples:");
            Console.WriteLine("  --iwad=path/to/iwad.wad --pwad=path/to/pwad.wad --map=MAP01");
            Console.WriteLine("  /iwad:C:\\iwads\\doom.wad /pwad:C:\\mods\\my.wad /map:MAP01");
            Console.WriteLine("  --palette=path/to/palette.pal --grp=path/to/resources.grp --map=MAP01");
            Console.WriteLine();
            Console.WriteLine("Notes:");
            Console.WriteLine("  - iwad and pwad must be paths to existing files, unless using --palette and --grp instead.");
            Console.WriteLine("  - palette and grp are alternative inputs; when provided they replace iwad/pwad.");
            Console.WriteLine("  - map must be a short identifier (1..32 chars; letters, digits, '_' or '-').");
        }

        [GeneratedRegex("^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled)]
        private static partial Regex MapRegex();
    }
}
