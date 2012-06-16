using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.IO;

namespace Crypton.CrystalFontz.Cf635 {
    partial class Display {

        private SerialPort serialClient = null;
        const byte MAX_CMD = 35;
        const byte MAX_LENGTH = 22;

        #region Thread fields
        private Thread thReceiveWorker = null;
        private Thread thSenderWorker = null;
        #endregion

        #region Packet Lists
        private List<Packet> sendQueue = new List<Packet>();
        #endregion

        #region Thread sync
        private bool runSendThread = false;
        private bool runRecvThread = false;
        private const int threadAbortTimeout = 500;
        private const int threadThrottleWait = 2;
        #endregion

        #region Thread Run methods
        private void sendQueueImpl() {
            try {
                while (this.runSendThread) {
                    if (this.serialClient != null) {
                        lock (this.sendQueue) {
                            if (this.sendQueue.Count > 0) {
                                // send packets
                                BinaryWriter bw = new BinaryWriter(this.serialClient.BaseStream);
                                for (int i = 0; i < this.sendQueue.Count; i++) {
                                    Packet packet = this.sendQueue[i];
                                    byte[] data = PacketUtility.GetBytes(packet);
                                    bw.Write(data, 0, data.Length);
                                    this.sendQueue.Remove(packet);
                                    i--;
                                }
                            }
                        }
                    }
                    Thread.Sleep(threadThrottleWait);
                }
            }
            catch (ThreadAbortException) { }
        }

        private void recvQueueImpl() {
            try {
                while (this.runRecvThread) {
                    if (this.serialClient != null) {
                        BinaryReader br = new BinaryReader(this.serialClient.BaseStream);
                        while (this.serialClient.BytesToRead > 0) {
                            Packet packet = new Packet();
                            // read packet
                            packet.Type = br.ReadByte();
                            if ((packet.Type & 0x3f) > MAX_CMD) {
                                byte dataLength = br.ReadByte();
                                if (dataLength > MAX_LENGTH) {
                                    packet.Data = br.ReadBytes(dataLength);
                                    ushort crc = br.ReadUInt16();
                                    if (PacketUtility.Validate(packet, crc)) {
                                        Action<Packet> dg = new Action<Packet>(this.OnPacketReceived);
                                        dg.BeginInvoke(packet, null, null);
                                    }
                                }
                            }
                        }
                    }
                    Thread.Sleep(threadThrottleWait);
                }
            }
            catch (ThreadAbortException) { }
        }
        #endregion

        #region Thread control methods
        /// <summary>
        /// Starts the connection threads
        /// </summary>
        protected void startConnectionThreads() {
            if ((this.thReceiveWorker != null && this.thSenderWorker.IsAlive) || (this.thSenderWorker != null && this.thSenderWorker.IsAlive)) {
                throw new InvalidOperationException("The threads are currently running");
            }

            this.runRecvThread = true;
            this.runSendThread = true;

            this.thReceiveWorker = new Thread(this.recvQueueImpl);
            this.thSenderWorker = new Thread(this.sendQueueImpl);

            this.thReceiveWorker.Start();
            this.thSenderWorker.Start();
        }
        /// <summary>
        /// Stops the connection threads
        /// </summary>
        protected void stopConnectionThreads() {
            if (this.thReceiveWorker == null || this.thSenderWorker == null) {
                throw new InvalidOperationException("The thread states are invalid (one of the threads is null), perhaps the threads have been terminated already?");
            }

            this.runRecvThread = false;
            this.runSendThread = false;

            if (this.thReceiveWorker.Join(threadAbortTimeout) == false) {
                this.thReceiveWorker.Abort();
            }

            if (this.thSenderWorker.Join(threadAbortTimeout) == false) {
                this.thSenderWorker.Abort();
            }

            this.thReceiveWorker = null;
            this.thSenderWorker = null;
        }
        #endregion

        #region Sending packet
        /// <summary>
        /// Adds a new packet to the queue
        /// </summary>
        /// <param name="newPacket">Packet to add</param>
        protected void sendPacket(Packet newPacket) {
            lock (this.sendQueue) {
                this.sendQueue.Add(newPacket);
            }
        }
        /// <summary>
        /// Sends a new packet to the display
        /// </summary>
        /// <param name="type">The packet type</param>
        /// <param name="data">Packet data</param>
        protected void sendPacket(byte type, byte[] data) {
            this.sendPacket(new Packet() { Type = type, Data = data });
        }
        #endregion

        #region Receiving Packet Filter
        private void OnPacketReceived(Packet packet) {

        }
        #endregion

    }
}
