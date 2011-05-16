using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crypton.Hardware.CrystalFontz;
using System.Diagnostics;
using System.Threading;

namespace InfoService.Modules {
    class HDD : Module {

        PerformanceCounter pfc = null;
        Stopwatch time = null;

        public HDD(CrystalFontz635 cf)
            : base(cf) {
            pfc = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            LcdModule.SendData(0, 0, "HDD TOTAL LOAD");
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

        void flash(int delay) {
            LcdModule.SetLED(0, 100, 0);
            Thread.Sleep(delay);
            LcdModule.SetLED(0, 0, 0);
        }

        public override void Draw(Stopwatch elapsed) {
            if (elapsed.ElapsedMilliseconds >= 100) {

                float loadPercent = pfc.NextValue();
                float rawPercent = loadPercent * 100f;

                if (rawPercent > 0) {
                    flash(20);
                }

                int barCount = (int)(loadPercent / 5.0f);
                LcdModule.SendData(1, 0, Math.Round(loadPercent).ToString().PadRight(3));
                LcdModule.SendData(1, 3, "%");
                string bars = string.Empty;
                bars = bars.PadRight(barCount, Convert.ToChar(0));
                bars = bars.PadRight(20);
                LcdModule.SendString(3, 0, bars);
                elapsed.Reset();
                elapsed.Start();
            }
        }
    }
}
