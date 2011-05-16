using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Crypton.Hardware.CrystalFontz;

namespace InfoService.Modules {
    class Sys : Module {
        PerformanceCounter pfcUptime = null;
        PerformanceCounter pfcProcesses = null;
        PerformanceCounter pfcThreads = null;
        PerformanceCounter pfCPU = null;
        Stopwatch time = null;

        public Sys(CrystalFontz635 cf)
            : base(cf) {
            LcdModule.SendData(0, 0, "SYSTEM LOAD INFO");
            pfcUptime = new PerformanceCounter("System", "System Up Time");
            pfcProcesses = new PerformanceCounter("System", "Processes");
            pfcThreads = new PerformanceCounter("System", "Threads");
            pfCPU = new PerformanceCounter("Processor", "% Processor Time", "_Total");
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

        TimeSpan old = TimeSpan.MinValue;

        public override void Draw(Stopwatch elapsed) {
            if (elapsed.ElapsedMilliseconds >= 100) {
                TimeSpan tsUptime = TimeSpan.FromSeconds(pfcUptime.NextValue());
                int processCount = (int)pfcProcesses.NextValue();
                int threadCount = (int)pfcThreads.NextValue();
                float percent = pfCPU.NextValue();

                LcdModule.SendString(1, 0, string.Format("UP: {0}d {1}h {2}m {3}s", tsUptime.Days, tsUptime.Hours, tsUptime.Minutes, tsUptime.Seconds).PadRight(20));
                // LD:100% 
                // P:1000
                LcdModule.SendString(2, 0, string.Format("LD:{0}%", Math.Round(percent)).PadRight(8));
                LcdModule.SendString(2, 8, string.Format("P:{0}", processCount).PadRight(6));
                LcdModule.SendString(2, 14, string.Format("T:{0}", threadCount).PadRight(6));

                int barCount = (int)(percent / 5.0f);
                double halfBar = (int)(percent % 5.0f);
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

                string bars = string.Empty;
                bars = bars.PadRight(barCount, Convert.ToChar(0));
                bars += Convert.ToChar(1);
                bars = bars.PadRight(20);
                LcdModule.SendString(3, 0, bars);
                elapsed.Reset();
                elapsed.Start();
            }
        }

        public override void Switch(TimeSpan elapsed) {
            pfcThreads.Dispose();
            pfcProcesses.Dispose();
            pfcUptime.Dispose();
        }
    }
}
