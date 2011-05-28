using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crypton.Hardware.CrystalFontz;
using System.Management;

namespace InfoService.Modules {
    class Battery : Module {

        ManagementObject moBattery = null;

        enum BatteryChemistry : ushort {
            Other = 1,
            Unknown,
            LeadAcid,
            NiCad,
            NiMH,
            LiIon,
            ZincAir,
            LiPoly
        }
        enum BatteryStatus : ushort {
            Discharge = 1,
            AC,
            Full,
            Low,
            Critical,
            Charge,
            ChargeHigh,
            ChargeLow,
            ChargeCritical,
            Undefined,
            Partial
        }

        public Battery(CrystalFontz635 cf635) : base(cf635) {
            cf635.SetLED(0, 0, 0);
            cf635.SetLED(1, 0, 0);
            cf635.SetLED(2, 0, 0);
            cf635.SetLED(3, 0, 0);
        }
        public override bool Draw(TimeSpan elapsed) {
            if (elapsed.TotalMilliseconds >= 200) {
                ManagementObjectSearcher mosBattery = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                ManagementObjectCollection mocBatteries = mosBattery.Get();
                if (mocBatteries.Count > 0) {
                    ManagementObjectCollection.ManagementObjectEnumerator iebatteries = mocBatteries.GetEnumerator();
                    iebatteries.MoveNext();
                    moBattery = (ManagementObject)iebatteries.Current;
                    iebatteries.Dispose();
                }
                if (moBattery != null) {
                    string caption = moBattery["Caption"] as string;
                    string name = moBattery["Name"] as string;
                    ushort chemistry = (ushort)moBattery["Chemistry"];
                    ushort status = (ushort)moBattery["BatteryStatus"];
                    ushort chargeRemaining = (ushort)moBattery["EstimatedChargeRemaining"];
                    uint runtime = (uint)moBattery["EstimatedRunTime"];

                    LcdModule.SendString(0, 0, name.ToUpper().PadRight(20, ' '));
                    LcdModule.SendString(1, 0, ((BatteryChemistry)chemistry).ToString().ToUpper().PadRight(10, ' '));
                    LcdModule.SendString(1, 10, ((BatteryStatus)status).ToString().ToUpper().PadRight(10, ' '));
                    LcdModule.SendString(2, 0, (chargeRemaining.ToString() + "% REMAINING").PadRight(20, ' '));
                    LcdModule.SendString(3, 0, (runtime.ToString() + " MIN REMAIN").PadRight(20, ' '));

                    switch ((BatteryStatus)status) {
                        case BatteryStatus.AC:
                            LcdModule.SetLED(1, 100, 0);
                            break;
                        case BatteryStatus.Discharge:
                            LcdModule.SetLED(1, 0, 100);
                            break;
                        default:
                            LcdModule.SetLED(1, 0, 0);
                            break;
                    }

                    LcdModule.SetLED(2,chargeRemaining, 100 - chargeRemaining);
                    if (runtime < 10) {
                        LcdModule.SetLED(3, 0, 100);
                    }
                    else if (runtime >= 10 && runtime < 30) {
                        LcdModule.SetLED(3, 50, 50);
                    }
                    else {
                        LcdModule.SetLED(3, 100, 0);
                    }
                    

                    moBattery.Dispose();
                }
                else {
                    LcdModule.SendString(0, 0, "No Battery");
                    LcdModule.SendString(1, 0, "Installed");
                }
                mosBattery.Dispose();
                mocBatteries.Dispose();                
            }
            else {
                return false;
            }
            return true;
        }
    }
}
