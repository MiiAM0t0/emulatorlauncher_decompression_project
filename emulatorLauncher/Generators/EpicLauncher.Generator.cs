﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.Serialization;
using System.Threading;
using System.Security.Policy;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common.Launchers;

namespace EmulatorLauncher
{
    partial class ExeLauncherGenerator : Generator
    {
        class EpicGameLauncher : GameLauncher
        {
            public EpicGameLauncher(Uri uri)
            {
                LauncherExe = EpicLibrary.GetEpicGameExecutableName(uri);
            }

            public override int RunAndWait(ProcessStartInfo path)
            {
                bool epicLauncherExists = Process.GetProcessesByName("EpicGamesLauncher").Any();

                KillExistingLauncherExes();

                Process.Start(path);

                var epicGame = GetLauncherExeProcess();
                if (epicGame != null)
                {
                    epicGame.WaitForExit();

                    if (!epicLauncherExists || (Program.SystemConfig.isOptSet("killsteam") && Program.SystemConfig.getOptBoolean("killsteam")))
                    {
                        foreach (var ui in Process.GetProcessesByName("EpicGamesLauncher"))
                        {
                            try { ui.Kill(); }
                            catch { }
                        }
                    }
                }

                return 0;
            }
        }
    }
}
