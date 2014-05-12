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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardioRehab
{
    public partial class DoctorMain : Form
    {
        private DatabaseClass db;

        private int userid;
        private String localIP;
        private String patientIP;

        private AsyncCallback socketBioWorkerCallback;
        public Socket socketBioListener;
        public Socket bioSocketWorker;

        public DoctorMain(int currentuser, DatabaseClass openDB)
        {
            db = openDB;
            userid = currentuser;

            GetLocalIP();
            CheckRecord();
            InitializeComponent();
            InitializeBioSockets();
        }

        #region Helper functions

        private void GetLocalIP()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            int Ipcounter = 0;
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (Ipcounter == 0)
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

        #endregion

        #region socket connection with patient for Bio information

        private void InitializeBioSockets()
        {
            try
            {
                //create listening socket
                socketBioListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress addy = System.Net.IPAddress.Parse(localIP);
                IPEndPoint iplocal = new IPEndPoint(addy, 5000);
                //bind to local IP Address
                socketBioListener.Bind(iplocal);
                //start listening -- 4 is max connections queue, can be changed
                socketBioListener.Listen(4);
                //create call back for client connections -- aka maybe recieve video here????
                socketBioListener.BeginAccept(new AsyncCallback(OnBioSocketConnection), null);
            }
            catch (SocketException e)
            {
                //something went wrongpatient
                MessageBox.Show(e.Message);
            }

        }
        private void OnBioSocketConnection(IAsyncResult asyn)
        {

            Console.WriteLine("got connection");
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
                MessageBox.Show(e.Message);
            }

        }
        private void WaitForBioData(System.Net.Sockets.Socket soc)
        {
            Console.WriteLine("wait for bio data");
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
                MessageBox.Show(e.Message);
            }
        }

        private void OnBioDataReceived(IAsyncResult asyn)
        {
            try
            {
                Console.WriteLine("data here!");
                BioSocketPacket socketID = (BioSocketPacket)asyn.AsyncState;
                //end receive
                int end = 0;
                end = socketID.packetSocket.EndReceive(asyn);

                //just getting simple text right now -- needs to be changed
                char[] chars = new char[end + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int len = d.GetChars(socketID.dataBuffer, 0, end, chars, 0);
                System.String tmp = new System.String(chars);
                tmp = Regex.Replace(tmp, @"\t|\n|\r", " ");
                
                // DEBUGGING
                Console.WriteLine("data passed");
                Console.WriteLine(tmp);

                System.String[] name = tmp.Split('|');
                System.String[] data = name[1].Split(' ');
                
                // Set the UI in the main thread.
                this.Invoke((MethodInvoker)(() =>
                {   
                    switch(name[0].Trim())
                    {
                        case "patient1":
                            if(data[0].Trim() == "HR")
                            {
                                hrValue1.Text = data[1] + " bpm";
                            }
                            else if(data[0].Trim() == "OX")
                            {
                                oxValue1.Text = data[1] + "%";
                            }
                            else if(data[0].Trim() == "BP")
                            {
                                bpValue1.Text = data[1] +  "/" + data[2];
                            }
                            break;
                        case "patient2":
                            if(data[0].Trim() == "HR")
                            {
                                hrValue2.Text = data[1] + " bpm";
                            }
                            else if(data[0].Trim() == "OX")
                            {
                                oxValue2.Text = data[1] + "%";
                            }
                            else if(data[0].Trim() == "BP")
                            {
                                bpValue2.Text = data[1] +  "/" + data[2];
                            }
                            break;
                        case "patient3":
                            if(data[0].Trim() == "HR")
                            {
                                hrValue3.Text = data[1] + " bpm";
                            }
                            else if(data[0].Trim() == "OX")
                            {
                                oxValue3.Text = data[1] + "%";
                            }
                            else if(data[0].Trim() == "BP")
                            {
                                bpValue3.Text = data[1] +  "/" + data[2];
                            }
                            break;
                        case "patient4":
                            if(data[0].Trim() == "HR")
                            {
                                hrValue4.Text = data[1] + " bpm";
                            }
                            else if(data[0].Trim() == "OX")
                            {
                                oxValue4.Text = data[1] + "%";
                            }
                            else if(data[0].Trim() == "BP")
                            {
                                bpValue4.Text = data[1] +  "/" + data[2];
                            }
                            break;
                        case "patient5":
                            if(data[0].Trim() == "HR")
                            {
                                hrValue5.Text = data[1] + " bpm";
                            }
                            else if(data[0].Trim() == "OX")
                            {
                                oxValue5.Text = data[1] + "%";
                            }
                            else if(data[0].Trim() == "BP")
                            {
                                bpValue5.Text = data[1] +  "/" + data[2];
                            }
                            break;
                        case "patient6":
                            if(data[0].Trim() == "HR")
                            {
                                hrValue6.Text = data[1] + " bpm";
                            }
                            else if(data[0].Trim() == "OX")
                            {
                                oxValue6.Text = data[1] + "%";
                            }
                            else if(data[0].Trim() == "BP")
                            {
                                bpValue6.Text = data[1] +  "/" + data[2];
                            }
                            break;
                    }
  
                }));

                WaitForBioData(bioSocketWorker);
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
        #endregion
    }
}
