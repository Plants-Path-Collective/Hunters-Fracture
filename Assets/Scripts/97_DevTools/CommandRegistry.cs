#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Core;

namespace DevTools
{
    /// <summary>Metadata of one registered console command.</summary>
    internal sealed class CommandInfo
    {
        public string Name;
        public string Description;
        public MethodInfo Method;
        public ParameterInfo[] Parameters;
        public string Usage;
    }

    /// <summary>
    /// Finds [ConsoleCommand] methods, parses a command line and invokes the matching method.
    /// </summary>
    internal static class CommandRegistry
    {
        private static readonly Dictionary<string, CommandInfo> commands =
            new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

        // Third-party / engine assemblies that never contain console commands (speeds up startup).
        private static readonly string[] IgnoredAssemblyPrefixes =
        {
            "System", "mscorlib", "netstandard", "Microsoft", "Mono.", "Unity.", "UnityEngine",
            "UnityEditor", "nunit", "Newtonsoft", "Bee.", "ExCSS", "JetBrains", "SyntaxTree",
            "FMOD", "DOTween", "Cinemachine"
        };

        public static int Count => commands.Count;

        public static IEnumerable<CommandInfo> All =>
            commands.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

        public static bool TryGet(string name, out CommandInfo info) => commands.TryGetValue(name, out info);

        // ── Discovery ─────────────────────────────────────────────────────────

        /// <summary>Scans every relevant assembly for static methods marked with [ConsoleCommand].</summary>
        public static void Build()
        {
            commands.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || IsIgnored(assembly)) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

