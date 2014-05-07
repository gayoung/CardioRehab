using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardioRehab
{
    public partial class PatientMain : Form
    {
        public PatientMain(int currentuser)
        {
            int user = currentuser;

            InitializeComponent();
        }
    }
}
