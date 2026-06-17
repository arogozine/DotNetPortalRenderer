using SoftwareRendererModels;
using System.CommandLine;
using System.Diagnostics;

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

            var dukeGrpOption = new Option<string?>("--grp")
            {
                Description = "Duke GRP folder path"
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
                string? grp = result.GetValue(dukeGrpOption);
                bool usingPaletteGrp = !string.IsNullOrEmpty(grp);
                string? iwad = result.GetValue(iwadOption);

                if (string.IsNullOrEmpty(iwad))
                {
                    result.AddError("Error: Missing required argument: --iwad");
                }
            });

            AddFileValidationCheck(iwadOption);
            AddFileValidationCheck(pwadOption);
            AddPathValidationCheck(dukeGrpOption);

            AddDoomOrDukeExclusive(iwadOption);
            AddDoomOrDukeExclusive(pwadOption);
            AddDoomOrDukeExclusive(dukeGrpOption);

            var rootCommand = new RootCommand("Software Renderer")
            {
                iwadOption,
                pwadOption,
                mapOption,
                dukeGrpOption
            };

            rootCommand.SetAction(parseResult =>
            {
                var iwad = parseResult.GetValue(iwadOption);
                var pwad = parseResult.GetValue(pwadOption);
                var map = parseResult.GetValue(mapOption);
                var grp = parseResult.GetValue(dukeGrpOption);

                HandleCommand(iwad, pwad, map!, grp);
            });

            return rootCommand.Parse(args).Invoke();

            void AddDoomOrDukeExclusive(Option<string?> option)
            {
                option.Validators.Add((result) =>
                {
                    string? grp = result.GetValue(dukeGrpOption);
                    string? iwad = result.GetValue(iwadOption);
                    string? pwad = result.GetValue(pwadOption);

                    bool usingPaletteGrp = !string.IsNullOrEmpty(grp);
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

            static void AddPathValidationCheck(Option<string?> option)
            {
                option.Validators.Add((result) =>
                {
                    string? path = result.GetValue(option);

                    if (path != null && !Path.Exists(path))
                    {
                        result.AddError($"Path '{path}' doesn't exist");
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

        private static void HandleCommand(string? iwad, string? pwad, string map, string? dukePath)
        {
            var parsedArgs = new Arguments
            {
                IWad = iwad,
                PWad = pwad,
                Map = map,
                DukePath = dukePath
            };

            try
            {
                using var skiaWindow = SoftwareRendererWindow.CreateNew(parsedArgs);
                // Collect after parsing map, we don't need those objects anymore
                GC.Collect();
                skiaWindow.Run();
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }
        }
    }
}
