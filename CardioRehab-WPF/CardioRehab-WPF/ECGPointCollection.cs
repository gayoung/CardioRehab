using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.DynamicDataDisplay.Common;

namespace DynamicDataDisplaySample.ECGViewModel
{
    public class ECGPointCollection : RingArray <ECGPoint>
    {
        private const int TOTAL_POINTS = 250;

        public ECGPointCollection()
            : base(TOTAL_POINTS) // here i set how much values to show 
        {    
        }
    }

    public class ECGPoint
    {        
        public double ECGtime { get; set; }
        
        public double ECG { get; set; }

        public ECGPoint(double ecg, double time)
        {
            this.ECGtime = time;
            this.ECG = ecg;
        }
    }
}
