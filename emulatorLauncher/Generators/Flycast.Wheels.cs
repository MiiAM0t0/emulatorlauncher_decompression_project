﻿using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.Joysticks;
using EmulatorLauncher.Common.EmulationStation;
using System.Windows;

namespace EmulatorLauncher
{
    partial class FlycastGenerator
    {
        private void ConfigureFlycastWheels(IniFile ini, string mappingPath, string system)
        {
            if (!SystemConfig.getOptBoolean("use_wheel"))
                return;

            SimpleLogger.Instance.Info("[WHEELS] Configuring wheels.");

            Dictionary<string, int> same_wheel = new Dictionary<string, int>();
            int nsameWheel = 0;

            bool isArcade = system != "dreamcast";
            bool serviceMenu = SystemConfig.isOptSet("flycast_service_menu") && SystemConfig.getOptBoolean("flycast_service_menu");
            List<Wheel> usableWheels = new List<Wheel>();
            string wheelTech1 = "sdl";
            string wheelTech2 = "sdl";
            int wheelNb = 0;
            int wheelIndex1 = -1;
            int wheelIndex2 = -1;
            Wheel wheel1 = null;
            Wheel wheel2 = null;
            WheelMappingInfo wheelmapping1 = null;
            WheelSDLMappingInfo wheelSDLmapping1 = null;
            WheelMappingInfo wheelmapping2 = null;
            WheelSDLMappingInfo wheelSDLmapping2 = null;
            string wheelGuid1 = "nul";
            string wheelGuid2 = "nul";
            string wheeltype1 = "default";
            string wheeltype2 = "default";
            string gamecontrollerDB = Path.Combine(AppConfig.GetFullPath("tools"), "gamecontrollerdb.txt");
            SdlToDirectInput sdlWheel1 = null;
            SdlToDirectInput sdlWheel2 = null;

            if (!File.Exists(gamecontrollerDB))
            {
                SimpleLogger.Instance.Info("[WHEELS] gamecontrollerdb.txt file not found in tools folder. Controller mapping will not be available.");
                gamecontrollerDB = null;
            }

            // Retrieve wheels
            foreach (var controller in this.Controllers.Where(c => !c.IsKeyboard))
            {
                var drivingWheel = Wheel.GetWheelType(controller.DevicePath.ToUpperInvariant());

                if (drivingWheel != WheelType.Default)
                    usableWheels.Add(new Wheel()
                    {
                        Name = controller.Name,
                        VendorID = controller.VendorID.ToString(),
                        ProductID = controller.ProductID.ToString(),
                        DevicePath = controller.DevicePath.ToLowerInvariant(),
                        DinputIndex = controller.DirectInput != null ? controller.DirectInput.DeviceIndex : controller.DeviceIndex,
                        SDLIndex = controller.SdlController != null ? controller.SdlController.Index : controller.DeviceIndex,
                        XInputIndex = controller.XInput != null ? controller.XInput.DeviceIndex : controller.DeviceIndex,
                        ControllerIndex = controller.DeviceIndex,
                        Type = drivingWheel
                    });
            }

            wheelNb = usableWheels.Count;
            SimpleLogger.Instance.Info("[WHEELS] Found " + wheelNb + " usable wheels.");
            if (wheelNb < 1)
                return;

            // Setup first wheel
            usableWheels.Sort((x, y) => x.GetWheelPriority().CompareTo(y.GetWheelPriority()));

            wheel1 = usableWheels[0];
            wheelIndex1 = wheel1.DinputIndex;

            var c1 = this.Controllers.FirstOrDefault(c => c.DeviceIndex == wheel1.ControllerIndex);

            if (c1 != null)
            {
                bool isSDL = c1.SdlController != null;

                if (isSDL)
                {
                    SimpleLogger.Instance.Info("[WHEELS] Wheel 1 is a SDL device");
                    wheelIndex1 = wheel1.SDLIndex;
                }
                else
                    SimpleLogger.Instance.Info("[WHEELS] Wheel 1 is not a SDL device");
            }

            wheeltype1 = wheel1.Type.ToString();
            SimpleLogger.Instance.Info("[WHEELS] Wheel 1, wheeltype identified : " + wheeltype1);
            wheelIndex1 = wheel1.DinputIndex;
            SimpleLogger.Instance.Info("[WHEELS] Wheel 1 directinput index : " + wheelIndex1);

            try
            {
                if (!WheelMappingInfo.InstanceW.TryGetValue(wheeltype1, out wheelmapping1))
                    WheelMappingInfo.InstanceW.TryGetValue("default", out wheelmapping1);
            }
            catch { SimpleLogger.Instance.Info("[WHEELS] No DInput mapping in yml file four wheel 1."); }

            try
            {
                if (!WheelSDLMappingInfo.InstanceWSDL.TryGetValue(wheeltype1, out wheelSDLmapping1))
                    SimpleLogger.Instance.Info("[WHEELS] No SDL mapping found to configure first wheel.");
            }
            catch { SimpleLogger.Instance.Info("[WHEELS] No SDL mapping in yml file four wheel 1."); }

            if (wheelSDLmapping1 == null)
                wheelTech1 = "raw";

            if (wheelTech1 == "sdl")
                wheelGuid1 = wheelSDLmapping1.WheelGuid;
            else
                wheelGuid1 = wheelmapping1.WheelGuid;

            if (gamecontrollerDB != null)
            {
                SimpleLogger.Instance.Info("[WHEEL] Wheel 1. Configuring mapping for wheel " + wheelGuid1);

                sdlWheel1 = GameControllerDBParser.ParseByGuid(gamecontrollerDB, wheelGuid1);

                string deviceName = wheelSDLmapping1.SDLDeviceName;

                string mappingFile = Path.Combine(mappingPath, "SDL_" + deviceName + ".cfg");
                if (isArcade)
                    mappingFile = Path.Combine(mappingPath, "SDL_" + deviceName + "_arcade.cfg");

                // Do not generate twice the same mapping file
                if (same_wheel.ContainsKey(mappingFile))
                    nsameWheel = same_wheel[mappingFile];
                else
                    nsameWheel = 0;

                same_wheel[mappingFile] = nsameWheel + 1;

                if (nsameWheel > 0)
                    return;

                if (File.Exists(mappingFile))
                    File.Delete(mappingFile);

                using (var ctrlini = new IniFile(mappingFile, IniOptions.UseSpaces))
                {
                    SimpleLogger.Instance.Info("[WHEELS] Writing wheel 1 mapping to " + mappingFile);
                    
                    ctrlini.ClearSection("analog");
                    ctrlini.ClearSection("digital");
                    ctrlini.ClearSection("emulator");

                    List<string> analogBinds = new List<string>();
                    List<string> digitalBinds = new List<string>();

                    if (!isArcade)
                    {

                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Steer, -1) + ":btn_analog_left");
                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Steer, 1) + ":btn_analog_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Throttle, -1) + ":btn_trigger_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Brake, -1) + ":btn_trigger_left");

                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpup") + ":btn_dpad1_up");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpdown") + ":btn_dpad1_down");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpleft") + ":btn_dpad1_left");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpright") + ":btn_dpad1_right");

                        if (SystemConfig.isOptSet("flycast_gears") && SystemConfig["flycast_gears"] == "a_x")
                        {
                            digitalBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Geardown) + ":btn_x");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Gearup) + ":btn_a");
                        }

                        else if (SystemConfig.isOptSet("flycast_gears") && SystemConfig["flycast_gears"] == "manual")
                        {
                            digitalBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Gear4) + ":btn_b");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Gear2) + ":btn_a");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Gear3) + ":btn_y");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Gear1) + ":btn_x");
                        }

                        else
                        {
                            digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "b") + ":btn_b");
                            digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "a") + ":btn_a");
                            digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "y") + ":btn_y");
                            digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "x") + ":btn_x");
                        }

                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "back") + ":btn_menu");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "start") + ":btn_start");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "rightstick") + ":btn_d");
                    }
                    else
                    {
                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Steer, -1) + ":btn_analog_left");
                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Steer, 1) + ":btn_analog_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Throttle, -1) + ":btn_trigger_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel1, wheel1, wheelmapping1, wheelmapping1.Brake, -1) + ":btn_trigger_left");

                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpup") + ":btn_dpad1_up");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpdown") + ":btn_dpad1_down");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpleft") + ":btn_dpad1_left");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "dpright") + ":btn_dpad1_right");

                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "b") + ":btn_b");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "a") + ":btn_a");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "y") + ":btn_y");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "x") + ":btn_x");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "back") + ":btn_d");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "start") + ":btn_start");
                        
                        if (serviceMenu)
                        {
                            digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "rightstick") + ":btn_dpad2_down");               // service menu
                            digitalBinds.Add(GetDinputKeyName(sdlWheel1, wheel1, wheelmapping1, "leftstick") + ":btn_dpad2_up");                 // test
                        }
                    }

                    for (int i = 0; i < analogBinds.Count; i++)
                        ctrlini.WriteValue("analog", "bind" + i, analogBinds[i]);

                    for (int i = 0; i < digitalBinds.Count; i++)
                        ctrlini.WriteValue("digital", "bind" + i, digitalBinds[i]);

                    ctrlini.WriteValue("emulator", "dead_zone", "1");
                    ctrlini.WriteValue("emulator", "mapping_name", deviceName);
                    ctrlini.WriteValue("emulator", "rumble_power", "100");
                    ctrlini.WriteValue("emulator", "version", "3");

                    ctrlini.Save();
                }

                ini.WriteValue("input", "maple_sdl_joystick_" + wheelIndex1, "0");
                ini.WriteValue("input", "device1", "0");
                ini.WriteValue("input", "device1.1", "1");

                if (SystemConfig.isOptSet("flycast_extension1") && !string.IsNullOrEmpty(SystemConfig["flycast_extension1"]))
                    ini.WriteValue("input", "device1.2", SystemConfig["flycast_extension1"]);
                else
                    ini.WriteValue("input", "device1.2", "10");
            }

            // Setup second wheel
            if (wheelNb > 1 && gamecontrollerDB != null)
            {
                wheel2 = usableWheels[1];
                wheeltype2 = wheel2.Type.ToString();
                SimpleLogger.Instance.Info("[WHEELS] Wheel 2, wheeltype identified : " + wheeltype2);
                wheelIndex2 = wheel2.SDLIndex;
                SimpleLogger.Instance.Info("[WHEELS] Wheel 2 directinput index : " + wheelIndex2);

                try
                {
                    if (!WheelMappingInfo.InstanceW.TryGetValue(wheeltype2, out wheelmapping2))
                        WheelMappingInfo.InstanceW.TryGetValue("default", out wheelmapping2);
                }
                catch { SimpleLogger.Instance.Info("[WHEELS] No DInput mapping in yml file four wheel 2."); }

                try
                {
                    if (!WheelSDLMappingInfo.InstanceWSDL.TryGetValue(wheeltype2, out wheelSDLmapping2))
                        SimpleLogger.Instance.Info("[WHEELS] No SDL mapping found to configure second wheel.");
                }
                catch { SimpleLogger.Instance.Info("[WHEELS] No SDL mapping in yml file four wheel 2."); }

                if (wheelSDLmapping2 == null)
                    wheelTech2 = "raw";

                if (wheelTech2 == "sdl")
                    wheelGuid2 = wheelSDLmapping2.WheelGuid;
                else
                    wheelGuid2 = wheelmapping2.WheelGuid;

                SimpleLogger.Instance.Info("[WHEEL] Wheel 2. Configuring mapping for wheel " + wheelGuid2);

                sdlWheel2 = GameControllerDBParser.ParseByGuid(gamecontrollerDB, wheelGuid2);

                string deviceName2 = wheelSDLmapping2.SDLDeviceName;

                string mappingFile2 = Path.Combine(mappingPath, "SDL_" + deviceName2 + ".cfg");
                if (isArcade)
                    mappingFile2 = Path.Combine(mappingPath, "SDL_" + deviceName2 + "_arcade.cfg");

                // Do not generate twice the same mapping file
                if (same_wheel.ContainsKey(mappingFile2))
                    nsameWheel = same_wheel[mappingFile2];
                else
                    nsameWheel = 0;

                same_wheel[mappingFile2] = nsameWheel + 1;

                if (nsameWheel > 0)
                    return;

                if (File.Exists(mappingFile2))
                    File.Delete(mappingFile2);

                using (var ctrlini = new IniFile(mappingFile2, IniOptions.UseSpaces))
                {
                    SimpleLogger.Instance.Info("[WHEELS] Writing wheel 2 mapping to " + mappingFile2);

                    ctrlini.ClearSection("analog");
                    ctrlini.ClearSection("digital");
                    ctrlini.ClearSection("emulator");

                    List<string> analogBinds = new List<string>();
                    List<string> digitalBinds = new List<string>();

                    if (!isArcade)
                    {

                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Steer, -1) + ":btn_analog_left");
                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Steer, 1) + ":btn_analog_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Throttle, -1) + ":btn_trigger_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Brake, -1) + ":btn_trigger_left");

                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpup") + ":btn_dpad1_up");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpdown") + ":btn_dpad1_down");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpleft") + ":btn_dpad1_left");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpright") + ":btn_dpad1_right");

                        if (SystemConfig.isOptSet("flycast_gears") && SystemConfig["flycast_gears"] == "a_x")
                        {
                            digitalBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Geardown) + ":btn_x");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Gearup) + ":btn_a");
                        }

                        else if (SystemConfig.isOptSet("flycast_gears") && SystemConfig["flycast_gears"] == "manual")
                        {
                            digitalBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Gear4) + ":btn_b");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Gear2) + ":btn_a");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Gear3) + ":btn_y");
                            digitalBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Gear1) + ":btn_x");
                        }

                        else
                        {
                            digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "b") + ":btn_b");
                            digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "a") + ":btn_a");
                            digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "y") + ":btn_y");
                            digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "x") + ":btn_x");
                        }

                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "back") + ":btn_menu");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "start") + ":btn_start");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "rightstick") + ":btn_d");
                    }
                    else
                    {
                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Steer, -1) + ":btn_analog_left");
                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Steer, 1) + ":btn_analog_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Throttle, -1) + ":btn_trigger_right");
                        analogBinds.Add(GetWheelKeyName(sdlWheel2, wheel2, wheelmapping2, wheelmapping2.Brake, -1) + ":btn_trigger_left");

                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpup") + ":btn_dpad1_up");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpdown") + ":btn_dpad1_down");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpleft") + ":btn_dpad1_left");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "dpright") + ":btn_dpad1_right");

                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "b") + ":btn_b");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "a") + ":btn_a");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "y") + ":btn_y");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "x") + ":btn_x");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "back") + ":btn_d");
                        digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "start") + ":btn_start");

                        if (serviceMenu)
                        {
                            digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "rightstick") + ":btn_dpad2_down");               // service menu
                            digitalBinds.Add(GetDinputKeyName(sdlWheel2, wheel2, wheelmapping2, "leftstick") + ":btn_dpad2_up");                 // test
                        }
                    }

                    for (int i = 0; i < analogBinds.Count; i++)
                        ctrlini.WriteValue("analog", "bind" + i, analogBinds[i]);

                    for (int i = 0; i < digitalBinds.Count; i++)
                        ctrlini.WriteValue("digital", "bind" + i, digitalBinds[i]);

                    ctrlini.WriteValue("emulator", "dead_zone", "1");
                    ctrlini.WriteValue("emulator", "mapping_name", deviceName2);
                    ctrlini.WriteValue("emulator", "rumble_power", "100");
                    ctrlini.WriteValue("emulator", "version", "3");

                    ctrlini.Save();
                }

                ini.WriteValue("input", "maple_sdl_joystick_" + wheelIndex2, "1");
                ini.WriteValue("input", "device2", "0");
                ini.WriteValue("input", "device2.1", "1");

                if (SystemConfig.isOptSet("flycast_extension2") && !string.IsNullOrEmpty(SystemConfig["flycast_extension2"]))
                    ini.WriteValue("input", "device2.2", SystemConfig["flycast_extension2"]);
                else
                    ini.WriteValue("input", "device2.2", "10");
            }
            
        }

        private static string GetWheelKeyName(SdlToDirectInput ctrl, Wheel wheel, WheelMappingInfo wheelMapping, string buttonKey, int invertedAxis = 0)
        {
            if (wheel == null || buttonKey == null)
                return "99";

            if (buttonKey.StartsWith("button_"))
            {
                SimpleLogger.Instance.Info("[WHEELS] Configuring " + buttonKey);

                int buttonID = (buttonKey.Substring(7).ToInteger());

                SimpleLogger.Instance.Info("[WHEELS] Mapping " + buttonKey + " to button " + buttonID);

                return buttonID.ToString();
            }

            else
                return GetDinputKeyName(ctrl, wheel, wheelMapping, buttonKey, invertedAxis);
        }

        private static string GetDinputKeyName(SdlToDirectInput ctrl, Wheel wheel, WheelMappingInfo wheelMapping, string button, int plus = 0)
        {
            SimpleLogger.Instance.Info("[WHEELS] Configuring " + button);

            if (ctrl == null ||ctrl.ButtonMappings == null)
                return "99";

            if (!ctrl.ButtonMappings.ContainsKey(button))
                return "99";

            if (button.StartsWith("b"))
            {
                int buttonID = (button.Substring(1).ToInteger());

                SimpleLogger.Instance.Info("[WHEELS] Mapping " + button + " to button " + buttonID);

                return buttonID.ToString();
            }

            else if (button.StartsWith("h"))
            {
                int hatID = button.Substring(3).ToInteger();

                SimpleLogger.Instance.Info("[WHEELS] Mapping " + button + " to hat " + hatID);

                switch (hatID)
                {
                    case 1: return "256";
                    case 2: return "259";
                    case 4: return "257";
                    case 8: return "258";
                };
            }

            else if (button.StartsWith("a") || button.StartsWith("-a") || button.StartsWith("+a"))
            {
                int axisID = button.Substring(1).ToInteger();

                if (button.StartsWith("-a") || button.StartsWith("+a"))
                    axisID = button.Substring(2).ToInteger();

                SimpleLogger.Instance.Info("[WHEELS] Mapping " + button + " to axis " + plus + axisID);

                return plus == 1 ? "+" + axisID : "-" + axisID ;
            }

            return "99";
        }
    }
}