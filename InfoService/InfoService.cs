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

namespace InfoService {
    public partial class InfoService : ServiceBase {
        public InfoService() {
            InitializeComponent();
        }

        CrystalFontz635 cf635 = null;
        Thread thRunner = null;
        bool exit = false;
        List<Type> modules = new List<Type>();
        Module currentModule = null;

        public void Start() {
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
                throw new InvalidOperationException("Failed to create client");
            cf635.Reset();
            cf635.SetCursorStyle(CursorStyles.None);
            cf635.OnKeyDown += new KeyDownEventHandler(cf635_OnKeyDown);

            modules.Add(typeof(Sys));
            modules.Add(typeof(RAM));
            //modules.Add(typeof(HDD));
            modules.Add(typeof(HDDSpace));
            modules.Add(typeof(Net));

            thRunner = new Thread(runModules);
            thRunner.Name = "Module runner";
            thRunner.Start();
        }

        bool next = false;

        void cf635_OnKeyDown(CrystalFontz635 api, KeyCodes pressedKeys) {
            switch (pressedKeys) {
                case KeyCodes.Enter:
                    SetWaitLed();
                    next = true;
                    break;
            }
        }

        void SetWaitLed() {
            lock (cf635) {
                cf635.SetLED(0, 100, 100);
            }
        }

        void ResetWaitLed() {
            lock (cf635) {
                cf635.SetLED(0, 0, 0);
            }
        }

        void runModules() {
            while (!exit) {
                for (int i = 0; i < modules.Count && !exit; i++) {
                    next = false;
                    cf635.ClearScreen();
                    cf635.SendString(0, 0, "loading...");
                    try {
                        currentModule = (Module)Activator.CreateInstance(modules[i], cf635);
                    }
                    catch {
                        continue;
                    }
                    using (currentModule) {
                        Stopwatch sw = new Stopwatch();
                        Stopwatch sw_sample = new Stopwatch();
                        sw_sample.Start();
                        sw.Start();
                        currentModule.Ready(sw.Elapsed);
                        do {
                            try {
                                currentModule.Sample(sw_sample.Elapsed);
                                sw_sample.Reset();
                                sw_sample.Start();
                                currentModule.Draw(sw);
                            }
                            catch {
                            }
                            Thread.Sleep(1);
                        } while (exit == false && next == false);
                        try {
                            currentModule.Switch(sw.Elapsed);
                        }
                        catch {
                        }
                        ResetEvents();
                        ResetWaitLed();
                    }
                }
            }
        }

        private void ResetEvents() {
            cf635.ResetOnKeyDown();
            cf635.OnKeyDown += cf635_OnKeyDown;
        }

        public void Stop() {
            exit = true;
            next = true;
            thRunner.Join();
            cf635.SetBacklight(0);
            cf635.ClearScreen();
            cf635.SetLED(0, 0, 0);
            cf635.SetLED(1, 0, 0);
            cf635.SetLED(2, 0, 0);
            cf635.SetLED(3, 0, 0);
            cf635.Dispose();
        }

        protected override void OnStart(string[] args) {
            Start();
            base.OnStart(args);
        }

        protected override void OnStop() {
            Stop();
            base.OnStop();
        }
    }
}
