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
using DepthImageFormat = Microsoft.Kinect.DepthImageFormat;

using DynamicDataDisplaySample.ECGViewModel;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.ComponentModel;

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
        private String patientIP = "142.244.115.209";
        private String localIP;

        const int MAX_CLIENTS = 6;

        private AsyncCallback socketBioWorkerCallback;
        private List<Socket> bioSockets_list = new List<Socket>();
        private List<Socket> bioSocketWorkers_list = new List<Socket>();

        private KinectSensorChooser sensorChooser;

        bool _isInit;
        private WriteableBitmap outputImage;
        private WriteableBitmap inputImage1;
        private WriteableBitmap inputImage2;
        private WriteableBitmap inputImage3;
        private WriteableBitmap inputImage4;
        private WriteableBitmap inputImage5;
        private WriteableBitmap inputImage6;
        private byte[] pixels = new byte[0];

        private ColorClient _videoClient;
        //private AudioClient _audioClient;

        private static ColorListener _videoListener;

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


        public DoctorWindow(int currentuser, DatabaseClass openDB)
        {
            db = openDB;
            userid = currentuser;

            GetLocalIP();
            CheckRecord();
            InitializeComponent();

            // patients send the biodata from port 5000-5005
            int[] ports = new int[6]{5000,5001,5002,5003,5004,5005};
            InitializeBioSockets(ports);

            InitializeKinect();

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
                    if(Ipcounter == 0)
                    {
                        localIP = addr.ToString();
                    }
                    if (Ipcounter == 1)
                    {
                        localIP = addr.ToString();

                    }
                    break;
                }
            }
        }
        private void CheckRecord()
        {
            String query = "SELECT local_ip FROM doctor WHERE doctor_id=" + userid;
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
        /// Retrieves the local IP address of the patient from the
        /// patient database table.
        /// </summary>
        private void GetPatientIP()
        {
            String query = "SELECT wireless_ip FROM patient WHERE patient_id=" + patientid;
            SQLiteCommand cmd = new SQLiteCommand(query, db.m_dbconnection);

            SQLiteDataReader reader = cmd.ExecuteReader();

            // current user does not have any IP addresses in the database record
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    patientIP = reader["wireless_ip"].ToString();
                    break;
                }
            }
            reader.Dispose();
            cmd.Dispose();
        }

        #endregion

        #region socket connection with patient for Bio information
        // code for this section was modified from 
        // http://social.msdn.microsoft.com/Forums/en-US/f3151296-8064-4358-98a3-7ecf3d2c474b/multiple-ports-listening-on-c?forum=ncl

        private void InitializeBioSockets(int[] portArray)
        {
            int index = 0;
            foreach(int portNum in portArray)
            {
                try
                {
                    State state = new State();
                    state.state_index = index++;
                    state.port = portNum;

                    //create listening socket
                    Socket currentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                this.Close() ;
 
            }

        }
        private void WaitForBioData(System.Net.Sockets.Socket soc, int port)
        {
            Console.WriteLine("wait for bio data");
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
                Console.WriteLine("data here!");
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
                
                // DEBUGGING
                Console.WriteLine("data passed");
                Console.WriteLine(receivedData);

                processData(receivedData);
                WaitForBioData(socketData.packetSocket, state.port);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException e)
            {
                MessageBox.Show(e.Message);
            }

        }

        private void processData(String tmp)
        {
            String[] sentData = tmp.Split('|');
            String[] name = sentData[0].Split('-');
            patientid = Convert.ToInt32(name[1]);

            Console.WriteLine(patientid);

            for (int i = 1; i < sentData.Length; i++)
            {
                String[] data = sentData[i].Split(' ');

                // Set the UI in the main thread.
                this.Dispatcher.Invoke((Action)(() =>
                {
                    switch (name[0].Trim())
                    {
                        case "patient1":
                            if (data[0].Trim() == "HR")
                            {
                                hrValue1.Content = data[1] + " bpm";
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                oxiValue1.Content = data[1] + "%";
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                bpValue1.Content = data[1] + "/" + data[2];
                            }
                            break;
                        case "patient2":
                            if (data[0].Trim() == "HR")
                            {
                                hrValue2.Content = data[1] + " bpm";
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                oxiValue2.Content = data[1] + "%";
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                bpValue2.Content = data[1] + "/" + data[2];
                            }
                            break;
                        case "patient3":
                            if (data[0].Trim() == "HR")
                            {
                                hrValue3.Content = data[1] + " bpm";
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                oxiValue3.Content = data[1] + "%";
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                bpValue3.Content = data[1] + "/" + data[2];
                            }
                            break;
                        case "patient4":
                            if (data[0].Trim() == "HR")
                            {
                                hrValue4.Content = data[1] + " bpm";
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                oxiValue4.Content = data[1] + "%";
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                bpValue4.Content = data[1] + "/" + data[2];
                            }
                            break;
                        case "patient5":
                            if (data[0].Trim() == "HR")
                            {
                                hrValue5.Content = data[1] + " bpm";
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                oxiValue5.Content = data[1] + "%";
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                bpValue5.Content = data[1] + "/" + data[2];
                            }
                            break;
                        case "patient6":
                            if (data[0].Trim() == "HR")
                            {
                                hrValue6.Content = data[1] + " bpm";
                            }
                            else if (data[0].Trim() == "OX")
                            {
                                oxiValue6.Content = data[1] + "%";
                            }
                            else if (data[0].Trim() == "BP")
                            {
                                bpValue6.Content = data[1] + "/" + data[2];
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
        private void InitializeKinect()
        {
            Console.WriteLine("InitializeKinect");
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += sensorChooser_KinectChanged;
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

            // Don't try this unless there is a kinect.
            if ((sensorChooser.Kinect != null) && (patientIP != null))
            {
                //// Receiving video from patient1.
                _videoClient = new ColorClient();
                _videoClient.ColorFrameReady += _videoClient_ColorFrameReady;
                _videoClient.Connect(patientIP, 4555);

                if (_videoClient.IsConnected)
                {
                    connect1.Visibility = System.Windows.Visibility.Hidden;
                }

                //_videoClient2 = new ColorClient();
                //_videoClient2.ColorFrameReady += _videoClient2_ColorFrameReady;
                //_videoClient2.Connect("192.168.184.39", 4556);


                // kinect sending video out on port 4531
                _videoListener = new ColorListener(this.sensorChooser.Kinect, 4531, ImageFormat.Jpeg);
                _videoListener.Start();

                /*/ Recieving audio from patient.
                _audioClient = new AudioClient();
                _audioClient.AudioFrameReady += _audioClient_AudioFrameReady;
                _audioClient.Connect("192.168.184.19", 4533); */
            }
        }

        void _videoClient_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            this.patientFrame1.Source = e.ColorFrame.BitmapImage;
        }

        void _videoClient2_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            this.patientFrame2.Source = e.ColorFrame.BitmapImage;
            
        }

       /* private void InitializeAudio()
        {
            mybufferwp = new BufferedWaveProvider(wf);
            mybufferwp.BufferDuration = TimeSpan.FromMinutes(5);
            wo.Init(mybufferwp);
            wo.Play();
        }*/

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
                    e.OldSensor.DepthStream.Range = DepthRange.Default;
                    e.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    e.OldSensor.DepthStream.Disable();
                    e.OldSensor.SkeletonStream.Disable();
                    e.OldSensor.ColorStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                    Console.WriteLine("InvalidOperation Exception was thrown1.");
                }
            }

            if (e.NewSensor != null)
            {
                try
                {
                    e.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    e.NewSensor.SkeletonStream.Enable();
                    e.NewSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                    try
                    {
                        e.NewSensor.DepthStream.Range = DepthRange.Near;
                        e.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;

                        //seated mode could come in handy on the bike -- uncomment below
                        //e.NewSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        e.NewSensor.DepthStream.Range = DepthRange.Default;
                        e.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                    Console.WriteLine("InvalidOperation Exception was thrown2.");
                }
            }
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
                if (!_videoClient.IsConnected)
                {
                    _videoClient.Connect(patientIP, 4555);
                }

               if(_videoClient.IsConnected)
               {
                   connect1.Visibility = System.Windows.Visibility.Hidden;
               }
            }
        }
        #endregion
    }
}
