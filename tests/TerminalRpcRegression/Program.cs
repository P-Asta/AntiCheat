using System;
using System.IO;
using System.Linq;
using AntiCheat;
using AntiCheat.Patches;
using GameNetcodeStuff;
using Unity.Netcode;

static class Program
{
    static int failures, count;
    static void Run(string name, Action test)
    {
        count++;
        PatchHelper.Player = new PlayerControllerB(); PatchHelper.CheckResult = true; PatchHelper.RemoteAllowed = true;
        PatchHelper.Messages.Clear(); PatchHelper.Operations.Clear(); PatchHelper.Money = 1000;
        PatchHelper.terminal = new Terminal(); StartOfRound.Instance = new StartOfRound();
        StartOfRound.Instance.currentLevel = StartOfRound.Instance.levels[0];
        RpcGuard.Enabled = true;
        PluginConfig.FreeBuy.Value = false; PluginConfig.FreeBuy2.Value = false; PluginConfig.FreeBuy_Kick.Value = false;
        PluginConfig.RemoteTerminal.Value = false; PluginConfig.OperationLog.Value = true;
        PluginConfig.ShipSetting_ChangToFreeMoon.Value = false;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    }

    static byte[] Payload(string kind, int id=0,int credits=900,int[] items=null,bool warranty=false,bool present=true,int? claimedLength=null,int itemCount=1)
    {
        using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
        if(kind=="items")
        {
            w.Write(present);
            if(present)
            {
                items ??= new[]{id}; w.Write(claimedLength ?? items.Length);
                foreach(int i in items) w.Write(i);
            }
            WritePacked(w,credits); WritePacked(w,itemCount);
        }
        else { WritePacked(w,id); WritePacked(w,credits); if(kind=="vehicle") w.Write(warranty); }
        return ms.ToArray();
    }

    // Explicit full-width NGO encoding: size=5 followed by the ZigZag uint.
    static void WritePacked(BinaryWriter writer, int value)
    { writer.Write((byte)5); writer.Write(unchecked((uint)((value << 1) ^ (value >> 31)))); }

    static void Call(string kind,byte[] payload,bool allowed,string reason=null,int offset=7,bool noAllocation=false,bool invalidPacked=false)
    {
        var state = new ReaderState { Bytes = new byte[offset].Concat(payload).ToArray(), Position = offset };
        if(invalidPacked) state.Bytes[offset+(kind=="items"?9:0)] = 0;
        var reader = new FastBufferReader { State = state };
        bool actual = kind switch {
            "items" => TerminalTransactionPatch.BuyItemsServerRpc(PatchHelper.terminal,reader,default),
            "vehicle" => TerminalTransactionPatch.BuyVehicleServerRpc(PatchHelper.terminal,reader,default),
            "unlockable" => StartOfRoundTransactionPatch.BuyShipUnlockableServerRpc(StartOfRound.Instance,reader,default),
            _ => StartOfRoundTransactionPatch.ChangeLevelServerRpc(StartOfRound.Instance,reader,default)
        };
        if(actual!=allowed) throw new Exception($"allowed={actual}, expected {allowed}");
        if(state.Position!=offset) throw new Exception($"cursor={state.Position}, expected {offset}");
        string messages=string.Join(",",PatchHelper.Messages);
        if(messages!=(reason??"")) throw new Exception($"messages={messages}, expected {reason??"none"}");
        if(noAllocation && state.ArrayReads!=0) throw new Exception("array allocation attempted before rejecting bad shape");
        if(state.EmptyPackedReads!=0) throw new Exception("packed integer reader attempted unsafe peek past payload");
        if(state.MalformedPackedReads!=0) throw new Exception("malformed byte count reached permissive native packed decoder");
        if(!allowed && PatchHelper.Operations.Count!=0) throw new Exception("rejected request emitted successful operation log");
    }

