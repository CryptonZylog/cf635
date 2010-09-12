using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypton.Hardware.CrystalFontz {
    /// <summary>
    /// Thrown when there is a problem with a connection
    /// </summary>
    public class CommunicationException : Exception {
        public enum ErrorCodes {
            DeviceNotFound,
            GeneralError
        }
        /// <summary>
        /// Gets the communication code
        /// </summary>
        public ErrorCodes Code {
            get;
            private set;
        }
        public CommunicationException(string message, ErrorCodes code) : base(message) {
            this.Code = code;
        }
    }
}
