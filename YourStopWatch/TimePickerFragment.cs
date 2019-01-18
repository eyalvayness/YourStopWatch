using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace YourStopWatch
{
    public class TimePickerFragment : DialogFragment, TimePickerDialog.IOnTimeSetListener
    {

        public static readonly string TAG = "TimePickerFragment";
        public DateTime selectedTime;
        
        public static TimePickerFragment NewInstance()
        {
            TimePickerFragment frag = new TimePickerFragment();
            return frag;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            DateTime curr = DateTime.Now;
            bool is24h = true;
            TimePickerDialog dialog = new TimePickerDialog(Activity, this, curr.Hour, curr.Minute, is24h);
            return dialog;
        }

        public void OnTimeSet(TimePicker view, int hourOfDay, int minute)
        {
            DateTime curr = DateTime.Now;
            DateTime selected = new DateTime(curr.Year, curr.Month, curr.Day, hourOfDay, minute, 0);
            this.selectedTime = selected;
        }
    }
}