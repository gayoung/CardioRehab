using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.DynamicDataDisplay.Common;

namespace DynamicDataDisplaySample.ECGViewModel
{
    public class ECGPointCollection : RingArray <ECGPoint>
    {
        private const int TOTAL_POINTS = 30;

        public ECGPointCollection()
            : base(TOTAL_POINTS) // here i set how much values to show 
        {    
        }
    }

    public class ECGPoint
    {        
        public DateTime Date { get; set; }
        
        public double ECG { get; set; }

        public ECGPoint(double ecg, DateTime date)
        {
            this.Date = date;
            this.ECG = ecg;
        }
    }
}
