using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using Crypton.Hardware.CrystalFontz;
using System.Threading;

namespace InfoService.Modules {
    class Weather : Module {
        DateTime lastAccess = DateTime.MinValue;

        #region props

        XmlNode xcondition;
        XmlNode xlocation;
        XmlNode xwind;
        XmlNode xatmosphere;
        XmlNode xastronomy;

        XmlNode xtoday;
        XmlNode xtomorrow;

        int display = 1;

        #endregion

        #region Symbols

        byte[] sym_sun = new byte[]{
                                Convert.ToByte("00000000",2),
                                Convert.ToByte("11100100",2),
                                Convert.ToByte("00011000",2),
                                Convert.ToByte("00100100",2),
                                Convert.ToByte("00100100",2),
                                Convert.ToByte("00011000",2),
                                Convert.ToByte("10100100",2),
                                Convert.ToByte("00000000",2)
                         };

        byte[] sym_rain = new byte[] {
                                Convert.ToByte("10010000",2),
                                Convert.ToByte("00001000",2),
                                Convert.ToByte("00001100",2),
                                Convert.ToByte("00011110",2),
                                Convert.ToByte("00111110",2),
                                Convert.ToByte("00111110",2),
                                Convert.ToByte("00011100",2),
                                Convert.ToByte("00000000",2)
        };

        byte[] sym_cloud = new byte[] {
                                Convert.ToByte("00000000",2),
                                Convert.ToByte("00001000",2),
                                Convert.ToByte("00011100",2),
                                Convert.ToByte("10111110",2),
                                Convert.ToByte("00000000",2),
                                Convert.ToByte("00000000",2),
                                Convert.ToByte("00000000",2),
                                Convert.ToByte("00000000",2)
        };

        byte[] sym_thunder = new byte[] {
                                Convert.ToByte("10000100",2),
                                Convert.ToByte("10001000",2),
                                Convert.ToByte("10010000",2),
                                Convert.ToByte("10111110",2),
                                Convert.ToByte("10000100",2),
                                Convert.ToByte("10001000",2),
                                Convert.ToByte("10010000",2),
                                Convert.ToByte("10100000",2)
        };

