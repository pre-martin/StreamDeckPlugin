﻿// Copyright (C) 2024 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Threading.Tasks;
using StreamDeckSimHub.Installer.Tools;

namespace StreamDeckSimHub.Installer.Actions
{
    /// <summary>
    /// Stops the Stream Deck software, if it is running.
    /// </summary>
    public class StopStreamDeckSoftware : AbstractInstallerAction
    {
        public override string Name => "Stopping Stream Deck software";

        protected override async Task<ActionResult> ExecuteInternal()
        {
            if (IsStreamDeckRunning())
            {
                SetAndLogInfo("Stopping Stream Deck software");
                var process = ProcessTools.GetProcess(Configuration.StreamDeckProcessName);
                process?.Kill();

                if (!await WaitForStreamDeckKilled())
                {
                    SetAndLogError("The Stream Deck software could not be stopped. Please stop it manually and try again.");
                    return ActionResult.Error;
                }
            }
            else
            {
                SetAndLogInfo("Stream Deck software is not running. Stopping not required.");
            }

            if (IsPluginRunning())
            {
                SetAndLogInfo("Stopping Stream Deck SimHub Plugin");
                var process = ProcessTools.GetProcess(Configuration.PluginProcessName);
                process?.Kill();

                if (!await WaitForPluginKilled())
                {
                    SetAndLogError("The Stream Deck SimHub Plugin could not be stopped. Please kill it manually and try again.");
                    return ActionResult.Error;
                }
            }
            else
            {
                SetAndLogInfo("Plugin is not running.");
            }

            SetAndLogInfo("The Stream Deck software stopped.");
            return ActionResult.Success;
        }

        private bool IsStreamDeckRunning()
        {
            return ProcessTools.IsProcessRunning(Configuration.StreamDeckProcessName);
        }

        private bool IsPluginRunning()
        {
            return ProcessTools.IsProcessRunning(Configuration.PluginProcessName);
        }

        private async Task<bool> WaitForStreamDeckKilled()
        {
            for (var i = 0; i < 10; i++)
            {
                if (!IsStreamDeckRunning()) return true;
                await Task.Delay(1000);
            }

            return false;
        }

        private async Task<bool> WaitForPluginKilled()
        {
            for (var i = 0; i < 10; i++)
            {
                if (!IsPluginRunning()) return true;
                await Task.Delay(1000);
            }

            return false;
        }
    }
}