using RenderingEngine.Models;
using System.CommandLine;

namespace SoftwareRenderer
{
    internal partial class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            var iwadOption = new Option<string?>("--iwad")
            {
                Description = "Doom IWAD file path"
            };

            var pwadOption = new Option<string?>("--pwad")
            {
                Description = "Doom PWAD file path"
            };

            var mapOption = new Option<string>("--map")
            {
                Description = "Map name or identifier",
                Required = true
            };

            var grpOption = new Option<string?>("--grp")
            {
                Description = "Duke GRP file path"
            };

            var paletteOption = new Option<string?>("--palette")
            {
                Description = "Palette file path"
            };

            mapOption.Validators.Add((result) =>
            {
                string? map = result.GetValue(mapOption);

                if (!IsValidMapName(map))
                {
                    result.AddError("Error: Invalid map value. Must be 1..32 characters and contain only letters, digits, '_' or '-'.");
                }
            });

            iwadOption.Validators.Add((result) =>
            {
                string? palette = result.GetValue(paletteOption);
                string? grp = result.GetValue(grpOption);
                bool usingPaletteGrp = !string.IsNullOrEmpty(palette) || !string.IsNullOrEmpty(grp);
                string? iwad = result.GetValue(iwadOption);

                if (string.IsNullOrEmpty(iwad))
                {
                    result.AddError("Error: Missing required argument: --iwad");
                }
            });

            AddFileValidationCheck(iwadOption);
            AddFileValidationCheck(pwadOption);
            AddFileValidationCheck(grpOption);
            AddFileValidationCheck(paletteOption);
            AddDoomOrDukeExclusive(paletteOption);
            AddDoomOrDukeExclusive(iwadOption);
            AddDoomOrDukeExclusive(pwadOption);
            AddDoomOrDukeExclusive(grpOption);
            AddPalletteAndGrpRequired(paletteOption);
            AddPalletteAndGrpRequired(grpOption);

            var rootCommand = new RootCommand("Software Renderer")
            {
                iwadOption,
                pwadOption,
                mapOption,
                grpOption,
                paletteOption
            };

            rootCommand.SetAction(parseResult =>
            {
                var iwad = parseResult.GetValue(iwadOption);
                var pwad = parseResult.GetValue(pwadOption);
                var map = parseResult.GetValue(mapOption);
                var grp = parseResult.GetValue(grpOption);
                var palette = parseResult.GetValue(paletteOption);

                HandleCommand(iwad, pwad, map!, grp, palette);
            });

            return rootCommand.Parse(args).Invoke();

            void AddPalletteAndGrpRequired(Option<string?> option)
            {
                option.Validators.Add((result) =>
                {
                    string? palette = result.GetValue(paletteOption);
                    string? grp = result.GetValue(grpOption);

                    if (string.IsNullOrEmpty(palette) != string.IsNullOrEmpty(grp))
                    {
                        result.AddError("When using palette/grp mode, both --palette and --grp must be provided.");
                    }
                });
            }

            void AddDoomOrDukeExclusive(Option<string?> option)
            {
                option.Validators.Add((result) =>
                {
                    string? palette = result.GetValue(paletteOption);
                    string? grp = result.GetValue(grpOption);
                    string? iwad = result.GetValue(iwadOption);
                    string? pwad = result.GetValue(pwadOption);

                    bool usingPaletteGrp = !string.IsNullOrEmpty(palette) || !string.IsNullOrEmpty(grp);
                    bool usingWads = !string.IsNullOrEmpty(iwad) || !string.IsNullOrEmpty(pwad);

                    if (usingPaletteGrp && usingWads)
                    {
                        result.AddError("Cannot mix iwad/pwad with palette/grp. Choose one set of arguments.");
                    }
                });
            }

            static void AddFileValidationCheck(Option<string?> option)
            {
                option.Validators.Add((result) =>
                {
                    string? path = result.GetValue(option);

                    if (path != null && !File.Exists(path))
                    {
                        result.AddError($"File '{path}' doesn't exist");
                    }
                });
            }

            static bool IsValidMapName(string? map)
            {
                if (string.IsNullOrEmpty(map) || map.Length > 32)
                    return false;

                return map.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
            }
        }

        private static void HandleCommand(string? iwad, string? pwad, string map, string? grp, string? palette)
        {
            var parsedArgs = new Arguments
            {
                IWad = iwad,
                PWad = pwad,
                Map = map,
                Palette = palette,
                Grp = grp,
            };

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
    }
}
