using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BluetoothLEGONXT
{
    /// <summary>
    /// Extension methods used in Windows Forms applications.
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Sets the Text property and immediately refreshes the specified control.
        /// </summary>
        /// <param name="control">The control the refresh.</param>
        /// <param name="text">The text to display.</param>
        public static void SetText(this Control control, string text)
        {
            control.Text = text;
            control.Update();
        }

    }
}
