using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InfoService {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main() {
            InfoService inf = new InfoService();
            if (Debugger.IsAttached) {
                
                inf.Start();
                Console.ReadKey();
            } else {
                ServiceBase.Run(inf);
            }
        }
    }
}
