using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crypton.Hardware.CrystalFontz;
using System.Threading;
using System.Diagnostics;

namespace InfoService {
    public abstract class Module : IDisposable {
        public CrystalFontz635 LcdModule {
            get;
            set;
        }
        public Module(CrystalFontz635 cf) {
            this.LcdModule = cf;
            this.LcdModule.ClearScreen();
        }
        public virtual bool Init() {
            return true;
        }
        public virtual bool Draw(TimeSpan elapsed) {
            LcdModule.SetCursorPosition(0, 0);
            return true;
        }
        public virtual void Dispose() {
        }
    }
}
