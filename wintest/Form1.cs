using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace wintest {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private void crystalFontz6351_OnKeyDown(Crypton.Hardware.CrystalFontz.CrystalFontz635 api, Crypton.Hardware.CrystalFontz.KeyCodes pressedKeys) {
            label1.Text = pressedKeys.ToString();
        }
    }
}
