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
        public virtual void Initialize() {
        }
        public virtual void Ready(TimeSpan elapsed) {
        }
        public virtual void Sample(TimeSpan elapsed) {
        }
        public virtual void Draw(Stopwatch elapsed) {
            LcdModule.SetCursorPosition(0, 0);
        }
        public virtual void Switch(TimeSpan elapsed) {
        }

        public virtual void Dispose() {
        }
    }
}
