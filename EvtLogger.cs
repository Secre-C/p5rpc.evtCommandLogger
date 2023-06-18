using System;
using Reloaded.Hooks;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json.Serialization;
using Reloaded.Memory;
using static Reloaded.Hooks.Definitions.X86.FunctionAttribute;
using Reloaded.Hooks.Definitions.X86;

namespace p5rpc.evtCommandLogger
{
    public unsafe class EvtLogger
    {
        private delegate long GetEvtObjects(long a1, int a2, char a3, char a4, char a5);
        [Function(new[] { Register.ebx }, Register.ebx, StackCleanup.Caller)]
        private delegate void GetEvtObjectPtr(long a1);
        private delegate byte EvtFlagCheck(long a1);

        private IHook<GetEvtObjects> _getEvtObjects;
        private IHook<GetEvtObjectPtr> _getEvtObjectPtr;
        private IHook<EvtFlagCheck> _evtFlagCheck;
        public EvtLogger(IReloadedHooks hooks, IModLoader modLoader, Utils utils)
        {
            utils.IScanner.AddMainModuleScan("48 89 5C 24 ?? 44 88 4C 24 ?? 44 88 44 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 40", (result) =>
            {
                if (result.Found)
                {
                    long getEvtObjectsAdr = result.Offset + utils.baseAddress;
                    utils.DebugLog($"Found GetEvtObjects Function -> {getEvtObjectsAdr:X8}");

                    _getEvtObjects = hooks.CreateHook<GetEvtObjects>((a1, a2, a3, a4, a5) =>
                    {
                        long* plVar1;
                        long lVar9;
                        int iVar14;
                        ushort uVar17;
                        long lVar19;

                        plVar1 = *(long**)(a1 + 0x11c0);
                        uVar17 = 0xffff;
                        iVar14 = *(int*)(plVar1 + 2);
                        if (iVar14 < 1)
                        {
                            utils.DebugLog("return 0");
                            return 0;
                        }
                        if (a2 < 0)
                        {
                            utils.DebugLog("return 1");
                            return 0;
                        }
                        if (iVar14 <= a2)
                        {
                            utils.DebugLog("return 2");
                            return 0;
                        }
                        if (a2 < iVar14 >> 1)
                        {
                            lVar9 = *plVar1;
                            while (true)
                            {
                                if (lVar9 == 0)
                                {
                                    utils.DebugLog("return 3");
                                    return 0;
                                }
                                if (*(int*)(lVar9 + 4) == a2) break;
                                lVar9 = *(long*)(lVar9 + 0x30);
                            }
                        }
                        else
                        {
                            lVar9 = plVar1[1];
                            while (true)
                            {
                                if (lVar9 == 0)
                                {
                                    utils.DebugLog("return 4");
                                    return 0;
                                }
                                if (*(int*)(lVar9 + 4) == a2) break;
                                lVar9 = *(long*)(lVar9 + 0x28);
                            }
                        }
                        lVar19 = *(long*)(lVar9 + 0x10);

                        EvtObjectStruct* evtStruct = (EvtObjectStruct*)lVar19;
                        if (lVar19 == 0x0)
                        {
                            utils.DebugLog("return 5");
                            return 0;
                        }
                        if ((*(long*)(lVar19 + 0x38) & 0x40000000) != 0)
                        {
                            utils.DebugLog("return 6");
                            return 0;
                        }

                        string[] animContext = { "AnimMajorId", "AnimMinorId", "AnimSubId" };
                        switch (evtStruct->Type)
                        {
                            case EvtObjectType.Field:
                                animContext[0] = "Load GFS";
                                animContext[1] = "CLT MajorId";
                                animContext[2] = "CLT MinorId";
                                break;

                            case EvtObjectType.Character:
                                animContext[0] = "be GAP MinorId";
                                animContext[1] = "ae GAP MinorId";
                                break;

                            case EvtObjectType.FieldCharacter:
                                animContext[0] = "bf GAP MinorId";
                                animContext[1] = "af GAP MinorId";
                                break;

                            case EvtObjectType.Enemy:
                                animContext[0] = "GAP MinorId";
                                break;

                            default:
                                break;
                        }
                        utils.Log($"Object: {evtStruct->Type} || ID: {evtStruct->Id} || ResMajorId: {evtStruct->ResourceMajorId} || ResMinorId: {evtStruct->ResourceMinorId} || ResSubId: {evtStruct->ResourceSubId}\n            {animContext[0]}: {utils.GfsLoadBitfield(evtStruct->AnimationMajorId, evtStruct->Type)} || {animContext[1]}: {evtStruct->AnimationMinorId} || {animContext[2]}: {evtStruct->AnimationSubId}\n", Color.PaleGreen);
                        
                        long result = _getEvtObjects.OriginalFunction(a1, a2, a3, a4, a5);

                        return result;
                    }, getEvtObjectsAdr).Activate();
                }
                else
                {
                    utils.DebugLog("Could not find GetEvtObjects Function", Color.PaleVioletRed);
                }
            });

            utils.IScanner.AddMainModuleScan("40 57 48 83 EC 20 4C 8B 41 ?? 48 8B F9 41 F6 40 ?? 01", (result) =>
            {
                string[] conditionals = { "==", "!=", "<", ">", "<=", ">=" };
                if (result.Found)
                {
                    long evtFlagCheckAdr = result.Offset + utils.baseAddress;
                    utils.DebugLog($"Found EvtFlagCheck Function -> {evtFlagCheckAdr:X8}");

                    _evtFlagCheck = hooks.CreateHook<EvtFlagCheck>((a1) => //hook evt flag check to print general evt command struct
                    {
                        EvtCmdGeneral * command = *(EvtCmdGeneral**)(a1 + 0x20);

                        byte result = _evtFlagCheck.OriginalFunction(a1);

                        if ((command->SkipCommand & 1) != 0)
                            utils.Log($"Command: {Encoding.ASCII.GetString(BitConverter.GetBytes(command->CommandType))} || Frame: {command->Frame:D4} || Duration: {command->Duration:D3} || ObjectId: {command->ObjectId:D2} || ForceSkipCommand -> {(command->SkipCommand & 1) != 0}", Color.LightBlue);
                        else
                            utils.Log($"Command: {Encoding.ASCII.GetString(BitConverter.GetBytes(command->CommandType))} || Frame: {command->Frame:D4} || Duration: {command->Duration:D3} || ObjectId: {command->ObjectId:D2} || ( {command->EvtFlagType} {utils.FlagConvert(command->EvtFlagType, command->EvtFlagId)} {conditionals[(int)command->EvtFlagConditionalType]} {command->EvtFlagValue} ) -> {result != 0}", Color.LightBlue);

                        return result;
                    }, evtFlagCheckAdr).Activate();
                }
                else
                {
                    utils.DebugLog("Could Not Find EvtFlagCheck Function", Color.PaleVioletRed);
                }
            });
        }

