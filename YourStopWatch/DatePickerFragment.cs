using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace YourStopWatch
{
    public class DatePickerFragment : DialogFragment, DatePickerDialog.IOnDateSetListener
    {
        public static readonly string TAG = "DatePickerFragment";
        public DateTime selectedDate;

        public static DatePickerFragment NewInstance()
        {
            DatePickerFragment frag = new DatePickerFragment();
            return frag;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            DateTime curr = DateTime.Now;
            DatePickerDialog dialog = new DatePickerDialog(Activity, this, curr.Year, curr.Month - 1, curr.Day);
            return dialog;
        }

        public async Task ShowDialog(FragmentManager manager, string tag)
        {
            base.Show(manager, tag);
            return new Task();
        }

        public void OnDateSet(DatePicker view, int year, int month, int day)
        {
            selectedDate = new DateTime(year, month + 1, day);
        }
    }
}