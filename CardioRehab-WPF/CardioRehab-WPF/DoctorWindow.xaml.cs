using Coding4Fun.Kinect.KinectService.Common;
using Coding4Fun.Kinect.KinectService.Listeners;
using Coding4Fun.Kinect.KinectService.WpfClient;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ColorImageFormat = Microsoft.Kinect.ColorImageFormat;
using ColorImageFrame = Microsoft.Kinect.ColorImageFrame;

using DynamicDataDisplaySample.ECGViewModel;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.ComponentModel;
using NAudio.Wave;
using System.Drawing;

namespace CardioRehab_WPF
{
    /// <summary>
    /// Interaction logic for DoctorWindow.xaml
    /// </summary>
    public partial class DoctorWindow : Window, INotifyPropertyChanged
    {
        private DatabaseClass db;

        private int userid;
        private int patientid;
        private int patientAge;

        // < 60% of max heart rate
        private double minHrRange;
        // > 80% of max heart rate
        private double maxHrRange;
        // maximum heart rate for the patient
        // 220 - their age
        private int maxHr;
        // currently just set to 94%
        private int minO2 = 94;
        // max sys/dia bp currently just set to 170/110
        private int maxSys = 170;
        private int maxDia = 110;

        private bool hasBadData = false;

        private List<String> patientIPCollection = new List<String>();

        private String localIP;

        const int MAX_CLIENTS = 6;

        private AsyncCallback socketBioWorkerCallback;
        private List<Socket> bioSockets_list = new List<Socket>();
        private List<Socket> bioSocketWorkers_list = new List<Socket>();

        private KinectSensorChooser sensorChooser;

        private WriteableBitmap outputImage;
        private byte[] pixels = new byte[0];

        private ColorClient _videoClient;
        private ColorClient _videoClient2;

        private List<ColorListener> videoListenerCollection = new List<ColorListener>();

        WaveOut wo = new WaveOut();
        WaveFormat wf = new WaveFormat(16000, 1);
        BufferedWaveProvider mybufferwp = null;
        float oldVolume;

        private AudioClient _audioClient;
        //private AudioClient _audioClient2;

        private List<AudioListener> audioListenerCollection = new List<AudioListener>();
        //private static AudioListener _audioListener;

        private Random _Random;
        private int _maxECG;

        public int MaxECG
        {
            get { return _maxECG; }
            set { _maxECG = value; this.OnPropertyChanged("MaxECG"); }
        }

        private int _minECG;
        public int MinECG
        {
            get { return _minECG; }
            set { _minECG = value; this.OnPropertyChanged("MinECG"); }
        }

        public ECGPointCollection ecgPointCollection;
        DispatcherTimer updateCollectionTimer;
        private int i = 0;

        private FullScreenWindow fullscreenview = null;
        bool[] warningStatus = new bool[6];


        public DoctorWindow(int currentuser, DatabaseClass openDB)
        {
            db = openDB;
            userid = currentuser;

            GetLocalIP();
            CheckRecord();
            InitializeComponent();

            // patients send the biodata from port 5000-5005
            int[] ports = new int[6] { 5000, 5001, 5002, 5003, 5004, 5005 };
            InitializeBioSockets(ports);

        }

        private void DoctorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            int[] kinectOutPorts = new int[6] { 4531, 4532, 4533, 4534, 4535, 4536 };
            InitializeKinect(kinectOutPorts);
            //InitializeAudio();

            InitializeECG();

            this.DataContext = this;
        }

        /// <summary>
        /// class representation of the bio data of the patient as
        /// TCP packet
        /// </summary>
        class BioSocketPacket
        {
            public System.Net.Sockets.Socket packetSocket;
            public byte[] dataBuffer = new byte[666];
        }

        /// <summary>
        /// Keeps track of the state of each client connection
        /// </summary>
        class State
        {
            public int state_index = 0;
            public int port;
        }

        /// <summary>
        /// Keeps track of where the received data came from (ie. from which port)
        /// </summary>
        class ReceivedDataState
        {
            public BioSocketPacket socketData;
            public int port;
        }

        #region Helper functions

