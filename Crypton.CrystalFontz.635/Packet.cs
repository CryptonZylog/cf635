using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypton.CrystalFontz.Cf635 {
    /// <summary>
    /// Specifies a packet
    /// </summary>
    public class Packet {

        /// <summary>
        /// Gets or sets the packet type
        /// </summary>
        public byte Type {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the packet data
        /// </summary>
        public byte[] Data {
            get;
            set;
        }

        /// <summary>
        /// Creates a new packet
        /// </summary>
        public Packet() {
            this.Type = 0;
            this.Data = new byte[0];
        }

        public override string ToString() {
            return base.ToString();
        }
    }
}
