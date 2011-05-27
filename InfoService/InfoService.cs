using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Crypton.Hardware.CrystalFontz;
using InfoService.Modules;
using System.Threading;
using System.Globalization;

namespace InfoService {
    public partial class InfoService : ServiceBase {
        public InfoService() {
            InitializeComponent();
        }

        CrystalFontz635 cf635 = null;
        Thread thAppManager = null;
        Thread thCurrentApp = null;
        AutoResetEvent thAppMgrReset = new AutoResetEvent(false);
        bool exit = false;
        List<Type> modules = new List<Type>();
        Module currentModule = null;

        public void Start() {
            StartDevice();

            modules.Add(typeof(Weather));
            modules.Add(typeof(Sys));
            modules.Add(typeof(RAM));
            modules.Add(typeof(Battery));
            //modules.Add(typeof(HDD));
            //modules.Add(typeof(HDDSpace));
            //modules.Add(typeof(Net));

            InitializeAppManager();
        }


        protected void TerminateAllCommThreads() {
            lock (this) {
                if (thAppManager != null && thAppManager.IsAlive) {
                    thAppManager.Abort();
                }
                if (thCurrentApp != null && thCurrentApp.IsAlive) {
                    thCurrentApp.Abort();
                }
                thCurrentApp = null;
                thAppManager = null;
                GC.Collect();
            }
            if (cf635 != null)
                cf635.ResetOnKeyDown();
        }

        protected void StopDevice() {
            TerminateAllCommThreads();
            lock (this) {
                if (cf635 != null) {
                    cf635.SetBacklight(0);
                    cf635.ClearScreen();
                    cf635.Dispose();
                    cf635 = null;
                    GC.Collect();
                }
            }
        }

        protected void StartDevice() {
            TerminateAllCommThreads();
            lock (this) {
                if (cf635 == null) {
                    const int BAUD = 115200; // 115200
                    for (int i = 0; i < 10; i++) {
                        try {
                            cf635 = new CrystalFontz635(BAUD, "COM3");
                            break;
                        }
                        catch (Exception ex) {
                            Thread.Sleep(100);
                            cf635 = null;
                        }
                    }
                    if (cf635 == null)
                        throw new InvalidOperationException("Failed to create device");
                    cf635.Reset();
                    cf635.SetCursorStyle(CursorStyles.None);
                    cf635.OnKeyDown += new KeyDownEventHandler(cf635_OnKeyDown);
                }
            }
        }

        private void InitializeAppManager() {
            if (thAppManager != null)
                TerminateAllCommThreads();
            thAppMgrReset = new AutoResetEvent(false);
            thAppManager = new Thread(AppManager);
            thAppManager.Name = "App Manager";
            thAppManager.Start();
        }

        protected void VerifyDevice() {
            lock (this) {
                if (cf635 == null)
                    StartDevice();

                cf635.Ping();

            }
        }


        void cf635_OnKeyDown(CrystalFontz635 api, KeyCodes pressedKeys) {
            switch (pressedKeys) {
                case KeyCodes.Enter:
                    bool t = thAppMgrReset.Set();
                    Console.WriteLine("Key press");
                    break;
            }
        }

        private void AppManager() {
            try {
                while (true) {
                    for (int i = 0; i < modules.Count && !exit; i++) {
                        //TerminateAllCommThreads();
                        VerifyDevice();
                        cf635.Reset();
                        cf635.SetCursorStyle(CursorStyles.None);
                        try {
                            currentModule = (Module)Activator.CreateInstance(modules[i], cf635);
                        }
                        catch {
                            continue;
                        }
                        using (currentModule) {
                            thCurrentApp = new Thread(new ThreadStart(delegate {
                                try {
                                    Stopwatch sw = new Stopwatch();
                                    sw.Start();
                                    while (true) {
                                        bool resetWatch = currentModule.Draw(sw.Elapsed);
                                        if (resetWatch) {
                                            sw.Reset();
                                            sw.Start();
                                        }
                                        Thread.Sleep(1);
                                    }
                                }
                                catch (ThreadAbortException) {
                                    //  thAppMgrReset.Set();
                                }
                                catch (Exception ex) {
                                    Console.WriteLine(ex.ToString());
                                    thAppMgrReset.Set();
                                }
                            }));
                            thCurrentApp.Priority = ThreadPriority.BelowNormal;
                            thCurrentApp.CurrentCulture = CultureInfo.InvariantCulture;
                            thCurrentApp.CurrentUICulture = CultureInfo.InvariantCulture;
                            thCurrentApp.Name = "App Thread";
                            thCurrentApp.Start();
                            thAppMgrReset.WaitOne();
                            thCurrentApp.Abort();
                            ResetEvents();
                            GC.Collect();
                        }
                    }
                }
            }
            catch (ThreadAbortException) {
                // thread quit
            }
        }

        private void ResetEvents() {
            cf635.ResetOnKeyDown();
            cf635.OnKeyDown += cf635_OnKeyDown;
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus) {
            try {
                switch (powerStatus) {
                    case PowerBroadcastStatus.QuerySuspend:
                        TerminateAllCommThreads();
                        StopDevice();
                        break;
                    case PowerBroadcastStatus.ResumeSuspend:
                        TerminateAllCommThreads();
                        StartDevice();
                        InitializeAppManager();
                        break;
                    case PowerBroadcastStatus.Suspend:
                        TerminateAllCommThreads();
                        StopDevice();
                        break;
                    case PowerBroadcastStatus.ResumeAutomatic:
                        TerminateAllCommThreads();
                        StartDevice();
                        InitializeAppManager();
                        break;
                }
            }
            catch {
                return false;
            }
            return base.OnPowerEvent(powerStatus);
        }

        protected override void OnContinue() {
            TerminateAllCommThreads();
            StartDevice();
            InitializeAppManager();
            base.OnContinue();
        }

        protected override void OnPause() {
            TerminateAllCommThreads();
            StopDevice();
            base.OnPause();
        }

        protected override void OnShutdown() {
            TerminateAllCommThreads();
            StopDevice();
            base.OnShutdown();
        }

        protected override void OnStop() {
            TerminateAllCommThreads();
            StopDevice();
            base.OnStop();
        }

        protected override void OnStart(string[] args) {
            Start();
            base.OnStart(args);
        }

    }
}
