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

namespace YourStopWatch
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        TextView textMessage;
        ImageView imgView;
        Bitmap bmp;
        Canvas canv;
        Paint background, secPaint, minPaint, hourPaint;
        Timer timer;
        TextView timerView;
        Button startButton, stopButton;
        int hun = 0, sec = 0, min = 0, hour = 0;
        float secOffset = 0, minOffset = 25, hourOffset = 50;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            startButton = FindViewById<Button>(Resource.Id.startButton);
            stopButton = FindViewById<Button>(Resource.Id.stopButton);
            timerView = FindViewById<TextView>(Resource.Id.timerView);
            imgView = FindViewById<ImageView>(Resource.Id.imgView);

            bmp = Bitmap.CreateBitmap(500, 500, Bitmap.Config.Argb8888);
            canv = new Canvas(bmp);

            background = new Paint
            {
                Color = Color.White
            };
            secPaint = new Paint
            {
                Color = Color.Pink,
                               
            };
            minPaint = new Paint
            {
                Color = Color.MediumBlue,

            };
            hourPaint = new Paint
            {
                Color = Color.OrangeRed,

            };
            canv.DrawBitmap(bmp, 0, 0, background);
            UpdateClock();
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);

            startButton.Click += delegate 
            {
                RestartClock();
                timer = new Timer
                {
                    Interval = 10
                };
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            };

            stopButton.Click += delegate
            {
                if (timer == null)
                    return;
                hour = 0;
                min = 0;
                sec = 0;
                hun = 0;
                timer.Dispose();
                timer = null;
            };
        }
        
        private void RestartClock()
        {
            timerView.Text = "0:0:0:0";
            canv.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            imgView.SetImageBitmap(bmp);
        }

        private void UpdateClock()
        {
            canv.DrawArc(secOffset, secOffset, 500 - secOffset, 500 - secOffset, -90, (sec + hun / 100f)*6, true, secPaint);
            canv.DrawArc(minOffset, minOffset, 500 - minOffset, 500 - minOffset, -90, (min + sec / 60f)*6, true, minPaint);
            canv.DrawArc(hourOffset, hourOffset, 500 - hourOffset, 500 - hourOffset, -90, (hour + min / 60f)*15, true, hourPaint);
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
}

