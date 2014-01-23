using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BluetoothLEGONXT
{
    public partial class SelectPortForm : Form
    {
        private PortHelpForm phf = new PortHelpForm();

        public string selectedPort = "";

        public SelectPortForm()
        {
            InitializeComponent();
        }

        private void portComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (portComboBox.SelectedItem!=null)
            {
                connectButton.Enabled = true;
            }

        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (portComboBox.SelectedItem != null)
            {
                connectButton.Enabled = true;
                selectedPort = portComboBox.SelectedValue.ToString();
            }
        }

        private void SelectPortForm_Load(object sender, EventArgs e)
        {
            portComboBox.DataSource = SerialPort.GetPortNames();
            portComboBox.Focus();
        }

        private void questionButton_Click(object sender, EventArgs e)
        {
            phf.ShowDialog();
        }
    }
}
