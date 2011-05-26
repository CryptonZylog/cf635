using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Crypton.Hardware.CrystalFontz;
using System.Diagnostics;
using System.Globalization;
using System.Management;

namespace InfoService.Modules {
    class RAM : Module {

        PerformanceCounter pfRAM = null;
        ulong TotalInstalled = 0;
        Stopwatch time = null;

        public RAM(CrystalFontz635 cf)
            : base(cf) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            pfRAM = new PerformanceCounter("Memory", "Available MBytes");
            using (ManagementObject mo = new ManagementObject(@"Win32_ComputerSystem.Name=""" + Environment.MachineName + "\"")) {
                TotalInstalled = (ulong)mo["TotalPhysicalMemory"];
            }
            pfRAM.NextValue();
            LcdModule.ClearScreen();
            LcdModule.SetCursorPosition(0, 0);
            LcdModule.SendData(0, 0, string.Format("RAM USAGE (T:{0}Mb)", Math.Round(TotalInstalled / 1024.0 / 1024.0)).PadRight(20));
            LcdModule.SetCGRAM(0,
                new byte[] {
                    0x3f,
                    0x3f,
                    0x3f,
                    0x3f,
                    0x3f,
                    0x3f,
                    0x3f,
                    0x3f
                });
            time = new Stopwatch();
            time.Start();
        }

        public override bool Draw(TimeSpan elapsed) {

            float availMBytes = pfRAM.NextValue();
            float usedMBytes = (TotalInstalled / 1024 / 1024) - availMBytes;
            float percentFree = (availMBytes / (TotalInstalled / 1024 / 1024)) * 100.0f;
            float percentUsed = (usedMBytes / (TotalInstalled / 1024 / 1024)) * 100.0f;
            /*RAM USAGE (T:{0}Mb)
             *U:1234Mb F:5678Mb
             *%USED %FREE
             *------->USED
             */
            if (elapsed.TotalMilliseconds >= 300) {
                int barCount = (int)(percentUsed / 5.0f);
                double halfBar = (int)(percentUsed % 5.0f);
                int halfBarlen = (int)(halfBar / 0.6);

                //renber half bar
                byte bmp_halfBar = 0x0;
                for (int l = 0, bit = 5; l < halfBarlen && bit >= 0; l++, bit--) {
                    bmp_halfBar |= (byte)(1 << bit);
                }

                LcdModule.SetCGRAM(1,
                new byte[] {
                    bmp_halfBar,
                    bmp_halfBar,
                    bmp_halfBar,
                    bmp_halfBar,
                    bmp_halfBar,
                    bmp_halfBar,
                    bmp_halfBar,
                    bmp_halfBar
                });

                LcdModule.SendString(1, 0, string.Format("U:{0}Mb", Math.Round(usedMBytes)).PadRight(10));
                LcdModule.SendString(1, 10, string.Format("F:{0}Mb", Math.Round(availMBytes)).PadRight(10));
                LcdModule.SendString(2, 0, string.Format("U:{0}%", Math.Round(percentUsed)).PadRight(10));
                LcdModule.SendString(2, 10, string.Format("F:{0}%", Math.Round(percentFree)).PadRight(10));

                string bars = string.Empty;
                bars = bars.PadRight(barCount, Convert.ToChar(0));
                bars += Convert.ToChar(1);
                bars = bars.PadRight(20);
                LcdModule.SendString(3, 0, bars);

                return true;
            }

            return false;
        }



    }
}
