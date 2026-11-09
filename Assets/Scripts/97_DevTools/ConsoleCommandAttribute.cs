using System;
using System.Diagnostics;

namespace DevTools
{
    /// <summary>
    /// Marks a static method as a dev console command. The console discovers these through
    /// reflection at startup, so commands can live next to the system they control.
    ///
    /// The attribute is [Conditional], so usages are stripped from release builds automatically
    /// (the commands stay in your code, they just are never registered).
    ///
    /// Supported parameter types: string, int, long, float, double, bool, any enum, and a final
    /// "params string[]". Optional parameters (default values) are supported.
    /// A command may return void or a string (printed as output).
    ///
    /// Example:
    ///   [ConsoleCommand("timescale", "Sets Time.timeScale.")]
    ///   private static string TimeScale(float scale = 1f) { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        /// <summary>Command name typed in the console. Case-insensitive. Use dots to group (combat.start).</summary>
        public string Name { get; }

        /// <summary>One-line description shown by 'help'.</summary>
        public string Description { get; }

        public ConsoleCommandAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }
}