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
using OxyPlot.Xamarin.Android;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Collections.Generic;

namespace YourStopWatch
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Android.Support.V7.App.AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        LinearLayout outputContainer;
        GridLayout listButtonsGrid;
        BottomNavigationView navigation;
        ImageView stopwatchImgView;
        Bitmap bmp;
        Canvas canv;
        RelativeLayout container, stopwatchLayout, listLayout, graphicsLayout, settingsLayout;
        Paint contour, background, secPaint, minPaint, hourPaint;
        DateTime absoluteRef, oldTimer;
        Timer timer;
        View addedView = null;
        TextView timerView;
        const string dbName = "SavedTimesDataBase.db3", settingsName = "AppSettings";
        readonly static string dbFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), dbPath = System.IO.Path.Combine(dbFolder, dbName);
        bool showCircle, areSettingsUnlocked;
        int milli = 0, sec = 0, min = 0, hour = 0, maxHour = 6, bitmapLength = 500, maxHourWeekly;
        const int maxSec = 60, maxMin = 60;
        float secOffset, minOffset, hourOffset;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            container = FindViewById<RelativeLayout>(Resource.Id.container);
            container.LayoutTransition = new LayoutTransition();
            addedView = null;
            navigation = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            navigation.SetOnNavigationItemSelectedListener(this);

            GetAndApplySettings();
            
            stopwatchLayout = StopWatchLayoutSetup();
            listLayout = ListLayoutSetup();
            graphicsLayout = GraphicsLayoutSetup();
            settingsLayout = SettingsLayoutSetup();

            StopWatchLayoutOutput();

            ToggleButtons(ButtonsState.End);

        }

        protected override void OnResume()
        {
            base.OnResume();

            if (GetAndApplySettings())
                UpdateTimerFromAbsoluteReference();
        }

        protected override void OnPause()
        {
            base.OnPause();
            SaveSettings(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SaveSettings(false);
        }

        private void UpdateTimerFromAbsoluteReference()
        {
            timer.Enabled = false;
            DateTime actual = DateTime.Now;
            milli = actual.Millisecond - absoluteRef.Millisecond;
            sec = actual.Second - absoluteRef.Second;
            min = actual.Minute - absoluteRef.Minute;
            hour = actual.Hour - absoluteRef.Hour;

            MapTimerValues();

            milli += oldTimer.Millisecond;
            sec += oldTimer.Second;
            min += oldTimer.Minute;
            hour += oldTimer.Hour;

            MapTimerValues();

            oldTimer = new DateTime(actual.Year, actual.Month, actual.Day, hour, min, sec, milli);
            absoluteRef = actual;

            UpdateTimer();
            timer.Enabled = true;
        }

        public void MapTimerValues()
        {
            while (milli < 0)
            {
                milli += 1000;
                sec--;
            }
            while (sec < 0)
            {
                sec += 60;
                min--;
            }
            while (min < 0)
            {
                min += 60;
                hour--;
            }

            while (milli >= 1000)
            {
                milli -= 1000;
                sec++;
            }
            while (sec >= 60)
            {
                sec -= 60;
                min++;
            }
            while (min >= 60)
            {
                min -= 60;
                hour++;
            }
        }

        public void GetNewTimer()
        {
            timer = new Timer(10);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
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
            showCircle = settings.GetBoolean("showCircle", true);
            areSettingsUnlocked = settings.GetBoolean("areSettingsUnlocked", true);
            maxHourWeekly = settings.GetInt("maxHourWeekly", 14);

            secOffset = bitmapLength / 5f;
            minOffset = bitmapLength / 10f;
            hourOffset = 0;

            bool wasTimerRunning = settings.GetBoolean("isTimerRunning", false);

            oldTimer = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, min, sec, milli);

            if (wasTimerRunning)
                absoluteRef = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
            settings.GetInt("refMilliseconds", 0),
            settings.GetInt("refSeconds", 0),
            settings.GetInt("refMinutes", 0),
            settings.GetInt("refHours", 0));

            return wasTimerRunning;
        }

        private void SaveSettings(bool keepActivity)
        {
            var settings = Application.Context.GetSharedPreferences(settingsName, FileCreationMode.Private).Edit();
            settings.PutInt("bitmapLength", bitmapLength);
            settings.PutInt("maxHour", maxHour);
            settings.PutInt("timerMillisenconds", milli);
            settings.PutInt("timerSeconds", sec);
            settings.PutInt("timerMinutes", min);
            settings.PutInt("timerHours", hour);
            settings.PutInt("maxHourWeekly", maxHourWeekly);

            if (timer != null && keepActivity)
            {
                settings.PutBoolean("isTimerRunning", timer.Enabled);
                settings.PutInt("refMillisenconds", DateTime.Now.Millisecond);
                settings.PutInt("refSeconds",  DateTime.Now.Second);
                settings.PutInt("refMinutes", DateTime.Now.Minute);
                settings.PutInt("refHours", DateTime.Now.Hour);
            }
            else
                settings.PutBoolean("isTimerRunning", false);

            settings.PutBoolean("areSettingsUnlocked", areSettingsUnlocked);
            settings.PutBoolean("showCircle", showCircle);
            settings.Commit();
        }

        private RelativeLayout StopWatchLayoutSetup()
        {
            RelativeLayout layout = (RelativeLayout)((LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.stopwatch_layout, null);

            Button startButton = layout.FindViewById<Button>(Resource.Id.startButton);
            Button stopButton = layout.FindViewById<Button>(Resource.Id.stopButton);
            Button pauseButton = layout.FindViewById<Button>(Resource.Id.pauseButton);

            timerView = layout.FindViewById<TextView>(Resource.Id.timerView);
            stopwatchImgView = layout.FindViewById<ImageView>(Resource.Id.imgView);

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

            SetButtonsListeners(startButton, stopButton, pauseButton);

            return layout;
        }

        private RelativeLayout ListLayoutSetup()
        {
            RelativeLayout layout = (RelativeLayout)((LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.list_layout, null);
            outputContainer = layout.FindViewById<LinearLayout>(Resource.Id.outputContainer);
            outputContainer.LayoutTransition = new LayoutTransition();
            listButtonsGrid = layout.FindViewById<GridLayout>(Resource.Id.listButtonsGrid);
            Button resetAllButton = layout.FindViewById<Button>(Resource.Id.resetAllButton);
            Button manualAdd = layout.FindViewById<Button>(Resource.Id.directAdd);

            var db = new SQLiteConnection(dbPath);
            db.CreateTable<Time>();
            var table = db.Table<Time>().OrderBy(t => t.TimeSaved);

            foreach (var t in table)
                AddTimeToOutputList(t, new SQLiteConnection(dbPath));

            manualAdd.Click += delegate
            {
                var dateBuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
                DatePicker datePicker = new DatePicker(this);
                dateBuilder.SetView(datePicker);
                dateBuilder.SetNegativeButton("Cancel", delegate { return; });
                dateBuilder.SetPositiveButton("OK", delegate 
                {
                    var timeBuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
                    TimePicker timePicker = new TimePicker(this);
                    timePicker.SetIs24HourView(new Java.Lang.Boolean(true));
                    timePicker.Hour = 0;
                    timePicker.Minute = 0;
                    timeBuilder.SetView(timePicker);
                    timeBuilder.SetNegativeButton("Cancel", delegate { return; });
                    timeBuilder.SetPositiveButton("Save Time", delegate 
                    {
                        Time t = new Time
                        {
                            TimeSaved = new DateTime(datePicker.Year, datePicker.Month + 1, datePicker.DayOfMonth, timePicker.Hour, timePicker.Minute, 0)
                        };
                        db.Insert(t);
                        AddTimeToOutputList(t, db);
                    });
                    timeBuilder.Create().Show();
                });
                dateBuilder.Create().Show();
            };

            resetAllButton.Click += delegate
            {
                var alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Confirmation");
                alert.SetMessage("Do you really want to permanently delete all of the recorded times ?");

                alert.SetPositiveButton("YES", delegate 
                {
                    db.DropTable<Time>();
                    outputContainer.RemoveAllViews();
                    resetAllButton.Enabled = false;
                });
                alert.SetNegativeButton("NO", delegate { return; });
                alert.Create().Show();
            };

            return layout;
        }

        private RelativeLayout GraphicsLayoutSetup()
        {
            RelativeLayout layout = (RelativeLayout)((LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.graphics_layout, null);
            TextView title = layout.FindViewById<TextView>(Resource.Id.graphicsTitle);
            Spinner spinner = layout.FindViewById<Spinner>(Resource.Id.spinner_dropdown);

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, new string[] { "This Week", "This Month", "This Year" });
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;
            spinner.ItemSelected += (s, e) =>
            {
                string choosed = e.Parent.GetItemAtPosition(e.Position).ToString();
                switch (choosed)
                {
                    case "This Week":
                        UpdateGraphics(GraphicTimeLine.Week);
                        break;
                    case "This Month":
                        UpdateGraphics(GraphicTimeLine.Month);
                        break;
                    case "This Year":
                        UpdateGraphics(GraphicTimeLine.Year);
                        break;
                    default:
                        break;
                }
            };
            return layout;
        }

        private RelativeLayout SettingsLayoutSetup()
        {
            RelativeLayout layout = (RelativeLayout)((LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.settings_layout, null);
            SeekBar radiusSB = layout.FindViewById<SeekBar>(Resource.Id.radius_seek_bar);
            TextView radiusSBValue = layout.FindViewById<TextView>(Resource.Id.seekbar_value);
            ToggleButton toggleBoxShowCircle = layout.FindViewById<ToggleButton>(Resource.Id.show_circle_checkbox);
            NumberPicker maxHourPicker = layout.FindViewById<NumberPicker>(Resource.Id.max_hour_picker);
            EditText maxWeeklyEdit = layout.FindViewById<EditText>(Resource.Id.max_weekly_edit_text);
            RadioButton lockParams = layout.FindViewById<RadioButton>(Resource.Id.lock_params);

            lockParams.Checked = !areSettingsUnlocked;
            radiusSB.Enabled = areSettingsUnlocked;
            toggleBoxShowCircle.Enabled = areSettingsUnlocked;
            maxHourPicker.Enabled = areSettingsUnlocked;
            maxWeeklyEdit.Enabled = areSettingsUnlocked;

            lockParams.CheckedChange += delegate
            {
                areSettingsUnlocked = !lockParams.Checked;
                radiusSB.Enabled = areSettingsUnlocked;
                toggleBoxShowCircle.Enabled = areSettingsUnlocked;
                maxHourPicker.Enabled = areSettingsUnlocked;
                maxWeeklyEdit.Enabled = areSettingsUnlocked;
            };

            maxWeeklyEdit.Text = maxHourWeekly.ToString();
            maxWeeklyEdit.TextChanged += delegate 
            {
                try
                {
                    maxHourWeekly = int.Parse(maxWeeklyEdit.Text);
                }
                catch (Exception)
                {
                    return;
                }
            };

            maxHourPicker.MinValue = 1;
            maxHourPicker.MaxValue = 8;
            maxHourPicker.Value = maxHour;
            maxHourPicker.ValueChanged += delegate
            {
                maxHour = maxHourPicker.Value;
            };

            radiusSB.SetProgress(bitmapLength, false);
            radiusSBValue.Text = radiusSB.Progress.ToString() + "px";
            radiusSB.ProgressChanged += delegate
            {
                radiusSBValue.Text = radiusSB.Progress.ToString() + "px";
                bitmapLength = radiusSB.Progress;

                secOffset = bitmapLength / 5f;
                minOffset = bitmapLength / 10f;
            };

            toggleBoxShowCircle.TextOff = "Off";
            toggleBoxShowCircle.TextOn = "On";
            toggleBoxShowCircle.Checked = showCircle;
            toggleBoxShowCircle.CheckedChange += delegate
            {
                showCircle = toggleBoxShowCircle.Checked;
            };

            return layout;
        }

        private void CommonPageOutput(RelativeLayout layout)
        {
            if (addedView != null)
                container.RemoveView(addedView);
            container.AddView(layout);
            addedView = layout;
        }

        private void StopWatchLayoutOutput()
        {
            bmp = Bitmap.CreateBitmap(bitmapLength, bitmapLength, Bitmap.Config.Argb8888);
            canv = new Canvas(bmp);

            CommonPageOutput(stopwatchLayout);

            UpdateClock();
            ButtonsState state;
            if (timer == null)
                state = ButtonsState.End;
            else if (!timer.Enabled)
                state = ButtonsState.Pause;
            else
                state = ButtonsState.Start;

            ToggleButtons(state);
        }

        private void ListLayoutOutput()
        {
            CommonPageOutput(listLayout);
            Button resetAllButton = FindViewById<Button>(Resource.Id.resetAllButton);

            if (outputContainer.ChildCount == 0)
            {
                Toast.MakeText(this, "There is no registered time for the moment.", ToastLength.Long).Show();
                resetAllButton.Enabled = false;
            }
            else
                resetAllButton.Enabled = true;
        }

        private void GrachicsLayoutOutput()
        {
            CommonPageOutput(graphicsLayout);
            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner_dropdown);
            switch (spinner.SelectedItem.ToString())
            {
                case "This Week":
                    UpdateGraphics(GraphicTimeLine.Week);
                    break;
                case "This Month":
                    UpdateGraphics(GraphicTimeLine.Month);
                    break;
                case "This Year":
                    UpdateGraphics(GraphicTimeLine.Year);
                    break;
                default:
                    break;
            }
        }

        private void UpdateGraphics(GraphicTimeLine timeLine)
        {
            float[] hourPerSubDiv = new float[(int)timeLine];
            PlotView plotView = graphicsLayout.FindViewById<PlotView>(Resource.Id.plotView);
            PlotModel model = new PlotModel();

            DateTimeAxis dateTimeAxis = new DateTimeAxis { Position = AxisPosition.Bottom, Minimum = -0.5, Maximum = (int)timeLine - 0.5, IsZoomEnabled = false, IsPanEnabled = false, IsAxisVisible = false };
            CategoryAxis categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Minimum = -0.5, Maximum = (int)timeLine - 0.5, LabelFormatter = GetCorrectLabelFormatting(timeLine), IsZoomEnabled = false, IsPanEnabled = false };
            LinearAxis oridnateAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, MinimumRange = 4, IsZoomEnabled = false, IsPanEnabled = false};

            LineSeries meanSeries = new LineSeries { MarkerSize = 0, Color = OxyColors.Red };
            ColumnSeries subDivSeries = new ColumnSeries { FillColor = OxyColors.Green, StrokeColor = OxyColors.Green };
            ColumnSeries todaySeries = new ColumnSeries { FillColor = OxyColors.Orange, StrokeColor = OxyColors.Orange };

            var db = new SQLiteConnection(dbPath);
            db.CreateTable<Time>();
            var table = db.Table<Time>();

            if (timeLine == GraphicTimeLine.Week)
            {
                foreach (Time t in table)
                {
                    if (t.TimeSaved.Year == DateTime.Now.Year && GetWeekDiference(DateTime.Now, t.TimeSaved) == 0)
                        hourPerSubDiv[GetDayOfWeek(t.TimeSaved)] += t.TimeSaved.Hour + t.TimeSaved.Minute / 60f;
                }

                for (int i = 0; i < (int)timeLine; i++)
                {
                    if (i == GetDayOfWeek(DateTime.Now))
                    {
                        todaySeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                        todaySeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                    }
                    else
                    {
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                    }
                }
                meanSeries.Points.Add(new DataPoint(-0.5, maxHourWeekly / 7f));
                meanSeries.Points.Add(new DataPoint((int)timeLine - 0.5, maxHourWeekly / 7f));
            }
            else if (timeLine == GraphicTimeLine.Month)
            {
                //DateTime endOfWeek = DateTime.Now.DayOfWeek != 0 ? DateTime.Now.AddDays(7 - (int)DateTime.Now.DayOfWeek) : DateTime.Now;
                foreach (Time t in table)
                {
                    if (GetWeekDiference(DateTime.Now, t.TimeSaved) < 4)
                        hourPerSubDiv[3 - GetWeekDiference(DateTime.Now, t.TimeSaved)] += t.TimeSaved.Hour + t.TimeSaved.Minute / 60f;
                }

                for (int i = 0; i < (int)timeLine; i++)
                {
                    if (i + 1 == (int)timeLine)
                    {
                        todaySeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                        todaySeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                    }
                    else
                    {
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                    }
                }

                meanSeries.Points.Add(new DataPoint(-0.5, maxHourWeekly));
                meanSeries.Points.Add(new DataPoint((int)timeLine - 0.5, maxHourWeekly));
            }
            else if (timeLine == GraphicTimeLine.Year)
            {
                foreach (Time t in table)
                {
                    if (t.TimeSaved.Year == DateTime.Now.Year)
                        hourPerSubDiv[t.TimeSaved.Month - 1] += t.TimeSaved.Hour + t.TimeSaved.Minute / 60f;
                }

                for (int i = 0; i < (int)timeLine; i++)
                {
                    if (i + 1 == DateTime.Now.Month)
                    {
                        todaySeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                        todaySeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                    }
                    else
                    {
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                    }
                }
                meanSeries.Points.Add(new DataPoint(-0.5, maxHourWeekly * 5));
                meanSeries.Points.Add(new DataPoint((int)timeLine - 0.5, maxHourWeekly * 5));
            }

            model.Axes.Add(categoryAxis);
            model.Axes.Add(oridnateAxis);
            model.Series.Add(subDivSeries);
            model.Series.Add(todaySeries);

            model.Axes.Add(dateTimeAxis);
            model.Series.Add(meanSeries);

            plotView.Model = model;
        }

        private void SettingsLayoutOutput()
        {
            CommonPageOutput(settingsLayout);
        }

        private int GetWeekDiference(DateTime actual, DateTime other)
        {
            int i = 0;
            while (Math.Abs(GetDayOfWeek(other) - GetDayOfWeek(actual)) != Math.Abs(other.DayOfYear - actual.AddDays(-7 * i).DayOfYear))
            {
                i++;
            }
            return i;
        }

        private int GetDayOfWeek(DateTime time)
        {
            return ((int)time.DayOfWeek + 8) % 7 - 1;
        }

        private Func<double, string> GetCorrectLabelFormatting(GraphicTimeLine timeLine)
        {
            switch (timeLine)
            {
                case GraphicTimeLine.Week:
                    return GetDayOfWeekName;
                case GraphicTimeLine.Month:
                    return GetWeekNumber;
                case GraphicTimeLine.Year:
                    return GetMonthName;
                default:
                    return null;
            }
        }

        private string GetDayOfWeekName(double input)
        {
            string[] dayOfWeekNames = new string[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            return dayOfWeekNames[(int)input];
        }

        private string GetWeekNumber(double input)
        {
            string[] weekNumber = new string[] { "Week #1", "Week #2", "Week #3", "Week #4"};
            return weekNumber[(int)input];
        }

        private string GetMonthName(double input)
        {
            string[] monthName = new string[] { "Jan", "Feb", "Mar", "Avr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            return monthName[(int)input];
        }

        private void SetButtonsListeners(Button startButton, Button stopButton, Button pauseButton)
        {
            startButton.Click += delegate
            {
                ToggleButtons(ButtonsState.Start);
                UpdateClock();
                absoluteRef = DateTime.Now;
                oldTimer = new DateTime(absoluteRef.Year, absoluteRef.Month, absoluteRef.Day, hour, min, sec, milli);
                GetNewTimer();
                timer.Enabled = true;
            };

            pauseButton.Click += delegate
            {
                ToggleButtons(ButtonsState.Start);
                if (timer.Enabled)
                {
                    timer.Enabled = false;
                    pauseButton.Text = "continue";
                }
                else if (!timer.Enabled)
                {
                    absoluteRef = DateTime.Now;
                    oldTimer = new DateTime(absoluteRef.Year, absoluteRef.Month, absoluteRef.Day, hour, min, sec, milli);
                    timer.Enabled = true;
                    pauseButton.Text = "pause";
                }
            };

            stopButton.Click += delegate
            {
                ToggleButtons(ButtonsState.End);
                Time savedTime = new Time
                {
                    TimeSaved = ExtractTimerSpan()
                };

                var db = new SQLiteConnection(dbPath);
                db.CreateTable<Time>();
                db.Insert(savedTime);
                AddTimeToOutputList(savedTime, db);
                UpdateClock();
            };
        }

        private void AddTimeToOutputList(Time t, SQLiteConnection db)
        {
            LayoutInflater inflater = (LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService);
            View timeView = inflater.Inflate(Resource.Layout.single_output_layout, null);
            TextView textOutput = timeView.FindViewById<TextView>(Resource.Id.outText);
            Button removeButton = timeView.FindViewById<Button>(Resource.Id.removeButton);
            outputContainer.AddView(timeView, outputContainer.IndexOfChild(listButtonsGrid));

            textOutput.Text = string.Format("{0}\n{1}", t.TimeSaved.ToLongDateString(), t.TimeSaved.ToShortTimeString());

            removeButton.Click += delegate
            {
                var alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Confirmation");
                alert.SetMessage("Do you really want to permanently delete this recorded time ?");

                alert.SetPositiveButton("YES", delegate
                {
                    outputContainer.RemoveView(timeView);
                    db.Delete<Time>(t.Id);
                    db.Close();
                    Toast.MakeText(this, $"The {t.TimeSaved.ToLongTimeString()} of {t.TimeSaved.ToShortDateString()} has been deleted.", ToastLength.Long).Show();
                });
                alert.SetNegativeButton("NO", delegate { return; });
                alert.Create().Show();
            };

            if (graphicsLayout == null)
                return;
            Spinner spinner = graphicsLayout.FindViewById<Spinner>(Resource.Id.spinner_dropdown);
            if (spinner == null)
                return;
            switch (spinner.SelectedItem.ToString())
            {
                case "This Week":
                    UpdateGraphics(GraphicTimeLine.Week);
                    break;
                case "This Month":
                    UpdateGraphics(GraphicTimeLine.Month);
                    break;
                case "This Year":
                    UpdateGraphics(GraphicTimeLine.Year);
                    break;
                default:
                    break;
            }
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

        private void ToggleButtons(ButtonsState state)
        {
            Button startButton = stopwatchLayout.FindViewById<Button>(Resource.Id.startButton);
            Button stopButton = stopwatchLayout.FindViewById<Button>(Resource.Id.stopButton);
            Button pauseButton = stopwatchLayout.FindViewById<Button>(Resource.Id.pauseButton);

            if (state == ButtonsState.Start)
            {
                startButton.Enabled = false;
                pauseButton.Enabled = true;
                stopButton.Enabled = true;
            }
            else if (state == ButtonsState.Pause)
            {
                startButton.Enabled = false;
                pauseButton.Enabled = true;
                stopButton.Enabled = false;
            }
            else if (state == ButtonsState.End)
            {
                startButton.Enabled = true;
                pauseButton.Enabled = false;
                stopButton.Enabled = false;
            }
        }

        public void UpdateClock()
        {
            timerView.Text = $"{hour}:{min}:{sec}:{milli/10}";
            canv.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            canv.DrawArc(hourOffset, hourOffset, bitmapLength - hourOffset, bitmapLength - hourOffset, -90, (hour + min / 60f + sec / 3600f) * 360f / maxHour, true, hourPaint);
            canv.DrawArc(minOffset, minOffset, bitmapLength - minOffset, bitmapLength - minOffset, -90, (min + sec / 60f) * 360f / maxMin, true, minPaint);
            canv.DrawArc(secOffset, secOffset, bitmapLength - secOffset, bitmapLength - secOffset, -90, (sec + milli / 1000f) * 360f / maxSec, true, secPaint);
            if (showCircle)
                canv.DrawCircle(bitmapLength / 2f, bitmapLength / 2f, bitmapLength / 2f, contour);
            stopwatchImgView.SetImageBitmap(bmp);
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
                    StopWatchLayoutOutput();
                    return true;
                case Resource.Id.navigation_list:
                    ListLayoutOutput();
                    return true;
                case Resource.Id.navigation_graphics:
                    GrachicsLayoutOutput();
                    return true;
                case Resource.Id.navigation_settings:
                    SettingsLayoutOutput();
                    return true;
            }
            return false;
        }
    }
    
    public enum GraphicTimeLine
    {
        Week = 7,
        Month = 4,
        Year = 12
    }

    public enum ButtonsState
    {
        Start,
        End,
        Pause
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

