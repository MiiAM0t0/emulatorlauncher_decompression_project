﻿using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Data.SQLite;
using System.IO.Compression;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.Launchers;

namespace EmulatorLauncher
{
    partial class ExeLauncherGenerator : Generator
    {
        class AmazonGameLauncher : GameLauncher
        {
            public AmazonGameLauncher(Uri uri)
            {
                LauncherExe = AmazonLibrary.GetAmazonGameExecutableName(uri);
            }
           
            public override int RunAndWait(ProcessStartInfo path)
            {
                bool uiExists = Process.GetProcessesByName("Amazon Games UI").Any();

                KillExistingLauncherExes();

                Process.Start(path);

                var amazonGame = GetLauncherExeProcess();
                if (amazonGame != null)
                {
                    amazonGame.WaitForExit();

                    if (!uiExists || (Program.SystemConfig.isOptSet("killsteam") && Program.SystemConfig.getOptBoolean("killsteam")))
                    {
                        foreach (var ui in Process.GetProcessesByName("Amazon Games UI"))
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
