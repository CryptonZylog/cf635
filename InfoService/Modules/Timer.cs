using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Crypton.Hardware.CrystalFontz;

namespace InfoService.Modules {
    class Timer : Module {

        static System.Threading.Timer timer = null;
        static DateTime dateStarted = DateTime.MinValue;
        static TimeSpan tsDueTime = new TimeSpan(0, 0, 0, 0);
        static bool exit = false;

        public Timer(CrystalFontz635 cf)
            : base(cf) {
                cf.ResetOnKeyDown();
        }

        public override bool Init() {
            LcdModule.OnKeyDown += new Crypton.Hardware.CrystalFontz.KeyDownEventHandler(LcdModule_OnKeyDown);
            return true;
        }

        void LcdModule_OnKeyDown(Crypton.Hardware.CrystalFontz.CrystalFontz635 api, Crypton.Hardware.CrystalFontz.KeyCodes pressedKeys) {
            if (pressedKeys == KeyCodes.Left || pressedKeys == KeyCodes.Right) {
                LcdModule.SendCharacter(0, 6, Convert.ToChar(32));
                LcdModule.SendCharacter(0, 7, Convert.ToChar(32));
                LcdModule.SendCharacter(0, 9, Convert.ToChar(32));
                LcdModule.SendCharacter(0, 10, Convert.ToChar(32));
                LcdModule.SendCharacter(0, 12, Convert.ToChar(32));
                LcdModule.SendCharacter(0, 13, Convert.ToChar(32));
                LcdModule.SendCharacter(2, 6, Convert.ToChar(32));
                LcdModule.SendCharacter(2, 7, Convert.ToChar(32));
                LcdModule.SendCharacter(2, 9, Convert.ToChar(32));
                LcdModule.SendCharacter(2, 10, Convert.ToChar(32));
                LcdModule.SendCharacter(2, 12, Convert.ToChar(32));
                LcdModule.SendCharacter(2, 13, Convert.ToChar(32));
            }
            switch (pressedKeys) {
                case KeyCodes.Left:
                    if (timer == null && cursorPosition >= CursorPos.Hour2) {
                        cursorPosition--;
                    }
                    break;
                case KeyCodes.Right:
                    if (timer == null && cursorPosition + 1 <= CursorPos.Second2) {
                        cursorPosition++;
                    }
                    break;
                case KeyCodes.Up:
                    switch (cursorPosition) {
                        case CursorPos.Hour1:
                            if (tsDueTime.Hours + 10 < 24) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours + 10, tsDueTime.Minutes, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Hour2:
                            if (tsDueTime.Hours + 1 < 24) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours + 1, tsDueTime.Minutes, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Minute1:
                            if (tsDueTime.Minutes + 10 < 60) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes + 10, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Minute2:
                            if (tsDueTime.Minutes + 1 < 60) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes + 1, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Second1:
                            if (tsDueTime.Seconds + 10 < 60) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes, tsDueTime.Seconds + 10);
                            }
                            break;
                        case CursorPos.Second2:
                            if (tsDueTime.Seconds + 1 < 60) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes, tsDueTime.Seconds + 1);
                            }
                            break;
                    }
                    break;
                case KeyCodes.Down:
                    switch (cursorPosition) {
                        case CursorPos.Hour1:
                            if (tsDueTime.Hours - 10 >= 0) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours - 10, tsDueTime.Minutes, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Hour2:
                            if (tsDueTime.Hours - 1 >= 0) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours - 1, tsDueTime.Minutes, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Minute1:
                            if (tsDueTime.Minutes - 10 >= 0) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes - 10, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Minute2:
                            if (tsDueTime.Minutes - 1 >= 0) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes - 1, tsDueTime.Seconds);
                            }
                            break;
                        case CursorPos.Second1:
                            if (tsDueTime.Seconds - 10 >= 0) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes, tsDueTime.Seconds - 10);
                            }
                            break;
                        case CursorPos.Second2:
                            if (tsDueTime.Seconds - 1 >= 0) {
                                tsDueTime = new TimeSpan(tsDueTime.Hours, tsDueTime.Minutes, tsDueTime.Seconds - 1);
                            }
                            break;
                    }
                    break;
                case KeyCodes.Enter:
                    if (timer == null) {
                        // init & start timer
                        timer = new System.Threading.Timer(ring, null, 100, (int)tsDueTime.TotalMilliseconds);
                        dateStarted = DateTime.Now;
                        cursorPosition = CursorPos.Disabled;
                    }
                    break;
                case KeyCodes.Exit:
                    if (timer != null) {
                        // reset all
                        dateStarted = DateTime.MinValue;
                        timer.Dispose();
                        timer = null;
                        exit = true;
                        InfoService.Reset();
                    }
                    break;
            }
        }


        enum CursorPos : int {
            Disabled,
            Hour1,
            Hour2,
            Minute1,
            Minute2,
            Second1,
            Second2
        }

        CursorPos cursorPosition = CursorPos.Disabled;
        bool blinkState = false;

        bool setOne = false;
        void ring(object o) {
            if (setOne) {
                timer.Dispose();
                timer = null;
                while (!exit) {
                    Console.Beep(1000, 100);
                    Thread.Sleep(100);
                }
            }
            setOne = true;
        }

        public override bool Draw(TimeSpan elapsed) {
            if (timer == null) {
                // not set, setup needed
                LcdModule.SetLED(0, 0, 100);
                if (cursorPosition == CursorPos.Disabled)
                    cursorPosition = CursorPos.Hour1;
                LcdModule.SendString(1, 0, string.Format("RING: {0:00}:{1:00}:{2:00}", tsDueTime.Hours, tsDueTime.Minutes, tsDueTime.Seconds));
                switch (cursorPosition) {
                    case CursorPos.Hour1:
                        if (blinkState) {
                            LcdModule.SendCharacter(0, 6, Convert.ToChar(26));
                            LcdModule.SendCharacter(2, 6, Convert.ToChar(27));
                            blinkState = false;
                        }
                        else {
                            LcdModule.SendCharacter(0, 6, Convert.ToChar(32));
                            LcdModule.SendCharacter(2, 6, Convert.ToChar(32));
                            blinkState = true;
                        }
                        break;
                    case CursorPos.Hour2:
                        if (blinkState) {
                            LcdModule.SendCharacter(0, 7, Convert.ToChar(26));
                            LcdModule.SendCharacter(2, 7, Convert.ToChar(27));
                            blinkState = false;
                        }
                        else {
                            LcdModule.SendCharacter(0, 7, Convert.ToChar(32));
                            LcdModule.SendCharacter(2, 7, Convert.ToChar(32));
                            blinkState = true;
                        }
                        break;
                    case CursorPos.Minute1:
                        if (blinkState) {
                            LcdModule.SendCharacter(0, 9, Convert.ToChar(26));
                            LcdModule.SendCharacter(2, 9, Convert.ToChar(27));
                            blinkState = false;
                        }
                        else {
                            LcdModule.SendCharacter(0, 9, Convert.ToChar(32));
                            LcdModule.SendCharacter(2, 9, Convert.ToChar(32));
                            blinkState = true;
                        }
                        break;
                    case CursorPos.Minute2:
                        if (blinkState) {
                            LcdModule.SendCharacter(0, 10, Convert.ToChar(26));
                            LcdModule.SendCharacter(2, 10, Convert.ToChar(27));
                            blinkState = false;
                        }
                        else {
                            LcdModule.SendCharacter(0, 10, Convert.ToChar(32));
                            LcdModule.SendCharacter(2, 10, Convert.ToChar(32));
                            blinkState = true;
                        }
                        break;
                    case CursorPos.Second1:
                        if (blinkState) {
                            LcdModule.SendCharacter(0, 12, Convert.ToChar(26));
                            LcdModule.SendCharacter(2, 12, Convert.ToChar(27));
                            blinkState = false;
                        }
                        else {
                            LcdModule.SendCharacter(0, 12, Convert.ToChar(32));
                            LcdModule.SendCharacter(2, 12, Convert.ToChar(32));
                            blinkState = true;
                        }
                        break;
                    case CursorPos.Second2:

                        if (blinkState) {
                            LcdModule.SendCharacter(0, 13, Convert.ToChar(26));
                            LcdModule.SendCharacter(2, 13, Convert.ToChar(27));
                            blinkState = false;
                        }
                        else {
                            LcdModule.SendCharacter(0, 13, Convert.ToChar(32));
                            LcdModule.SendCharacter(2, 13, Convert.ToChar(32));
                            blinkState = true;
                        }
                        break;
                }
            }
            else {
                // display time info
                cursorPosition = CursorPos.Disabled;
                LcdModule.SendString(0, 0, "LEFT: " + (DateTime.Now - dateStarted).ToString());
                LcdModule.SendString(1, 0, "X - CANCEL/EXIT");
            }

            Thread.Sleep(100);
            return base.Draw(elapsed);
        }

        public override void Dispose() {
            exit = true;
            base.Dispose();
        }
    }
}
