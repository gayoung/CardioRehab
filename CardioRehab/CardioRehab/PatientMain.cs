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

            this.SizeChanged += new EventHandler(PatientMain_Resize);
        }

        private void PatientMain_Resize(object sender, EventArgs e)
        {
            int currentWidth = this.Width;
            int currentHeight = this.Height;

            panel1.Width = (int)(currentWidth * 0.70);
            panel1.Height = (int)(currentHeight * 0.9);

            int newdoctorPanelX = panel1.Location.X + panel1.Width + 10;
            int doctorPanelY = panel1.Location.Y;

            Console.WriteLine("doctorPanelY: " + doctorPanelY.ToString());

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

    }
}
