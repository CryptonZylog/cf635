using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using Crypton.Hardware.CrystalFontz;

namespace InfoService.Modules {
    class Weather : Module {
        DateTime lastAccess = DateTime.MinValue;

        #region props

        XmlNode xcondition;
        XmlNode xlocation;
        XmlNode xwind;
        XmlNode xatmosphere;
        XmlNode xastronomy;
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

        #endregion

        public Weather(CrystalFontz635 cf)
            : base(cf) {
        }

        public override bool Draw(TimeSpan elapsed) {
            if ((DateTime.Now - lastAccess).TotalMinutes >= 5) {
                LoadWeather();
                lastAccess = DateTime.Now;
            }

            if (elapsed.TotalMilliseconds >= 200) {
                // marquee
                LcdModule.SendString(0, 0, xlocation.Attributes["city"].Value + " " + xlocation.Attributes["region"].Value);
                LcdModule.SendString(0, 16, (xcondition.Attributes["temp"].Value + "C").PadRight(4));
                LcdModule.SendString(1, 0, ("WIND:" + xwind.Attributes["speed"].Value + "kmh" + " \xA0 " + xwind.Attributes["chill"].Value + "C").PadRight(20));
                LcdModule.SendString(2, 0, (xatmosphere.Attributes["humidity"].Value + "%HUM " + xatmosphere.Attributes["pressure"].Value + "mb" + (int.Parse(xatmosphere.Attributes["rising"].Value) > 0 ? Convert.ToChar(222) : Convert.ToChar(224))).PadRight(20));
                LcdModule.SendString(3, 0, (xcondition.Attributes["text"].Value).PadRight(20));
                return true;
            }



            return false;
        }

        private void LoadWeather() {
            LcdModule.SetLED(0, 100, 100);
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(new WebClient().DownloadString("http://weather.yahooapis.com/forecastrss?w=12769489&u=c"));

            XmlNode xchannel = xdoc.SelectSingleNode("/rss/channel");

            xcondition = xchannel["item"]["yweather:condition"];
            xlocation = xchannel["yweather:location"];
            xwind = xchannel["yweather:wind"];
            xatmosphere = xchannel["yweather:atmosphere"];
            xastronomy = xchannel["yweather:astronomy"];


            LcdModule.SetLED(0, 100, 0);
        }
    }
}
