using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crypton.Hardware.CrystalFontz;
using System.Threading;
using System.Globalization;

namespace test {
    class Program {
        static void Main(string[] args) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            using (CrystalFontz635 cf = new CrystalFontz635(115200, "COM3")) {
                cf.SetLED(0, 100, 0);
            }
            Console.ReadKey();
        }
    }
}
