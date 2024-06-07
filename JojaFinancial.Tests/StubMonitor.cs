using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;

namespace JojaFinancial.Tests
{
    internal class StubMonitor
        : IMonitor
    {
        List<string> Errors { get; } = new List<string>();
        List<string> Warnings { get; } = new List<string>();
        List<string> Info { get; } = new List<string>();

        public bool IsVerbose => false;

        public void Log(string message, LogLevel level = LogLevel.Trace)
        {
            switch (level)
            {
                case LogLevel.Error:
                    this.Errors.Add(message);
                    break;
                case LogLevel.Warn:
                    this.Warnings.Add(message);
                    break;
                case LogLevel.Info:
                    this.Info.Add(message);
                    break;
                default:
                    break;
            }
        }

        public void LogOnce(string message, LogLevel level = LogLevel.Trace)
        {
            this.Log(message, level);
        }

        public void VerboseLog(string message)
        {
        }

        public void VerboseLog([InterpolatedStringHandlerArgument("")] ref VerboseLogStringHandler message)
        {
        }

        private static object modEntryLock = new object();

        /// <summary>
        ///   Runs mod.ModEntry, but wraps that call to deal with localization and inserts property values and much
        ///   other reflection shenanigans.
        /// </summary>
        public static void PrepMod(Mod mod, StubMonitor stubMonitor, StubModHelper stubHelper)
        {
            var prop = typeof(Mod).GetProperty(nameof(mod.Monitor), BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            prop!.SetValue(mod, stubMonitor);
            prop = typeof(Mod).GetProperty(nameof(mod.Helper), BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            prop!.SetValue(mod, stubHelper);

            // mod.Entry is expected to set up Localization, which, alas, mucks with some static
            // properties.  Lacking a better plan, we'll use reflection to hijack that process
            // and do it under a lock to enable multithreaded test-running, if we ever get that far.
            lock (modEntryLock) {
                var a = Assembly.Load(new AssemblyName("NermNermNerm.Stardew.LocalizeFromSource"))!;
                var sdvLocalizeMethodsType = a.GetType("NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize")!;
                var translatorsField = sdvLocalizeMethodsType.GetField("translators", BindingFlags.NonPublic | BindingFlags.Static)!;
                var translatorsDict = (System.Collections.IDictionary)(translatorsField.GetValue(null)!);
                List<KeyValuePair<object,object>>? entries = new List<KeyValuePair<object, object>>();
                if (translatorsField is not null)
                {
                    foreach (object key in translatorsDict.Keys)
                    {
                        entries.Add(new KeyValuePair<object, object>(key, translatorsDict[key]!));
                    }
                    translatorsDict.Clear();
                }

                mod.Entry(stubHelper);

                // Undo the creation of the new translator entry so that we don't spend time attempting to re-reading the default.json
                if (entries is not null && entries.Any())
                {
                    translatorsDict.Clear();
                    foreach (var pair in entries)
                    {
                        translatorsDict[pair.Key] = pair.Value;
                    }
                }
                else
                {
                    // This is our first time through, so we need to undo the psuedo-loc setting
                    object? firstKey = null;
                    foreach (object key in translatorsDict.Keys)
                    {
                        firstKey = key;
                        break;
                    }
                    object translator = translatorsDict[firstKey!]!;

                    prop = translator.GetType().GetProperty("DoPseudoLoc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    prop!.SetValue(translator, false);
                }
            }
        }
    }
}
