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
                    cf.SendData(0, 0, DateTime.Now.TimeOfDay.ToString());
                    Thread.Sleep(500);
                    Console.ReadKey();
            }
            Console.ReadKey();
        }
    }
}
