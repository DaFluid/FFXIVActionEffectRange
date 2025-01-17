﻿global using static ActionEffectRange.Game;
global using static ActionEffectRange.Plugin;
global using static Dalamud.Logging.PluginLog;
global using ActionEffectRange.Utils;
global using System;
global using System.Numerics;

using ActionEffectRange.Actions;
using ActionEffectRange.Actions.Data;
using ActionEffectRange.Drawing;
using ActionEffectRange.Helpers;
using ActionEffectRange.UI;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ActionEffectRange
{
    public class Plugin : IDalamudPlugin
    {
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static IFramework Framework { get; private set; } = null!;
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static IClientState ClientState { get; private set; } = null!;
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static IObjectTable ObejctTable { get; private set; } = null!;
        [PluginService]
        //[RequiredVersion("1.0")]
        internal static IBuddyList BuddyList { get; private set; } = null!;

        [PluginService] 
        //[RequiredVersion("1.0")]
        internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        
        private static ActionWatcher? _actionWatcher;

        private const string commandToggleConfig = "/actioneffectrange";

        private static bool _enabled;
        internal static bool Enabled
        {
            get => _enabled;
            set
            {
                if (value != _enabled)
                {
                    if (value) 
                    {
                        EffectRangeDrawing.Reset();
                        _actionWatcher?.Enable();
                    }
                    else
                    {
                        EffectRangeDrawing.Reset();
                        _actionWatcher?.Disable();
                    }
                    _enabled = value;
                }
            }
        }
        internal static bool DrawWhenCasting;
        
        internal static Configuration Config = null!;
        internal static bool InConfig = false;

        public Plugin()
        {
            Config = PluginInterface.GetPluginConfig() as Configuration 
                ?? new Configuration();

            _actionWatcher = new ActionWatcher(GameInteropProvider);

            InitializeCommands();

            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
            PluginInterface.UiBuilder.Draw += ConfigUi.Draw;

            PluginInterface.UiBuilder.Draw += EffectRangeDrawing.OnTick;

            ClientState.Logout += OnLogOut;
            ClientState.TerritoryChanged += CheckTerritory;

            RefreshConfig(true);
        }

        private static void InitializeCommands()
        {
            CommandManager.AddHandler(commandToggleConfig, 
                new CommandInfo((_, _) => InConfig = !InConfig)
            {
                HelpMessage = "Toggle the Configuration Window of ActionEffectRange",
                ShowInHelp = true
            });
        }

        private static void OnOpenConfigUi()
        {
            InConfig = true;
        }

        private static void CheckTerritory(ushort terr)
        {
            if (IsPvPZone)
            {
                Enabled = Config.Enabled && Config.EnabledPvP;
                DrawWhenCasting = false;
            }
            else
            {
                Enabled = Config.Enabled;
                DrawWhenCasting = Config.DrawWhenCasting;
            }
        }

        private static void OnLogOut()
        {
            EffectRangeDrawing.Reset();
        }

        internal static void RefreshConfig(bool reloadSavedList = false)
        {
            EffectRangeDrawing.RefreshConfig();
            CheckTerritory(ClientState.TerritoryType);
            //Enabled = Config.Enabled;

            if (reloadSavedList)
                ActionData.ReloadCustomisedData();
        }

        public static void LogUserDebug(string msg)
        {
            if (Config.LogDebug) LogDebug(msg);
        }


        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            PluginInterface.SavePluginConfig(Config);

            CommandManager.RemoveHandler(commandToggleConfig);
            
            PluginInterface.UiBuilder.Draw -= EffectRangeDrawing.OnTick;

            ClientState.Logout -= OnLogOut;
            ClientState.TerritoryChanged -= CheckTerritory;

            PluginInterface.UiBuilder.Draw -= ConfigUi.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;

            _actionWatcher?.Dispose();
            ClassJobWatcher.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
