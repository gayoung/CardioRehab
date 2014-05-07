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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            String username = textBox1.Text;
            String password = textBox2.Text;

            DatabaseClass db = new DatabaseClass();
            try
            {
                db.ConnectToDB();
            }
            catch(FileLoadException error)
            {
                MessageBox.Show(error.Message);
            }

            String sql = "SELECT * FROM Authentication WHERE username=" + username.Trim() + "AND password=" + password.Trim();
        }
    }
}
