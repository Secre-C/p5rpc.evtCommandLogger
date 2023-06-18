using p5rpc.evtCommandLogger.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static p5rpc.evtCommandLogger.EvtLogger;

namespace p5rpc.evtCommandLogger
{
    public unsafe class Utils
    {
        public ILogger _logger;
        public long baseAddress { get; set; }
        public IStartupScanner IScanner { get; set; }
        public Config _config;
        public Utils(IReloadedHooks hooks, ILogger logger, IModLoader modLoader, Config _configurable)
        {
            _config = _configurable;
            _logger = logger;
            using var thisProcess = Process.GetCurrentProcess();
            baseAddress = thisProcess.MainModule.BaseAddress.ToInt64();

            modLoader.GetController<IStartupScanner>().TryGetTarget(out var startupScanner);
            IScanner = startupScanner;
        }

        public string FlagConvert(evtFlagType flagType, int flag)
        {
            if (flagType == evtFlagType.Bitflag)
            {
                if (flag >= 0x5000000)
                { flag = (flag - 0x5000000) + 12288; }

                else if (flag >= 0x4000000)
                { flag = (flag - 0x4000000) + 11776; }

                else if (flag >= 0x3000000)
                { flag = (flag - 0x3000000) + 11264; }

                else if (flag >= 0x2000000)
                { flag = (flag - 0x2000000) + 6144; }

                else if (flag >= 0x1000000)
                { flag = (flag - 0x1000000) + 3072; }
            }
            return $"{flag:D}";
        }

        public string GfsLoadBitfield(int bitField, EvtObjectType ObjType)
        {
            if (ObjType != EvtObjectType.Field)
                return $"{bitField}";

            if (bitField == -1)
                return "All";

            string fieldsLoaded = "0,";

            for (int i = 0; i < 7; i++)
            {
                if (((bitField >> i) & 1) == 1)
                    fieldsLoaded += $" {i + 1},";
            }

            return fieldsLoaded;
        }

        public void Log(string log)
        {
            _logger.WriteLineAsync($"[EvtLogger] {log}");
        }
        public void Log(string log, Color color)
        {
            _logger.WriteLineAsync($"[EvtLogger] {log}", color);
        }
        public void DebugLog(string log)
        {
            if (_config.Debug)
                _logger.WriteLine($"[EvtLoggerDebug] {log}", Color.DimGray);
        }
        public void DebugLog(string log, Color color)
        {
            if (_config.Debug)
                _logger.WriteLine($"[EvtLoggerDebug] {log}");
        }
    }
}
