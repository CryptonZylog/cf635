﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crypton.Hardware.CrystalFontz;
using System.Diagnostics;
using System.Threading;

namespace InfoService.Modules {
    class Net : Module {

        PerformanceCounter pfc = null;
        Stopwatch time = null;

        public Net(CrystalFontz635 cf)
            : base(cf) {
            pfc = new PerformanceCounter("IPv4", "Datagrams Received/sec");
            LcdModule.SendData(0, 0, "NET LOAD");
            LcdModule.SendData(1, 0, "datagrams/second");
            time = new Stopwatch();
            time.Start();
        }

        void flash(int delay) {
            LcdModule.SetLED(0, 100, 0);
            Thread.Sleep(delay);
            LcdModule.SetLED(0, 0, 0);
        }

        public override bool Draw(TimeSpan elapsed) {

            float count = pfc.NextValue();
            if (elapsed.TotalMilliseconds >= 100) {
                double rawCount = Math.Floor((double)count);
                LcdModule.SendString(2, 8, (rawCount.ToString() + "dg/s").PadRight(10));
                if (rawCount > 0) {
                    flash(25);
                }

                return true;
            }

            return false;
        }


    }
}
