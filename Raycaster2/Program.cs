using RenderingEngine.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SoftwareRenderer
{
    internal partial class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (!TryParseArgs(args, out var iwadPath, out var pwadPath, out var mapName))
            {
                PrintUsage();
                return;
            }

            var app = new SilkSkiaApp(new Arguments { IWad = iwadPath, PWad = pwadPath, Map = mapName });
            app.Run();
        }

        private static bool TryParseArgs(string[] args,
            [NotNullWhen(true)] out string? iwad,
            out string? pwad,
            [NotNullWhen(true)] out string? map)
        {
            iwad = null;
            pwad = null;
            map = null;

            foreach (var raw in args ?? [])
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
                }
            }

            // Validate required file paths
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

            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage examples:");
            Console.WriteLine("  --iwad=path/to/iwad.wad --pwad=path/to/pwad.wad --map=MAP01");
            Console.WriteLine("  /iwad:C:\\iwads\\doom.wad /pwad:C:\\mods\\my.wad /map:MAP01");
            Console.WriteLine();
            Console.WriteLine("Notes:");
            Console.WriteLine("  - iwad and pwad must be paths to existing files.");
            Console.WriteLine("  - map must be a short identifier (1..32 chars; letters, digits, '_' or '-').");
        }

        [GeneratedRegex("^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled)]
        private static partial Regex MapRegex();
    }
}
