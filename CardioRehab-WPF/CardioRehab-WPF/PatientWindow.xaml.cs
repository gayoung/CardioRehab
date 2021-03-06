﻿using Coding4Fun.Kinect.KinectService.Common;
using Coding4Fun.Kinect.KinectService.Listeners;
using Coding4Fun.Kinect.KinectService.WpfClient;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using mshtml;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ColorImageFormat = Microsoft.Kinect.ColorImageFormat;
using ColorImageFrame = Microsoft.Kinect.ColorImageFrame;

namespace CardioRehab_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PatientWindow : Window
    {
        private DatabaseClass db;

        private int user;
        private int age;
        // currently under assumption that
        // first output from the loop is LAN and second is wireless
        private String doctorIp = "192.168.184.9";
        private String patientLocalIp;
        private String wirelessIP;

        private int patientIndex;
        private DispatcherTimer mimicPhoneTimer;

        private AsyncCallback socketBioWorkerCallback;
        public Socket socketBioListener;
        public Socket bioSocketWorker;
        public Socket socketToClinician = null;
        public Socket unitySocketListener;
        public Socket unitySocketWorker = null;

        //kinect sensor 
        private KinectSensorChooser sensorChooser;

        //kinect listeners
        private static ColorListener _videoListener;
        private static AudioListener _audioListener;

        //kinect clients
        private ColorClient _videoClient;

        private WriteableBitmap outputImage;
        private byte[] pixels = new byte[0];

        WaveOut wo = new WaveOut();
        WaveFormat wf = new WaveFormat(16000, 1);
        BufferedWaveProvider mybufferwp = null;

        private AudioClient _audioClient;

        TextWriter _writer;

        /// <summary>
        /// Constructor for this class
        /// </summary>
        /// <param name="currentuser"> database ID for the current user</param>
        public PatientWindow(int chosen, int currentuser, DatabaseClass openDB)
        {
            db = openDB;
            user = currentuser;
            patientIndex = chosen + 1;

            GetLocalIP();
            CheckRecord();
            InitializeComponent();

            //_writer = new TextBoxStreamWriter(txtMessage);
            //Console.SetOut(_writer);

            ConnectToUnity();
            InitializeVR();
            InitializeBioSockets();
            CreateSocketConnection();

            // disable this function if InitializeBioSockets function is active
            //InitTimer();
        }

        private void PatientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeKinect();
            //InitializeAudio();

        }

        /// <summary>
        /// class representation of the bio data of the patient as
        /// TCP packet
        /// </summary>
        class BioSocketPacket
        {
            public System.Net.Sockets.Socket packetSocket;
            public byte[] dataBuffer = new byte[1024];
        }

        #region Helper functions

        private void InitializeVR()
        {
            String debugpath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            String projectpath = debugpath.Replace("\\CardioRehab-WPF\\CardioRehab-WPF\\bin\\Debug\\CardioRehab-WPF.exe", "");
            projectpath = projectpath + "\\BikeVR\\BikeVR.html";

            // make this path relative later
            try
            {
                UnityWindow.Navigate(projectpath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error at InitializeVR");
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// This method calls mimicPhoneTimer_Tick method which calls the PhoneTestMethod
        /// to mimic the data sent by the phone at port 4444.
        /// 
        /// Used to test the application without having access to 6 phones to mock 6 proper patients.
        /// 
        /// The code was modified from
        /// http://stackoverflow.com/questions/6169288/execute-specified-function-every-x-seconds
        /// </summary>
        public void InitTimer()
        {
            mimicPhoneTimer = new System.Windows.Threading.DispatcherTimer();
            mimicPhoneTimer.Tick += new EventHandler(mimicPhoneTimer_Tick);
            mimicPhoneTimer.Interval = new TimeSpan(0, 0, 2); ; // 2 seconds
            mimicPhoneTimer.Start();
        }

        /// <summary>
        /// Function called by the timer class.
        /// 
        /// This method is called every 2 seconds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mimicPhoneTimer_Tick(object sender, EventArgs e)
        {
            PhoneTestMethod();
        }

        /// <summary>
        /// This method updates the database record of the patient with the latest
        /// wifi and local IP addresses.
        /// </summary>
        private void CheckRecord()
        {
            String query = "SELECT wireless_ip FROM patient WHERE patient_id=" + user;
            SQLiteCommand cmd = new SQLiteCommand(query, db.m_dbconnection);

            SQLiteDataReader reader = cmd.ExecuteReader();

            // current user does not have any IP addresses in the database record
            if (reader.HasRows)
            {
                //Console.WriteLine("at update");
                SQLiteCommand updatecmd = new SQLiteCommand(db.m_dbconnection);
                updatecmd.CommandText = "UPDATE patient SET wireless_ip = @wireless, local_ip = @local where patient_id = @id";

                updatecmd.Parameters.AddWithValue("@wireless", wirelessIP);
                updatecmd.Parameters.AddWithValue("@local", patientLocalIp);
                updatecmd.Parameters.AddWithValue("@id", user);

                updatecmd.ExecuteNonQuery();
                updatecmd.Dispose();
            }
            reader.Dispose();
            cmd.Dispose();
        }

        /// <summary>
        /// Retrieves the local IP address of the doctor from the
        /// doctor database table.
        /// </summary>
        private void GetDoctoIP()
        {
            String query = "SELECT doctor_id FROM patient WHERE patient_id=" + user;
            SQLiteCommand cmd = new SQLiteCommand(query, db.m_dbconnection);

            SQLiteDataReader reader = cmd.ExecuteReader();

            // current user does not have any IP addresses in the database record
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    String docId = reader["doctor_id"].ToString();
                    String docquery = "SELECT local_ip FROM doctor WHERE doctor_id=" + docId;

                    SQLiteCommand doccmd = new SQLiteCommand(docquery, db.m_dbconnection);
                    SQLiteDataReader docResult = doccmd.ExecuteReader();

                    if (docResult.HasRows)
                    {
                        while (docResult.Read())
                        {
                            doctorIp = docResult["local_ip"].ToString();
                            break;
                        }
                    }
                    else
                    {
                        MessageBox.Show("doctor does not have an IP in the db");
                    }
                    docResult.Dispose();
                    doccmd.Dispose();
                    break;

                }
            }
            reader.Dispose();
            cmd.Dispose();
        }

        /// <summary>
        /// This method is used to get both LAN and wireless IP of the current user
        /// </summary>
        private void GetLocalIP()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            int Ipcounter = 0;
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    //Console.WriteLine("All the IP addresses read: ");
                    //Console.WriteLine(addr.ToString());
                    if (Ipcounter == 0)
                    {
                        //patientLocalIp = addr.ToString();
                        wirelessIP = addr.ToString();
                    }
                    else if (Ipcounter == 1)
                    {
                        wirelessIP = addr.ToString();
                    }
                    Ipcounter++;
                }
            }
        }

        /// <summary>
        /// method to be used to test the code without the phone
        /// </summary>
        private void PhoneTestMethod()
        {
            String data;
            byte[] dataToClinician;
            byte[] dataToUnity;

            Random r = new Random();
            int heartRate = r.Next(60, 200);
            int oxygen = r.Next(93, 99);
            int systolic = r.Next(100, 180);
            int diastolic = r.Next(50, 120);

            // testing for bike data (values may not be in correct range)
            int powerVal = r.Next(20, 40);
            // should be between 100-200 (changed for faster testing)
            int speedVal = r.Next(150, 200);
            int cadenceVal = r.Next(40, 60);

            // modify patient UI labels
            hrValue.Dispatcher.Invoke((Action)(() => hrValue.Content = heartRate.ToString() + " bpm"));
            oxiValue.Dispatcher.Invoke((Action)(() => oxiValue.Content = oxygen.ToString() + " %"));
            bpValue.Dispatcher.Invoke((Action)(() => bpValue.Content = systolic.ToString() + "/" + diastolic.ToString()));

            String patientLabel = "patient" + patientIndex;

            try
            {
                //// mock data sent to the clinician
                //data = patientLabel + "-" + user.ToString() + "|" + "HR " + heartRate.ToString() + "\n";
                //dataToClinician = System.Text.Encoding.ASCII.GetBytes(data);
                //socketToClinician.Send(dataToClinician);

                //data = patientLabel + "-" + user.ToString() + "|" + "OX " + oxygen.ToString() + "\n";
                //dataToClinician = System.Text.Encoding.ASCII.GetBytes(data);
                //socketToClinician.Send(dataToClinician);

                //data = patientLabel + "-" + user.ToString() + "|" + "BP " + systolic.ToString() + " " + diastolic.ToString() + "\n";
                //dataToClinician = System.Text.Encoding.ASCII.GetBytes(data);
                //socketToClinician.Send(dataToClinician);

                //data = patientLabel + "-" + user.ToString() + "|" + "EC -592 -201 -133 -173 -172 -143 -372 -349 -336 -332 -314 -309 -295 -274 -265 -261 16 44 75 102 -123 -80 -44 -11 259\n";
                //dataToClinician = System.Text.Encoding.ASCII.GetBytes(data);
                //socketToClinician.Send(dataToClinician);

                if (unitySocketWorker != null)
                {
                    if (unitySocketWorker.Connected)
                    {
                        // mock data sent to the Unity Application
                        data = "PW " + powerVal.ToString() + "\n";
                        dataToUnity = System.Text.Encoding.ASCII.GetBytes(data);
                        unitySocketWorker.Send(dataToUnity);

                        data = "";

                        data = "WR " + speedVal.ToString() + "\n";
                        dataToUnity = System.Text.Encoding.ASCII.GetBytes(data);
                        unitySocketWorker.Send(dataToUnity);

                        data = "";

                        data = "CR " + cadenceVal.ToString() + "\n";
                        dataToUnity = System.Text.Encoding.ASCII.GetBytes(data);
                        unitySocketWorker.Send(dataToUnity);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        #endregion

        #region Setting up the socket connection for bio information
        private void InitializeBioSockets()
        {
            try
            {
                //create listening socket
                socketBioListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (wirelessIP != null)
                {
                    IPAddress addy = System.Net.IPAddress.Parse(wirelessIP);
                    IPEndPoint iplocal = new IPEndPoint(addy, 4444);
                    //bind to local IP Address
                    socketBioListener.Bind(iplocal);
                    //start listening -- 4 is max connections queue, can be changed
                    socketBioListener.Listen(4);
                    //create call back for client connections -- aka maybe recieve video here????
                    socketBioListener.BeginAccept(new AsyncCallback(OnBioSocketConnection), null);
                    //MessageBox.Show("Please enter the following IP address to the phone: " + wirelessIP + "and press Wifi Connect");

                }
                else
                {
                    MessageBox.Show("No wireless signal detected. Please connect the computer to a wireless network.");
                }

            }
            catch (SocketException e)
            {
                //something went wrong
                Console.WriteLine("SocketException thrown at InitializeBiosockets");
                MessageBox.Show(e.Message);
            }

        }
        private void OnBioSocketConnection(IAsyncResult asyn)
        {
            try
            {
               // Console.WriteLine("econntected");
                bioSocketWorker = socketBioListener.EndAccept(asyn);

                WaitForBioData(bioSocketWorker);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnSocketConnection: Socket has been closed\n");
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException thrown at OnBioSocketConnection");
                MessageBox.Show(e.Message);
            }

        }
        private void WaitForBioData(System.Net.Sockets.Socket soc)
        {
            try
            {
                //Console.WriteLine("WaitForBioData");
                if (socketBioWorkerCallback == null)
                {
                    socketBioWorkerCallback = new AsyncCallback(OnBioDataReceived);
                }

                BioSocketPacket sockpkt = new BioSocketPacket();
                sockpkt.packetSocket = soc;
                //start listening for data
                soc.BeginReceive(sockpkt.dataBuffer, 0, sockpkt.dataBuffer.Length, SocketFlags.None, socketBioWorkerCallback, sockpkt);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException thrown at WaitForBioData");
                MessageBox.Show(e.Message);
            }
        }

        private void OnBioDataReceived(IAsyncResult asyn)
        {
            try
            {
                BioSocketPacket socketID = (BioSocketPacket)asyn.AsyncState;
                //end receive
                int end = 0;
                end = socketID.packetSocket.EndReceive(asyn);

                // If the phone stops sending, then the EndReceive function returns 0
                // (i.e. zero bytes received)
                if(end == 0)
                {
                    socketID.packetSocket.Close();
                    socketBioListener.Close();
                    InitializeBioSockets();
                }
                // phone is connected!
                else
                {
                    char[] chars = new char[end + 1];
                    System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                    int len = d.GetChars(socketID.dataBuffer, 0, end, chars, 0);
                    System.String tmp = new System.String(chars);

                    //Console.WriteLine("received: "+tmp);

                    // need to be changed to properly label the patient according to the port used
                    if (!tmp.Contains('|'))
                    {
                        tmp = string.Concat("patient" + patientIndex.ToString() + "-" + user.ToString() + "|", tmp);
                    }

                    System.String[] name = tmp.Split('|');

                    //Console.WriteLine(name.Length);

                    if (name.Length == 2)
                    {
                        System.String[] data = name[1].Trim().Split(' ');

                        // Console.WriteLine(name[1]);

                        if ((data[0] == "HR") || (data[0] == "OX") || (data[0] == "BP") || (data[0] == "EC"))
                        {
                            byte[] dataToClinician = System.Text.Encoding.ASCII.GetBytes(tmp);

                            //Console.WriteLine(tmp);

                            if (socketToClinician != null)
                            {
                                socketToClinician.Send(dataToClinician);
                            }
                        }
                        else if ((data[0] == "PW") || (data[0] == "WR") || (data[0] == "CR"))
                        {
                            if (unitySocketWorker != null)
                            {
                                if (unitySocketWorker.Connected)
                                {
                                    tmp = new System.String(chars);
                                    //Console.WriteLine("connected: "+tmp);
                                    byte[] dataToUnity = System.Text.Encoding.ASCII.GetBytes(tmp);
                                    unitySocketWorker.Send(dataToUnity);
                                }
                            }
                        }

                        // Decide on what encouragement text should be displayed based on heart rate.
                        if (data[0] == "HR")
                        {
                            //BT
                            int number;
                            bool result = Int32.TryParse(data[1], out number);
                            if (result)
                            {
                                // remove null char
                                hrValue.Dispatcher.Invoke((Action)(() => hrValue.Content = data[1].Replace("\0", "").Trim() + " bpm"));
                            }

                        }

                        // Change the Sats display in the UI thread.
                        if (data[0] == "OX")
                        {
                            if (data.Length > 1)
                            {
                                // MethodInvoker had to be used to solve cross threading issue
                                if (data[1] != null && data[2] != null)
                                {
                                    oxiValue.Dispatcher.Invoke((Action)(() => oxiValue.Content = data[1] + " %"));
                                    // enable below to display hr from oximeter
                                    //hrValue.Dispatcher.Invoke((Action)(() => hrValue.Content = data[2].Replace("\0", "").Trim() + " bpm"));
                                }
                            }
                        }

                        else if (data[0] == "BP")
                        {
                            bpValue.Dispatcher.Invoke((Action)(() => bpValue.Content = data[1] + "/" + data[2]));
                        }
                    }
                    WaitForBioData(bioSocketWorker);
                }
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException at OnBioDataReceived");
                // this error is thrown when the doctor disconnects
                // need to add code to close sockets and close the application
                MessageBox.Show(e.Message);
            }

        }

        #endregion

        #region socket connection with the doctor

        private void CreateSocketConnection()
        {
            try
            {
                //GetDoctoIP();

                //create a new client socket
                socketToClinician = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                if (doctorIp != null)
                {
                    //Console.WriteLine(doctorIp);

                    System.Net.IPAddress remoteIPAddy = System.Net.IPAddress.Parse(doctorIp);
                    System.Net.IPEndPoint remoteEndPoint = new System.Net.IPEndPoint(remoteIPAddy, 5000 + patientIndex - 1);
                    socketToClinician.Connect(remoteEndPoint);

                    if (socketToClinician.Connected)
                    {
                        // later change the patientLocalIp to their wireless IP
                        // once the video and audio works smoother in wireless
                        byte[] startData = System.Text.Encoding.ASCII.GetBytes("start|" + patientIndex.ToString() + "-" + user.ToString() + "-" + patientLocalIp);
                        socketToClinician.Send(startData);
                    }
                }
                else
                {
                    MessageBox.Show("doctor IP is null");
                }

            }

            catch (SocketException e)
            {
                Console.WriteLine("SocketException thrown at CreateSocketConnection: " + e.ErrorCode.ToString());
                MessageBox.Show(e.Message);
            }
        }

        #endregion

        #region Connection with Unity
        /// <summary>
        /// This method creates a TCP connection to the external Unity application at
        /// IP address of 127.0.0.1(local host) and port 5555.
        /// </summary>
        private void ConnectToUnity()
        {
            try
            {
                unitySocketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress addy = System.Net.IPAddress.Parse("127.0.0.1");
                IPEndPoint iplocal = new IPEndPoint(addy, 4445);
                //bind to local IP Address
                unitySocketListener.Bind(iplocal);
                //start listening -- 4 is max connections queue, can be changed
                unitySocketListener.Listen(4);
                unitySocketListener.BeginAccept(new AsyncCallback(OnUnitySocketConnection), null);

                //create call back for client connections -- aka maybe recieve video here????
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException thrown at ConnectToUnity");
                Console.WriteLine(e.Message);
            }
        }

        private void OnUnitySocketConnection(IAsyncResult asyn)
        {
            try
            {
                unitySocketWorker = unitySocketListener.EndAccept(asyn);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("OnSocketConnection: Socket has been closed", "error");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message, "error");
            }
        }

        #endregion

        #region Kinect
        private void InitializeKinect()
        {
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += sensorChooser_KinectChanged;
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

            // Don't try this unless there is a kinect
            if (this.sensorChooser.Kinect != null)
            {
                //trying to get the video from the clinician -- this can fail
                _videoClient = new ColorClient();
                _videoClient.ColorFrameReady += _videoClient_ColorFrameReady;
                _videoClient.Connect(doctorIp, 4531 + patientIndex - 1);


                // Streaming video out on port 4555
                _videoListener = new ColorListener(this.sensorChooser.Kinect, 4560, ImageFormat.Jpeg);
                _videoListener.Start();

                //_audioClient = new AudioClient();
                //_audioClient.AudioFrameReady += _audioClient_AudioFrameReady;
                //_audioClient.Connect(doctorIp, 4541 + patientIndex - 1);

                ////for sending audio
                //_audioListener = new AudioListener(this.sensorChooser.Kinect, 4565 + patientIndex - 1);
                //_audioListener.Start();

            }

        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">event arguments</param>
        void sensorChooser_KinectChanged(object sender, KinectChangedEventArgs e)
        {
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
                }
            }
        }


        void NewSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
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

                this.patientFrame.Source = outputImage;

                // force the garbase collector to remove outputImage --> otherwise, causes mem leak
                outputImage = null;
                GC.Collect();
            };

        }

        //called when a video frame from the client is ready
        void _videoClient_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            this.doctorFrame.Source = e.ColorFrame.BitmapImage;
        }

        private void InitializeAudio()
        {
            wo.DesiredLatency = 100;
            mybufferwp = new BufferedWaveProvider(wf);
            mybufferwp.BufferDuration = TimeSpan.FromMinutes(5);
            mybufferwp.DiscardOnBufferOverflow = true;
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


        #endregion

        private void UnityWindow_LoadCompleted(object sender, NavigationEventArgs e)
        {
            //Console.WriteLine("unity window load completed function");
            mshtml.IHTMLDocument2 dom = (mshtml.IHTMLDocument2)UnityWindow.Document;
            dom.body.style.overflow = "hidden";
        }
    }

}





