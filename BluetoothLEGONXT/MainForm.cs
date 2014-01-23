// Name: Blu NXT®
// Author: © Marjan Tanevski
// Date: 05.03.2013 
// Description: A remote controling nxt windows application.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NxtNet;

// controls used to display specific AForge streams, values etc.
using AForge.Controls;
// AForge assemblies getting video stream
using AForge.Video.DirectShow;
using AForge.Video;

using System.IO.Ports;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;

namespace BluetoothLEGONXT
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        #region Fields...

        private const string NAME = "Blu NXT";
        // the nxt brick we are connecting to
        public static Nxt nxt = new Nxt();
        // to check if is already connected or not connected
        private bool isConnected = false; 
        // camera state
        private static bool cameraConnected = false;
        // video related
        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice FinalVideo;

        private System.Drawing.Color connectedColor = System.Drawing.Color.MidnightBlue;
        private System.Drawing.Color disconnectedColor = System.Drawing.Color.Firebrick;
        private System.Drawing.Color loadingColor = System.Drawing.Color.DarkOrange;
        // port selection form
        SelectPortForm spf = new SelectPortForm();
        // about box
        AboutForm af = new AboutForm();
        // we will be using motors on port B and port C
        private MotorState mState = new MotorState();

        private MotorPort[] motors = { MotorPort.PortA, MotorPort.PortB, MotorPort.PortC, MotorPort.All };
        //C
        private MotorPort motorC = MotorPort.PortC;
        //B
        private MotorPort motorB = MotorPort.PortB;
        //A
        private MotorPort motorA = MotorPort.PortA;
        // gets the selected motor
        private MotorPort SelectedMotor
        {
            get
            {
                if (motorComboBox.SelectedIndex == 3) // enum value for MotorPort.All is 255
                {
                    return MotorPort.All; // return MotorPort.All if 3 is selected
                }
                else
                {
                    // otherwise return the selected index casted as MotorPort
                    return (MotorPort)motorComboBox.SelectedIndex;
                }
            }
        }

        // regulation modes
        private MotorRegulationMode[] regulationModes = new MotorRegulationMode[] {
            MotorRegulationMode.Idle,
            MotorRegulationMode.Speed,
            MotorRegulationMode.Sync };
        // run states
        private MotorRunState[] runStates = new MotorRunState[] {
            MotorRunState.Idle,
            MotorRunState.RampUp,
            MotorRunState.Running,
            MotorRunState.RampDown };

        // the touch sensor
        TouchSensor tSensor = new TouchSensor();


        // n/a and empty strings
        private readonly string NA = "N/A";
        //private readonly string EMPTY = "";


        #endregion

        #region Reset status

        private void ResetStatusControls()
        {

            // change the text and values of the status group box to n/a
            this.SetText(NAME+"®");
            nameTextBox.SetText(NA);
            versionLabel.SetText(NA);
            btadressLabel.SetText(NA);
            freeFlashLabel.SetText(NA);
            batteryLabel.SetText(NA);
            batteryProgressBar.Value = batteryProgressBar.Minimum;
            keepAliveLabel.SetText(NA);
            signalLabel.SetText(NA);

            label0.BackColor = SystemColors.Control;
            label1.BackColor = SystemColors.Control;
            label2.BackColor = SystemColors.Control;

            // disable all the group boxes
            statusGroupBox.Enabled = false;
            motorsGroupBox.Enabled = false;
            moveItGroupBox.Enabled = false;
            nfsGroupBox.Enabled = false;
            playGroupBox.Enabled = false;
            startProgramGroupBox.Enabled = false;
            messageGroupBox.Enabled = false;
            lOGToolStripMenuItem.Enabled = false;

            // remove the key event handler
            this.KeyDown -= MainForm_KeyDown;
            this.KeyUp -= MainForm_KeyUp;


        }

        #endregion

        #region Update status

        private void UpdateStatusControls()
        {
            // Retrieving NXT brick name.
            nameTextBox.SetText("Retrieving...");
            DeviceInfo deviceInfo = nxt.GetDeviceInfo();
            nameTextBox.SetText(deviceInfo.Name);

            // change window text = nxt name + port
            this.SetText(deviceInfo.Name + " via " + spf.selectedPort + " - " + NAME + "®");

            // Retrieving version information.
            versionLabel.SetText("Retrieving...");
            versionLabel.SetText(nxt.GetVersion().ToString());

            // Retrieving battery level.
            ushort batteryLevel = nxt.GetBatteryLevel();
            batteryProgressBar.Value = batteryLevel;
            batteryLabel.SetText(String.Format(CultureInfo.CurrentCulture, "{0} V", ((decimal)batteryLevel) / 1000));
            
            // Displaying Bluetooth address.
            btadressLabel.SetText( deviceInfo.BluetoothAddress.ToHexString());

            // Displaying available memory.
            freeFlashLabel.SetText(String.Format(CultureInfo.CurrentCulture, "{0:N0} bytes", deviceInfo.FreeUserFlash));

            // Retrieving and displaying the keep alive time.
            keepAliveLabel.SetText("Retrieving...");
            ulong keepAliveTime = nxt.KeepAlive();
            keepAliveLabel.SetText(String.Format(CultureInfo.CurrentCulture, "{0:N0} msec", keepAliveTime));

            // Retrieving and displaying the keep alive time.
            signalLabel.SetText("Retrieving...");
            signalLabel.SetText(String.Format(CultureInfo.CurrentCulture, "{0:N0}", deviceInfo.SignalStrength));

            // Retrieving and displaying signal in the signal bars
            switch (deviceInfo.SignalStrength) // take values from 0-3
            {
                case 0: // if is 0 then bars are full [blue][blue][blue]
                    label0.BackColor = SystemColors.Highlight;
                    label1.BackColor = SystemColors.Highlight;
                    label2.BackColor = SystemColors.Highlight;
                    break;
                case 1://[blue][blue][white]
                    label0.BackColor = SystemColors.Highlight;
                    label1.BackColor = SystemColors.Highlight;
                    label2.BackColor = SystemColors.Control;
                    break;
                case 2:// [blue][white][white]
                    label0.BackColor = SystemColors.Highlight;
                    label1.BackColor = SystemColors.Control;
                    label2.BackColor = SystemColors.Control;
                    break;

                // TODO: Add more cases if signal can be higher?

                default: // anything else will empty the bars
                    label0.BackColor = SystemColors.Control;
                    label1.BackColor = SystemColors.Control;
                    label2.BackColor = SystemColors.Control;
                    break;
            }


            statusGroupBox.Enabled = true;
            motorsGroupBox.Enabled = true;
            moveItGroupBox.Enabled = true;
            nfsGroupBox.Enabled = true;
            playGroupBox.Enabled = true;
            startProgramGroupBox.Enabled = true;
            messageGroupBox.Enabled = true;
            lOGToolStripMenuItem.Enabled = true;
            // add the key event handler
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;

        }


        #endregion

        #region Connect...

        private void Connect(string port)
        {
            // change the cursor to waiting
            Cursor.Current = Cursors.WaitCursor;

            mainStatusStrip.BackColor = loadingColor;

            // change bottom label text 
            connectionToolStripStatusLabel.Text = "Connecting...";

            // Connecting to the NXT.
            nxt = new Nxt();

            // check for null reference
            if (nxt != null)
            {
                try
                {
                    // Connecting via the selected serial port for communication
                    nxt.Connect(port);
                    // play a tone if there was a successfull connection
                    nxt.PlayTone(700, 200);
                    isConnected = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            
            UpdateStatusControls();

            connectionToolStripStatusLabel.Text = "Connected";

            mainStatusStrip.BackColor = connectedColor;

            Cursor.Current = Cursors.Default;
        }

        #endregion

        #region Disconnect...

        public void Disconnect()
        {
            Cursor.Current = Cursors.WaitCursor;

            mainStatusStrip.BackColor = loadingColor;

            connectionToolStripStatusLabel.Text = "Disconnecting...";

            if (nxt != null)
            {
                try
                {
                    // play a shorter but louder tone before disconnect
                    nxt.PlayTone(700, 100);
                    nxt.Disconnect();
                    isConnected = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            ResetStatusControls();

            connectionToolStripStatusLabel.Text = "Disconnected";

            mainStatusStrip.BackColor = disconnectedColor;

            Cursor.Current = Cursors.Default;

        }

        public void DisconnectCamera()
        {
            if (cameraConnected == true)
            {
                FinalVideo.Stop();
                cameraConnected = false;
                cameraButton.Text = "Connect";
                cameraButton.Update();
            }
        }

        #endregion

        #region Menu items, buttons, etc..

        private void MainForm_Load(object sender, EventArgs e)
        {

            begin:string[] _ports = SerialPort.GetPortNames();

            if (_ports.Length < 1)
            {
                if (MessageBox.Show("You are not connected to the nxt!\n\rDo you want to try again?", "Not connected!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    goto begin;
                }
                else
                {
                    this.Close();
                }
            }

            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo VideoCaptureDevice in VideoCaptureDevices)
            {
                cameraComboBox.Items.Add(VideoCaptureDevice.Name);
            }
            cameraComboBox.SelectedIndex = 0;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            switch (e.CloseReason)
            {
                case CloseReason.ApplicationExitCall:
                case CloseReason.MdiFormClosing:
                case CloseReason.None:
                case CloseReason.TaskManagerClosing:
                case CloseReason.WindowsShutDown:
                    if (isConnected)
                    {
                        this.Disconnect();
                    }
                        DisconnectCamera();
                    break;
                case CloseReason.FormOwnerClosing:
                case CloseReason.UserClosing:

                    var result = MessageBox.Show("Are you sure you want to quit?", "Quit?",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        if (isConnected)
                        {
                        Disconnect();
                        }
                        DisconnectCamera();
                    }
                    break;


                default:
                    this.Disconnect();
                    DisconnectCamera();
                    break;
            }
        }

        private void MainForm_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.S:
                case Keys.Space:
                    e.IsInputKey = true;
                    break;
                default:
                    break;
            }

        }


        #region Menu items

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();

            string port = "";

            if (ports.Length == 0)
            {
                MessageBox.Show("No bluetooth ports are currently in use", "NO PORTS");
            }
            else
            {
                foreach (string com in ports)
                {
                    if (!String.IsNullOrEmpty(com))
                    {
                        port = com;
                        try
                        {
                            nxt.Connect(com.ToString());
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            break;
                        }

                        if (!String.IsNullOrEmpty(nxt.GetVersion().ToString()))
                        {
                            MessageBox.Show("Success");
                            port = com;

                            this.UpdateStatusControls();
                            break;
                        }

                    }
                }
            }
            //try
            //{
            //    // change the cursor to waiting
            //    Cursor.Current = Cursors.WaitCursor;

            //    // change bottom label text 
            //    connectionToolStripStatusLabel.Text = "Connecting...";

            //    Connect(port);

            //    connectionToolStripStatusLabel.Text = "Connected";
            //    UpdateStatusControls();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
            //finally
            //{
            //    // when done change it back
            //    Cursor.Current = Cursors.Default;
            //}
        }

        private void connectPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                if (spf.ShowDialog() == DialogResult.OK)
                {
                    Connect(spf.selectedPort);
                    isConnected = true;
                }
            }
            else
            {
                MessageBox.Show("You are already connected to the NXT!", "CONNECT ISSUE");
            }

        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                this.Disconnect();
            }
            else
            {
                MessageBox.Show("You are not connected to the NXT!", "DISCONNECT ISSUE");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void lastErrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(nxt.LastError.ToString(), "Last Error");
        }


        private void lastResponseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(nxt.LastResponse.ToHexString(), "Last Response");
        }

        #endregion

        #region Status

        private void renameButton_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(nameTextBox.Text))
            {
                Cursor.Current = Cursors.WaitCursor;
                nxt.SetBrickName(this.nameTextBox.Text);
                this.SetText(this.nameTextBox.Text + " via " + spf.selectedPort + " - " + NAME + "®");
                Cursor.Current = Cursors.Default;

                MessageBox.Show("NXT renamed successfully.", "NXT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            UpdateStatusControls();
        }

        #endregion

        #region Sound

        private void playSoundButton_Click(object sender, EventArgs e)
        {

            try
            {

                // play a soundfile in the NXT the extension of the soundfile will be added
                // if it fails try adding the .rso extension
                // if loop is checked play indefinitely otherwise run only once
                nxt.PlaySoundFile(playSoundTextBox.Text.ToString(), loopCheckBox.Checked);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void stopSoundButton_Click(object sender, EventArgs e)
        {
            try
            {

                nxt.StopSoundPlayback();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EXCEPTION ERROR");
            }
        }

        private void freqToneButton_Click(object sender, EventArgs e)
        {
            try
            {
                nxt.PlayTone((int)(freqUpDown.Value),(int)(secUpDown.Value*1000));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Program

        private void runProgramButton_Click(object sender, EventArgs e)
        {
            try
            {
                nxt.StartProgram(programTextBox.Text.ToString());
                currentAppLabel.Text = nxt.GetCurrentProgramName();
                currentAppLabel.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void stopProgramButton_Click(object sender, EventArgs e)
        {
            try
            {
                nxt.StopProgram();
                currentAppLabel.Text = NA;
                currentAppLabel.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Message
        private void sendButton_Click(object sender, EventArgs e)
        {
            byte[] message = new byte[58];
            byte send = 0;
            send = Convert.ToByte(sendUpDown.Value);
            message = sendTextBox.Text.ToAsciiBytes();

            try
            {
                nxt.StartProgram("#0MailBluceiver.rxe");
                Thread.Sleep(1000);
                nxt.MessageWrite(send, message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void readButton_Click(object sender, EventArgs e)
        {
            //byte local = 0, remote = 0;

            //local = Convert.ToByte(localUpDown.Value);
            //remote = Convert.ToByte(remoteUpDown.Value);

            byte[] message;

            try
            {
                message = nxt.MessageRead(0, 0, removeCheckBox.Checked);
                readTextBox.Text = message.ToHexString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Motors
        private void resetMotorButton_Click(object sender, EventArgs e)
        {
            try
            {
                nxt.ResetMotorPosition(SelectedMotor, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void setMotorStateButton_Click(object sender, EventArgs e)
        {
            mState.Power = (sbyte)powerUpDown.Value;
            mState.TurnRatio = (sbyte)turnRatioUpDown.Value;

            mState.Mode = ((modeOnCheck.Checked) ? MotorModes.On : MotorModes.Coast) |
                ((modeBrakeCheck.Checked) ? MotorModes.Brake : MotorModes.Coast) |
                ((modeRegulatedCheck.Checked) ? MotorModes.Regulated : MotorModes.Coast);


            //mState.Regulation=regulationModeComboBox.SelectedIndex;
            //mState.RunState=runStateComboBox.SelectedIndex;

            mState.Regulation = regulationModes[regulationModeComboBox.SelectedIndex];
            mState.RunState = runStates[runStateComboBox.SelectedIndex];

            // tacho limit
            try
            {
                mState.TachoLimit = (uint)(Math.Max(0, Math.Min(100000, int.Parse(tachoLimitTextBox.Text))));
            }
            catch
            {
                mState.TachoLimit = 1000;
                tachoLimitTextBox.Text = mState.TachoLimit.ToString();
            }

            try
            {
                nxt.SetOutputState(SelectedMotor, mState.Power, mState.Mode, mState.Regulation, mState.TurnRatio, mState.RunState, mState.TachoLimit);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void getMotorStateButton_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            MotorState currentState = null;

            try
            {
                currentState = nxt.GetOutputState(SelectedMotor);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show(ex.Message, "EXCEPTION ERROR");

            }
            if (currentState != null)
            {
                //This property returns the internal position counter value for the specified port
                tachoCountTextBox.Text = currentState.TachoCount.ToString();
                //Current position relative to last programmed movement. Range: -2147483648-2147483647.
                //This property reports the block-relative position counter value for the specified port
                blockTachoCountTextBox.Text = currentState.BlockTachoCount.ToString();
                //This property returns the program-relative position counter value for the specified port
                //Current position relative to last reset of the rotation sensor for this motor.Range: -2147483648-2147483647.
                rotationCountTextBox.Text = currentState.RotationCount.ToString();
            }
            Cursor.Current = Cursors.Default;
        }
        #endregion

        #region Move it BC

        private void upButton_Click(object sender, EventArgs e)
        {
            nxt.SetOutputState(motorB, 100, MotorModes.On, MotorRegulationMode.Speed, 0, MotorRunState.Running, 0);
            nxt.SetOutputState(motorC, 100, MotorModes.On, MotorRegulationMode.Speed, 0, MotorRunState.Running, 0);
        }

        private void downButton_Click(object sender, EventArgs e)
        {
            nxt.SetOutputState(motorB, -100, MotorModes.On, MotorRegulationMode.Speed, 0, MotorRunState.Running, 0);
            nxt.SetOutputState(motorC, -100, MotorModes.On, MotorRegulationMode.Speed, 0, MotorRunState.Running, 0);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            nxt.SetOutputState(motorB, 0, MotorModes.Brake, MotorRegulationMode.Idle, 0, MotorRunState.Idle, 0);
            nxt.SetOutputState(motorC, 0, MotorModes.Brake, MotorRegulationMode.Idle, 0, MotorRunState.Idle, 0);
        }

        private void leftButton_Click(object sender, EventArgs e)
        {
            nxt.SetOutputState(motorC, 0, MotorModes.Brake, MotorRegulationMode.Idle, 50, MotorRunState.Idle, 0);
            nxt.SetOutputState(motorB, 100, MotorModes.On, MotorRegulationMode.Speed, 50, MotorRunState.Running, 0);
        }

        private void rightButton_Click(object sender, EventArgs e)
        {
            nxt.SetOutputState(motorB, 0, MotorModes.Brake, MotorRegulationMode.Idle, 50, MotorRunState.Idle, 0);
            nxt.SetOutputState(motorC, 100, MotorModes.On, MotorRegulationMode.Speed, 50, MotorRunState.Running, 0);
        }


        #endregion

        #region Need For Speed
        private void turboButton_Click(object sender, EventArgs e)
        {
            nxt.SetOutputState(MotorPort.All, 100, MotorModes.On, MotorRegulationMode.Speed, 100, MotorRunState.Running, 0);
        }

        private void driftButton_Click(object sender, EventArgs e)
        {
            nxt.SetOutputState(MotorPort.All,0,MotorModes.Brake, MotorRegulationMode.Idle, 0, MotorRunState.Idle, 0);
            nxt.ResetMotorPosition(MotorPort.All, false);
            nxt.ResetMotorPosition(MotorPort.All, true);
        }

        private void s1updateButton_Click(object sender, EventArgs e)
        {
            try
            {
                nxt.SetInputMode(SensorPort.Port1, SensorType.Switch, SensorMode.Boolean);
                // Read sensor values.
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EXCEPTION MESSAGE");
            }

            SensorState state = nxt.GetInputValues(SensorPort.Port1);
            // Display sensor values.
            s1nameLabel.Text = "Touch";
            s1normProgressBar.Value = state.NormalizedValue;
            s1normLabel.Text = state.NormalizedValue.ToString(CultureInfo.CurrentCulture);
            s1rawLabel.Text = state.RawValue.ToString(CultureInfo.CurrentCulture);
            s1scaledLabel.Text = state.ScaledValue == 1 ? "pressed" : "released";

        }


        #endregion


        void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData==Keys.W || e.KeyData == Keys.Up)
            {
                nxt.SetOutputState(motorB, 100, MotorModes.On, MotorRegulationMode.Speed, 0, MotorRunState.Running, 0);
                nxt.SetOutputState(motorC, 100, MotorModes.On, MotorRegulationMode.Speed, 0, MotorRunState.Running, 0);
            }
            if (e.KeyData == Keys.S || e.KeyData == Keys.Down)
            {
                nxt.SetOutputState(motorB, -100, MotorModes.On, MotorRegulationMode.Speed, 50, MotorRunState.Running, 0);
                nxt.SetOutputState(motorC, -100, MotorModes.On, MotorRegulationMode.Speed, 50, MotorRunState.Running, 0);
            }
            if (e.KeyData == Keys.Space)
            {
                nxt.SetOutputState(motorB, 0, MotorModes.Brake, MotorRegulationMode.Idle, 0, MotorRunState.Idle, 0);
                nxt.SetOutputState(motorC, 0, MotorModes.Brake, MotorRegulationMode.Idle, 0, MotorRunState.Idle, 0);
            }
            if (e.KeyData == Keys.A || e.KeyData == Keys.Left)
            {
                nxt.SetOutputState(motorC, 0, MotorModes.Brake, MotorRegulationMode.Idle, 50, MotorRunState.Idle, 0);
                nxt.SetOutputState(motorB, 100, MotorModes.On, MotorRegulationMode.Speed, 50, MotorRunState.Running, 0);
            }
            if (e.KeyData == Keys.D || e.KeyData == Keys.Right)
            {
                nxt.SetOutputState(motorB, 0, MotorModes.Brake, MotorRegulationMode.Idle, 50, MotorRunState.Idle, 0);
                nxt.SetOutputState(motorC, 100, MotorModes.On, MotorRegulationMode.Speed, 50, MotorRunState.Running, 0);
            }
        }

        void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.W || e.KeyData == Keys.Up ||
                e.KeyData == Keys.S || e.KeyData == Keys.Down ||
                e.KeyData == Keys.A || e.KeyData == Keys.Left ||
                e.KeyData == Keys.D || e.KeyData == Keys.Right ||
                e.KeyData==Keys.Space)
                
            {
                nxt.SetOutputState(motorB, 0, MotorModes.Brake, MotorRegulationMode.Idle, 0, MotorRunState.Idle, 0);
                nxt.ResetMotorPosition(motorB, false);
                nxt.SetOutputState(motorC, 0, MotorModes.Brake, MotorRegulationMode.Idle, 0, MotorRunState.Idle, 0);
                nxt.ResetMotorPosition(motorC, false);
            }

        }

        private void cameraButton_Click(object sender, EventArgs e)
        {
            
            if (cameraConnected == false)
            {
                FinalVideo = new VideoCaptureDevice(VideoCaptureDevices[cameraComboBox.SelectedIndex].MonikerString);
                cameraSourcePlayer.VideoSource = FinalVideo;
                FinalVideo.Start();
                cameraConnected = true;
                cameraButton.Text = "Disconnect";
                cameraButton.Update();
            }
            else
            {
                FinalVideo.Stop();
                cameraConnected = false;
                cameraButton.Text = "Connect";
                cameraButton.Update();
            }
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"help.html");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            af.ShowDialog();
        }

        #endregion

    }
}
