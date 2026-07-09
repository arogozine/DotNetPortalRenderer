using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.CommandLine;
using System.Diagnostics;

namespace SoftwareRenderer
{
    internal partial class Program
    {
        static int Main(string[] args)
        {
            AsyncLogger.Default.AddLog(LogSeverity.Info, "Software Renderer Started");

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
                    const string error = "Invalid map value. Must be 1..32 characters and contain only letters, digits, '_' or '-'.";

                    AsyncLogger.Default.AddLog(LogSeverity.Warning, error);

                    result.AddError($"Error: {error}");
                }
            });

            iwadOption.Validators.Add((result) =>
            {
                string? iwad = result.GetValue(iwadOption);

                if (string.IsNullOrEmpty(iwad))
                {
                    const string error = "Missing required argument: --iwad";

                    AsyncLogger.Default.AddLog(LogSeverity.Warning, error);

                    result.AddError($"Error: {error}");
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
                        const string error = "Cannot mix iwad/pwad with palette/grp. Choose one set of arguments.";
                        AsyncLogger.Default.AddLog(LogSeverity.Warning, error);
                        result.AddError(error);
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
                        string error = $"File '{path}' doesn't exist";
                        AsyncLogger.Default.AddLog(LogSeverity.Warning, error);
                        result.AddError(error);
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
                        string error = $"File '{path}' doesn't exist";
                        AsyncLogger.Default.AddLog(LogSeverity.Warning, error);
                        result.AddError(error);
                    }
                });
            }

            static bool IsValidMapName(string? map)
            {
                if (string.IsNullOrEmpty(map) || map.Length > 32)
                    return false;

                return map.All(static (c) => char.IsLetterOrDigit(c) || c == '_' || c == '-');
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

            AsyncLogger.Default.AddLog(LogSeverity.Info, $"IWAD: {iwad}, PWAD: {pwad}, Duke Path: {dukePath}, Map: {map}");

            try
            {
                using var skiaWindow = SoftwareRendererWindow.CreateNew(parsedArgs);
                // Collect after parsing map, we don't need those objects anymore
                GC.Collect();

                AsyncLogger.Default.AddLog(LogSeverity.Info, "Skia Render Window Loaded");
                skiaWindow.Run();
            }
            catch (Exception ex)
            {
                Debugger.Break();

                AsyncLogger.Default.AddLog(LogSeverity.Error, "Critical Application Failure", ex);

            }

            AsyncLogger.Default.AddLog(LogSeverity.Info, "Application Exited");
            AsyncLogger.Default.WaitSync();
            AsyncLogger.Default.Dispose();
        }
    }
}