        private void GetLocalIP()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            int Ipcounter = 0;
            foreach (IPAddress addr in localIPs)
            {
                // if connected to wireless and ethernet --> then the length is 2 with
                // wireless IP address on index 1 and LAN on index 0 (only need wireless)
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (Ipcounter == 0)
                    {
                        //Console.WriteLine("localIP1: " + addr.ToString());
                        localIP = addr.ToString();
                    }
                    if (Ipcounter == 1)
                    {
                        //Console.WriteLine("localIP2: " + addr.ToString());
                        localIP = addr.ToString();

                    }
                    Ipcounter++;
                }
            }
        }

        /// <summary>
        /// Update the db record of the current doctor to record the current IP address.
        /// </summary>
        private void CheckRecord()
        {
            String query = "SELECT local_ip FROM doctor WHERE doctor_id=" + userid;

            // sometimes the db connection is closed (when going from expand to collapse)
            if(db.m_dbconnection.State != System.Data.ConnectionState.Open)
            {
                db.m_dbconnection.Open();
            }
            SQLiteCommand cmd = new SQLiteCommand(query, db.m_dbconnection);

            SQLiteDataReader reader = cmd.ExecuteReader();

            // current user does not have any IP addresses in the database record
            if (reader.HasRows)
            {
                SQLiteCommand updatecmd = new SQLiteCommand(db.m_dbconnection);
                updatecmd.CommandText = "UPDATE doctor SET local_ip = @local where doctor_id = @id";

                updatecmd.Parameters.AddWithValue("@local", localIP);
                updatecmd.Parameters.AddWithValue("@id", userid);

                updatecmd.ExecuteNonQuery();
                updatecmd.Dispose();
            }
            reader.Dispose();
            cmd.Dispose();
        }

        /// <summary>
        /// Currently just retrieves the birth date of the patient and calculate the age.
        /// </summary>
        private void GetPatientInfo()
        {
            String query = "SELECT strftime('%Y', date_birth) AS year FROM patient WHERE patient_id=" + patientid;

            // sometimes the db connection is closed (when going from expand to collapse)
            if (db.m_dbconnection.State != System.Data.ConnectionState.Open)
            {
                db.m_dbconnection.Open();
            }
            SQLiteCommand cmd = new SQLiteCommand(query, db.m_dbconnection);

            SQLiteDataReader reader = cmd.ExecuteReader();

            // current user does not have any IP addresses in the database record
            if (reader.HasRows)
            {
                while(reader.Read())
                {
                    patientAge = System.DateTime.Now.Year - Convert.ToInt32(reader["year"]);
                }
               
            }
            reader.Dispose();
            cmd.Dispose();
        }

        /// <summary>
        /// This method is called when the memo buttons are triggered and it 
        /// calls the PopupWindow UI that allows the user to leave notes
        /// about the selected patient during the session.
        /// </summary>
        /// <param name="index"> the index associated with the memo icon (which is associated with patients 1-6)</param>
        private void CreateMemoPopup(int index)
        {
            PopupWindow popup = new PopupWindow();
            popup.PatientLabel.Content = "Patient " + index.ToString();
            popup.NoteTime.Content = DateTime.Now.ToString("HH:mm:ss");
            popup.ShowDialog();
        }

        /// <summary>
        /// This method is called when the mute/unmute icons are clicked.  It
        /// togglees the icon images and also mute/unmute the audio for the selected
        /// patient.
        /// </summary>
        /// <param name="icon"> the selected muteIcon object </param>
        private void ToggleMuteIcon(System.Windows.Controls.Image icon)
        {
            String currentIcon = icon.Source.ToString();
            if (currentIcon.Contains("mute.png"))
            {
                icon.BeginInit();
                icon.Source = new BitmapImage(new Uri("mic.png", UriKind.RelativeOrAbsolute));
                icon.EndInit();
                wo.Volume = oldVolume;
                // add code to enable volume again
            }
            else
            {
                icon.BeginInit();
                icon.Source = new BitmapImage(new Uri("mute.png", UriKind.RelativeOrAbsolute));
                icon.EndInit();
                oldVolume = wo.Volume;
                wo.Volume = 0f;
                // add code to mute the patient
            }
        }

        /// <summary>
        /// This method toggles the uparrow.png and downarrow.png to let the doctor know the
        /// patients' biodata status.  If the heart rate is too high (> 80% of maximum HR) then
        /// the heart rate value is displayed as red color with the uparrow.png visible.
        /// (similar concept for oxygen sat and bp.)
        /// </summary>
        /// <param name="icon"> the image object placeholder in XAML file </param>
        /// <param name="currentLabel"> the label object in XAML file displaying the biodata value </param>
        /// <param name="newimg"> the name of the image file to be displayed </param>
        private void SetArrow(System.Windows.Controls.Image icon, Label currentLabel, String newimg)
        {
            //if the biodata is too low, then the font is displayed as blue.
            if(newimg == "downarrow.png")
            {
                currentLabel.Foreground = System.Windows.Media.Brushes.Blue;
            }
            //if the biodata is too high, then the font is displayed as blue.
            else
            {
                currentLabel.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
            // display the speciied images
            icon.BeginInit();
            icon.Source = new BitmapImage(new Uri(newimg, UriKind.RelativeOrAbsolute));
            icon.EndInit();
            icon.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// This method is triggered by expand button and it calls the FullScreenWindow object
        /// to display the selected patient view only. (instead of all 6 patients)
        /// </summary>
        /// <param name="patient"></param>
        private void ExpandedScreenView(int patient)
        {
            fullscreenview = new FullScreenWindow(userid, patient, db, this);
            this.Hide();
            fullscreenview.Show();
            fullscreenview.Closed += new EventHandler(ShowDoctorScreen);
        }

        /// <summary>
        /// Close the doctor window when the user triggers to close the
        /// application in fullscreen mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowDoctorScreen(object sender, EventArgs e)
        {
            this.Show();
        }

        /// <summary>
        /// This method is called from processData method below to process the heart rate
        /// data sent from the patient.  It displays the data received and also calls SetArrow function
        /// if the heart is > than 80% of max hr or if it is < than 60% of max hr.
        /// </summary>
        /// <param name="hrValue"> heart rate data received from the patient </param>
        /// <param name="hrValLabel"> label in XAML file to display the heart rate </param>
        /// <param name="hrWarnIcon"> image icon associated with high/low heart rate warning </param>
        /// <param name="patientBorder"> border object associated with the patient (border1 - 6) </param>
        private void ProcessHrData(String hrValue, Label hrValLabel, System.Windows.Controls.Image hrWarnIcon, Border patientBorder, int currentPatient)
        {
            hrValLabel.Content = hrValue.Trim();
            if (Convert.ToInt32(hrValue) < minHrRange)
            {
                SetArrow(hrWarnIcon, hrValLabel, "downarrow.png");
            }
            else if (Convert.ToInt32(hrValue) > maxHrRange)
            {
                hasBadData = true;
                SetArrow(hrWarnIcon, hrValLabel, "uparrow.png");
                patientBorder.BorderBrush = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                hrValLabel.Foreground = System.Windows.Media.Brushes.Black;
                hrWarnIcon.Visibility = Visibility.Hidden;

                if (!hasBadData)
                {
                    patientBorder.BorderBrush = System.Windows.Media.Brushes.DarkGreen;
                }
            }
            if (fullscreenview != null) 
            {
                if(fullscreenview.patientLabel == currentPatient)
                {
                    ModifyFulLScreenWindowHr(hrValue);
                }
            }
        }

        /// <summary>
        /// This method is called from processData method below to process the oxygen saturation level
        /// data sent from the patient.  It displays the data received and also calls SetArrow function
        /// if the oxygen sat % is < than 94% (mocked currently to test the functionality).
        /// </summary>
        /// <param name="oxValue"> oxygen sat % data received from the patient </param>
        /// <param name="oxValLabel"> label in XAML file to display the % sat </param>
        /// <param name="oxWarnIcon"> image icon associated with high/low % sat warning </param>
        /// <param name="patientBorder"> border object associated with the patient (border1 - 6) </param>
        private void ProcessOxData(String oxValue, Label oxValLabel, System.Windows.Controls.Image oxWarnIcon, Border patientBorder, int currentPatient)
        {
            oxValLabel.Content = oxValue.Trim();

            if (Convert.ToInt32(oxValue) < minO2)
            {
                hasBadData = true;
                SetArrow(oxWarnIcon, oxValLabel, "downarrow.png");
                patientBorder.BorderBrush = System.Windows.Media.Brushes.Blue;
            }
            else
            {
                oxValLabel.Foreground = System.Windows.Media.Brushes.Black;
                oxWarnIcon.Visibility = Visibility.Hidden;

                if (!hasBadData)
                {
                    patientBorder.BorderBrush = System.Windows.Media.Brushes.DarkGreen;
                }
            }

            if (fullscreenview != null)
            {
                if (fullscreenview.patientLabel == currentPatient)
                {
                    ModifyFullScreenWindowOx(oxValue);
                }
            }
        }

        /// <summary>
        /// This method is called from processData method below to process the blood pressure 
        /// data sent from the patient.  It displays the data received and also calls SetArrow function
        /// if the systolic bp is > 170 and if the diastolic bp is > 110 (mocked currently to test the functionality).
        /// </summary>
        /// <param name="sysValue"> systolic bp data received from the patient </param>
        /// <param name="diaValue"> diastolic bp data received from the patient </param>
        /// <param name="sysValLabel"> label in XAML file to display the systolic bp </param>
        /// <param name="diaValLabel"> label in XAML file to display the diastolic bp </param>
        /// <param name="bpWarnIcon"> image icon associated with high bp warning </param>
        /// <param name="patientBorder"> border object associated with the patient (border1 - 6) </param>
        private void ProcessBpData(String sysValue, String diaValue, Label sysValLabel, Label diaValLabel, System.Windows.Controls.Image bpWarnIcon, Border patientBorder, int currentPatient)
        {
            sysValLabel.Content = sysValue.Trim();
            diaValLabel.Content = diaValue.Trim();

            if (Convert.ToInt32(sysValue) > maxSys)
            {
                hasBadData = true;
                SetArrow(bpWarnIcon, sysValLabel, "uparrow.png");
                patientBorder.BorderBrush = System.Windows.Media.Brushes.OrangeRed;
            }
            else if (Convert.ToInt32(diaValue) > maxDia)
            {
                hasBadData = true;
                SetArrow(bpWarnIcon, diaValLabel, "uparrow.png");
                patientBorder.BorderBrush = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                sysValLabel.Foreground = System.Windows.Media.Brushes.Black;
                diaValLabel.Foreground = System.Windows.Media.Brushes.Black;
                bpWarnIcon.Visibility = Visibility.Hidden;

                if (!hasBadData)
                {
                    patientBorder.BorderBrush = System.Windows.Media.Brushes.DarkGreen;
                }
                hasBadData = false;
            }

            if (fullscreenview != null)
            {
                if (fullscreenview.patientLabel == currentPatient)
                {
                    ModifyFullScreenWindowBp(sysValue, diaValue);
                }
            }
        }

        /// <summary>
        /// This method is used to put the warning message about status of
        /// other patients when the doctor is in Full screen view mode of one patient.
        /// </summary>
        private void RaiseOtherPatientWarning()
        {
            int currentPatient = fullscreenview.patientLabel;
            String patientString = "";
            int warningindex = 1;

            while(warningindex < warningStatus.Length)
            {
                if((warningStatus[warningindex-1]) && (warningindex != currentPatient))
                {
                    patientString += "patient"+warningindex.ToString()+", ";
                }
                warningindex++;
            }

            if(warningStatus[warningStatus.Length-1])
            {
                patientString += "patient"+warningStatus.Length.ToString();
            }
            if(patientString != "")
            {
                fullscreenview.WarningLabel.Content = patientString + " need to be checked.";
                fullscreenview.WarningLabel.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                fullscreenview.WarningLabel.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        /// <summary>
        /// This method is called from the ProcessHrData to modify the label objects from the
        /// FullScreenWindow.  It calls SetArrow method to show the doctor the appropriate warning
        /// if the heart rate of the patient is abnormal. 
        /// </summary>
        /// <param name="hrValue"> heart rate value passed from the patient </param>
        private void ModifyFulLScreenWindowHr(String hrValue)
        {
            fullscreenview.hrValue.Content = hrValue.Trim();
            if (Convert.ToInt32(hrValue) < minHrRange)
            {
                SetArrow(fullscreenview.hrWarning, fullscreenview.hrValue, "downarrow.png");
            }
            else if (Convert.ToInt32(hrValue) > maxHrRange)
            {
                hasBadData = true;
                SetArrow(fullscreenview.hrWarning, fullscreenview.hrValue, "uparrow.png");
                fullscreenview.patientDataArea.BorderBrush = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                fullscreenview.hrValue.Foreground = System.Windows.Media.Brushes.Black;
                fullscreenview.hrWarning.Visibility = Visibility.Hidden;

                if (!hasBadData)
                {
                    fullscreenview.patientDataArea.BorderBrush = System.Windows.Media.Brushes.White;
                }
            }
        }

        /// <summary>
        /// This method is called from the ProcessOxData to modify the label objects from the
        /// FullScreenWindow.  It calls SetArrow method to show the doctor the appropriate warning
        /// if the oxgyen saturation of the patient is abnormal.
        /// </summary>
        /// <param name="oxValue"> oxygen saturation % data passed from the patient </param>
        private void ModifyFullScreenWindowOx(String oxValue)
        {
            fullscreenview.oxiValue.Content = oxValue.Trim();

            if (Convert.ToInt32(oxValue) < minO2)
            {
                SetArrow(fullscreenview.oxiWarning, fullscreenview.oxiValue, "downarrow.png");
                fullscreenview.patientDataArea.BorderBrush = System.Windows.Media.Brushes.Blue;
            }
            else
            {
                fullscreenview.oxiValue.Foreground = System.Windows.Media.Brushes.Black;
                fullscreenview.oxiWarning.Visibility = Visibility.Hidden;

                if (!hasBadData)
                {
                    fullscreenview.patientDataArea.BorderBrush = System.Windows.Media.Brushes.White;
                }
            }
        }

        /// <summary>
        /// This method is called from the ProcessBpData to modify the label objects from the
        /// FullScreenWindow.  It calls SetArrow method to show the doctor the appropriate warning
        /// if the blood pressure of the patient is abnormal.
        /// </summary>
        /// <param name="sysValue"> systolic value passed from the patient </param>
        /// <param name="diaValue"> diastolic value passed from the patient </param>
        private void ModifyFullScreenWindowBp(String sysValue, String diaValue)
        {
            fullscreenview.bpSysValue.Content = sysValue.Trim();
            fullscreenview.bpDiaValue.Content = diaValue.Trim();
            if (Convert.ToInt32(sysValue) > maxSys)
            {
                hasBadData = true;
                SetArrow(fullscreenview.bpWarning, fullscreenview.bpSysValue, "uparrow.png");
                fullscreenview.patientDataArea.BorderBrush = System.Windows.Media.Brushes.OrangeRed;
            }
            else if (Convert.ToInt32(diaValue) > maxDia)
            {
                hasBadData = true;
                SetArrow(fullscreenview.bpWarning, fullscreenview.bpDiaValue, "uparrow.png");
                fullscreenview.patientDataArea.BorderBrush = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                fullscreenview.bpSysValue.Foreground = System.Windows.Media.Brushes.Black;
                fullscreenview.bpDiaValue.Foreground = System.Windows.Media.Brushes.Black;
                fullscreenview.bpWarning.Visibility = Visibility.Hidden;
                if (!hasBadData)
                {
                    fullscreenview.patientDataArea.BorderBrush = System.Windows.Media.Brushes.White;
                }
                hasBadData = false;
            }
        }

        #endregion

        #region socket connection with patient for Bio information
        // code for this section was modified from 
        // http://social.msdn.microsoft.com/Forums/en-US/f3151296-8064-4358-98a3-7ecf3d2c474b/multiple-ports-listening-on-c?forum=ncl

        private void InitializeBioSockets(int[] portArray)
        {
            int index = 0;
            foreach (int portNum in portArray)
            {
                try
                {
                    State state = new State();
                    state.state_index = index++;
                    state.port = portNum;

                    //create listening socket
                    Socket currentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //Console.WriteLine("listening on :  " + localIP + ":" + portNum.ToString());
                    IPAddress addy = IPAddress.Parse(localIP);
                    IPEndPoint iplocal = new IPEndPoint(addy, portNum);
                    //bind to local IP Address
                    currentSocket.Bind(iplocal);
                    //start listening -- 4 is max connections queue, can be changed
                    currentSocket.Listen(4);
                    //create call back for client connections -- aka maybe recieve video here????
                    currentSocket.BeginAccept(new AsyncCallback(OnBioSocketConnection), state);
                    bioSockets_list.Add(currentSocket);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("error at InitializeBioSockets");
                    MessageBox.Show(e.Message);
                }
            }

        }

        /// <summary>
        /// Call back function that is invoked when the client is connected
        /// </summary>
        /// <param name="asyn"></param>
        private void OnBioSocketConnection(IAsyncResult asyn)
        {
            var state = asyn.AsyncState as State;
            var port = state.port;
            var currentSocket = bioSockets_list[state.state_index];

            try
            {
                bioSocketWorkers_list.Add(currentSocket.EndAccept(asyn));
                WaitForBioData(bioSocketWorkers_list[bioSocketWorkers_list.Count - 1], port);
                // start accepting connections from other clients
                currentSocket.BeginAccept(new AsyncCallback(OnBioSocketConnection), state);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnSocketConnection: Socket has been closed\n");
            }
            catch (SocketException e)
            {
                Console.WriteLine("error at OnBioSocketConnection");
                MessageBox.Show(e.Message);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("argument exception at OnBioSocketConnection");
                Console.WriteLine(ex.Message);
                this.Close();

            }

        }
        private void WaitForBioData(System.Net.Sockets.Socket soc, int port)
        {
            try
            {
                if (socketBioWorkerCallback == null)
                {
                    socketBioWorkerCallback = new AsyncCallback(OnBioDataReceived);
                }

                ReceivedDataState state = new ReceivedDataState();
                BioSocketPacket sockpkt = new BioSocketPacket();
                state.socketData = sockpkt;
                state.port = port;

                sockpkt.packetSocket = soc;
                //start listening for data
                soc.BeginReceive(sockpkt.dataBuffer, 0, sockpkt.dataBuffer.Length, SocketFlags.None, socketBioWorkerCallback, state);
            }
            catch (SocketException e)
            {
                Console.WriteLine("error at wait for biodata");
                MessageBox.Show(e.Message);
            }
        }

        private void OnBioDataReceived(IAsyncResult asyn)
        {
            try
            {
                var state = asyn.AsyncState as ReceivedDataState;
                var socketData = state.socketData;
                var port = state.port;

                //end receive
                int end = 0;
                end = socketData.packetSocket.EndReceive(asyn);

                //just getting simple text right now -- needs to be changed
                char[] chars = new char[end + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int len = d.GetChars(socketData.dataBuffer, 0, end, chars, 0);
                String receivedData = new String(chars);
                receivedData = Regex.Replace(receivedData, @"\t|\n|\r", " ");

                processData(receivedData);
                WaitForBioData(socketData.packetSocket, state.port);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException e)
            {
                Console.WriteLine("OnBioDataReceived SocketException");
                MessageBox.Show(e.Message);
            }

        }

        private void processData(String tmp)
        {
            String[] sentData = tmp.Split('|');
            String[] name = sentData[0].Split('-');

            for (int i = 1; i < sentData.Length; i++)
            {
                String[] data = sentData[i].Split(' ');

                // Set the UI in the main thread.
                this.Dispatcher.Invoke((Action)(() =>
                {
                    String currentLabel = name[0].Trim();
                    //Console.WriteLine("name at processData");
                    //Console.WriteLine(name[0]);
                    switch (currentLabel)
                    {
                        case "patient1":
                            if (data[0].Trim() == "HR")
                            {
                                ProcessHrData(data[1], hrValue1, hrWarning1, border1, 1);
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                ProcessOxData(data[1], oxiValue1, oxiWarning1, border1, 1); 
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                ProcessBpData(data[1], data[2], bpSysValue1, bpDiaValue1, bpWarning1, border1, 1);
                            }
                            if(hasBadData)
                            {
                                warningStatus[0] = true;
                            }
                            else
                            {
                                warningStatus[0] = false;
                            }
                            if(fullscreenview != null)
                            {
                                RaiseOtherPatientWarning();
                            }
                            break;

                        case "patient2":
                            if (data[0].Trim() == "HR")
                            {
                                ProcessHrData(data[1], hrValue2, hrWarning2, border2, 2);
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                ProcessOxData(data[1], oxiValue2, oxiWarning2, border2, 2); 
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                ProcessBpData(data[1], data[2], bpSysValue2, bpDiaValue2, bpWarning2, border2, 2);
                            }
                            if (hasBadData)
                            {
                                warningStatus[1] = true;
                            }
                            else
                            {
                                warningStatus[1] = false;
                            }
                            if (fullscreenview != null)
                            {
                                RaiseOtherPatientWarning();
                            }
                            break;

                        case "patient3":
                            if (data[0].Trim() == "HR")
                            {
                                ProcessHrData(data[1], hrValue3, hrWarning3, border3, 3);
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                ProcessOxData(data[1], oxiValue3, oxiWarning3, border3, 3); 
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                ProcessBpData(data[1], data[2], bpSysValue3, bpDiaValue3, bpWarning3, border3, 3);
                            }
                            if (hasBadData)
                            {
                                warningStatus[2] = true;
                            }
                            else
                            {
                                warningStatus[2] = false;
                            }
                            if (fullscreenview != null)
                            {
                                RaiseOtherPatientWarning();
                            }
                            break;

                        case "patient4":
                            if (data[0].Trim() == "HR")
                            {
                                ProcessHrData(data[1], hrValue4, hrWarning4, border4, 4);
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                ProcessOxData(data[1], oxiValue4, oxiWarning4, border4, 4); 
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                ProcessBpData(data[1], data[2], bpSysValue4, bpDiaValue4, bpWarning4, border4, 4);
                            }
                            if (hasBadData)
                            {
                                warningStatus[3] = true;
                            }
                            else
                            {
                                warningStatus[3] = false;
                            }
                            if (fullscreenview != null)
                            {
                                RaiseOtherPatientWarning();
                            }
                            break;

                        case "patient5":
                            if (data[0].Trim() == "HR")
                            {
                                ProcessHrData(data[1], hrValue5, hrWarning5, border5, 5);
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                ProcessOxData(data[1], oxiValue5, oxiWarning5, border5, 5); 
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                ProcessBpData(data[1], data[2], bpSysValue5, bpDiaValue5, bpWarning5, border5, 5);
                            }
                            if (hasBadData)
                            {
                                warningStatus[4] = true;
                            }
                            else
                            {
                                warningStatus[4] = false;
                            }
                            if (fullscreenview != null)
                            {
                                RaiseOtherPatientWarning();
                            }
                            break;

                        case "patient6":
                            if (data[0].Trim() == "HR")
                            {
                                ProcessHrData(data[1], hrValue6, hrWarning6, border6, 6);
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                ProcessOxData(data[1], oxiValue6, oxiWarning6, border6, 6); 
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                ProcessBpData(data[1], data[2], bpSysValue6, bpDiaValue6, bpWarning6, border6, 6);
                            }
                            if (hasBadData)
                            {
                                warningStatus[5] = true;
                            }
                            else
                            {
                                warningStatus[5] = false;
                            }
                            if (fullscreenview != null)
                            {
                                RaiseOtherPatientWarning();
                            }
                            break;
                        default:
                            if (name[0].Contains("start"))
                            {
                                String[] restofData = sentData[1].Split('-');
                                int index = Convert.ToInt32(restofData[0].Trim());
                                //Console.WriteLine(restofData[2]);
                                patientIPCollection.Insert(index - 1, restofData[2].Trim());
                                patientid = Convert.ToInt32(restofData[1]);

                                GetPatientInfo();
                                maxHr = 220 - patientAge;
                                minHrRange = maxHr * 0.6;
                                maxHrRange = maxHr * 0.8;

                            }
                            break;
                    }

                }));
            }
        }

        public void CloseSockets()
        {
            foreach (Socket m_mainSocket in bioSockets_list)
            {
                if (m_mainSocket != null)
                {
                    m_mainSocket.Close();
                }
            }
            for (int i = 0; i < bioSocketWorkers_list.Count; i++)
            {
                if (bioSocketWorkers_list[i] != null)
                {
                    bioSocketWorkers_list[i].Close();
                    bioSocketWorkers_list[i] = null;
                }
            }
        }
        #endregion

        #region Kinect
        private void InitializeKinect(int[] ports)
        {
            //Console.WriteLine("InitializeKinect");
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += sensorChooser_KinectChanged;
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

            // Don't try this unless there is a kinect.
            if (sensorChooser.Kinect != null)
            {
                _videoClient = new ColorClient();
                _videoClient.ColorFrameReady += _videoClient_ColorFrameReady;
                _videoClient.Connect("192.168.184.57", 4555);

                //_videoClient2 = new ColorClient();
                //_videoClient2.ColorFrameReady += _videoClient2_ColorFrameReady;
                //_videoClient2.Connect("192.168.184.19", 4556);

                foreach (int portNum in ports)
                {
                    ColorListener _videoListener = new ColorListener(this.sensorChooser.Kinect, portNum, ImageFormat.Jpeg);
                    _videoListener.Start();
                    videoListenerCollection.Add(_videoListener);
                }

                _audioClient = new AudioClient();
                _audioClient.AudioFrameReady += _audioClient_AudioFrameReady;
                _audioClient.Connect("192.168.184.57", 4565);

                //_audioClient2 = new AudioClient();
                //_audioClient2.AudioFrameReady += _audioClient2_AudioFrameReady;
                //_audioClient2.Connect("192.168.184.19", 4538);

                //for sending audio
                //_audioListener = new AudioListener(this.sensorChooser.Kinect, 4541);
                //_audioListener.Start();

                foreach (int portNum in ports)
                {
                    AudioListener _audioListener = new AudioListener(this.sensorChooser.Kinect, portNum + 10);
                    _audioListener.Start();
                    audioListenerCollection.Add(_audioListener);
                }

            }
        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">event arguments</param>
        void sensorChooser_KinectChanged(object sender, KinectChangedEventArgs e)
        {

            //MessageBox.Show(e.NewSensor == null ? "No Kinect" : e.NewSensor.Status.ToString());

            if (e.OldSensor != null)
            {
                try
                {
                    e.OldSensor.ColorStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                    Console.WriteLine("InvalidOperation Exception was thrown1.");
                }
                catch(ArgumentOutOfRangeException)
                {
                    Console.WriteLine("sensorChooser_kinectChanged1 argument out of range exception");
                }
            }

            if (e.NewSensor != null)
            {
                try
                {
                    e.NewSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    e.NewSensor.ColorFrameReady += NewSensor_ColorFrameReady;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                    Console.WriteLine("InvalidOperation Exception was thrown2.");
                }
                catch(ArgumentOutOfRangeException)
                {
                    Console.WriteLine("sensorChooser_kinectChanged2 argument out of range exception");
                }
            }
        }

        void NewSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if(fullscreenview != null)
                {
                    if (frame == null)
                    {
                        return;
                    }

                    if (pixels.Length == 0)
                    {
                        this.pixels = new byte[frame.PixelDataLength];
                    }
                    frame.CopyPixelDataTo(this.pixels);

                    outputImage = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);

                    outputImage.WritePixels(
                        new Int32Rect(0, 0, frame.Width, frame.Height), this.pixels, frame.Width * 4, 0);

                    fullscreenview.doctorFrame.Source = outputImage;

                    // force the garbase collector to remove outputImage --> otherwise, causes mem leak
                    outputImage = null;
                    GC.Collect();
                }
            };

        }

        #endregion

        #region mockECG

        public void InitializeECG()
        {
            ecgPointCollection = new ECGPointCollection();

            updateCollectionTimer = new DispatcherTimer();
            updateCollectionTimer.Interval = TimeSpan.FromMilliseconds(100);
            updateCollectionTimer.Tick += new EventHandler(updateCollectionTimer_Tick);
            updateCollectionTimer.Start();

            var ds = new EnumerableDataSource<ECGPoint>(ecgPointCollection);
            ds.SetXMapping(x => dateAxis.ConvertToDouble(x.Date));
            ds.SetYMapping(y => y.ECG);
            plotter.AddLineGraph(ds, Colors.SlateGray, 2, "ECG");
            plotter.VerticalAxis.Remove();
            MaxECG = 1;
            MinECG = -1;
        }

        void updateCollectionTimer_Tick(object sender, EventArgs e)
        {
            i++;
            _Random = new Random();
            ecgPointCollection.Add(new ECGPoint(_Random.NextDouble(), DateTime.Now));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Connect button triggers
        private void connect1_Click(object sender, RoutedEventArgs e)
        {
            if (sensorChooser.Kinect != null)
            {
                _videoClient.Connect("192.168.184.57", 4555);
                _audioClient.Connect("192.168.184.57", 4565);
                //Console.WriteLine("patient IP: " + patientIPCollection[0]);
                //if (patientIPCollection[0] != null)
                //{
                //    _videoClient.Connect(patientIPCollection[0], 4555);
                //}
                //if(patientIPCollection[0] != null)
                //{
                //    _audioClient.Connect(patientIPCollection[0], 4565);
                //}
            }
            connect1.Visibility = System.Windows.Visibility.Hidden;
        }

        private void connect2_Click(object sender, RoutedEventArgs e)
        {
            if (sensorChooser.Kinect != null)
            {
                if (patientIPCollection[0] != null)
                {
                    //_videoClient2.Connect("192.168.184.19", 4556);
                    //_audioClient2.Connect("192.168.184.19", 4538);
                }
            }
            connect2.Visibility = System.Windows.Visibility.Hidden;

        }

        void _videoClient_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            this.patientFrame1.Source = e.ColorFrame.BitmapImage;
            if(fullscreenview != null)
            {
                if(fullscreenview.patientLabel == 1)
                {
                    fullscreenview.patientFrame.Source = e.ColorFrame.BitmapImage;
                }
            }
        }

        void _videoClient2_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            this.patientFrame2.Source = e.ColorFrame.BitmapImage;
            if (fullscreenview != null)
            {
                if(fullscreenview.patientLabel == 2)
                {
                    fullscreenview.patientFrame.Source = e.ColorFrame.BitmapImage;
                }
            }

        }

        private void InitializeAudio()
        {
            wo.DesiredLatency = 100;
            mybufferwp = new BufferedWaveProvider(wf);
            mybufferwp.BufferDuration = TimeSpan.FromMinutes(5);
            wo.Init(mybufferwp);
            wo.Play();
        }

        void _audioClient_AudioFrameReady(object sender, AudioFrameReadyEventArgs e)
        {
            if (mybufferwp != null)
            {
                mybufferwp.AddSamples(e.AudioFrame.AudioData, 0, e.AudioFrame.AudioData.Length);
            }
        }

        //void _audioClient2_AudioFrameReady(object sender, AudioFrameReadyEventArgs e)
        //{
        //    if (mybufferwp != null)
        //    {
        //        mybufferwp.AddSamples(e.AudioFrame.AudioData, 0, e.AudioFrame.AudioData.Length);
        //    }
        //}

        #endregion

        #region Memo Button Triggers

        private void memo1_Click(object sender, RoutedEventArgs e)
        {
            CreateMemoPopup(1);
        }
        private void memo2_Click(object sender, RoutedEventArgs e)
        {
            CreateMemoPopup(2);
        }
        private void memo3_Click(object sender, RoutedEventArgs e)
        {
            CreateMemoPopup(3);
        }
        private void memo4_Click(object sender, RoutedEventArgs e)
        {
            CreateMemoPopup(4);
        }
        private void memo5_Click(object sender, RoutedEventArgs e)
        {
            CreateMemoPopup(5);
        }
        private void memo6_Click(object sender, RoutedEventArgs e)
        {
            CreateMemoPopup(6);
        }

        #endregion

        #region Mute Button Triggers

        private void mute1_Click(object sender, RoutedEventArgs e)
        {
            ToggleMuteIcon(muteIcon1);
        }
        private void mute2_Click(object sender, RoutedEventArgs e)
        {
            ToggleMuteIcon(muteIcon2);
        }
        private void mute3_Click(object sender, RoutedEventArgs e)
        {
            ToggleMuteIcon(muteIcon3);
        }
        private void mute4_Click(object sender, RoutedEventArgs e)
        {
            ToggleMuteIcon(muteIcon4);
        }
        private void mute5_Click(object sender, RoutedEventArgs e)
        {
            ToggleMuteIcon(muteIcon5);
        }
        private void mute6_Click(object sender, RoutedEventArgs e)
        {
            ToggleMuteIcon(muteIcon6);
        }

        #endregion

        #region Expand Button Triggers

        private void expand1_Click(object sender, RoutedEventArgs e)
        {
            ExpandedScreenView(1);
        }
        private void expand2_Click(object sender, RoutedEventArgs e)
        {
            ExpandedScreenView(2);
        }
        private void expand3_Click(object sender, RoutedEventArgs e)
        {
            ExpandedScreenView(3);
        }
        private void expand4_Click(object sender, RoutedEventArgs e)
        {
            ExpandedScreenView(4);
        }
        private void expand5_Click(object sender, RoutedEventArgs e)
        {
            ExpandedScreenView(5);
        }
        private void expand6_Click(object sender, RoutedEventArgs e)
        {
            ExpandedScreenView(6);
        }


        #endregion

    }
}




