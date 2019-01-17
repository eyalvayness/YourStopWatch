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
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Android.Support.V7.App.AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        LinearLayout outputContainer;
        BottomNavigationView navigation;
        ImageView imgView;
        Bitmap bmp;
        Canvas canv;
        RelativeLayout container;
        Paint contour, background, secPaint, minPaint, hourPaint;
        DateTime absoluteRef;
        Timer timer;
        View addedView = null;
        TextView timerView;
        Button startButton, stopButton, pauseButton, resetButton;
        readonly string dbFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        const string dbName = "SavedTimesDataBase.db3", settingsName = "AppSettings";
        int milli = 0, sec = 0, min = 0, hour = 0, maxHour = 6, bitmapLength = 500;
        const int maxSec = 60, maxMin = 60;
        float secOffset, minOffset, hourOffset;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            GetAndApplySettings();

            container = FindViewById<RelativeLayout>(Resource.Id.container);
            LayoutTransition trans = new LayoutTransition();
            container.LayoutTransition = trans;
            addedView = null;
            StopWatchPageOutput();

            ToggleButtonsEndTimer();

            navigation = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            navigation.SetOnNavigationItemSelectedListener(this);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (GetAndApplySettings())
                UpdateTimerFromAbsoluteReference();
        }

        protected override void OnRestart()
        {
            base.OnRestart();

            if (GetAndApplySettings())
                UpdateTimerFromAbsoluteReference();
        }

        protected override void OnPause()
        {
            base.OnPause();
            SaveSettings();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SaveSettings();
        }

        private void UpdateTimerFromAbsoluteReference()
        {
            timer.Enabled = false;
            DateTime actual = DateTime.Now;
            milli = actual.Millisecond - absoluteRef.Millisecond;
            sec = actual.Second - absoluteRef.Second;
            min = actual.Minute - absoluteRef.Minute;
            hour = actual.Hour - absoluteRef.Hour;

            if (milli < 0)
            {
                milli += 1000;
                sec--;
            }
            if (sec < 0)
            {
                sec += 60;
                min--;
            }
            if (min < 0)
            {
                min += 60;
                hour--;
            }

            UpdateTimer();
            timer.Enabled = true;
        }

        private bool GetAndApplySettings()
        {
            var settings = Application.Context.GetSharedPreferences(settingsName, FileCreationMode.Private);
            bitmapLength = settings.GetInt("bitmapLength", 500);
            maxHour = settings.GetInt("maxHour", 6);
            milli = settings.GetInt("timerMilliseconds", 0);
            sec = settings.GetInt("timerSeconds", 0);
            min = settings.GetInt("timerMinutes", 0);
            hour = settings.GetInt("timerHours", 0);

            secOffset = bitmapLength / 5f;
            minOffset = bitmapLength / 10f;
            hourOffset = 0;

            return settings.GetBoolean("isTimerRunning", false);
        }

        private void SaveSettings()
        {
            var settings = Application.Context.GetSharedPreferences(settingsName, FileCreationMode.Private).Edit();
            settings.PutInt("bitmapLength", bitmapLength);
            settings.PutInt("maxHour", maxHour);
            settings.PutInt("timerMillisenconds", milli);
            settings.PutInt("timerSeconds", sec);
            settings.PutInt("timerMinutes", min);
            settings.PutInt("timerHours", hour);
            if (timer != null)
                settings.PutBoolean("isTimerRunning", timer.Enabled);
            else
                settings.PutBoolean("isTimerRunning", false);

            settings.Commit();
        }

        private void CommonPageSetup(int pageId)
        {
            if (addedView != null)
                container.RemoveView(addedView);

            LayoutInflater inflater = (LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService);
            addedView = inflater.Inflate(pageId, null);
            container.AddView(addedView);
        }

        private void StopWatchPageSetup()
        {

        }

        private void StopWatchPageOutput()
        {
            CommonPageSetup(Resource.Layout.stopwatch_layout);
            startButton = FindViewById<Button>(Resource.Id.startButton);
            stopButton = FindViewById<Button>(Resource.Id.stopButton);
            pauseButton = FindViewById<Button>(Resource.Id.pauseButton);
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

        private void ListPageOutput()
        {
            CommonPageSetup(Resource.Layout.list_layout);
            outputContainer = FindViewById<LinearLayout>(Resource.Id.outputContainer);
            TextView title = FindViewById<TextView>(Resource.Id.dashboardTitle);
            title.Gravity = GravityFlags.CenterHorizontal;

            var db = new SQLiteConnection(System.IO.Path.Combine(dbFolder, dbName));
            db.CreateTable<Time>();
            var table = db.Table<Time>();


            if (table.Count() == 0)
                Toast.MakeText(this, "There is no registered time for the moment.", ToastLength.Long).Show();

            foreach (var t in table)
            {
                LayoutInflater inflater = (LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService);
                View timeView = inflater.Inflate(Resource.Layout.single_output_layout, null);
                TextView textOutput = timeView.FindViewById<TextView>(Resource.Id.outText);
                Button removeButton = timeView.FindViewById<Button>(Resource.Id.removeButton);
                outputContainer.AddView(timeView);

                textOutput.Text = string.Format("{0}\n{1}", t.TimeSaved.ToLongDateString(), t.TimeSaved.ToLongTimeString());

                removeButton.Click += delegate
                {
                    outputContainer.RemoveView(timeView);
                    db.Delete<Time>(t.Id);
                    Toast.MakeText(this, $"The {t.TimeSaved.ToLongTimeString()} of {t.TimeSaved.ToShortDateString()} has been deleted.", ToastLength.Long).Show();
                };
            }
        }

        private void SetButtonsListeners()
        {
            resetButton.Click += delegate
            {
                var db = new SQLiteConnection(System.IO.Path.Combine(dbFolder, dbName));
                db.CreateTable<Time>();
                db.DropTable<Time>();
                db.Close();
            };

            startButton.Click += delegate
            {
                ToggleButtonsStartTimer();
                UpdateClock();
                absoluteRef = DateTime.Now;
                timer = new Timer(10);
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = true;
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

            stopButton.Click += delegate
            {
                ToggleButtonsEndTimer();
                Time savedTime = new Time
                {
                    TimeSaved = ExtractTimerSpan()
                };

                var db = new SQLiteConnection(System.IO.Path.Combine(dbFolder, dbName));
                db.CreateTable<Time>();
                db.Insert(savedTime);
                db.Close();
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
            stopButton.Enabled = true;
        }

        private void ToggleButtonsPauseTimer()
        {
            startButton.Enabled = false;
            pauseButton.Enabled = true;
            stopButton.Enabled = false;
        }

        private void ToggleButtonsEndTimer()
        {
            startButton.Enabled = true;
            pauseButton.Enabled = false;
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
            if (milli >= 1000)
            {
                milli = 0;
                sec++;
                if (sec >= 60)
                {
                    sec = 0;
                    min++;
                    UpdateTimerFromAbsoluteReference();
                    if (min >= 60)
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
                case Resource.Id.navigation_stopwatch:
                    StopWatchPageOutput();
                    return true;
                case Resource.Id.navigation_list:
                    ListPageOutput();
                    return true;
                case Resource.Id.navigation_graphics:
                    return true;
                case Resource.Id.navigation_settings:
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

