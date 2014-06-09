#region Using declarations
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Timers;
using System.Runtime.InteropServices;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using Coding4Fun.Kinect.KinectService.Common;
using Coding4Fun.Kinect.KinectService.WpfClient;
using ColorImageFormat = Microsoft.Kinect.ColorImageFormat;
using ColorImageFrame = Microsoft.Kinect.ColorImageFrame;
using System.Diagnostics;
using System.Threading;
//for regular sockets
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio;
using System.Net.NetworkInformation;
using Coding4Fun.Kinect.KinectService.Listeners;
#endregion
namespace VideoAudioTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variable declarations

        // Audio 
        WaveOut wo = new WaveOut();
        WaveFormat wf = new WaveFormat(16000, 1);
        BufferedWaveProvider mybufferwp = null;

        private WriteableBitmap outputImage;
        private byte[] pixels = new byte[0];

        //kinect sensor 
        private KinectSensorChooser sensorChooser;

        //kinect listeners
        private static ColorListener _videoListener;
        private static AudioListener _audioListener;

        //kinect clients
        private ColorClient _videoClient;
        private AudioClient _audioClient;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            //start the kinect
            InitializeKinect();
            //InitializeAudio();

        }

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

                //trying to get the video from the clinician -- this can fail
                _videoClient = new ColorClient();
                _videoClient.ColorFrameReady += _videoClient_ColorFrameReady;
                _videoClient.Connect("192.168.184.43", 4531);

                // Streaming video out on port 4555
                _videoListener = new ColorListener(this.sensorChooser.Kinect, 4555, ImageFormat.Jpeg);
                _videoListener.Start();

                //trying to get the audio from the client -- this can fail
                _audioClient = new AudioClient();
                _audioClient.AudioFrameReady += _audioClient_AudioFrameReady;
                _audioClient.Connect("192.168.184.43", 4533);

                //for sending audio
                _audioListener = new AudioListener(this.sensorChooser.Kinect, 4535);
                _audioListener.Start();

            }

        }

        private void InitializeAudio()
        {
            wo.DesiredLatency = 100;
            mybufferwp = new BufferedWaveProvider(wf);
            mybufferwp.BufferDuration = TimeSpan.FromMinutes(5);
            wo.Init(mybufferwp);
            wo.Play();
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
                    e.OldSensor.ColorStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.

                }
            }

            if (e.NewSensor != null)
            {
                try
                {
                    e.NewSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    e.NewSensor.ColorFrameReady += NewSensor_ColorFrameReady;

                }
                catch (InvalidOperationException)
                {
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

                this.MyImage.Source = outputImage;

            };

        }

        //called when a video frame from the client is ready
        void _videoClient_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {

            this.OtherImage.Source = e.ColorFrame.BitmapImage;

        }

        void _audioClient_AudioFrameReady(object sender, AudioFrameReadyEventArgs e)
        {
            if (mybufferwp != null)
            {
                mybufferwp.AddSamples(e.AudioFrame.AudioData, 0, e.AudioFrame.AudioData.Length);
            }
        }

        private void connect_button_Click(object sender, RoutedEventArgs e)
        {
            _videoClient.Connect("192.168.184.43", 4531);
            _audioClient.Connect("192.168.184.43", 4533);
        }
        #endregion

    }
}