        public struct EvtCmdGeneral
        {
            public int unk0;
            public int CommandType;
            public short Field04;
            public short Field06;
            public int ObjectId;
            public int SkipCommand;
            public int Frame;
            public int Duration;
            public int DataOffset;
            public int DataSize;
            public evtFlagType EvtFlagType;
            public int EvtFlagId;
            public uint EvtFlagValue;
            public evtFlagConditionalType EvtFlagConditionalType;
        };

        public struct EvtObjectStruct
        {
            public int Id;
            public EvtObjectType Type;
            public int Field08;
            public int Field0C;
            public int ResourceMajorId;
            public short ResourceSubId;
            public short ResourceMinorId;
            public int Field1C;
            public int AnimationMajorId;
            public int AnimationMinorId;
            public int AnimationSubId;
            public int Field28;
            public int Field2C;
        }

        public enum evtFlagType
        {
            None = 0,
            Adachi_False = 1,
            Local_Data = 2,
            Bitflag = 3,
            Count = 4
        }

        public enum evtFlagConditionalType
        {
            Equals = 0,
            DoesNotEqual = 1,
            LessThan = 2,
            MoreThan = 3,
            LessThanEqualTo = 4,
            MoreThanEqualTo = 5,
        }
        public enum EvtObjectType
        {
            Field = 0x00000003,
            ParticlePak = 0x00000007,
            Camera = 0x00000009,
            SymShadow = 0x00000401,
            Item = 0x00000601,
            ResrcTableNpc = 0x00020101,
            Particle = 0x01000002,
            Character = 0x01000101,
            FieldCharacter = 0x02000101,
            Persona = 0x04000201,
            Enemy = 0x00000301,
            FieldObject = 0x02000701,
            Image = 0x00000005,
            Env = 0x00000004,
            Movie = 0x00000008
        }
    }
}
