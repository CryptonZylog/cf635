using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Crypton.Hardware.CrystalFontz {

    /// <summary>
    /// Contains enumeration of buttons on the module keypad
    /// </summary>
    public enum KeyCodes : byte {
        /// <summary>
        /// Placeholder, no key
        /// </summary>
        None,
        /// <summary>
        /// UP key
        /// </summary>
        Up,
        /// <summary>
        /// DOWN key
        /// </summary>
        Down,
        /// <summary>
        /// LEFT key
        /// </summary>
        Left,
        /// <summary>
        /// RIGHT key
        /// </summary>
        Right,
        /// <summary>
        /// ENTER key
        /// </summary>
        Enter,
        /// <summary>
        /// EXIT key
        /// </summary>
        Exit
    }
    /// <summary>
    /// Contains enumeration of cursor styles
    /// </summary>
    public enum CursorStyles : byte {
        /// <summary>
        /// No cursor will be displayed
        /// </summary>
        None,
        /// <summary>
        /// Blinking block
        /// </summary>
        BlinkingBlock,
        /// <summary>
        /// Underscore
        /// </summary>
        Underscore,
        /// <summary>
        /// Alternates between a blinking block and underscore
        /// </summary>
        BlinkingBlockUnderscore,
        /// <summary>
        /// Inverted blinking block
        /// </summary>
        InvertedBlinkingBlock
    }
    /// <summary>
    /// Defines a method to be executed when a series of keys are being pushed
    /// </summary>
    /// <param name="api">Reference to the current API that fired the event</param>
    /// <param name="pressedKeys">Bitfield of pressed keys</param>
    public delegate void KeyDownEventHandler(CrystalFontz635 api, KeyCodes pressedKeys);
    /// <summary>
    /// Defines a method to be executed when a series of keys are released from a pressed state
    /// </summary>
    /// <param name="api">Reference to the current API that fired the event</param>
    /// <param name="releasedKeys">Bitfield of released keys</param>
    public delegate void KeyUpEventHandler(CrystalFontz635 api, KeyCodes releasedKeys);

    /// <summary>
    /// Contains information about the current state of the keypad after reading the keypad in polled mode
    /// </summary>
    public class KeypadInfo {
        /// <summary>
        /// Bitmask of currently pressed keys
        /// </summary>
        public KeyCodes PressedKeys {
            get;
            internal set;
        }
        /// <summary>
        /// Bitmask of keys that were pressed since last poll
        /// </summary>
        public KeyCodes PressedLastPoll {
            get;
            internal set;
        }
        /// <summary>
        /// Bitmask of keys that were released since last poll
        /// </summary>
        public KeyCodes ReleasedLastPoll {
            get;
            internal set;
        }

        internal KeypadInfo() {
        }
    }

    public class ReportInfo {

    }

    /// <summary>
    /// Provides access to the CF635 LCD display
    /// </summary>
    public class CrystalFontz635 : /*Component,*/ IDisposable {
        #region Constants
        const int MIN_ROW = 0;
        const int MAX_ROW = 3;
        const int MIN_COL = 0;
        const int MAX_COL = 19;
        #endregion

        internal SerialPort spLcd = null;
        int baudRate = 0;
        bool exit = false;
        Thread receivingAsync = null;
        Dispatcher disp = null;

        #region Constructors
        /// <summary>
        /// Creates a new CrystalFontz API driver for CF635 LCD display. This constructor tries to search for the LCD display
        /// and throws CommunicationException if no devices found. It will try connect via baud rates 115200, 19200 and 9600
        /// </summary>
        public CrystalFontz635() {
            // get available COM ports on the system
            string[] ports = SerialPort.GetPortNames();
            // try each port to see if we can open it and detect a device on it
            bool found = false;
            foreach (var port in ports) {
                for (int _try = 1; _try <= 3; _try++) {
                    switch (_try) {
                        case 1:
                            baudRate = 115200;
                            break;
                        case 2:
                            baudRate = 19200;
                            break;
                        case 3:
                            baudRate = 9600;
                            break;
                    }
                    try {
                        spLcd = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One);
                        spLcd.ReadBufferSize = 16 * 1024;
                        spLcd.WriteTimeout = 2500;
                        spLcd.ReadTimeout = 2500;
                        spLcd.Open();
                        found = true;
                        break;
                    }
                    catch {
                        // nope
                    }
                }
                if (found) {
                    break;
                }
            }
            if (!found) {
                spLcd = null;
                throw new CommunicationException("Could not find a responding LCD device", CommunicationException.ErrorCodes.DeviceNotFound);
            }
            else {
                startMethods();
            }
        }

        /// <summary>
        /// Connects to an LCD module with given baud rate and COM port. Throws a CommunicationException if could not connect
        /// </summary>
        /// <param name="baudRate">The connection baudrate. Default is 115200</param>
        /// <param name="comPort">COM port to use, for ex. COM4</param>
        public CrystalFontz635(int baudRate, string comPort) {
            try {
                spLcd = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.One);
                spLcd.ReadBufferSize = 16 * 1024;
                spLcd.WriteTimeout = 2500;
                spLcd.ReadTimeout = 2500;
                spLcd.Open();
                startMethods();
            }
            catch (Exception ex) {
#if(DEBUG)
                if (Debugger.IsAttached)
                    throw ex;
#endif
                throw new CommunicationException("Could not find a responding LCD device", CommunicationException.ErrorCodes.DeviceNotFound);
            }
        }

        #endregion

        // thread which receives asynchronous messages from module (like button presses)
        void receiveAsync() {
            while (!exit && spLcd != null && spLcd.IsOpen) {
                var packet = default(Packet);
                bool ok = disp.WaitForReturn(0x80, out packet);
                if (ok && packet.IsValid) {
                    Debug.WriteLine("Key press: " + (KeyCodes)packet.Data[0]);
                    if (packet.Data[0] < 7) { // key is pressed
                        KeyCodes code = (KeyCodes)packet.Data[0];
                        if (OnKeyDown != null) {
                            OnKeyDown(this, code);
                        }
                    }
                    else { // key is released
                        KeyCodes code = (KeyCodes)packet.Data[0];
                        if (OnKeyUp != null) {
                            OnKeyUp(this, code);
                        }
                    }
                }
            }
        }

        #region Events
        /// <summary>
        /// Fires when a key is released
        /// </summary>
        public event KeyUpEventHandler OnKeyUp;
        /// <summary>
        /// Fires when a key is pressed
        /// </summary>
        public event KeyDownEventHandler OnKeyDown;
        /// <summary>
        /// Resets OnKeyDown event to have no listeners
        /// </summary>
        public void ResetOnKeyDown() {
            OnKeyDown = null;
        }
        #endregion

        #region Helpers
        private void startMethods() {
            disp = new Dispatcher(this);
            receivingAsync = new Thread(receiveAsync);
            receivingAsync.Start();
        }
        private bool checkReceive(byte expected, Packet received) {
            return true;  // experimental
        }
        #endregion

        #region LCD Methods
        /// <summary>
        /// Sends a ping packet to the LCD and expects a ping reply. If no reply is received, throws a CommunicationException
        /// Timeout: 5 seconds
        /// </summary>
        public void Ping() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            byte[] someData = new byte[] { 0xab, 0xac, 0xad, 0x01 };
            try {
                var receive = disp.Transaction(new Packet() {
                    Type = 0x0,
                    Data = someData
                }, 0x40);
                if (receive.Data.Length != someData.Length)
                    throw new CommunicationException("Received ping data does not match what was sent", CommunicationException.ErrorCodes.GeneralError);
                for (int i = 0; i < someData.Length; i++) {
                    if (someData[i] != receive.Data[i])
                        throw new CommunicationException("Received ping data does not match what was sent", CommunicationException.ErrorCodes.GeneralError);
                }
                // all looks good
            }
            catch (Exception ex) {
                throw new InvalidOperationException("Failed to execute Ping command, look for details in InnerException", ex);
            }
        }
        /// <summary>
        /// <para>Returns the hardware and firmware version on the module</para>
        /// <para>The first element is the hardware version, the second is firmware</para>
        /// </summary>
        /// <returns></returns>
        public Version[] GetVersion() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            string versInfo = string.Empty;
            lock (this) {
                var resp = disp.Transaction(new Packet() {
                    Type = 0x01,
                    Data = new byte[0]
                });
                versInfo = Encoding.ASCII.GetString(resp.Data);
            }
            Version vHardware = default(Version), vFirmware = default(Version);
            {
                // hardware
                int hwStart = versInfo.IndexOf(":h") + 2;
                int fwStart = versInfo.IndexOf(",v", hwStart);
                vHardware = new Version(versInfo.Substring(hwStart, fwStart - hwStart));

                //firmware
                vFirmware = new Version(versInfo.Substring(fwStart + 2));
            }
            return new Version[] { vHardware, vFirmware };
        }
        /// <summary>
        /// Writes arbitrary data to flash memory on the module. Note! Maximum size of data is 16 bytes, less data will be padded with zeros
        /// </summary>
        /// <param name="data">Data to write, max 16 bytes (will be padded with zeros)</param>
        public void WriteUserFlash(byte[] data) {
            const int MAX_FLASH = 16; // maximum 16 bytes of storage

            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (data.Length > MAX_FLASH) {
                // max 16 bytes
                throw new ArgumentOutOfRangeException("Maximum supported size of data is 16 bytes in user flash");
            }
            byte[] write = new byte[MAX_FLASH];
            if (data != null) {
                Array.Copy(data, 0, write, 0, data.Length);
            }
            disp.Transaction(new Packet() {
                Type = 0x02,
                Data = write
            });
        }
        /// <summary>
        /// This will read user flash data and return it as a 16 byte array
        /// </summary>
        /// <returns></returns>
        public byte[] ReadUserFlash() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            var resp = disp.Transaction(new Packet() {
                Type = 0x03,
                Data = new byte[0]
            });
            return resp.Data;
        }
        /// <summary>
        /// Stores current state of LCD as the boot state
        /// </summary>
        public void StoreBoot() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            var resp = disp.Transaction(new Packet() {
                Type = 0x04,
                Data = new byte[0]
            });
        }
        /// <summary>
        /// Reboots the LCD module
        /// </summary>
        public void RebootModule() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            disp.Transaction(new Packet() {
                Type = 0x05,
                Data = new byte[] { 8, 18, 99 }
            });
        }
        /// <summary>
        /// <para>Resets the host (computer currently running)</para>
        /// <para>WARNING! This will reset the host computer (and lead to possible data loss) when the reset line is connected to GPIO-3, see data sheet for more details.</para>
        /// </summary>
        public void ResetHost() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            disp.Transaction(new Packet() {
                Type = 0x05,
                Data = new byte[] { 12, 28, 97 }
            });
        }
        /// <summary>
        /// <para>Turns the host power off</para>
        /// <para>WARNING! This will turn off power to the host computer (and lead to possible data loss) when the power control line is connected to GPIO-2, see data sheet for more details.</para>
        /// </summary>
        public void PowerOffHost() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            disp.Transaction(new Packet() {
                Type = 0x05,
                Data = new byte[] { 3, 11, 95 }
            });
        }
        /// <summary>
        /// Clears the contents of DDRAM and moves cursor to top-left position
        /// </summary>
        public void ClearScreen() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            disp.Transaction(new Packet() {
                Type = 0x06
            });
        }
        /// <summary>
        /// <para>Sets the CGRAM character bitmap at specified index (0-7)</para>
        /// <para>The bitmap array contains bitmap of the character. For each row, any value between 0 and 63 is valid. The most significant bit (at the left) is the left pixel column, and the rightmost bit is the ending column. If bit 7 is set in any of the lines, the line will blink</para>
        /// </summary>
        /// <param name="index">The index of the character, 0 to 7</param>
        /// <param name="bitmap">The bitmap image of the character</param>
        public void SetCGRAM(int index, byte[] bitmap) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (index < 0 || index > 7)
                throw new ArgumentOutOfRangeException("The index must be between 0 and 7");
            if (bitmap.Length > 8) {
                throw new ArgumentOutOfRangeException("The bitmap size must be less than or equal to 8 bytes");
            }
            byte[] output = new byte[9];
            output[0] = (byte)index;
            if (bitmap != null)
                Array.Copy(bitmap, 0, output, 1, bitmap.Length);
            disp.Transaction(new Packet() {
                Type = 0x09,
                Data = output
            });
        }
        /// <summary>
        /// Reads LCD CGRAM or DDRAM memory, depending on the address
        /// </summary>
        /// <param name="address">Read address native to the controller. See datasheet for details</param>
        /// <returns></returns>
        public byte[] ReadLCDMemory(byte address) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (address < 0x40 || address > 0xf3)
                throw new ArgumentOutOfRangeException("The address must fall between the ranges of CGRAM-DDRAM memory");
            var resp = disp.Transaction(new Packet() {
                Type = 0x0a,
                Data = new byte[] { address }
            });
            byte[] data = new byte[8];
            Array.Copy(resp.Data, 1, data, 0, resp.Data.Length - 1);
            return data;
        }
        /// <summary>
        /// Sets cursor position
        /// </summary>
        /// <param name="row">Row, 0-3 where 0 is the first row</param>
        /// <param name="column">Column, 0-19, where 0 is the first column</param>
        public void SetCursorPosition(int row, int column) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (row < MIN_ROW || row > MAX_ROW)
                throw new ArgumentOutOfRangeException("The row must be between 0 and 3");
            if (column < MIN_COL || column > MAX_COL)
                throw new ArgumentOutOfRangeException("The column must be between 0 and 19");

            disp.Transaction(new Packet() {
                Type = 0x0b,
                Data = new byte[] { (byte)column, (byte)row }
            });
        }
        /// <summary>
        /// Sets cursor style
        /// </summary>
        /// <param name="style">Cursor style</param>
        public void SetCursorStyle(CursorStyles style) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");

            disp.Transaction(new Packet() {
                Type = 0x0c,
                Data = new byte[] { (byte)style }
            });

        }
        /// <summary>
        /// Sets display contrast
        /// </summary>
        /// <param name="value">Display contrast, 0-254 are valid where 254 is very dark and 0 is very light</param>
        public void SetContrast(int value) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (value < 0 || value > 254)
                throw new ArgumentOutOfRangeException("The value must be between 0 and 254");

            disp.Transaction(new Packet() {
                Type = 0x0d,
                Data = new byte[] { (byte)value }
            });

        }
        /// <summary>
        /// Sets backlight amount
        /// </summary>
        /// <param name="value">0-100 where 0 is off and 100 is completely on</param>
        public void SetBacklight(int value) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException("The value must be between 0 and 100");

            disp.Transaction(new Packet() {
                Type = 0x0e,
                Data = new byte[] { (byte)value }
            });

        }
        public void SetupFanReporting(bool fan1, bool fan2, bool fan3, bool fan4) {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public void SetFanPower(int fan1, int fan2, int fan3, int fan4) {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        /// <summary>
        /// Reads DOW (Dallas one wire) device information and returns the ROM ID of the device
        /// </summary>
        /// <param name="dowAddress">DOW device address (0-31)</param>
        /// <returns></returns>
        public byte[] ReadDOWInfo(byte dowAddress) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (dowAddress < 0 || dowAddress > 31)
                throw new ArgumentOutOfRangeException("The address must be between 0 and 31");

            var resp = disp.Transaction(new Packet() {
                Type = 0x12,
                Data = new byte[] { dowAddress }
            });
            byte[] data = new byte[8];
            Array.Copy(resp.Data, 1, data, 0, resp.Data.Length - 1);
            return data;

        }
        public void SetupTemperatureReporting(bool[] devices) {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public void DOWTransaction() {
            throw new NotImplementedException("DOW functions are not implemented yet");
        }
        /// <summary>
        /// Sends data to the S6A0073 LCD controller
        /// </summary>
        /// <param name="locationCode">Location code: 0-data register, 1-control register (RE=0), 2-control register (RE=1)</param>
        /// <param name="data">Data to send</param>
        public void SendControllerCommand(byte locationCode, byte data) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (locationCode < 0 || locationCode > 2)
                throw new ArgumentOutOfRangeException("The locationCode must be between 0 and 2");
            disp.Transaction(new Packet() {
                Type = 0x16,
                Data = new byte[] { locationCode, data }
            });
        }
        /// <summary>
        /// Configures key reporting, see datasheet for details
        /// </summary>
        /// <param name="pressMask"></param>
        /// <param name="releaseMask"></param>
        public void ConfigureKeyReporting(KeyCodes pressMask, KeyCodes releaseMask) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            disp.Transaction(new Packet() {
                Type = 0x17,
                Data = new byte[] { (byte)pressMask, (byte)releaseMask }
            });
        }
        /// <summary>
        /// Reads the keypad information, in polled mode and returns information about the current state of the keypad
        /// </summary>
        /// <returns></returns>
        public KeypadInfo ReadKeypad() {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            KeypadInfo ki = new KeypadInfo();
            var resp = disp.Transaction(new Packet() {
                Type = 0x18
            });
            ki.PressedKeys = (KeyCodes)resp.Data[0];
            ki.PressedLastPoll = (KeyCodes)resp.Data[1];
            ki.ReleasedLastPoll = (KeyCodes)resp.Data[2];
            return ki;
        }
        public void SetFanPowerFailSafe(bool fan1, bool fan2, bool fan3, bool fan4, int ticks) {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public void SetFanTachometerGlitchFilter(int dcFan1, int dcFan2, int dcFan3, int dcFan4) {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public bool[] QueryFanPower() {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public bool[] QueryFanFailsafe() {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public void SetPowerSwitchFunction(bool pullMode, int GPIOpin) {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public void SetWatchdog(bool enabled) {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public void ResetWatchdog() {
            throw new NotImplementedException("SCAB functions are not implemented yet");
        }
        public ReportInfo ReadReportAndStatus() {
            throw new NotImplementedException("IMPLEMENT!");
            //TODO: implement!  0x1E
        }
        /// <summary>
        /// Sends text data to the LCD
        /// </summary>
        /// <param name="row">Row, 0-3</param>
        /// <param name="column">Column, 0-19</param>
        /// <param name="text">Text, max 20 characters</param>
        public void SendData(int row, int column, string text) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (row < MIN_ROW || row > MAX_ROW)
                throw new ArgumentOutOfRangeException("The row must be between 0 and 3");
            if (column < MIN_COL || column > MAX_COL)
                throw new ArgumentOutOfRangeException("The column must be between 0 and 19");
            if (string.IsNullOrEmpty(text))
                text = string.Empty;
            if (text.Length > 20)
                throw new ArgumentOutOfRangeException("String is longer than 20 characters");
            byte[] output = null;
            using (MemoryStream ms = new MemoryStream()) {
                ms.WriteByte((byte)column);
                ms.WriteByte((byte)row);
                byte[] bt = Encoding.ASCII.GetBytes(text);
                ms.Write(bt, 0, bt.Length);
                output = ms.ToArray();
            }
            disp.Transaction(new Packet() {
                Type = 0x1f,
                Data = output
            });
        }
        /// <summary>
        /// Sends arbitrary string data to LCD
        /// </summary>
        /// <param name="row">Row, 0-3</param>
        /// <param name="column">Column, 0-19</param>
        /// <param name="text">Text, max 20 characters</param>
        public void SendString(int row, int column, string text) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (row < MIN_ROW || row > MAX_ROW)
                throw new ArgumentOutOfRangeException("The row must be between 0 and 3");
            if (column < MIN_COL || column > MAX_COL)
                throw new ArgumentOutOfRangeException("The column must be between 0 and 19");
            if (string.IsNullOrEmpty(text))
                text = string.Empty;
            if (text.Length > 20)
                text = text.Substring(0, 20);
            for (int col = column, ch = 0; col <= MAX_COL && ch < text.Length; col++, ch++) {
                SendCharacter(row, col, text[ch]);
            }
        }
        /// <summary>
        /// Sends arbitrary character to the LCD
        /// </summary>
        /// <param name="row">Row, 0-3</param>
        /// <param name="column">Column, 0-19</param>
        /// <param name="value">Character code</param>
        public void SendCharacter(int row, int column, char value) {
            if (spLcd == null)
                throw new InvalidOperationException("Not connected to an LCD module");
            if (row < MIN_ROW || row > MAX_ROW)
                throw new ArgumentOutOfRangeException("The row must be between 0 and 3");
            if (column < MIN_COL || column > MAX_COL)
                throw new ArgumentOutOfRangeException("The column must be between 0 and 19");
            byte[] output = null;
            using (MemoryStream ms = new MemoryStream()) {
                ms.WriteByte((byte)column);
                ms.WriteByte((byte)row);
                ms.WriteByte((byte)value);
                output = ms.ToArray();
            }
            disp.Transaction(new Packet() {
                Type = 0x1f,
                Data = output
            }, 0x5f);
        }
        public enum SupportedBaudRates : int {
            Rate19200 = 19200,
            Rate115200 = 115200
        }
        public void SetBaudRate(SupportedBaudRates rate) {
            bool successRequest = false;
            byte rateNum = 1;
            switch (rate) {
                case SupportedBaudRates.Rate115200:
                    rateNum = 1;
                    break;
                case SupportedBaudRates.Rate19200:
                    rateNum = 0;
                    break;
            }
            var resp = disp.Transaction(new Packet() {
                Type = 0x21,
                Data = new byte[] { rateNum }
            });
            if (resp.Type == 0x61) {
                successRequest = true;
            }
            else {
                successRequest = false;
            }

            if (successRequest) {
                string comport = spLcd.PortName;

                Dispose();
                exit = false;
                spLcd = new SerialPort(comport, (int)rate);
                Ping();
                startMethods();
            }
            else {
                throw new InvalidOperationException("Failed to switch to new baud rate");
            }
        }
        /// <summary>
        /// Sets GPIO pin at index to a set state. See datasheet (Command 0x22 for more details)
        /// </summary>
        /// <param name="gpioIndex">Index of GPIO pin</param>
        /// <param name="state">State of the pin</param>
        public void ConfigureGPIO(byte gpioIndex, byte state, byte function) {
            lock (this) {
                byte[] output = new byte[3];
                output[0] = gpioIndex;
                output[1] = state;
                output[2] = function;

                disp.Transaction(new Packet() {
                    Type = 0x22,
                    Data = output
                });
            }
        }
        /// <summary>
        /// Sets GPIO pin at index to a set state. See datasheet (Command 0x22 for more details)
        /// </summary>
        /// <param name="gpioIndex">Index of GPIO pin</param>
        /// <param name="state">State of the pin</param>
        public void ConfigureGPIO(byte gpioIndex, byte state) {
            lock (this) {
                byte[] output = new byte[2];
                output[0] = gpioIndex;
                output[1] = state;

                disp.Transaction(new Packet() {
                    Type = 0x22,
                    Data = output
                });
            }
        }
        /// <summary>
        /// Sets the bi-colour LEDs
        /// </summary>
        /// <param name="index">LED index, 0-3 (where 0 is top LED)</param>
        /// <param name="green">Green intensity (0 off,- 100 on)</param>
        /// <param name="red">Red intensity (0 off,- 100 on)</param>
        public void SetLED(int index, int green, int red) {
            if (index < 0 || index > 3)
                throw new IndexOutOfRangeException("LED index must be between 0 and 3 (where 0 is the top LED)");
            if (green < 0 || green > 100)
                throw new ArgumentOutOfRangeException("Green intensity must be between 0 (off) and 100 (on)");
            if (red < 0 || red > 100)
                throw new ArgumentOutOfRangeException("Red intensity must be between 0 (off) and 100 (on)");
            switch (index) {
                case 0:
                    ConfigureGPIO(11, (byte)green);
                    ConfigureGPIO(12, (byte)red);
                    break;
                case 1:
                    ConfigureGPIO(9, (byte)green);
                    ConfigureGPIO(10, (byte)red);
                    break;
                case 2:
                    ConfigureGPIO(7, (byte)green);
                    ConfigureGPIO(8, (byte)red);
                    break;
                case 3:
                    ConfigureGPIO(5, (byte)green);
                    ConfigureGPIO(6, (byte)red);
                    break;
            }
        }
        /// <summary>
        /// Resets the LCD module: turn off all LEDs, set normal contrast, full backlight, clear all text
        /// </summary>
        public void Reset() {
            SetContrast(125);
            ClearScreen();
            SetBacklight(100);
            SetCursorPosition(0, 0);
            SetCursorStyle(CursorStyles.BlinkingBlock);
            for (byte index = 1; index <= 3; index++) {
                SetLED(index, 0, 0);
            }
        }
        public void ReadGPIO() {
            //TODO: implement!
        }
        #endregion

        public void Dispose() {
            if (exit) {
                throw new ObjectDisposedException("The API has already been disposed");
            }
            receivingAsync.Abort();
            disp.Stop();
            spLcd.Dispose();
            exit = true;
        }

        ~CrystalFontz635() {
            if (!exit)
                Dispose();
        }
    }
}
