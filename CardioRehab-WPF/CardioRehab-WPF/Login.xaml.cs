using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CardioRehab_WPF
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            FocusManager.SetFocusedElement(loginscreen, usernameinput);
        }

        private void login_button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("button clicked");
            String username = usernameinput.Text;
            String password = passwordinput.Password;
            int chosenIndex = indexinput.SelectedIndex;

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

            String sql = "SELECT * FROM authentication WHERE username='" + username.Trim() + "' AND password='" + password.Trim() + "'";
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
                            PatientWindow patientWindow = new PatientWindow(chosenIndex, userid, db);
                            patientWindow.Show();
                            patientWindow.Closed += new EventHandler(MainWindowClosed);
                            this.Hide();
                        }
                        else if (role.Trim() == "Doctor") // user is a doctor
                        {
                            Console.WriteLine("authenticated user is a doctor");
                            // open doctor window
                            DoctorWindow doctorWindow = new DoctorWindow(userid, db);
                            doctorWindow.Show();
                            doctorWindow.Closed += new EventHandler(MainWindowClosed);
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
                    warning_label.Visibility = System.Windows.Visibility.Visible;
                }
                reader.Dispose();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show(ex.Message);
            }

            cmd.Dispose();
            db.m_dbconnection.Close();
        }

        // need this function to close the hidden login form
        private void MainWindowClosed(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
