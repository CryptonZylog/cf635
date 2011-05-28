using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Crypton.Hardware.CrystalFontz {
    class Dispatcher {
        private CrystalFontz635 cf635 = null;

        const int MAXSZ = 512;
        const int CLEANCNT = 16;

        private CircularBuffer<Packet> inbox = new CircularBuffer<Packet>(64);
        private Queue<Packet> outbox = new Queue<Packet>();

        private AutoResetEvent thOutboxQueue = new AutoResetEvent(false);
        private AutoResetEvent thInboxQueue = new AutoResetEvent(false);

        private Thread thInbox = null;
        private Thread thOutbox = null;

        public Dispatcher(CrystalFontz635 cf635) {
            this.cf635 = cf635;
            thInbox = new Thread(new ThreadStart(InboxQueue));
            thInbox.Priority = ThreadPriority.Highest;
            thOutbox = new Thread(new ThreadStart(OutboxQueue));
            thOutbox.Priority = ThreadPriority.Highest;

            thInbox.Start();
            thOutbox.Start();
        }

        private void OutboxQueue() {
            try {
                while (true) {
                    lock (this.outbox) {
                        while (this.outbox.Count > 0) {
                            var packet = this.outbox.Dequeue();
                            PacketBuilder.SendPacket(this.cf635.spLcd, packet);
                        }
                    }
                    thOutboxQueue.WaitOne();
                }
            }
            catch (ThreadAbortException) {
            }
        }

        private void InboxQueue() {
            try {
                while (true) {
                    var packet = default(Packet);
                    lock (cf635.spLcd) {
                        if (cf635.spLcd.BytesToRead > 0) {
                            packet = PacketBuilder.ReceivePacket(cf635.spLcd);
                        }
                    }
                    if (packet != null) {
                        lock (this.inbox) {
                            this.inbox.Push(packet);
                        }
                    }
                    thInboxQueue.Set();
                    Thread.Sleep(1);
                }
            }
            catch (ThreadAbortException) {
            }
        }

        public void SchedulePacket(Packet outgoing) {
            lock (this.outbox) {
                this.outbox.Enqueue(outgoing);
            }
            thOutboxQueue.Set();
        }

        public bool WaitForReturn(byte expectingType, out Packet response) {
            bool success = false;
            response = new Packet();
            while (true) {
                success = thInboxQueue.WaitOne(2000);
                response.IsValid = false;
                lock (this.inbox) {
                    for (int i = 0; i < this.inbox.Size; i++) {
                        var packet = this.inbox.Current;
                        if (packet != null && packet.Type == expectingType) {
                            response = packet;
                            success = true;
                            this.inbox.Current = null;
                            break;
                        }
                        this.inbox.Position++;
                    }
                }
                if (response.IsValid)
                    break;
            }
            return success;
        }

        public Packet Transaction(Packet outgoing, byte expectingType) {
            SchedulePacket(outgoing);
            Packet ret;
            bool success = WaitForReturn(expectingType, out ret);
            return ret;
        }

        public Packet Transaction(Packet outgoing) {
            SchedulePacket(outgoing);
            Packet ret;
            bool success = WaitForReturn((byte)(0x40 | outgoing.Type), out ret);
            return ret;
        }

        public void Stop() {
            thInbox.Abort();
            thOutbox.Abort();
        }
    }
}
