using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AntiCheat.Patches;
using HarmonyLib;
using Unity.Netcode;

static class Program
{
    static int cases, failures;
    static readonly (string Handler, string Route)[] Legacy = {
        ("__rpc_handler_255411420", "enemy"),
        ("__rpc_handler_760742013", "drop"),
        ("__rpc_handler_4121569671", "kill"),
        ("__rpc_handler_2013428264", "move"),
        ("__rpc_handler_4280509730", "activate")
    };
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Run(string name, Action action)
    {
        cases++;
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.GetBaseException().Message); }
    }
    static int Main()
    {
        Run("optional target selection and dispatch", () => {
            var type = Assembly.GetExecutingAssembly().GetType("AntiCheat.Patches.LegacyRpcCompatibilityPatch");
            Assert(type != null, "optional legacy selector is missing");
            var targetMethod = type.GetMethod("TargetMethods", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var prefix = type.GetMethod("Prefix", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert(targetMethod != null && prefix != null, "selector/prefix missing");
            AntiCheatPlugin.Log.Warnings.Clear();
            var targets = ((IEnumerable<MethodBase>)targetMethod.Invoke(null, null)).ToArray();
#if LEGACY_RPC_ALL
            const int expected = 5;
#elif LEGACY_RPC_MIXED
            const int expected = 2;
#else
            const int expected = 0;
#endif
            Assert(targets.Length == expected, $"expected {expected} available legacy targets, got {targets.Length}");
            Assert(targets.Distinct().Count() == targets.Length, "duplicate target");
            Assert(AntiCheatPlugin.Log.Warnings.Count == 5 - expected, "missing targets must each emit a warning");
            foreach (var legacy in Legacy.Where(entry => targets.All(method => method.Name != entry.Handler)))
                Assert(AntiCheatPlugin.Log.Warnings.Any(warning => warning.Contains(legacy.Handler)), "warning missing handler identity: " + legacy.Handler);
            foreach (var method in targets)
            {
                Assert(Legacy.Any(entry => entry.Handler == method.Name), "non-legacy RPC selected");
                foreach (var allowed in new[] { false, true })
                {
                    DispatchCapture.ReturnValue = allowed;
                    DispatchCapture.Name = null;
                    var target = new NetworkBehaviour();
                    var reader = new FastBufferReader { Position = 17 };
                    var rpcParams = new __RpcParams { Sender = 1234 };
                    var args = prefix.GetParameters().Select(parameter => parameter.Name switch {
                        "__originalMethod" => (object)method,
                        "target" => target,
                        "reader" => reader,
                        "rpcParams" => rpcParams,
                        _ => throw new Exception("Unexpected prefix parameter")
                    }).ToArray();
                    bool result = (bool)prefix.Invoke(null, args);
                    Assert(result == allowed, "original validation decision changed");
                    Assert(DispatchCapture.Name == Legacy.Single(entry => entry.Handler == method.Name).Route, "wrong validator called");
                    Assert(ReferenceEquals(DispatchCapture.Target, target) && DispatchCapture.Reader.Position == 17 && DispatchCapture.Parameters.Sender == 1234,
                        "RPC arguments changed while dispatching");
                }
            }
        });
        Run("real Harmony patches only available legacy handlers", () => {
            var type = Assembly.GetExecutingAssembly().GetType("AntiCheat.Patches.LegacyRpcCompatibilityPatch");
            Assert(type != null, "optional legacy selector is missing");
            var patches = new Harmony("AstaAntiCheat.LegacyRpcCompatibilityRegression").CreateClassProcessor(type).Patch();
#if LEGACY_RPC_ALL
            const int expected = 5;
#elif LEGACY_RPC_MIXED
            const int expected = 2;
#else
            const int expected = 0;
#endif
            Assert(patches.Count == expected, $"Harmony must install {expected} legacy patches, got {patches.Count}");
        });
        Console.WriteLine($"{cases - failures}/{cases} passed");
        return failures == 0 ? 0 : 1;
    }
}
