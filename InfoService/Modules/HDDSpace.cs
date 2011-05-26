using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crypton.Hardware.CrystalFontz;
using System.IO;

namespace InfoService.Modules {
    class HDDSpace : Module {

        class CachedDriveInfo {
            public long AvailableFreeSpace {
                get;
                private set;
            }
            public string DriveFormat {
                get;
                private set;
            }
            public DriveType DriveType {
                get;
                private set;
            }
            public bool IsReady {
                get;
                private set;
            }
            public string Name {
                get;
                private set;
            }
            public long TotalFreeSpace {
                get;
                private set;
            }
            public long TotalSize {
                get;
                private set;
            }
            public string VolumeLabel {
                get;
                private set;
            }

            public CachedDriveInfo(DriveInfo di) {
                this.Name = di.Name;
                this.IsReady = di.IsReady;
                this.DriveType = di.DriveType;
                if (IsReady) {
                    this.AvailableFreeSpace = di.AvailableFreeSpace;
                    this.DriveFormat = di.DriveFormat;
                    this.TotalFreeSpace = di.TotalFreeSpace;
                    this.TotalSize = di.TotalSize;
                    this.VolumeLabel = di.VolumeLabel;
                }
            }
        }

        CachedDriveInfo[] drives = null;
        int index = 0;

        public HDDSpace(CrystalFontz635 cf)
            : base(cf) {
            DriveInfo[] _drives = DriveInfo.GetDrives();
            drives = new CachedDriveInfo[_drives.Length];
            for (int i = 0; i < _drives.Length; i++) {
                var _drive = _drives[i];               
                cf.SendString(0, 0, ("Loading " + _drive.Name).PadRight(20));
                drives[i] = new CachedDriveInfo(_drive);
            }
            cf.OnKeyDown += new KeyDownEventHandler(cf_OnKeyDown);
        }

        void cf_OnKeyDown(CrystalFontz635 api, KeyCodes pressedKeys) {
            switch (pressedKeys) {
                case KeyCodes.Up:
                    if (index - 1 > 0) {
                        index--;
                    }
                    break;
                case KeyCodes.Down:
                    if (index + 1 < drives.Length) {
                        index++;
                    }
                    break;
            }
        }
        public override bool Draw(TimeSpan elapsed) {
            base.Draw(elapsed);
            if (drives == null || drives.Length == 0) {
                LcdModule.SendString(0, 0, "NO DRIVES!");
                return false;
            }
            if (elapsed.TotalMilliseconds >= 1000) {
                CachedDriveInfo drive = drives[index];
                LcdModule.SetLED(0, drive.IsReady ? 100 : 0, drive.IsReady ? 0 : 100);
                string line1 = string.Format("{0} {1}".PadRight(20), drive.Name, drive.DriveType.ToString().ToUpper());
                LcdModule.SendString(0, 0, line1);
                double szTotalMb = drive.TotalSize / 1024 / 1024.0;
                double szFreeMb = drive.TotalFreeSpace / 1024 / 1024.0;
                string szTotal = Math.Round(szTotalMb > 1024.0 ? szTotalMb / 1024.0 : szTotalMb) + (szTotalMb > 1024.0 ? "G" : "M");
                string szFree = Math.Round(szFreeMb > 1024.0 ? szFreeMb / 1024.0 : szFreeMb) + (szFreeMb > 1024.0 ? "G" : "M");
                string line2 = string.Format("SIZE:{0} FREE:{1}", szTotal, szFree).PadRight(20);
                LcdModule.SendString(1, 0, line2);
                return true;
            }

            return false;
        }

        public override void Dispose() {
            drives = null;
            GC.Collect();
            base.Dispose();
        }
    }
}
