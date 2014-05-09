using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardioRehab
{
    public partial class PatientMain : Form
    {
        private int user;
        // currently under assumption that
        // first output from the loop is LAN and second is wireless
        private String docLocalIp;
        private String patientLocalIp;
        private String wirelessIP;

        private AsyncCallback socketBioWorkerCallback;
        public Socket socketBioListener;
        public Socket bioSocketWorker;
        //static string fname = string.Format("Tiny Tim-{0:yyyy-MM-dd hh.mm.ss.tt}.txt", DateTime.Now);

        int[] oxdata = new int[1000];
        int[] hrdata = new int[1000];
        int[] bpdata = new int[1000];
        public int hrcount, oxcount, bpcount;

        /// <summary>
        /// Constructor for this class
        /// </summary>
        /// <param name="currentuser"> database ID for the current user</param>
        public PatientMain(int currentuser)
        {
            user = currentuser;

            GetLocalIP();
            CheckRecord();
            InitializeComponent();
            //used to start socket server for bioSockets
            InitializeBioSockets();
            MessageBox.Show("Please enter the following IP address to the phone: " + wirelessIP);

            this.SizeChanged += new EventHandler(PatientMain_Resize);
        }

        #region Helper functions

        private void CheckRecord()
        {
            DatabaseClass db = new DatabaseClass();
            try
            {
                db.m_dbconnection.Open();
            }
            catch (FileLoadException error)
            {
                // db connection failed
                MessageBox.Show(error.Message);
            }

            String query = "SELECT wireless_ip FROM patient WHERE patient_id=" + user;
            SQLiteCommand cmd = new SQLiteCommand(query, db.m_dbconnection);
           
            SQLiteDataReader reader = cmd.ExecuteReader();

            // correct username and password
            if (!reader.HasRows)
            {
                String updatesql = "UPDATE patient SET wireless_ip="+ wirelessIP +
                    ", local_ip=" + patientLocalIp + " WHERE patient_id=" + user;

                SQLiteCommand updatecmd = new SQLiteCommand(updatesql, db.m_dbconnection);
                updatecmd.ExecuteNonQuery();

            }
            reader.Dispose();
        }

        /// <summary>
        /// This method is used to adjust the size of the form components
        /// when the form window is resized (min/max).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PatientMain_Resize(object sender, EventArgs e)
        {
            int currentWidth = this.Width;
            int currentHeight = this.Height;

            panel1.Width = (int)(currentWidth * 0.70);
            panel1.Height = (int)(currentHeight * 0.9);

            int newdoctorPanelX = panel1.Location.X + panel1.Width + 10;
            int doctorPanelY = panel1.Location.Y;

            doctorPanel.Location = new Point(newdoctorPanelX, doctorPanelY);
            doctorPanel.Width = (int)(currentWidth * 0.25);
            doctorPanel.Height = (int)(currentHeight * 0.23);

            int patientPanelY = doctorPanelY + doctorPanel.Height + 10;

            patientPanel.Location = new Point(newdoctorPanelX, patientPanelY);
            patientPanel.Width = (int)(currentWidth * 0.25);
            patientPanel.Height = (int)(currentHeight * 0.23);

            int BiostatPanellY = patientPanelY + patientPanel.Height + 10;

            BiostatPanel.Location = new Point(newdoctorPanelX, BiostatPanellY);
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
                    if(Ipcounter == 0)
                    {
                        patientLocalIp = addr.ToString();
                    }
                    else if(Ipcounter == 1)
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

            //BT creates file
            //using (StreamWriter sw = new StreamWriter(File.Create(fname)))
            //{
            //    sw.WriteLine("Session started.");
            //}
            //*

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
                //MessageBox.Show(tmp);

                if (!tmp.Contains('|'))
                {
                    // MessageBox.Show(tmp);
                    tmp = string.Concat("patient1|", tmp);
                    //MessageBox.Show(tmp);
                }
                System.String[] name = tmp.Split('|');

                System.String[] fakeECG = new String[1] { "ECG" };


                if (name.Length == 2)
                {
                    System.String[] data = name[1].Split(' ');
                    String timeStamp = GetTimestamp(DateTime.Now);

                    byte[] dataToClinician = System.Text.Encoding.ASCII.GetBytes(tmp);

                    //socketToClinician.Send(dataToClinician);
                    //MessageBox.Show("Got stuff!");

                    // Decide on what encouragement text should be displayed based on heart rate.
                    if (data[0] == "HR")
                    {
                        //BT
                        hrdata[hrcount] = Convert.ToInt32(data[1]);
                        hrcount++;
                        //using (StreamWriter sw = File.AppendText(fname))
                        //{
                        //    sw.WriteLine(timeStamp + " |" + "HR " + data[1]);
                        //}
                        //*

                        // Below target zone.
                        hrValue.Invoke((MethodInvoker)(() => hrValue.Text = data[1] + " bpm"));
                    }

                    // Change the Sats display in the UI thread.
                    else if (data[0] == "OX")
                    {
                        //BT
                        oxdata[oxcount] = Convert.ToInt32(data[1]); ;
                        oxcount++;
                        //using (StreamWriter sw = File.AppendText(fname))
                        //{
                        //    sw.WriteLine(timeStamp + " |" + "OX " + data[1]);
                        //}

                        // MethodInvoker had to be used to solve cross threading issue
                        if(data[1] != null && data[2] != null)
                        {
                            oxiValue.Invoke((MethodInvoker)(() => oxiValue.Text = data[1] + " %"));
                            hrValue.Invoke((MethodInvoker)(() => hrValue.Text = data[2] + " bpm"));
                        }
                    }

                    if (data[0] == "BP")
                    {
                        //BT
                        bpdata[bpcount] = Convert.ToInt32(data[1]); ;
                        bpcount++;
                        //using (StreamWriter sw = File.AppendText(fname))
                        //{
                        //    sw.WriteLine(timeStamp + " |" + "BP " + data[1]);
                        //}
                        bpValue.Invoke((MethodInvoker)(() => bpValue.Text = data[1] + "/" + data[2]));
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
                MessageBox.Show(e.Message);
            }

        }

        #endregion

    }
}