    static int Main()
    {
        foreach(var kind in new[]{"items","vehicle","unlockable","moon"})
        {
            Run(kind+" valid request preserves original cursor",()=>Call(kind,Payload(kind),true));
            Run(kind+" negative ID rejected before indexing",()=>Call(kind,Payload(kind,id:-1),false,"RpcInvalidIndex"));
            Run(kind+" upper boundary rejected",()=>Call(kind,Payload(kind,id:kind=="vehicle"?1:2),false,"RpcInvalidIndex"));
            Run(kind+" negative credits rejected independently of FreeBuy",()=>Call(kind,Payload(kind,credits:-1),false,"RpcInvalidCredits"));
            Run(kind+" truncated payload rejected and cursor restored",()=>Call(kind,new byte[]{1},false,"RpcMalformedPayload",noAllocation:true));
            Run(kind+" empty payload rejected before unsafe packed peek",()=>Call(kind,Array.Empty<byte>(),false,"RpcMalformedPayload",noAllocation:true));
            Run(kind+" malformed packed byte count rejected",()=>Call(kind,Payload(kind),false,"RpcMalformedPayload",invalidPacked:true));
            Run(kind+" host bypasses unreadable payload",()=>{PatchHelper.CheckResult=false;Call(kind,Array.Empty<byte>(),true,noAllocation:true);});
            Run(kind+" unknown sender rejected without parsing",()=>{PatchHelper.CheckResult=false;PatchHelper.Player=null;Call(kind,Array.Empty<byte>(),false,noAllocation:true);});
            Run(kind+" disabled feature allows valid optional behavior",()=>{RpcGuard.Enabled=false;Call(kind,Payload(kind,credits:-1),true);});
            Run(kind+" disabled feature quietly cancels invalid index",()=>{RpcGuard.Enabled=false;Call(kind,Payload(kind,id:-1),false);});
            Run(kind+" remote-terminal policy returns before parsing",()=>{PluginConfig.RemoteTerminal.Value=true;PatchHelper.RemoteAllowed=false;Call(kind,Array.Empty<byte>(),false,noAllocation:true);});
        }
        Run("null items array rejected before vanilla null dereference",()=>Call("items",Payload("items",present:false),false,"RpcMalformedPayload",noAllocation:true));
        Run("negative array length rejected before allocation",()=>Call("items",Payload("items",claimedLength:-1),false,"RpcMalformedPayload",noAllocation:true));
        Run("huge array length rejected before allocation",()=>Call("items",Payload("items",claimedLength:int.MaxValue),false,"RpcMalformedPayload",noAllocation:true));
        Run("array length exceeding remaining bytes rejected before allocation",()=>Call("items",Payload("items",claimedLength:12),false,"RpcMalformedPayload",noAllocation:true));
        Run("native 12-item boundary accepted",()=>Call("items",Payload("items",items:Enumerable.Repeat(0,12).ToArray()),true));
        Run("native server 13-item rejection happens before allocation",()=>Call("items",Payload("items",items:Enumerable.Repeat(0,13).ToArray()),false,"RpcMalformedPayload",noAllocation:true));
        Run("missing item count trailer rejected",()=>{var p=Payload("items");Call("items",p.Take(p.Length-5).ToArray(),false,"RpcMalformedPayload");});
        Run("missing item credits rejected before unsafe packed peek",()=>{var p=Payload("items");Call("items",p.Take(p.Length-10).ToArray(),false,"RpcMalformedPayload");});
        foreach(var kind in new[]{"vehicle","unlockable","moon"})
            Run(kind+" missing credits rejected before unsafe packed peek",()=>Call(kind,Payload(kind).Take(5).ToArray(),false,"RpcMalformedPayload"));
        foreach(var kind in new[]{"items","vehicle","unlockable","moon"})
        foreach(byte header in new byte[]{0,6,7})
        foreach(int packedOffset in kind=="items"?new[]{9,14}:new[]{0,5})
            Run($"{kind} rejects packed header {header:X2} at {packedOffset} before native decoder",()=>{
                var p=Payload(kind).Concat(new byte[8]).ToArray();p[packedOffset]=header;
                Call(kind,p,false,"RpcMalformedPayload");
            });
        Run("missing item catalog is quietly rejected",()=>{PatchHelper.terminal.buyableItemsList=null;Call("items",Payload("items"),false);});
        Run("modded host catalog item ID accepted",()=>{PatchHelper.terminal.buyableItemsList=Enumerable.Range(0,20).Select(_=>new Item()).ToArray();Call("items",Payload("items",id:19),true);});
        Run("modded host moon index 11 accepted",()=>{StartOfRound.Instance.levels=Enumerable.Range(0,12).Select(_=>new SelectableLevel{PlanetName="Mod"}).ToArray();Call("moon",Payload("moon",id:11),true);});
        Run("legitimate free item at zero balance accepted",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.Money=0;PatchHelper.terminal.buyableItemsList[0].creditsWorth=0;Call("items",Payload("items",credits:0),true);});
        Run("100 percent discount is legitimate free purchase",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.itemSalesPercentages[0]=0;Call("items",Payload("items",credits:1000),true);});
        Run("single discounted item rounded down to zero is legal",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.buyableItemsList[0].creditsWorth=1;PatchHelper.terminal.itemSalesPercentages[0]=50;Call("items",Payload("items",credits:1000),true);});
        Run("two discounted units round only after quantity multiplication",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.buyableItemsList[0].creditsWorth=1;PatchHelper.terminal.itemSalesPercentages[0]=50;Call("items",Payload("items",credits:1000,items:new[]{0,0}),false,"FreeBuy_Item");});
        Run("two discounted units paid one credit are legal",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.buyableItemsList[0].creditsWorth=1;PatchHelper.terminal.itemSalesPercentages[0]=50;Call("items",Payload("items",credits:999,items:new[]{0,0}),true);});
        Run("paid item with unchanged balance remains blocked",()=>{PluginConfig.FreeBuy.Value=true;Call("items",Payload("items",credits:1000),false,"FreeBuy_Item");});
        Run("free unlockable remains legal",()=>{PluginConfig.FreeBuy.Value=true;StartOfRound.Instance.unlockablesList.unlockables[0].shopSelectionNode.itemCost=0;Call("unlockable",Payload("unlockable",credits:1000),true);});
        Run("valid warranty exchange remains legal",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.hasWarrantyTicket=true;Call("vehicle",Payload("vehicle",credits:1000,warranty:true),true);});
        Run("negative dropship count is rejected",()=>Call("items",Payload("items",itemCount:-2),false,"RpcInvalidCount"));
        Run("native unchanged-count sentinel is accepted",()=>Call("items",Payload("items",itemCount:-1),true));
        Run("zero dropship count is accepted",()=>Call("items",Payload("items",itemCount:0),true));
        Run("count has no invented upper limit",()=>Call("items",Payload("items",itemCount:100),true));
        Run("optional count validation can be disabled",()=>{RpcGuard.Enabled=false;Call("items",Payload("items",itemCount:-2),true);});
        Run("free vehicle purchase node is accepted",()=>{PluginConfig.FreeBuy.Value=true;VehiclePurchaseNode().itemCost=0;Call("vehicle",Payload("vehicle",credits:1000),true);});
        Run("vehicle zero-percent sale is accepted",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.itemSalesPercentages[2]=0;Call("vehicle",Payload("vehicle",credits:1000),true);});
        Run("vehicle discount rounding to zero follows native purchase calculation",()=>{PluginConfig.FreeBuy.Value=true;VehiclePurchaseNode().itemCost=1;PatchHelper.terminal.itemSalesPercentages[2]=50;Call("vehicle",Payload("vehicle",credits:1000),true);});
        Run("unknown vehicle purchase price is not accused",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.terminalNodes.allKeywords=Array.Empty<TerminalKeyword>();Call("vehicle",Payload("vehicle",credits:1000),true);});
        Run("display vehicle price cannot override actual purchase node",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.buyableVehicles[0].creditsWorth=0;Call("vehicle",Payload("vehicle",credits:1000),false,"FreeBuy_Item");});
        Run("terminal graph cycles do not hang price lookup",()=>{PluginConfig.FreeBuy.Value=true;var n=VehiclePurchaseNode();n.itemCost=0;n.terminalOptions=new[]{new CompatibleNoun{result=n}};Call("vehicle",Payload("vehicle",credits:1000),true);});
        Run("free route from paid moon allowed by default",()=>{PluginConfig.FreeBuy.Value=true;StartOfRound.Instance.currentLevel=StartOfRound.Instance.levels[1];Call("moon",Payload("moon",credits:1000),true);});
        Run("paid-to-free restriction still obeys explicit setting",()=>{PluginConfig.FreeBuy.Value=true;PluginConfig.ShipSetting_ChangToFreeMoon.Value=true;StartOfRound.Instance.currentLevel=StartOfRound.Instance.levels[1];Call("moon",Payload("moon",credits:1000),false,"ChangeToFreeLevel");});
        Run("missing route keyword does not accuse or throw",()=>{PluginConfig.FreeBuy.Value=true;PatchHelper.terminal.terminalNodes.allKeywords=Array.Empty<TerminalKeyword>();Call("moon",Payload("moon"),true);});
        foreach(var kind in new[]{"items","vehicle","unlockable","moon"})
            Run(kind+" accepts native compact scalar encodings",()=>{
                // Native ZigZag encodings: ID 0 => 01; credits 900 => 42 38; count 1 => 11.
                byte[] p=kind=="items"?Payload(kind).Take(9).Concat(new byte[]{0x42,0x38,0x11}).ToArray():new byte[]{0x01,0x42,0x38};
                if(kind=="vehicle") p=p.Concat(new byte[]{0}).ToArray();
                Call(kind,p,true);
            });
        Console.WriteLine($"Tests: {count}; failures: {failures}");
        return failures==0?0:1;
    }

    static TerminalNode VehiclePurchaseNode() => PatchHelper.terminal.terminalNodes.allKeywords[1].compatibleNouns[0].result.terminalOptions[0].result;
}
