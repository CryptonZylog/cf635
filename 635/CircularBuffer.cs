using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypton.Hardware.CrystalFontz {
    public class CircularBuffer<T> {

        private T[] _buffer = null;
        int position = 0;

        public CircularBuffer(int size) {
            this._buffer = new T[size];
        }

        public int Position {
            get {
                return this.position;
            }
            set {
                if (value + 1 > this._buffer.Length)
                    value = 0;
                if (value < 0)
                    value = 0;
                this.position = value;
            }
        }

        public T Current {
            get {
                return this._buffer[position];
            }
            set {
                this._buffer[position] = value;
            }
        }

        public int Size {
            get {
                return this._buffer.Length;
            }
        }

        public void Push(T obj) {
            this._buffer[Position++] = obj;
        }

        public T Pull() {
            T obj = Current;
            Position++;
            return obj;
        }
    }
}
