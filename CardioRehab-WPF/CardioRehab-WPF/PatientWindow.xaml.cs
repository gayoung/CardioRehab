﻿using Coding4Fun.Kinect.KinectService.Common;
using Coding4Fun.Kinect.KinectService.Listeners;
using Coding4Fun.Kinect.KinectService.WpfClient;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ColorImageFormat = Microsoft.Kinect.ColorImageFormat;
using ColorImageFrame = Microsoft.Kinect.ColorImageFrame;
using DepthImageFormat = Microsoft.Kinect.DepthImageFormat;

namespace CardioRehab_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PatientWindow : Window
    {
        //static string fname = string.Format("Tiny Tim-{0:yyyy-MM-dd hh.mm.ss.tt}.txt", DateTime.Now);
        private DatabaseClass db;

        private int user;
        // currently under assumption that
        // first output from the loop is LAN and second is wireless
        private String doctorIp = "192.168.184.43";
        private String patientLocalIp;
        private String wirelessIP;

        private int patientIndex;
        private DispatcherTimer mimicPhoneTimer;

        private AsyncCallback socketBioWorkerCallback;
        public Socket socketBioListener;
        public Socket bioSocketWorker;
        public Socket socketToClinician;

        int[] oxdata = new int[1000];
        int[] hrdata = new int[1000];
        int[] bpdata = new int[1000];
        public int hrcount, oxcount, bpcount;

        //kinect sensor 
        private KinectSensorChooser sensorChooser;

        //kinect listeners
        private static DepthListener _depthListener;
        private static ColorListener _videoListener;
        private static AudioListener _audioListener;

        //kinect clients
        private ColorClient _videoClient;
        private DepthClient _depthClient;

        private WriteableBitmap outputImage;
        private byte[] pixels = new byte[0];

        WaveOut wo = new WaveOut();
        WaveFormat wf = new WaveFormat(16000, 1);
        BufferedWaveProvider mybufferwp = null;

        private AudioClient _audioClient;

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

            InitializeBioSockets();
            //CreateSocketConnection();

            // disable this function if InitializeBioSockets function is active
            //InitTimer();

            //this.SizeChanged += new EventHandler(PatientMain_Resize);


        }

        private void PatientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeKinect();
            InitializeAudio();

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

        #region Helper functions

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
        /// This method is used to adjust the size of the form components
        /// when the form window is resized (min/max). Need to modify this later to
        /// work with the new WPF UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*/
        private void PatientMain_Resize(object sender, EventArgs e)
        {
            int currentWidth = this.Width;
            int currentHeight = this.Height;

            panel1.Width = (int)(currentWidth * 0.70);
            panel1.Height = (int)(currentHeight * 0.9);

            int newdoctorFrameX = panel1.Location.X + panel1.Width + 10;
            int doctorFrameY = panel1.Location.Y;

            doctorFrame.Location = new Point(newdoctorFrameX, doctorFrameY);
            doctorFrame.Width = (int)(currentWidth * 0.25);
            doctorFrame.Height = (int)(currentHeight * 0.23);

            int patientFrameY = doctorFrameY + doctorFrame.Height + 10;

            patientFrame.Location = new Point(newdoctorFrameX, patientFrameY);
            patientFrame.Width = (int)(currentWidth * 0.25);
            patientFrame.Height = (int)(currentHeight * 0.23);

            int BiostatPanellY = patientFrameY + patientFrame.Height + 10;

            BiostatPanel.Location = new Point(newdoctorFrameX, BiostatPanellY);
            BiostatPanel.Width = (int)(currentWidth * 0.25);
            BiostatPanel.Height = (int)(currentHeight * 0.42);

            if(WindowState == FormWindowState.Maximized)
            {
                Font newStyle = new Font(hrLabel.Font.FontFamily, 22);
                hrLabel.Font = newStyle;
                hrValue.Font = newStyle;
                bpLabel.Font = newStyle;
                bpValue.Font = newStyle;
                oxiLabel.Font = newStyle;
                oxiValue.Font = newStyle;

            }
            else
            {
                Font newStyle = new Font(hrLabel.Font.FontFamily, 16);
                hrLabel.Font = newStyle;
                hrValue.Font = newStyle;
                bpLabel.Font = newStyle;
                bpValue.Font = newStyle;
                oxiLabel.Font = newStyle;
                oxiValue.Font = newStyle;
            }

            hrValue.Location = new Point(hrValue.Location.X, hrLabel.Location.Y + hrLabel.Height + 5);
            bpLabel.Location = new Point(bpLabel.Location.X, hrValue.Location.Y + hrValue.Height + 5);
            bpValue.Location = new Point(bpValue.Location.X, bpLabel.Location.Y + bpLabel.Height + 5);
            oxiLabel.Location = new Point(oxiLabel.Location.X, bpValue.Location.Y + bpValue.Height + 5);
            oxiValue.Location = new Point(oxiValue.Location.X, oxiLabel.Location.Y + oxiLabel.Height + 5);
        }/*/

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
                    Console.WriteLine("All the IP addresses read: ");
                    Console.WriteLine(addr.ToString());
                    if (Ipcounter == 0)
                    {
                        patientLocalIp = addr.ToString();
                    }
                    else if (Ipcounter == 1)
                    {
                        wirelessIP = addr.ToString();
                    }
                    Ipcounter++;
                }
            }
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("HH:mm:ss");
        }

        /// <summary>
        /// method to be used to test the code without the phone
        /// </summary>
        private void PhoneTestMethod()
        {
            //Console.WriteLine("phone test method");
            String data;
            byte[] dataToClinician;

            Random r = new Random();
            int heartRate = r.Next(60, 140);
            int oxygen = r.Next(93, 99);
            int systolic = r.Next(100, 180);
            int diastolic = r.Next(50, 105);

            // modify patient UI labels
            hrValue.Dispatcher.Invoke((Action)(() => hrValue.Content = heartRate.ToString() + " bpm"));
            oxiValue.Dispatcher.Invoke((Action)(() => oxiValue.Content = oxygen.ToString() + " %"));
            bpValue.Dispatcher.Invoke((Action)(() => bpValue.Content = systolic.ToString() + "/" + diastolic.ToString()));

            String patientLabel = "patient" + patientIndex;

            try
            {
                data = patientLabel + "-" + user.ToString() + "|" + "HR " + heartRate.ToString() + "\n";
                dataToClinician = System.Text.Encoding.ASCII.GetBytes(data);
                socketToClinician.Send(dataToClinician);

                data = patientLabel + "-" + user.ToString() + "|" + "OX " + oxygen.ToString() + "\n";
                dataToClinician = System.Text.Encoding.ASCII.GetBytes(data);
                socketToClinician.Send(dataToClinician);

                data = patientLabel + "-" + user.ToString() + "|" + "BP " + systolic.ToString() + " " + diastolic.ToString() + "\n";
                dataToClinician = System.Text.Encoding.ASCII.GetBytes(data);
                socketToClinician.Send(dataToClinician);
            }
            catch (Exception ex)
            {
                // doctor has terminated the connection
                Console.WriteLine(ex.Message);
                this.Close();
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
                    MessageBox.Show("Please enter the following IP address to the phone: " + wirelessIP + "and press Wifi Connect");

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

                //just getting simple text right now -- needs to be changed
                char[] chars = new char[end + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int len = d.GetChars(socketID.dataBuffer, 0, end, chars, 0);
                System.String tmp = new System.String(chars);

                // need to be changed to properly label the patient according to the port used
                if (!tmp.Contains('|'))
                {
                    // MessageBox.Show(tmp);
                    tmp = string.Concat("patient" + patientIndex.ToString() + "-" +user.ToString()+"|", tmp);
                }

                System.String[] name = tmp.Split('|');

                System.String[] fakeECG = new String[1] { "ECG" };


                if (name.Length == 2)
                {
                    System.String[] data = name[1].Trim().Split(' ');
                    String timeStamp = GetTimestamp(DateTime.Now);

                    byte[] dataToClinician = System.Text.Encoding.ASCII.GetBytes(tmp);

                    socketToClinician.Send(dataToClinician);

                    var regexlimit = new Regex("^[0-9 ]*$");

                    // Decide on what encouragement text should be displayed based on heart rate.
                    if (data[0] == "HR")
                    {
                        //BT
                        int number;
                        bool result = Int32.TryParse(data[1], out number);
                        if(result)
                        {
                            hrdata[hrcount] = Convert.ToInt32(data[1]);
                            hrcount++;
                            // remove null char
                            hrValue.Dispatcher.Invoke((Action)(() => hrValue.Content = data[1].Replace("\0", "") + " bpm"));
                        }

                        /*/
                        using (StreamWriter sw = File.AppendText(fname))
                        {
                            sw.WriteLine(timeStamp + " |" + "HR " + data[1]);
                        }
                         * /*/
                    }

                    // Change the Sats display in the UI thread.
                    else if (data[0] == "OX")
                    {
                        //BT
                        oxdata[oxcount] = Convert.ToInt32(data[1]); ;
                        oxcount++;
                        // MethodInvoker had to be used to solve cross threading issue
                        if (data[1] != null && data[2] != null)
                        {
                            oxiValue.Dispatcher.Invoke((Action)(() => oxiValue.Content = data[1] + " %"));
                           // hrValue.Dispatcher.Invoke((Action)(() => hrValue.Content = data[2] + " bpm"));
                        }
                    }

                    if (data[0] == "BP")
                    {
                        //BT
                        bpdata[bpcount] = Convert.ToInt32(data[1]); ;
                        bpcount++;
                        bpValue.Dispatcher.Invoke((Action)(() => bpValue.Content = data[1] + "/" + data[2]));
                    }
                }
                WaitForBioData(bioSocketWorker);
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

                    //Console.WriteLine(socketToClinician.Connected);

                    if(socketToClinician.Connected)
                    {
                        //Console.WriteLine("start message formation");
                        byte[] startData = System.Text.Encoding.ASCII.GetBytes("start|" + patientIndex.ToString() + "-" + user.ToString() + "-" + wirelessIP);
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
                Console.WriteLine("SocketException thrown at CreateSocketConnection: "+e.ErrorCode.ToString());
                MessageBox.Show(e.Message);
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
                //Console.WriteLine("kinect is not null");
                //trying to get the video from the clinician -- this can fail
                _videoClient = new ColorClient();
                _videoClient.ColorFrameReady += _videoClient_ColorFrameReady;
                _videoClient.Connect(doctorIp, 4531+patientIndex);


                // Streaming video out on port 4555
                _videoListener = new ColorListener(this.sensorChooser.Kinect, 4555+patientIndex-1, ImageFormat.Jpeg);
                _videoListener.Start();

                _audioClient = new AudioClient();
                _audioClient.AudioFrameReady += _audioClient_AudioFrameReady;
                _audioClient.Connect(doctorIp, 4541);

                //for sending audio
                _audioListener = new AudioListener(this.sensorChooser.Kinect, 4537);
                _audioListener.Start();

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
                    e.OldSensor.DepthStream.Range = DepthRange.Default;
                    e.OldSensor.DepthStream.Disable();
                    e.OldSensor.ColorStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    Console.Write("here?");
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.

                }
            }

            if (e.NewSensor != null)
            {
                try
                {
                    e.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    e.NewSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    e.NewSensor.ColorFrameReady += NewSensor_ColorFrameReady;


                    
                }
                catch (InvalidOperationException)
                {
                    Console.Write("here?2");
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

            };

        }

        //called when a video frame from the client is ready
        void _videoClient_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            //Console.WriteLine("new frame!");
            this.doctorFrame.Source = e.ColorFrame.BitmapImage;
        }

        private void InitializeAudio()
        {
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


        #endregion
    }

}