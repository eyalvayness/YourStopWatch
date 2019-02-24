using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using System.Timers;
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
using Microsoft.WindowsAzure.MobileServices;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Android.Gms.Auth.Api;
using Android.Gms.Common;
using Android.Runtime;
using Xamarin.Auth;
using System.Threading.Tasks;
using Android.Support.V4.Widget;

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
        RelativeLayout container = null, stopwatchLayout = null, listLayout = null, graphicsLayout = null, settingsLayout = null;
        Paint contour, background, secPaint, minPaint, hourPaint;
        DateTime absoluteRef, oldTimer;
        Timer timer;
        View addedView = null;
        TextView timerView;
        const string dbName = "SavedTimesDataBase.db3", settingsName = "AppSettings";
        readonly Dictionary<string, GraphicTimeLine> spinnerPossibilities = new Dictionary<string, GraphicTimeLine> { { "This Week", GraphicTimeLine.ThisWeek }, { "Last Week", GraphicTimeLine.LastWeek }, { "This Month", GraphicTimeLine.ThisMonth }, /*{ "Month", GraphicTimeLine.Month },*/ { "This Year", GraphicTimeLine.ThisYear } };
        readonly Dictionary<GraphicTimeLine, int> subDivPerTimeLine = new Dictionary<GraphicTimeLine, int> { { GraphicTimeLine.ThisWeek, 7 }, { GraphicTimeLine.LastWeek, 7 }, { GraphicTimeLine.ThisMonth, 4 }, { GraphicTimeLine.ThisYear, 12 } };
        readonly static string dbFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), dbPath = System.IO.Path.Combine(dbFolder, dbName);
        bool showCircle = true, areSettingsUnlocked = true, showAverageHour = true;
        int milli = 0, sec = 0, min = 0, hour = 0;
        int maxHour = 6, bitmapLength = 600, maxHourWeekly = 14;
        const int maxSec = 60, maxMin = 60;
        float secOffset, minOffset, hourOffset;

        MobileServiceClient client = new MobileServiceClient("https://yourstopwatchapp.azurewebsites.net");
        StopwatchUser currentUser = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            container = FindViewById<RelativeLayout>(Resource.Id.container);
            container.LayoutTransition = new LayoutTransition();
            addedView = null;
            navigation = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            navigation.SetOnNavigationItemSelectedListener(this);

            CurrentPlatform.Init();
            GetAndApplySettings();
            GetAndApplyUser();

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
            {
                UpdateTimerFromAbsoluteReference();
                if (timer != null)
                {
                    ToggleButtons(ButtonsState.Start);
                }
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            SaveUserAndSettings(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SaveUserAndSettings(false);
        }

        private void UpdateTimerFromAbsoluteReference()
        {
            if (timer == null)
                GetNewTimer();
            timer.Enabled = false;
            DateTime actual = DateTime.Now;
            milli = actual.Millisecond - absoluteRef.Millisecond + oldTimer.Millisecond;
            sec = actual.Second - absoluteRef.Second + oldTimer.Second;
            min = actual.Minute - absoluteRef.Minute + oldTimer.Minute;
            hour = actual.Hour - absoluteRef.Hour + oldTimer.Hour;

            int dayOffset = MapTimerValues();

            oldTimer = new DateTime(actual.Year, actual.Month, actual.Day + dayOffset, hour, min, sec, milli);
            absoluteRef = actual;

            UpdateTimer();
            timer.Enabled = true;
        }

        public int MapTimerValues()
        {
            int day = 0;

            while (milli < 0 || milli >= 1000)
            {
                if (milli < 0)
                {
                    milli += 1000;
                    sec--;
                }
                if (milli >= 1000)
                {
                    milli -= 1000;
                    sec++;
                }
            }
            while (sec < 0 || sec >= 60)
            {
                if (sec < 0)
                {
                    sec += 60;
                    min--;
                }
                if (sec >= 60)
                {
                    sec -= 60;
                    min++;
                }
            }
            while (min < 0 || min >= 60)
            {
                if (min < 0)
                {
                    min += 60;
                    hour--;
                }
                if (min >= 60)
                {
                    min -= 60;
                    hour++;
                }
            }

            while (hour < 0 || hour >= 24)
            {
                if (hour < 0)
                {
                    day++;
                    hour += 24;
                }
                if (hour >= 24)
                {
                    day--;
                    hour -= 24;
                }
            }

            return day;

            //while (milli >= 1000)
            //{
            //    milli -= 1000;
            //    sec++;
            //}
            //while (sec >= 60)
            //{
            //    sec -= 60;
            //    min++;
            //}
            //while (min >= 60)
            //{
            //    min -= 60;
            //    hour++;
            //}
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

            milli = settings.GetInt("timerMilliseconds", 0);
            sec = settings.GetInt("timerSeconds", 0);
            min = settings.GetInt("timerMinutes", 0);
            hour = settings.GetInt("timerHours", 0);

            bitmapLength = settings.GetInt("bitmapLength", 600);
            maxHour = settings.GetInt("maxHour", 6);
            maxHourWeekly = settings.GetInt("maxHourWeekly", 14);
            showAverageHour = settings.GetBoolean("showAverageHour", true);
            showCircle = settings.GetBoolean("showCircle", true);
            areSettingsUnlocked = settings.GetBoolean("areSettingsUnlocked", true);

            secOffset = bitmapLength / 5f;
            minOffset = bitmapLength / 10f;
            hourOffset = 0;

            bool wasTimerRunning = settings.GetBoolean("isTimerRunning", false);

            oldTimer = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, min, sec, milli);

            if (wasTimerRunning)
                absoluteRef = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
            settings.GetInt("refHours", 0),
            settings.GetInt("refMinutes", 0),
            settings.GetInt("refSeconds", 0),
            settings.GetInt("refMilliseconds", 0));

            return wasTimerRunning;
        }

        private async void GetAndApplyUser()
        {
            var settings = Application.Context.GetSharedPreferences(settingsName, FileCreationMode.Private);
            string userId = settings.GetString("userId", null);
            if (userId == null)
            {
                SignOrLogInStopwatchUser(false);
            }
            else
            {
                List<StopwatchUser> tempList = await client.GetTable<StopwatchUser>().Where(user => user.Id == userId).ToListAsync();
                if (tempList.Count > 0)
                    currentUser = tempList.ToArray()[0];
                Toast.MakeText(this, $"You are now logged in as \"{currentUser.Name}\"", ToastLength.Long).Show();
            }
        }

        private void SignOrLogInStopwatchUser(bool cancelable)
        {
            var alert = new Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle("No user connected");
            alert.SetCancelable(cancelable);
            alert.SetMessage("In order to register any recorded time, please log in or register to the app");
            alert.SetPositiveButton("Log in / Register", delegate
            {
                View signInLayout = ((LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.sign_in_layout, null);
                var alert2 = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert2.SetTitle("Log in / Register");
                alert2.SetCancelable(false);
                alert2.SetView(signInLayout);
                alert2.SetPositiveButton("Continue", async delegate
                {
                    string userName = signInLayout.FindViewById<EditText>(Resource.Id.usernameEditText).Text;
                    //string password = signInLayout.FindViewById<EditText>(Resource.Id.passwordEditText).Text;
                    List<StopwatchUser> tempList = await client.GetTable<StopwatchUser>().Where(user => user.Name == userName).ToListAsync();
                    if (tempList.Count > 0)
                    {
                        currentUser = tempList.ToArray()[0];
                        RunOnUiThread(() => 
                        {
                            Toast.MakeText(this, $"You are now logged in as {currentUser.Name}", ToastLength.Long).Show();
                            if (settingsLayout != null && settingsLayout.FindViewById<TextView>(Resource.Id.setting_account) != null)
                                settingsLayout.FindViewById<TextView>(Resource.Id.setting_account).Text = $"You are logged in as \"{currentUser.Name}\"";
                        });
                    }
                    else
                    {
                        currentUser = new StopwatchUser { Name = userName };
                        await client.GetTable<StopwatchUser>().InsertAsync(currentUser);
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, $"You were registered as {currentUser.Name}", ToastLength.Long).Show();
                            if (settingsLayout != null && settingsLayout.FindViewById<TextView>(Resource.Id.setting_account) != null)
                                settingsLayout.FindViewById<TextView>(Resource.Id.setting_account).Text = $"You are logged in as \"{currentUser.Name}\"";
                        });
                    }
                });
                alert2.Create().Show();
            });
            alert.Create().Show();
        }

        private void SaveUserAndSettings(bool keepActivity)
        {
            var settings = Application.Context.GetSharedPreferences(settingsName, FileCreationMode.Private).Edit();
            settings.PutInt("timerMillisenconds", milli);
            settings.PutInt("timerSeconds", sec);
            settings.PutInt("timerMinutes", min);
            settings.PutInt("timerHours", hour);
            settings.PutString("userId", currentUser?.Id);


            settings.PutInt("bitmapLength", bitmapLength);
            settings.PutInt("maxHour", maxHour);
            settings.PutInt("maxHourWeekly", maxHourWeekly);
            settings.PutBoolean("showAverageHour", showAverageHour);
            settings.PutBoolean("showCircle", showCircle);
            settings.PutBoolean("areSettingsUnlocked", areSettingsUnlocked);

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

            SwipeRefreshLayout refresher = layout.FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefresher);
            outputContainer = layout.FindViewById<LinearLayout>(Resource.Id.outputContainer);
            outputContainer.LayoutTransition = new LayoutTransition();
            listButtonsGrid = layout.FindViewById<GridLayout>(Resource.Id.listButtonsGrid);
            Button resetAllButton = layout.FindViewById<Button>(Resource.Id.resetAllButton);
            Button manualAdd = layout.FindViewById<Button>(Resource.Id.directAdd);

            OutputListLayoutReset();

            refresher.Refresh += delegate
            {
                OutputListLayoutReset();
                refresher.Refreshing = false;
            };

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
                    timeBuilder.SetPositiveButton("Save Time", async delegate
                    {
                        //Time t = new Time { SavedTime = new DateTime(datePicker.Year, datePicker.Month + 1, datePicker.DayOfMonth, timePicker.Hour, timePicker.Minute, 0) };
                        //var db = new SQLiteConnection(dbPath);
                        //db.CreateTable<Time>();
                        //db.Insert(t);
                        //db.Close();
                        if (currentUser == null)
                            return;
                        RegisteredTime t = new RegisteredTime { SavedTime = new DateTime(datePicker.Year, datePicker.Month + 1, datePicker.DayOfMonth, timePicker.Hour, timePicker.Minute, 0), TimeUserId = currentUser.Id, createdAt = DateTime.Now};
                        await client.GetTable<RegisteredTime>().InsertAsync(t);

                        OutputListLayoutReset();
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

                alert.SetPositiveButton("YES", async delegate
                {
                    //var db = new SQLiteConnection(dbPath);
                    //db.CreateTable<Time>();
                    //db.DropTable<Time>();
                    if (currentUser == null)
                        return;
                    var table = await client.GetTable<RegisteredTime>().Where(t => t.TimeUserId == currentUser.Id).ToEnumerableAsync();

                    foreach (RegisteredTime t in table)
                        await client.GetTable<RegisteredTime>().DeleteAsync(t);
                    outputContainer.RemoveAllViews();
                    outputContainer.AddView(listButtonsGrid);
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

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, new List<string>(spinnerPossibilities.Keys));
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;
            spinner.ItemSelected += delegate { UpdateGraphicsLayout(); };
            return layout;
        }

        private RelativeLayout SettingsLayoutSetup()
        {
            RelativeLayout layout = (RelativeLayout)((LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.settings_layout, null);
            SeekBar radiusSB = layout.FindViewById<SeekBar>(Resource.Id.radius_seek_bar);
            Button loginButton = layout.FindViewById<Button>(Resource.Id.loginButton);
            TextView accountText = layout.FindViewById<TextView>(Resource.Id.setting_account);
            TextView radiusSBValue = layout.FindViewById<TextView>(Resource.Id.seekbar_value);
            NumberPicker maxHourPicker = layout.FindViewById<NumberPicker>(Resource.Id.max_hour_picker);
            EditText maxWeeklyEdit = layout.FindViewById<EditText>(Resource.Id.max_weekly_edit_text);
            Switch switchBoxShowCircle = layout.FindViewById<Switch>(Resource.Id.show_circle_checkbox);
            Switch switchShowAverage = layout.FindViewById<Switch>(Resource.Id.show_average_hour);
            Switch lockParams = layout.FindViewById<Switch>(Resource.Id.lock_params);

            lockParams.Checked = !areSettingsUnlocked;
            radiusSB.Enabled = areSettingsUnlocked;
            switchBoxShowCircle.Enabled = areSettingsUnlocked;
            maxHourPicker.Enabled = areSettingsUnlocked;
            maxWeeklyEdit.Enabled = areSettingsUnlocked;
            loginButton.Enabled = areSettingsUnlocked;
            switchShowAverage.Enabled = areSettingsUnlocked;
                
            switchShowAverage.Checked = showAverageHour;
            maxWeeklyEdit.Text = maxHourWeekly.ToString();
            maxHourPicker.Value = maxHour;
            radiusSB.SetProgress(bitmapLength, true);
            radiusSBValue.Text = radiusSB.Progress.ToString() + "px";
            switchBoxShowCircle.Checked = showCircle;
            
            lockParams.CheckedChange += delegate
            {
                loginButton.Enabled = areSettingsUnlocked;
                areSettingsUnlocked = !lockParams.Checked;
                radiusSB.Enabled = areSettingsUnlocked;
                switchShowAverage.Enabled = areSettingsUnlocked;
                switchBoxShowCircle.Enabled = areSettingsUnlocked;
                maxHourPicker.Enabled = areSettingsUnlocked;
                maxWeeklyEdit.Enabled = areSettingsUnlocked;
            };  

            loginButton.Click += delegate
            {
                SignOrLogInStopwatchUser(true);
                loginButton.Text = "Change";
            };

            switchShowAverage.CheckedChange += delegate
            {
                showAverageHour = switchShowAverage.Checked;
            };

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
            maxHourPicker.ValueChanged += delegate
            {
                maxHour = maxHourPicker.Value;
            };

            radiusSB.ProgressChanged += delegate
            {
                radiusSBValue.Text = radiusSB.Progress.ToString() + "px";
                bitmapLength = radiusSB.Progress;

                secOffset = bitmapLength / 5f;
                minOffset = bitmapLength / 10f;
            };

            switchBoxShowCircle.TextOff = "Off";
            switchBoxShowCircle.TextOn = "On";
            switchBoxShowCircle.CheckedChange += delegate
            {
                showCircle = switchBoxShowCircle.Checked;
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

            if (outputContainer.ChildCount == 1)
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
            UpdateGraphicsLayout();
        }

        private void UpdateGraphicsLayout()
        {
            if (graphicsLayout == null)
                return;
            Spinner spinner = graphicsLayout.FindViewById<Spinner>(Resource.Id.spinner_dropdown);
            if (spinner == null)
                return;
            UpdateGraphics(spinnerPossibilities[spinner.SelectedItem.ToString()]);
        }

        private async void UpdateGraphics(GraphicTimeLine timeLine)
        {
            int subDiv = subDivPerTimeLine[timeLine];
            float averageLineHeight = GetAverageLineHeight(timeLine);
            float[] hourPerSubDiv = new float[subDiv];
            PlotView plotView = graphicsLayout.FindViewById<PlotView>(Resource.Id.plotView);
            PlotModel model = new PlotModel();

            DateTimeAxis dateTimeAxis = new DateTimeAxis { Position = AxisPosition.Bottom, Minimum = -0.5, Maximum = subDiv - 0.5, IsZoomEnabled = false, IsPanEnabled = false, IsAxisVisible = false };
            CategoryAxis categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Minimum = -0.5, Maximum = subDiv - 0.5, LabelFormatter = GetCorrectLabelFormatting(timeLine), IsZoomEnabled = false, IsPanEnabled = false };
            LinearAxis oridnateAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, IsZoomEnabled = false, IsPanEnabled = false};

            LineSeries meanSeries = new LineSeries { MarkerSize = 0, Color = OxyColors.Red };
            ColumnSeries subDivSeries = new ColumnSeries { FillColor = OxyColors.Green, StrokeColor = OxyColors.Green };
            ColumnSeries todaySeries = new ColumnSeries { FillColor = OxyColors.Orange, StrokeColor = OxyColors.Orange };

            //var db = new SQLiteConnection(dbPath);
            //db.CreateTable<Time>();
            //var table = db.Table<Time>();
            if (currentUser != null)
            {
                var table = await client.GetTable<RegisteredTime>().Where(t => t.TimeUserId == currentUser.Id).OrderBy(t => t.SavedTime).ToEnumerableAsync();
    
                if (timeLine == GraphicTimeLine.ThisWeek)
                {
                    foreach (RegisteredTime t in table)
                    {
                        if ((t.SavedTime.Year == DateTime.Now.Year || Math.Abs(t.SavedTime.Year - DateTime.Now.Year) == 1) && GetWeekDiference(DateTime.Now, t.SavedTime) == 0)
                            hourPerSubDiv[GetDayOfWeek(t.SavedTime)] += t.SavedTime.Hour + t.SavedTime.Minute / 60f;
                    }
    
                    for (int i = 0; i < subDiv; i++)
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
                }
                if (timeLine == GraphicTimeLine.LastWeek)
                {
                    foreach (RegisteredTime t in table)
                    {
                        if ((t.SavedTime.Year == DateTime.Now.Year || Math.Abs(t.SavedTime.Year - DateTime.Now.Year) == 1) && GetWeekDiference(DateTime.Now.AddDays(-7), t.SavedTime) == 0)
                            hourPerSubDiv[GetDayOfWeek(t.SavedTime)] += t.SavedTime.Hour + t.SavedTime.Minute / 60f;
                    }
    
                    for (int i = 0; i < subDiv; i++)
                    {
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                        subDivSeries.Items.Add(new ColumnItem(hourPerSubDiv[i], i));
                    }
                }
                else if (timeLine == GraphicTimeLine.ThisMonth)
                {
                    foreach (RegisteredTime t in table)
                    {
                        if ((t.SavedTime.Year == DateTime.Now.Year || Math.Abs(t.SavedTime.Year - DateTime.Now.Year) == 1) && GetWeekDiference(DateTime.Now, t.SavedTime) < 4)
                            hourPerSubDiv[3 - GetWeekDiference(DateTime.Now, t.SavedTime)] += t.SavedTime.Hour + t.SavedTime.Minute / 60f;
                    }
    
                    for (int i = 0; i < subDiv; i++)
                    {
                        if (i + 1 == subDiv)
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
                }
                else if (timeLine == GraphicTimeLine.ThisYear)
                {
                    foreach (RegisteredTime t in table)
                    {
                        if (t.SavedTime.Year == DateTime.Now.Year)
                            hourPerSubDiv[t.SavedTime.Month - 1] += t.SavedTime.Hour + t.SavedTime.Minute / 60f;
                    }
    
                    for (int i = 0; i < subDiv; i++)
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
                }
            }

            model.Axes.Add(categoryAxis);
            model.Axes.Add(oridnateAxis);
            model.Series.Add(subDivSeries);
            model.Series.Add(todaySeries);

            if (showAverageHour)
            {
                meanSeries.Points.Add(new DataPoint(-0.5, averageLineHeight));
                meanSeries.Points.Add(new DataPoint(subDiv - 0.5, averageLineHeight));

                model.Axes.Add(dateTimeAxis);
                model.Series.Add(meanSeries);
            }

            plotView.Model = model;
        }

        private void SettingsLayoutOutput()
        {
            CommonPageOutput(settingsLayout);
            
            if (currentUser != null && currentUser.Name != null)
            {
                FindViewById<TextView>(Resource.Id.setting_account).Text = $"You are logged in as \"{currentUser.Name}\"";
                FindViewById<Button>(Resource.Id.loginButton).Text = "Change";
            }
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
            return ((int)time.DayOfWeek + 6) % 7;
        }

        private float GetAverageLineHeight(GraphicTimeLine timeLine)
        {
            switch (timeLine)
            {
                case GraphicTimeLine.ThisWeek:
                case GraphicTimeLine.LastWeek:
                    return maxHourWeekly / 7f;
                case GraphicTimeLine.ThisMonth:
                    return maxHourWeekly;
                case GraphicTimeLine.ThisYear:
                    return maxHourWeekly * 4.345f;
                default:
                    return 0;
            }
        }

        private Func<double, string> GetCorrectLabelFormatting(GraphicTimeLine timeLine)
        {
            switch (timeLine)
            {
                case GraphicTimeLine.ThisWeek:
                case GraphicTimeLine.LastWeek:
                    return GetDayOfWeekName;
                case GraphicTimeLine.ThisMonth:
                    return GetWeekNumber;
                case GraphicTimeLine.ThisYear:
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

            stopButton.Click += async delegate
            {
                ToggleButtons(ButtonsState.End);
                if (currentUser != null)
                {
                    RegisteredTime t = new RegisteredTime { SavedTime = ExtractTimerSpan(), TimeUserId = currentUser.Id, createdAt = DateTime.Now };
                    //var db = new SQLiteConnection(dbPath);
                    //db.CreateTable<Time>();
                    //db.Insert(item);
                    //db.Close();
                    await client.GetTable<RegisteredTime>().InsertAsync(t);
                }
                else
                    ExtractTimerSpan();

                OutputListLayoutReset();
                UpdateClock();
            };
        }

        private async void OutputListLayoutReset()
        {
            //var db = new SQLiteConnection(dbPath);
            //db.CreateTable<Time>();
            //var table = db.Table<Time>().OrderBy(t => t.SavedTime);
            if (currentUser == null)
                return;
            var table = await client.GetTable<RegisteredTime>().Where(t => t.TimeUserId == currentUser.Id).OrderBy(t => t.SavedTime).ThenBy(t => t.createdAt).ToEnumerableAsync();
            outputContainer.RemoveAllViews();
            outputContainer.AddView(listButtonsGrid);

            foreach (var t in table)
            {
                if (Math.Abs(DateTime.Now.DayOfYear - t.SavedTime.DayOfYear + 365 * (DateTime.Now.Year - t.SavedTime.Year)) < 14)
                    AddTimeToOutputList(t/*, new SQLiteConnection(dbPath)*/);
            }
            if (outputContainer.ChildCount == 1)
                listButtonsGrid.FindViewById<Button>(Resource.Id.resetAllButton).Enabled = false;
            else
                listButtonsGrid.FindViewById<Button>(Resource.Id.resetAllButton).Enabled = true;
            //db.Close();
        }

        private void AddTimeToOutputList(RegisteredTime t/*, SQLiteConnection db*/)
        {
            LayoutInflater inflater = (LayoutInflater)BaseContext.GetSystemService(Context.LayoutInflaterService);
            View timeView = inflater.Inflate(Resource.Layout.single_output_layout, null);
            TextView textOutput = timeView.FindViewById<TextView>(Resource.Id.outText);
            Button removeButton = timeView.FindViewById<Button>(Resource.Id.removeButton);
            outputContainer.AddView(timeView, outputContainer.IndexOfChild(listButtonsGrid));

            textOutput.Text = string.Format("{0}\n{1}", t.SavedTime.ToLongDateString(), t.SavedTime.ToShortTimeString());

            removeButton.Click += delegate
            {
                var alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Confirmation");
                alert.SetMessage("Do you really want to permanently delete this recorded time ?");

                alert.SetPositiveButton("YES", async delegate
                {
                    outputContainer.RemoveView(timeView);
                    await client.GetTable<RegisteredTime>().DeleteAsync(t);
                    //db.Delete<Time>(t.Id);
                    //db.Close();
                    Toast.MakeText(this, $"The {t.SavedTime.ToLongTimeString()} of {t.SavedTime.ToShortDateString()} has been deleted.", ToastLength.Long).Show();
                });
                alert.SetNegativeButton("NO", delegate { return; });
                alert.Create().Show();
            };
            UpdateGraphicsLayout();
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
        ThisWeek,
        LastWeek,
        ThisMonth,
        Month,
        ThisYear
    }

    public enum ButtonsState
    {
        Start,
        End,
        Pause
    }

    public class StopwatchUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class RegisteredTime
    {
        public string Id { get; set; }
        public DateTime SavedTime { get; set; }
        public DateTime createdAt { get; set; }
        public string TimeUserId { get; set; }
    }

    /*[Table("Times")]
    public class Time : INotifyPropertyChanged
    {
        private int _id;
        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get { return _id; }
            set { this._id = value; OnPropertyChanged(nameof(Id)); }
        }

        private DateTime _savedTime;
        public DateTime SavedTime
        {
            get { return _savedTime; }
            set { this._savedTime = value; OnPropertyChanged(nameof(_savedTime)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }*/
}

