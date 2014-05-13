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

        const int MAX_CLIENTS = 6;

        private AsyncCallback socketBioWorkerCallback;
        private List<Socket> bioSockets_list = new List<Socket>();
        private List<Socket> bioSocketWorkers_list = new List<Socket>();

        public DoctorMain(int currentuser, DatabaseClass openDB)
        {
            db = openDB;
            userid = currentuser;

            GetLocalIP();
            CheckRecord();
            InitializeComponent();

            // patients send the biodata from port 5000-5005
            int[] ports = new int[6]{5000,5001,5002,5003,5004,5005};
            InitializeBioSockets(ports);
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
                Console.WriteLine("OnBioSocketConnection1");
                bioSocketWorkers_list.Add(currentSocket.EndAccept(asyn));

                Console.WriteLine("OnBioSocketConnection2");
                WaitForBioData(bioSocketWorkers_list[bioSocketWorkers_list.Count - 1], port);
                Console.WriteLine("OnBioSocketConnection3");
                // start accepting connections from other clients
                currentSocket.BeginAccept(new AsyncCallback(OnBioSocketConnection), state);
                Console.WriteLine("OnBioSocketConnection4");
               
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
                Application.Exit();
 
            }

        }
        private void WaitForBioData(System.Net.Sockets.Socket soc, int port)
        {
            Console.WriteLine("wait for bio data");
            try
            {
                if (socketBioWorkerCallback == null)
                {
                    Console.WriteLine("WaitForBioData1");
                    socketBioWorkerCallback = new AsyncCallback(OnBioDataReceived);
                }

                ReceivedDataState state = new ReceivedDataState();
                BioSocketPacket sockpkt = new BioSocketPacket();
                Console.WriteLine("WaitForBioData2");
                state.socketData = sockpkt;
                Console.WriteLine("WaitForBioData3");
                state.port = port;
                Console.WriteLine("WaitForBioData4");

                sockpkt.packetSocket = soc;
                Console.WriteLine("WaitForBioData5");
                //start listening for data
                soc.BeginReceive(sockpkt.dataBuffer, 0, sockpkt.dataBuffer.Length, SocketFlags.None, socketBioWorkerCallback, state);
                Console.WriteLine("WaitForBioData6");
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
            String[] name = tmp.Split('|');

                for(int i = 1; i < name.Length; i++)
                {
                    String[] data = name[i].Split(' ');

                    // Set the UI in the main thread.
                    this.Invoke((MethodInvoker)(() =>
                    {
                        switch (name[0].Trim())
                        {
                            case "patient1":
                                if (data[0].Trim() == "HR")
                                {
                                    hrValue1.Text = data[1] + " bpm";
                                }
                                else if (data[0].Trim() == "OX")
                                {
                                    oxValue1.Text = data[1] + "%";
                                }
                                else if (data[0].Trim() == "BP")
                                {
                                    bpValue1.Text = data[1] + "/" + data[2];
                                }
                                break;
                            case "patient2":
                                if (data[0].Trim() == "HR")
                                {
                                    hrValue2.Text = data[1] + " bpm";
                                }
                                else if (data[0].Trim() == "OX")
                                {
                                    oxValue2.Text = data[1] + "%";
                                }
                                else if (data[0].Trim() == "BP")
                                {
                                    bpValue2.Text = data[1] + "/" + data[2];
                                }
                                break;
                            case "patient3":
                                if (data[0].Trim() == "HR")
                                {
                                    hrValue3.Text = data[1] + " bpm";
                                }
                                else if (data[0].Trim() == "OX")
                                {
                                    oxValue3.Text = data[1] + "%";
                                }
                                else if (data[0].Trim() == "BP")
                                {
                                    bpValue3.Text = data[1] + "/" + data[2];
                                }
                                break;
                            case "patient4":
                                if (data[0].Trim() == "HR")
                                {
                                    hrValue4.Text = data[1] + " bpm";
                                }
                                else if (data[0].Trim() == "OX")
                                {
                                    oxValue4.Text = data[1] + "%";
                                }
                                else if (data[0].Trim() == "BP")
                                {
                                    bpValue4.Text = data[1] + "/" + data[2];
                                }
                                break;
                            case "patient5":
                                if (data[0].Trim() == "HR")
                                {
                                    hrValue5.Text = data[1] + " bpm";
                                }
                                else if (data[0].Trim() == "OX")
                                {
                                    oxValue5.Text = data[1] + "%";
                                }
                                else if (data[0].Trim() == "BP")
                                {
                                    bpValue5.Text = data[1] + "/" + data[2];
                                }
                                break;
                            case "patient6":
                                if (data[0].Trim() == "HR")
                                {
                                    hrValue6.Text = data[1] + " bpm";
                                }
                                else if (data[0].Trim() == "OX")
                                {
                                    oxValue6.Text = data[1] + "%";
                                }
                                else if (data[0].Trim() == "BP")
                                {
                                    bpValue6.Text = data[1] + "/" + data[2];
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
    }
}
