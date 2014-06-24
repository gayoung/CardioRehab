using DynamicDataDisplaySample.ECGViewModel;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;

namespace CardioRehab_WPF
{
    /// <summary>
    /// Interaction logic for FullScreenWindow.xaml
    /// </summary>
    public partial class FullScreenWindow : Window
    {
        private DatabaseClass db;
        private int userid;
        public int patientLabel;

        private Random _Random;
        //private int _maxECG;

        //public int MaxECG
        //{
        //    get { return _maxECG; }
        //    set { _maxECG = value; this.OnPropertyChanged("MaxECG"); }
        //}

        //private int _minECG;
        //public int MinECG
        //{
        //    get { return _minECG; }
        //    set { _minECG = value; this.OnPropertyChanged("MinECG"); }
        //}

        //public ECGPointCollection ecgPointCollection;
        //DispatcherTimer updateCollectionTimer;
        //private int i = 0;

        private DoctorWindow currentSplitScreen;

        public FullScreenWindow(int currentUser, int patientindex, DatabaseClass database, DoctorWindow hidden)
        {
            db = database;
            userid = currentUser;
            patientLabel = patientindex;
            currentSplitScreen = hidden;

            InitializeComponent();

            this.pateintId.Content = "Patient " + patientindex.ToString();

            //InitializeECG();
        }

        #region mockECG

        /// <summary>
        /// Initialize the mocked ECG
        /// </summary>
        //public void InitializeECG()
        //{
        //    ecgPointCollection = new ECGPointCollection();

        //    updateCollectionTimer = new DispatcherTimer();
        //    updateCollectionTimer.Interval = TimeSpan.FromMilliseconds(100);
        //    updateCollectionTimer.Tick += new EventHandler(updateCollectionTimer_Tick);
        //    updateCollectionTimer.Start();

        //    var ds = new EnumerableDataSource<ECGPoint>(ecgPointCollection);
        //    ds.SetXMapping(x => dateAxis.ConvertToDouble(x.Date));
        //    ds.SetYMapping(y => y.ECG);
        //    plotter.AddLineGraph(ds, Colors.SlateGray, 2, "ECG");
        //    plotter.VerticalAxis.Remove();
        //    MaxECG = 1;
        //    MinECG = -1;
        //}

        //void updateCollectionTimer_Tick(object sender, EventArgs e)
        //{
        //    i++;
        //    _Random = new Random();
        //    ecgPointCollection.Add(new ECGPoint(_Random.NextDouble(), DateTime.Now));
        //}
        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged(string propertyName)
        //{
        //    if (PropertyChanged != null)
        //        this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        //}

        #endregion

        /// <summary>
        /// This method is called when the collapse button is triggered.
        /// It switches the view from the current FullScreenWindow object to the object of
        /// DoctorWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            // when this method is changed to Close then it closes the entire app...
            currentSplitScreen.Show();
            currentSplitScreen = null;
            GC.Collect();
            this.Close();

        }

        /// <summary>
        /// This method is called when the note button is triggered.  It triggers the
        /// PopupWindow to be open.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoteButton_Click(object sender, RoutedEventArgs e)
        {
            PopupWindow popup = new PopupWindow();
            popup.PatientLabel.Content = "Patient " + patientLabel.ToString();
            popup.NoteTime.Content = DateTime.Now.ToString("HH:mm:ss");
            popup.ShowDialog();
        }

        /// <summary>
        /// This method is called when the mute button is triggered.  It mutes/unmutes
        /// the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            String currentIcon = muteIcon.Source.ToString();
            if (currentIcon.Contains("mute.png"))
            {
                muteIcon.BeginInit();
                muteIcon.Source = new BitmapImage(new Uri("mic.png", UriKind.RelativeOrAbsolute));
                muteIcon.EndInit();
                // add code to enable volume again
            }
            else
            {
                muteIcon.BeginInit();
                muteIcon.Source = new BitmapImage(new Uri("mute.png", UriKind.RelativeOrAbsolute));
                muteIcon.EndInit();
                // add code to mute the patient
            }
        }
    }
}
