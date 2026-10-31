// Controlled Unity/NGO boundaries; actual production prefixes are linked into this executable.
// Arrays use native int32 layout. Packed scalar reads mirror the inspected NGO uint/int
// decoder, including its surprising zero result for byte counts 0, 6 and 7.
using System;
using System.Collections.Generic;
using System.Reflection;
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type) {} public HarmonyPatch(string name) {} }
    public class HarmonyPrefix : Attribute {}
    public class HarmonyWrapSafe : Attribute {}
    public static class AccessTools { public static FieldInfo DeclaredField(Type t, string n) => t.GetField(n); }
}
namespace Unity.Netcode
{
    public class NetworkBehaviour {}
    public struct __RpcParams {}
    public class ReaderState { public byte[] Bytes; public int Position; public int ArrayReads, EmptyPackedReads, MalformedPackedReads; }
    public struct FastBufferReader
    {
        public ReaderState State;
        public int Position => State.Position;
        public int Length => State.Bytes.Length;
        public void Seek(int p) => State.Position = Math.Min(Length, p);
        public bool TryBeginRead(int n) => n >= 0 && n <= Length - Position;
        public void ReadValueSafe(out bool v, object unused = null)
        { if (!TryBeginRead(1)) throw new OverflowException(); v = State.Bytes[State.Position++] != 0; }
        public void ReadValueSafe(out byte v, object unused = null)
        { if (!TryBeginRead(1)) throw new OverflowException(); v = State.Bytes[State.Position++]; }
        public void ReadValueSafe(out int v, object unused = null)
        { if (!TryBeginRead(4)) throw new OverflowException(); v = BitConverter.ToInt32(State.Bytes, Position); State.Position += 4; }
        public void ReadValueSafe(out int[] v, object unused = null)
        {
            State.ArrayReads++;
            ReadValueSafe(out int n);
            if (n < 0 || n > 1000) throw new OverflowException("unsafe allocation attempted");
            v = new int[n];
            for (int i=0;i<n;i++) ReadValueSafe(out v[i]);
        }
    }
    public static class ByteUnpacker
    {
        public static void ReadValueBitPacked(FastBufferReader r, out int v)
        {
            if (r.Position == r.Length) { r.State.EmptyPackedReads++; throw new InvalidOperationException("unsafe packed peek"); }
            int start = r.Position;
            int size = r.State.Bytes[start] & 7;
            if (size == 0 || size > 5) r.State.MalformedPackedReads++;
            if (!r.TryBeginRead(size)) throw new OverflowException();
            r.State.Position += size;
            uint bits = 0;
            if (size >= 1 && size <= 4)
            {
                for (int i=0; i<size; i++) bits |= (uint)r.State.Bytes[start+i] << (8*i);
                bits >>= 3;
            }
            else if (size == 5) bits = BitConverter.ToUInt32(r.State.Bytes, start+1);
            v = (int)(bits >> 1) ^ -(int)(bits & 1);
        }
    }
}
namespace GameNetcodeStuff
{ public class PlayerControllerB { public string playerUsername = "sender"; public bool IsHost; } }
public class Item { public string itemName = "Item"; public int creditsWorth = 30; }
public class BuyableVehicle { public int creditsWorth = 400; }
public class Terminal : Unity.Netcode.NetworkBehaviour
{
    public Item[] buyableItemsList = { new Item(), new Item() };
    public BuyableVehicle[] buyableVehicles = { new BuyableVehicle() };
    public int[] itemSalesPercentages = { 100, 100, 100 };
    public bool hasWarrantyTicket;
    public TerminalNodes terminalNodes = new TerminalNodes();
}
public class TerminalNodes
{
    public TerminalKeyword[] allKeywords = {
        new TerminalKeyword(),
        new TerminalKeyword { word="buy", compatibleNouns = new[] {
            new CompatibleNoun { result = new TerminalNode { isConfirmationNode=true, buyVehicleIndex=0,
                terminalOptions = new[] { new CompatibleNoun { result = new TerminalNode { buyVehicleIndex=0, itemCost=400 } } } } }
        } }
    };
}
public class TerminalKeyword
{
    public string word = "route";
    public CompatibleNoun[] compatibleNouns = {
        new CompatibleNoun { result = new TerminalNode { name = "Freeroute", itemCost = 0, buyRerouteToMoon = 0 } },
        new CompatibleNoun { result = new TerminalNode { name = "Paidroute", itemCost = 50, buyRerouteToMoon = 1 } }
    };
}
public class CompatibleNoun { public TerminalNode result; }
public class TerminalNode
{
    public string name; public int itemCost; public int buyRerouteToMoon = -1, buyVehicleIndex = -1;
    public bool isConfirmationNode;
    public CompatibleNoun[] terminalOptions;
}
public class SelectableLevel { public string PlanetName; }
public class UnlockableItem { public string unlockableName = "Furniture"; public TerminalNode shopSelectionNode = new TerminalNode { itemCost = 100 }; }
public class UnlockablesList { public List<UnlockableItem> unlockables = new List<UnlockableItem> { new UnlockableItem(), new UnlockableItem() }; }
public class StartOfRound : Unity.Netcode.NetworkBehaviour
{
    public static StartOfRound Instance = new StartOfRound();
    public SelectableLevel[] levels = { new SelectableLevel { PlanetName = "Free" }, new SelectableLevel { PlanetName = "Paid" } };
    public SelectableLevel currentLevel;
    public UnlockablesList unlockablesList = new UnlockablesList();
    public GameNetcodeStuff.PlayerControllerB localPlayerController = new GameNetcodeStuff.PlayerControllerB();
}
namespace AntiCheat
{
    public class Setting { public bool Value; }
    public static class PluginConfig
    {
        public static Setting RemoteTerminal = new Setting(), FreeBuy = new Setting(), FreeBuy2 = new Setting(),
            FreeBuy_Kick = new Setting(), OperationLog = new Setting(), ShipSetting_ChangToFreeMoon = new Setting();
    }
}
namespace AntiCheat.Patches
{
    public class TestLocale
    {
        public string Msg_GetString(string key, Dictionary<string,string> r) => key;
        public string OperationLog_GetString(string key, Dictionary<string,string> r) => key;
        public string Item_GetString(string key) => key;
    }
    public static class PatchHelper
    {
        public static GameNetcodeStuff.PlayerControllerB Player = new GameNetcodeStuff.PlayerControllerB();
        public static bool CheckResult = true, RemoteAllowed = true;
        public static int Money = 1000;
        public static Terminal terminal;
        public static TestLocale locale = new TestLocale();
        public static List<string> Messages = new List<string>(), Operations = new List<string>();
        public static bool Check(Unity.Netcode.__RpcParams r, out GameNetcodeStuff.PlayerControllerB p) { p=Player; return CheckResult; }
        public static bool CheckRemoteTerminal(GameNetcodeStuff.PlayerControllerB p, string rpc) => RemoteAllowed;
        public static void ShowMessage(string s) => Messages.Add(s);
        public static void ShowMessageHostOnly(string s) => Operations.Add(s);
        public static void KickPlayer(GameNetcodeStuff.PlayerControllerB p) => Messages.Add("kick");
        public static void LogInfo(GameNetcodeStuff.PlayerControllerB p, params string[] s) {}
    }
    public static class RpcGuard
    {
        public static bool Enabled = true;
        public static bool Reject(GameNetcodeStuff.PlayerControllerB p,string rpc,string reason)
        { PatchHelper.Messages.Add(reason); return false; }
    }
}