                foreach (Type type in types)
                {
                    if (type.IsGenericTypeDefinition) continue;

                    const BindingFlags flags = BindingFlags.Static | BindingFlags.Public |
                                               BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                    foreach (MethodInfo method in type.GetMethods(flags))
                    {
                        var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        if (attribute != null) Register(attribute, method);
                    }
                }
            }
        }

        private static bool IsIgnored(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            foreach (string prefix in IgnoredAssemblyPrefixes)
                if (name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void Register(ConsoleCommandAttribute attribute, MethodInfo method)
        {
            if (method.IsGenericMethodDefinition) return;

            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!IsSupported(parameters[i], i == parameters.Length - 1))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[DevConsole] '{attribute.Name}' skipped: unsupported parameter " +
                        $"'{parameters[i].Name}' ({parameters[i].ParameterType.Name}).");
                    return;
                }
            }

            if (commands.ContainsKey(attribute.Name))
            {
                UnityEngine.Debug.LogWarning($"[DevConsole] Duplicate command '{attribute.Name}' ignored.");
                return;
            }

            commands[attribute.Name] = new CommandInfo
            {
                Name = attribute.Name,
                Description = attribute.Description,
                Method = method,
                Parameters = parameters,
                Usage = BuildUsage(attribute.Name, parameters)
            };
        }

        private static bool IsParams(ParameterInfo p) => p.GetCustomAttribute<ParamArrayAttribute>() != null;

        private static bool IsSupported(ParameterInfo p, bool isLast)
        {
            Type t = p.ParameterType;
            if (t == typeof(string[])) return isLast && IsParams(p);
            return t == typeof(string) || t == typeof(int) || t == typeof(long) || t == typeof(float) ||
                   t == typeof(double) || t == typeof(bool) || t.IsEnum;
        }

        private static string BuildUsage(string name, ParameterInfo[] parameters)
        {
            var sb = new StringBuilder("Usage: ").Append(name);
            foreach (ParameterInfo p in parameters)
            {
                if (IsParams(p)) sb.Append(" <").Append(p.Name).Append("...>");
                else if (p.HasDefaultValue) sb.Append(" [").Append(p.Name).Append(':').Append(TypeLabel(p.ParameterType)).Append(']');
                else sb.Append(" <").Append(p.Name).Append(':').Append(TypeLabel(p.ParameterType)).Append('>');
            }
            return sb.ToString();
        }

        private static string TypeLabel(Type t)
        {
            if (t.IsEnum) return string.Join("|", Enum.GetNames(t));
            if (t == typeof(string)) return "text";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(int) || t == typeof(long)) return "int";
            return "number";
        }

        // ── Autocomplete ──────────────────────────────────────────────────────

        /// <summary>Returns command names starting with the given prefix, sorted.</summary>
        public static List<string> Match(string prefix) =>
            commands.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

        // ── Execution ─────────────────────────────────────────────────────────

        /// <summary>Parses and runs one command line. Errors are printed to the console, never thrown.</summary>
        public static void Execute(string line)
        {
            line = line.Trim();
            if (line.StartsWith("/")) line = line.Substring(1); // Minecraft-style optional slash

            List<string> tokens = Tokenize(line);
            if (tokens.Count == 0) return;

            string name = tokens[0];
            if (!commands.TryGetValue(name, out CommandInfo command))
            {
                DevConsole.PrintError($"Unknown command '{name}'. {Suggest(name)}");
                return;
            }

            if (!TryBindArguments(command, tokens, out object[] args, out string error))
            {
                DevConsole.PrintError(error);
                DevConsole.Print(command.Usage, CONSOLE_LINE_KIND.Output);
                return;
            }

            try
            {
                object result = command.Method.Invoke(null, args);
                if (result is string text && text.Length > 0)
                    DevConsole.Print(text, CONSOLE_LINE_KIND.Output);
            }
            catch (TargetInvocationException ex)
            {
                // Goes through Debug.LogException, so it shows up in the console log like any other error.
                UnityEngine.Debug.LogException(ex.InnerException ?? ex);
            }
        }

        /// <summary>Splits a line into tokens. Double quotes group words: scene "My Scene".</summary>
        public static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            bool hasToken = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (hasToken)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        hasToken = false;
                    }
                    continue;
                }

                current.Append(c);
                hasToken = true;
            }

            if (hasToken) tokens.Add(current.ToString());
            return tokens;
        }

        private static bool TryBindArguments(CommandInfo command, List<string> tokens,
            out object[] args, out string error)
        {
            ParameterInfo[] ps = command.Parameters;
            int given = tokens.Count - 1;
            bool hasParams = ps.Length > 0 && IsParams(ps[ps.Length - 1]);
            int positional = hasParams ? ps.Length - 1 : ps.Length;

            args = new object[ps.Length];
            error = null;

            if (!hasParams && given > positional)
            {
                error = $"Too many arguments (expected at most {positional}).";
                return false;
            }

            for (int i = 0; i < positional; i++)
            {
                if (i < given)
                {
                    if (!TryConvert(tokens[i + 1], ps[i].ParameterType, ps[i].Name, out args[i], out error))
                        return false;
                }
                else if (ps[i].HasDefaultValue)
                {
                    args[i] = Type.Missing; // reflection fills in the declared default
                }
                else
                {
                    error = $"Missing argument '{ps[i].Name}'.";
                    return false;
                }
            }

            if (hasParams)
            {
                int rest = Math.Max(0, given - positional);
                var extra = new string[rest];
                for (int k = 0; k < rest; k++) extra[k] = tokens[1 + positional + k];
                args[ps.Length - 1] = extra;
            }

            return true;
        }

        private static bool TryConvert(string token, Type type, string paramName, out object value, out string error)
        {
            value = null;
            error = null;
            CultureInfo inv = CultureInfo.InvariantCulture;

            if (type == typeof(string)) { value = token; return true; }

            if (type == typeof(int) && int.TryParse(token, NumberStyles.Integer, inv, out int i)) { value = i; return true; }
            if (type == typeof(long) && long.TryParse(token, NumberStyles.Integer, inv, out long l)) { value = l; return true; }
            if (type == typeof(float) && float.TryParse(token, NumberStyles.Float, inv, out float f)) { value = f; return true; }
            if (type == typeof(double) && double.TryParse(token, NumberStyles.Float, inv, out double d)) { value = d; return true; }

            if (type == typeof(bool))
            {
                switch (token.ToLowerInvariant())
                {
                    case "true": case "1": case "on": case "yes": value = true; return true;
                    case "false": case "0": case "off": case "no": value = false; return true;
                }
            }

            if (type.IsEnum)
            {
                try
                {
                    object parsed = Enum.Parse(type, token, true);
                    if (Enum.IsDefined(type, parsed)) { value = parsed; return true; }
                }
                catch (ArgumentException) { /* falls through to the error below */ }

                error = $"'{token}' is not a valid {paramName}. Valid values: {string.Join(", ", Enum.GetNames(type))}.";
                return false;
            }

            error = $"Argument '{paramName}': cannot read '{token}' as {TypeLabel(type)}.";
            return false;
        }

        private static string Suggest(string typed)
        {
            List<string> similar = commands.Keys
                .Where(k => k.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(5)
                .ToList();
            return similar.Count > 0 ? "Did you mean: " + string.Join(", ", similar) + "?" : "Type 'help' for the list.";
        }
    }
}
#endif