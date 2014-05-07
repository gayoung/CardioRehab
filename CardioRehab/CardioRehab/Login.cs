using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace CardioRehab
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String username = textBox1.Text;
            String password = textBox2.Text;

            DatabaseClass db = new DatabaseClass();
            try
            {
                db.m_dbconnection.Open();
            }
            catch(FileLoadException error)
            {
                // db connection failed
                MessageBox.Show(error.Message);
            }

            String sql = "SELECT * FROM authentication WHERE username='" + username.Trim() + "' AND password='" + password.Trim()+"'";
            SQLiteCommand cmd = new SQLiteCommand(sql, db.m_dbconnection);
            try
            {
                SQLiteDataReader reader = cmd.ExecuteReader();

                // correct username and password
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        String role = reader["role"].ToString();
                        int userid = int.Parse(reader["id"].ToString());

                        if (role.Trim() == "Patient") // user is a patient
                        {
                            Console.WriteLine("authenticated user is a patient");
                            // open patient window
                            PatientMain patientWindow = new PatientMain(userid);
                            patientWindow.Show();
                            patientWindow.FormClosed += new FormClosedEventHandler(MainWindowClosed);
                            this.Hide();
                        }
                        else if (role.Trim() == "Doctor") // user is a doctor
                        {
                            Console.WriteLine("authenticated user is a doctor");
                            // open doctor window
                            DoctorMain doctorWindow = new DoctorMain(userid);
                            doctorWindow.Show();
                            doctorWindow.FormClosed += new FormClosedEventHandler(MainWindowClosed);
                            this.Hide();
                        }
                        else // user is an admin
                        {
                            Console.WriteLine("authenticated user is a admin");
                            // open admin window
                        }
                    }

                }
                // incorrect username and password
                else
                {
                    label4.Show();
                }
                reader.Dispose();
            }
            catch(SQLiteException ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            cmd.Dispose();
            db.m_dbconnection.Close();
        }

        // need this function to close the hidden login form
        private void MainWindowClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }
    }
}
