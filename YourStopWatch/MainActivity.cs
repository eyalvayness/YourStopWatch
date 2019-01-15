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
using SQLite;
using System.ComponentModel;
using Android.Content;
using Android.Animation;

namespace YourStopWatch
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Android.Support.V7.App.AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        View addedView;
        BottomNavigationView navigation;
        ImageView imgView;
        Bitmap bmp;
        Canvas canv;
        RelativeLayout container;
        Paint contour, background, secPaint, minPaint, hourPaint;
        Timer timer;
        TextView timerView;
        Button startButton, stopButton, pauseButton, cancelButton, resetButton;
        readonly string dbFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        const string dbName = "SavedTimesDataBase.db3";
        int milli = 0, sec = 0, min = 0, hour = 0;
        const int maxSec = 60, maxMin = 60, maxHour = 6, bitmapLength = 500;
        const float secOffset = 100, minOffset = 50, hourOffset = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            container = FindViewById<RelativeLayout>(Resource.Id.container);
            LayoutTransition trans = new LayoutTransition();
            container.LayoutTransition = trans;
            addedView = null;
            MainPageSetup();

            ToggleButtonsEndTimer();

            navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);
        }

        private void CommonPageSetup(int pageId)
        {
            if (addedView != null)
                container.RemoveView(addedView);

            LayoutInflater inflater = (LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService);
            addedView = inflater.Inflate(pageId, null);
            container.AddView(addedView);
        }

        private void MainPageSetup()
        {
            CommonPageSetup(Resource.Layout.home_page);
            startButton = FindViewById<Button>(Resource.Id.startButton);
            stopButton = FindViewById<Button>(Resource.Id.stopButton);
            pauseButton = FindViewById<Button>(Resource.Id.pauseButton);
            cancelButton = FindViewById<Button>(Resource.Id.cancelButton);
            resetButton = FindViewById<Button>(Resource.Id.resetButton);

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
            SetButtonsListeners();
            if (timer == null)
                ToggleButtonsEndTimer();
            else if (!timer.Enabled)
            {
                pauseButton.Text = "continue";
                ToggleButtonsPauseTimer();
            }
            else
                ToggleButtonsStartTimer();
        }

        private void DashboarPageSetup()
        {
            CommonPageSetup(Resource.Layout.dashboard_page);
        }

        private void SetButtonsListeners()
        {
            resetButton.Click += delegate
            {
                var db = new SQLiteConnection(System.IO.Path.Combine(dbFolder, dbName));
                db.CreateTable<Time>();
                db.DropTable<Time>();
            };

            startButton.Click += delegate
            {
                ToggleButtonsStartTimer();
                UpdateClock();
                timer = new Timer(10);
                timer.AutoReset = true;
                timer.Elapsed += Timer_Elapsed;
                timer.Enabled = true;
            };

            pauseButton.Click += delegate
            {
                if (timer.Enabled)
                {
                    ToggleButtonsPauseTimer();
                    timer.Enabled = false;
                    pauseButton.Text = "continue";
                }
                else if (!timer.Enabled)
                {
                    ToggleButtonsStartTimer();
                    timer.Enabled = true;
                    pauseButton.Text = "pause";
                }
            };

            cancelButton.Click += delegate
            {
                ToggleButtonsEndTimer();
                ExtractTimerSpan();
                UpdateClock();
            };

            stopButton.Click += delegate
            {
                ToggleButtonsEndTimer();
                Time savedTime = new Time
                {
                    TimeSaved = ExtractTimerSpan()
                };

                var db = new SQLiteConnection(System.IO.Path.Combine(dbFolder, dbName));
                db.CreateTable<Time>();
                if (db.Table<Time>().Count() == 0)
                    Console.WriteLine(string.Format("{0} is empty", dbName));
                db.Insert(savedTime);

                var table = db.Table<Time>();
                string toDisplay = "----------------------" + "\n";
                foreach (var t in table)
                {
                    toDisplay += string.Format("{0} : {1} {2}\n", t.Id, t.TimeSaved.ToLongDateString(), t.TimeSaved.ToLongTimeString());
                }
                Console.WriteLine(toDisplay + "-----------------------");
                UpdateClock();
            };
        }

        private DateTime ExtractTimerSpan()
        {
            DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, min, sec, milli);
            hour = 0;
            min = 0;
            sec = 0;
            milli = 0;
            UpdateClock();
            timer.Enabled = false;
            timer.Dispose();
            timer = null;
            return time;
        }

        private void ToggleButtonsStartTimer()
        {
            startButton.Enabled = false;
            pauseButton.Enabled = true;
            cancelButton.Enabled = true;
            stopButton.Enabled = true;
        }

        private void ToggleButtonsPauseTimer()
        {
            startButton.Enabled = false;
            pauseButton.Enabled = true;
            cancelButton.Enabled = false;
            stopButton.Enabled = false;
        }

        private void ToggleButtonsEndTimer()
        {
            startButton.Enabled = true;
            pauseButton.Enabled = false;
            cancelButton.Enabled = false;
            stopButton.Enabled = false;
        }

        public void UpdateClock()
        {
            timerView.Text = $"{hour}:{min}:{sec}:{milli/10}";
            canv.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            canv.DrawArc(hourOffset, hourOffset, bitmapLength - hourOffset, bitmapLength - hourOffset, -90, (hour + min / 60f) * 360f / maxHour, true, hourPaint);
            canv.DrawArc(minOffset, minOffset, bitmapLength - minOffset, bitmapLength - minOffset, -90, (min + sec / 60f) * 360f / maxMin, true, minPaint);
            canv.DrawArc(secOffset, secOffset, bitmapLength - secOffset, bitmapLength - secOffset, -90, (sec + milli / 1000f) * 360f / maxSec, true, secPaint);
            canv.DrawCircle(bitmapLength / 2f, bitmapLength / 2f, bitmapLength / 2f, contour);
            imgView.SetImageBitmap(bmp);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateTimer();
        }

        public void UpdateTimer()
        {
            milli += 10;
            if (milli == 1000)
            {
                milli = 0;
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
            RunOnUiThread(() => { UpdateClock(); });
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            if (navigation.SelectedItemId == item.ItemId)
                return false;

            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    MainPageSetup();
                    return true;
                case Resource.Id.navigation_dashboard:
                    DashboarPageSetup();
                    return true;
                case Resource.Id.navigation_notifications:
                    return true;
            }
            return false;
        }
    }

    [Table("Times")]
    public class Time : INotifyPropertyChanged
    {
        private int _id;
        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get { return _id; }
            set { this._id = value; OnPropertyChanged(nameof(Id)); }
        }

        private DateTime _timeSaved;
        [NotNull]
        public DateTime TimeSaved
        {
            get { return _timeSaved; }
            set { this._timeSaved = value; OnPropertyChanged(nameof(_timeSaved)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

