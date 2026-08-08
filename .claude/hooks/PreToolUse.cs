#!/usr/bin/dotnet run

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using JsonDocument doc = JsonDocument.Parse(Console.In.ReadToEnd());

if (!TryGetStringElement(doc, "tool_name", out string toolName)) {
    Console.Error.WriteLine("PreToolUse misconfiguration issue");
    return 1;
}

if (!string.Equals(toolName, "Bash", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(toolName, "powershell", StringComparison.OrdinalIgnoreCase))
{
    return 0;
}

if (!TryGetStringElement(doc, "tool_input.command", out string command)) {
    Console.Error.WriteLine("PreToolUse misconfiguration issue");
    return 1;
}

Span<string> patterns = [
    "password=",
    "secret=",
    "api_key=",
    "apikey=",
    "token=",
    "aws_access_key",
    "aws_secret",
    "private_key",
];

foreach (string pattern in patterns) {
    if (Regex.IsMatch(command, Regex.Escape(pattern), RegexOptions.IgnoreCase))
    {
        Block("Potential secret detected");
        return 2;
    }
}

foreach (Regex sandboxPattern in GetSandboxDenyPatterns())
{
    if (sandboxPattern.IsMatch(command))
    {
        Block($"Command matches sandboxed credential pattern: {sandboxPattern}");
        return 2;
    }
}

if (IsDestructiveDeleteCommand(command))
{
    Block("Recursive deletes (rm -r/-rf, rd/rmdir /s, Remove-Item -Recurse, etc.) must be run manually by the user, not by Claude.");
    return 2;
}

return 0;

// AI Assisted: quote-aware tokenizer so words inside "..."/'...' (e.g. an echo of
// "rm -rf") aren't misread as an actual rm invocation with a recursive flag.
static bool IsDestructiveDeleteCommand(string command)
{
    foreach (string segment in Regex.Split(command, @"[;&|\r\n]+"))
    {
        string[] tokens = Tokenize(segment);

        for (int i = 0; i < tokens.Length; i++)
        {
            string name = Path.GetFileNameWithoutExtension(tokens[i]).ToLowerInvariant();
            string[] rest = tokens[(i + 1)..];

            if (string.Equals(name, "rm", StringComparison.OrdinalIgnoreCase) &&
                HasUnixRecursiveFlag(rest))
            {
                return true;
            }

            if ((string.Equals(name, "rd", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(name, "rmdir", StringComparison.OrdinalIgnoreCase)) &&
                HasCmdRecursiveFlag(rest))
            {
                return true;
            }

            if (IsPowerShellRemoveCommand(name) && HasPowerShellRecurseFlag(rest))
            {
                return true;
            }
        }
    }

    return false;
}

static string[] Tokenize(string segment)
{
    List<string> tokens = [];
    StringBuilder current = new();
    bool inSingleQuote = false;
    bool inDoubleQuote = false;
    bool tokenStarted = false;

    foreach (char c in segment)
    {
        if (inSingleQuote)
        {
            if (c == '\'') inSingleQuote = false;
            else current.Append(c);
            continue;
        }

        if (inDoubleQuote)
        {
            if (c == '"') inDoubleQuote = false;
            else current.Append(c);
            continue;
        }

        if (c == '\'')
        {
            inSingleQuote = true;
            tokenStarted = true;
            continue;
        }

        if (c == '"')
        {
            inDoubleQuote = true;
            tokenStarted = true;
            continue;
        }

        if (char.IsWhiteSpace(c))
        {
            if (tokenStarted)
            {
                tokens.Add(current.ToString());
                current.Clear();
                tokenStarted = false;
            }

            continue;
        }

        current.Append(c);
        tokenStarted = true;
    }

    if (tokenStarted) tokens.Add(current.ToString());

    return [.. tokens];
}

static bool HasUnixRecursiveFlag(string[] tokens)
{
    foreach (string token in tokens)
    {
        if (token.Equals("--recursive", StringComparison.OrdinalIgnoreCase)) return true;

        if (token.Length > 1 && token[0] == '-' && token[1] != '-')
        {
            foreach (char flag in token.AsSpan(1))
            {
                if (flag is 'r' or 'R') return true;
            }
        }
    }

    return false;
}

static bool HasCmdRecursiveFlag(string[] tokens)
{
    foreach (string token in tokens)
    {
        if (token.Equals("/s", StringComparison.OrdinalIgnoreCase)) return true;
    }

    return false;
}

static bool IsPowerShellRemoveCommand(string name) =>
    name is "remove-item" or "ri" or "rd" or "rmdir" or "del" or "erase" or "rm";

static bool HasPowerShellRecurseFlag(string[] tokens)
{
    foreach (string token in tokens)
    {
        if (token.Length < 2 || token[0] != '-') continue;

        string body = token[1..];
        int cut = body.IndexOfAny([':', '=']);
        if (cut >= 0) body = body[..cut];

        if (body.Length >= 3 && "recurse".StartsWith(body, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}

static void Block(string reason)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(
        new DecisionDetails { Decision = "block", Reason = reason },
        DecisionDetailsContext.Default.DecisionDetails));
}

static IEnumerable<Regex> GetSandboxDenyPatterns()
{
    string? projectDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR");
    string settingsPath = Path.Combine(
        string.IsNullOrEmpty(projectDir) ? "." : projectDir,
        ".claude", "settings.json");

    if (!File.Exists(settingsPath)) yield break;

    using JsonDocument settingsDoc = JsonDocument.Parse(File.ReadAllText(settingsPath));

    if (TryGetJsonElement(settingsDoc, "sandbox.credentials.files", out JsonElement files) &&
        files.ValueKind == JsonValueKind.Array)
    {
        foreach (JsonElement file in files.EnumerateArray())
        {
            if (IsDenyEntry(file) &&
                file.TryGetProperty("path", out JsonElement path) &&
                path.ValueKind == JsonValueKind.String)
            {
                yield return GlobToRegex(path.GetString()!);
            }
        }
    }

    if (TryGetJsonElement(settingsDoc, "sandbox.credentials.envVars", out JsonElement envVars) &&
        envVars.ValueKind == JsonValueKind.Array)
    {
        foreach (JsonElement envVar in envVars.EnumerateArray())
        {
            if (IsDenyEntry(envVar) &&
                envVar.TryGetProperty("name", out JsonElement name) &&
                name.ValueKind == JsonValueKind.String)
            {
                yield return new Regex(Regex.Escape(name.GetString()!), RegexOptions.IgnoreCase);
            }
        }
    }
}

static bool IsDenyEntry(JsonElement entry) =>
    entry.TryGetProperty("mode", out JsonElement mode) &&
    mode.ValueKind == JsonValueKind.String &&
    mode.GetString() == "deny";

static Regex GlobToRegex(string glob)
{
    StringBuilder sb = new();

    for (int i = 0; i < glob.Length; i++)
    {
        char c = glob[i];

        if (c == '*' && i + 1 < glob.Length && glob[i + 1] == '*')
        {
            sb.Append(".*");
            i++;
        }
        else if (c == '*')
        {
            sb.Append(@"[^/\\]*");
        }
        else if (c == '~')
        {
            sb.Append(@"[^\s]*");
        }
        else
        {
            sb.Append(Regex.Escape(c.ToString()));
        }
    }

    return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
}

static bool TryGetStringElement(JsonDocument doc,
    scoped ReadOnlySpan<char> path,
    out string value)
{
    bool valid = TryGetJsonElement(doc, path, out JsonElement jsonElement);
    value = jsonElement.ToString();
    return valid;
}

static bool TryGetJsonElement(JsonDocument doc,
    scoped ReadOnlySpan<char> path,
    out JsonElement jsonElement)
{
    jsonElement = doc.RootElement;

    if (jsonElement.ValueKind == JsonValueKind.Null ||
        jsonElement.ValueKind == JsonValueKind.Undefined)
    {
        return false;
    }

    foreach (var range in path.Split('.'))
    {
        if (!jsonElement.TryGetProperty(path[range], out jsonElement) ||
            jsonElement.ValueKind == JsonValueKind.Null ||
            jsonElement.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }
    }

    return true;
}

public class DecisionDetails
{
    public string? Decision { get; set; }
    public string? Reason { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DecisionDetails))]
internal partial class DecisionDetailsContext : JsonSerializerContext;
