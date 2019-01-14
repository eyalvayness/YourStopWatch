using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System.Timers;
using Android.Graphics.Drawables.Shapes;
using Android.Graphics.Drawables;
using System;

namespace YourStopWatch
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        TextView textMessage;
        ImageView imgView;
        Bitmap bmp;
        Canvas canv;
        Paint contour, background, secPaint, minPaint, hourPaint;
        Timer timer;
        TextView timerView;
        Button startButton, stopButton, pauseButton, cancelButton;
        Time savedTime;
        int hun = 0, sec = 0, min = 0, hour = 0;
        const int maxSec = 60, maxMin = 60, maxHour = 6, bitmapLength = 500;
        const float secOffset = 100, minOffset = 50, hourOffset = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            startButton = FindViewById<Button>(Resource.Id.startButton);
            stopButton = FindViewById<Button>(Resource.Id.stopButton);
            pauseButton = FindViewById<Button>(Resource.Id.pauseButton);
            cancelButton = FindViewById<Button>(Resource.Id.cancelButton);

            timerView = FindViewById<TextView>(Resource.Id.timerView);
            imgView = FindViewById<ImageView>(Resource.Id.imgView);

            bmp = Bitmap.CreateBitmap(bitmapLength, bitmapLength, Bitmap.Config.Argb8888);
            canv = new Canvas(bmp);

            contour = new Paint();
            background = new Paint();
            secPaint = new Paint();
            minPaint = new Paint();
            hourPaint = new Paint();

            contour.Color = Color.Black;
            contour.SetStyle(Paint.Style.Stroke);
            background.Color = Color.White;
            secPaint.Color = Color.Pink;
            minPaint.Color = Color.MediumBlue;
            hourPaint.Color = Color.OrangeRed;

            canv.DrawBitmap(bmp, 0, 0, background);
            UpdateClock();

            startButton.Click += delegate
            {
                ToggleButtonStartTimer();
                RestartClock();
                timer = new Timer
                {
                    Interval = 10
                };
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            };

            pauseButton.Click += delegate
            {
                if (pauseButton.Text == "pause")
                {
                    ToggleButtonPauseTimer();
                    timer.Stop();
                    pauseButton.Text = "continue";
                }
                else if (pauseButton.Text == "continue")
                {
                    ToggleButtonStartTimer();
                    timer.Start();
                    pauseButton.Text = "pause";
                }
            };

            stopButton.Click += delegate
            {
                ToggleButtonEndTimer();
                savedTime = new Time(ExtractTimerSpan());

            };

            ToggleButtonEndTimer();

            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);
        }

        private DateTime ExtractTimerSpan()
        {
            DateTime time = new DateTime();
            time.AddMilliseconds(hun * 10);
            time.AddSeconds(sec);
            time.AddMinutes(min);
            time.AddHours(hour);
            hour = 0;
            min = 0;
            sec = 0;
            hun = 0;
            timer.Dispose();
            timer = null;
            return time;
        }

        private void RestartClock()
        {
            timerView.Text = "0:0:0:0";
            canv.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            imgView.SetImageBitmap(bmp);
        }

        private void ToggleButtonStartTimer()
        {
            startButton.Enabled = false;
            pauseButton.Enabled = true;
            cancelButton.Enabled = true;
            stopButton.Enabled = true;
        }

        private void ToggleButtonPauseTimer()
        {
            startButton.Enabled = false;
            pauseButton.Enabled = true;
            cancelButton.Enabled = false;
            stopButton.Enabled = false;
        }

        private void ToggleButtonEndTimer()
        {
            startButton.Enabled = true;
            pauseButton.Enabled = false;
            cancelButton.Enabled = false;
            stopButton.Enabled = false;
        }

        private void UpdateClock()
        {
            canv.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            canv.DrawArc(secOffset, secOffset, bitmapLength - secOffset, bitmapLength - secOffset, -90, (sec + hun / 100f) * 360f / maxSec, true, secPaint);
            canv.DrawArc(minOffset, minOffset, bitmapLength - minOffset, bitmapLength - minOffset, -90, (min + sec / 60f) * 360f / maxMin, true, minPaint);
            canv.DrawArc(hourOffset, hourOffset, bitmapLength - hourOffset, bitmapLength - hourOffset, -90, (hour + min / 60f) * 360f / maxHour, true, hourPaint);
            canv.DrawCircle(bitmapLength / 2f, bitmapLength / 2f, bitmapLength / 2f, contour);
            imgView.SetImageBitmap(bmp);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            hun++;
            if (hun == 100)
            {
                hun = 0;
                sec++;
                if (sec == 60)
                {
                    sec = 0;
                    min++;
                    if (min == 60)
                    {
                        min = 0;
                        hour++;
                    }
                }
            }
            RunOnUiThread(() => 
            {
                timerView.Text = $"{hour}:{min}:{sec}:{hun}";
                UpdateClock();
            });
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    textMessage.SetText(Resource.String.title_home);
                    return true;
                case Resource.Id.navigation_dashboard:
                    textMessage.SetText(Resource.String.title_dashboard);
                    return true;
                case Resource.Id.navigation_notifications:
                    textMessage.SetText(Resource.String.title_notifications);
                    return true;
            }
            return false;
        }
    }

    public class Time
    {
        private int year;
        private int month;
        private int day;
        private int hour;
        private int min;
        private int sec;
        
        public Time(int year, int month, int day, int hour, int min, int sec)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.hour = hour;
            this.min = min;
            this.sec = sec;
        }

        public Time(DateTime time)
        {
            this.year = time.Year;
            this.month = time.Month;
            this.day = time.Day;
            this.hour = time.Hour;
            this.min = time.Minute;
            this.sec = time.Millisecond / 10;
        }

        public override string ToString()
        {
            return $"{this.hour}:{this.min}:{this.min}";
        }
    }
}