        byte[] sym_wind_n = new byte[] {
            Convert.ToByte("00000000",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00011100",2),
            Convert.ToByte("00101010",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00000000",2)
        };

        byte[] sym_wind_ne = new byte[] {
            Convert.ToByte("00000000",2),
            Convert.ToByte("00000000",2),
            Convert.ToByte("00000111",2),
            Convert.ToByte("00000011",2),
            Convert.ToByte("00000101",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00010000",2),
            Convert.ToByte("00000000",2)
        };

        byte[] sym_wind_nw = new byte[] {
            Convert.ToByte("00000000",2),
            Convert.ToByte("00000000",2),
            Convert.ToByte("00111000",2),
            Convert.ToByte("00110000",2),
            Convert.ToByte("00101000",2),
            Convert.ToByte("00000100",2),
            Convert.ToByte("00000010",2),
            Convert.ToByte("00000000",2)
        };

        byte[] sym_wind_w = new byte[] {
            Convert.ToByte("00000000",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00010000",2),
            Convert.ToByte("00111111",2),
            Convert.ToByte("00010000",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00000000",2),
            Convert.ToByte("00000000",2)
        };

        byte[] sym_wind_e = new byte[] {
            Convert.ToByte("00000000",2),
            Convert.ToByte("00000100",2),
            Convert.ToByte("00000010",2),
            Convert.ToByte("00111111",2),
            Convert.ToByte("00000010",2),
            Convert.ToByte("00000100",2),
            Convert.ToByte("00000000",2),
            Convert.ToByte("00000000",2)
        };

        byte[] sym_degree = new byte[] {
            Convert.ToByte("00001000",2),
            Convert.ToByte("00010100",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00000110",2),
            Convert.ToByte("00001001",2),
            Convert.ToByte("00001000",2),
            Convert.ToByte("00001001",2),
            Convert.ToByte("00000110",2)
        };

        #endregion

        public Weather(CrystalFontz635 cf)
            : base(cf) {
            cf.OnKeyDown += new KeyDownEventHandler(cf_OnKeyDown);
            cf.SetCGRAM(1, sym_degree);
        }

        void cf_OnKeyDown(CrystalFontz635 api, KeyCodes pressedKeys) {
            switch (pressedKeys) {
                case KeyCodes.Left:
                    if (display - 1 >= 1)
                        display--;
                    break;
                case KeyCodes.Right:
                    if (display + 1 <= 3)
                        display++;
                    break;
            }
            Draw(TimeSpan.MaxValue);
        }

        public override bool Draw(TimeSpan elapsed) {
            if ((DateTime.Now - lastAccess).TotalMinutes >= 5) {
                LoadWeather();
                lastAccess = DateTime.Now;
            }

            if (elapsed.TotalMilliseconds >= 200) {
                // marquee
                LcdModule.SendString(0, 0, (display == 1 ? '\x10' : ' ') + "NOW " + (display == 2 ? '\x10' : ' ') + "TODAY" + (display == 3 ? '\x10' : ' ') + "TOMORROW");

                switch (display) {
                    case 1:
                        LcdModule.SendString(1, 0, (xwind.Attributes["speed"].Value + "kmh" + " \xA0 " + xwind.Attributes["chill"].Value + "\x0001").PadRight(20));
                        LcdModule.SendString(2, 0, (xatmosphere.Attributes["humidity"].Value + "%H " + xatmosphere.Attributes["pressure"].Value + "mb" + (int.Parse(xatmosphere.Attributes["rising"].Value) > 0 ? Convert.ToChar(222) : Convert.ToChar(224)) + " " + (xcondition.Attributes["temp"].Value + "\x0001").PadRight(3)).PadRight(20));
                        LcdModule.SendString(3, 0, (xcondition.Attributes["text"].Value).PadRight(20));
                        break;
                    case 2: // today
                        LcdModule.SendString(1, 0, (xtoday.Attributes["day"].Value + " " + xtoday.Attributes["date"].Value).PadRight(20));
                        LcdModule.SendString(2, 0, (xtoday.Attributes["low"].Value + "..." + xtoday.Attributes["high"].Value + "\x0001").PadRight(20));
                        LcdModule.SendString(3, 0, (xtoday.Attributes["text"].Value).PadRight(20));
                        break;
                    case 3: // tomorrow
                        LcdModule.SendString(1, 0, (xtomorrow.Attributes["day"].Value + " " + xtomorrow.Attributes["date"].Value).PadRight(20));
                        LcdModule.SendString(2, 0, (xtomorrow.Attributes["low"].Value + "..." + xtomorrow.Attributes["high"].Value + "\x0001").PadRight(20));
                        LcdModule.SendString(3, 0, (xtomorrow.Attributes["text"].Value).PadRight(20));
                        break;
                }
                return true;
            }

            Thread.Sleep(2);
            return false;
        }

        private void LoadWeather() {
            LcdModule.SetLED(0, 100, 100);
            LcdModule.SendString(0, 0, "Loading weather...");
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(new WebClient().DownloadString("http://weather.yahooapis.com/forecastrss?w=12769489&u=c"));
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xdoc.NameTable);
            nsmgr.AddNamespace("yweather", "http://xml.weather.yahoo.com/ns/rss/1.0");

            XmlNode xchannel = xdoc.SelectSingleNode("/rss/channel");

            xcondition = xchannel["item"]["yweather:condition"];
            xlocation = xchannel["yweather:location"];
            xwind = xchannel["yweather:wind"];
            xatmosphere = xchannel["yweather:atmosphere"];
            xastronomy = xchannel["yweather:astronomy"];

            XmlNodeList xforecast = xchannel["item"].SelectNodes("yweather:forecast", nsmgr);
            xtoday = xforecast[0];
            xtomorrow = xforecast[1];

            LcdModule.SetLED(0, 0, 0);
        }
    }
}
