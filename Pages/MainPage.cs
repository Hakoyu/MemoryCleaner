using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rammap;
using System.Windows.Threading;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using HKW.Management;
using HKW.TomlParse;
using System.Reflection;
using System.Windows.Resources;
using System.Runtime.CompilerServices;
using MemoryCleaner.Lib;
using MemoryCleaner.Langs.MessageBox;
using MemoryCleaner.Langs.Pages.MainPage;

namespace MemoryCleaner.Pages
{
    public partial class MainPage
    {
        Thread rammpRun = null!;
        public delegate void MainPageEvent();
        public MainPageEvent ChangeI18n = null!;
        public MainPageEvent ConfigLoadError = null!;

        void BeforeInitialisation()
        {
            taskModeList.AddLast(new KeyValuePair<string, object>(MainPage_I18n.ResidualMode, residualMode));
            taskModeList.AddLast(new KeyValuePair<string, object>(MainPage_I18n.TimeMode, timeMode));
            currentMode = taskModeList.First!;
            Global.totalMemory = management.GetMemoryMetrics()!.Total;
        }
        void AfterInitialisation()
        {
            foreach (ComboBoxItem item in ComboBox_I18n.Items)
                i18nItems.Add(item.ToolTip.ToString()!, item);
            ComboBox_I18n.SelectedItem = i18nItems[Thread.CurrentThread.CurrentUICulture.Name];

            rammapMode.Add(RammapMode.EmptyWorkingSets, CheckBox_EmptyWorkingSets);
            rammapMode.Add(RammapMode.EmptySystemWorkingSets, CheckBox_EmptySystemWorkingSets);
            rammapMode.Add(RammapMode.EmptyModifiedPageList, CheckBox_EmptyModifiedPageList);
            rammapMode.Add(RammapMode.EmptyStandbyList, CheckBox_EmptyStandbyList);
            rammapMode.Add(RammapMode.EmptyPrioity0StandbyList, CheckBox_EmptyPrioity0StandbyList);

            Progressbar_MemoryMetrics.Maximum = Global.totalMemory;
            TextBox_TotalMemory.Text = Global.totalMemory.ToString();
            SetTimerGetMemoryMetrics();
            residualTask = new Thread(ResidualTaskTimer);
            timeTask = new Thread(TimeTaskTimer);
            rammpRun = new Thread(RammpRun);
        }
        void ConfigInitialize()
        {
            if (Global.CreateConfigFile())
            {
                try
                {
                    using TomlTable toml = TOML.Parse(Global.configPath);
                    CheckBox_EmptyWorkingSets.IsChecked = toml["RammapMode"]["EmptyWorkingSets"].AsBoolean;
                    CheckBox_EmptySystemWorkingSets.IsChecked = toml["RammapMode"]["EmptySystemWorkingSets"].AsBoolean;
                    CheckBox_EmptyModifiedPageList.IsChecked = toml["RammapMode"]["EmptyModifiedPageList"].AsBoolean;
                    CheckBox_EmptyStandbyList.IsChecked = toml["RammapMode"]["EmptyStandbyList"].AsBoolean;
                    CheckBox_EmptyPrioity0StandbyList.IsChecked = toml["RammapMode"]["EmptyPrioity0StandbyList"].AsBoolean;

                    while (!currentMode.Value.Value.GetType().Name.Contains(toml["TaskMode"]["Mode"].AsString))
                        currentMode = currentMode.Next!;
                    Frame_ModeSwitch.Content = currentMode.Value.Value;
                    Label_ModeSwitch.Content = currentMode.Value.Key;

                    residualMode.CheckBox_UsedMemoryMore.IsChecked = toml["ResidualMode"]["UsedMemoryMore"].AsBoolean;
                    residualMode.TextBox_UsedMemoryMoreSize.Text =
                        Global.MemorySizeParse(toml["ResidualMode"]["UsedMemoryMoreSize"].AsInteger).ToString();
                    residualMode.CheckBox_FreeMemoryLower.IsChecked = toml["ResidualMode"]["FreeMemoryLower"].AsBoolean;
                    residualMode.TextBox_FreeMemoryLowerSize.Text =
                        Global.MemorySizeParse(toml["ResidualMode"]["FreeMemoryLowerSize"].AsInteger).ToString();
                    residualMode.TextBox_IntervalTime.Text =
                        Global.IntervalTimeParse(toml["ResidualMode"]["IntervalTime"].AsInteger).ToString();

                    timeMode.TextBox_IntervalTime.Text =
                        Global.IntervalTimeParse(toml["TimeMode"]["IntervalTime"].AsInteger).ToString();

                    CheckBox_AutoMinimizedAndStart.IsChecked = toml["Extras"]["AutoMinimized"].AsBoolean;
                    autoMinimized = toml["Extras"]["AutoMinimized"].AsBoolean;
                    CheckBox_LanuchOnUserLogon.IsChecked = toml["Extras"]["AutoStartOnUserLogon"].AsBoolean;
                }
                catch
                {
                    management.Close();
                    StopTask();
                    ConfigLoadError();
                }
            }
            if (!File.Exists(Global.rammapPath))
            {
                using Stream stream = Application.GetResourceStream(Global.resourcesRAMMapUri).Stream;
                byte[] b = new byte[stream.Length];
                stream.Read(b, 0, b.Length);
                using FileStream fs = File.Create(Global.rammapPath);
                fs.Write(b, 0, b.Length);
            }
        }
        void GetRammapModeCheckedSize()
        {
            foreach (var cb in rammapMode.Values)
                if (((CheckBox)cb).IsChecked is true)
                    rammapModeCheckedSize += 1;
        }
        public void Close()
        {
            management.Close();
            StopTask();
            SaveConfig();
        }
        void SaveConfig()
        {
            try
            {
                using TomlTable toml = TOML.Parse(Global.configPath);
                toml["RammapMode"]["EmptyWorkingSets"] = CheckBox_EmptyWorkingSets.IsChecked!;
                toml["RammapMode"]["EmptySystemWorkingSets"] = CheckBox_EmptySystemWorkingSets.IsChecked!;
                toml["RammapMode"]["EmptyModifiedPageList"] = CheckBox_EmptyModifiedPageList.IsChecked!;
                toml["RammapMode"]["EmptyStandbyList"] = CheckBox_EmptyStandbyList.IsChecked!;
                toml["RammapMode"]["EmptyPrioity0StandbyList"] = CheckBox_EmptyPrioity0StandbyList.IsChecked!;

                toml["TaskMode"]["Mode"] = currentMode.Value.Value.GetType().Name.Replace("Page", "");

                toml["ResidualMode"]["UsedMemoryMore"] = residualMode.CheckBox_UsedMemoryMore.IsChecked!;
                toml["ResidualMode"]["UsedMemoryMoreSize"] = int.Parse(residualMode.TextBox_UsedMemoryMoreSize.Text);
                toml["ResidualMode"]["FreeMemoryLower"] = residualMode.CheckBox_FreeMemoryLower.IsChecked!;
                toml["ResidualMode"]["FreeMemoryLowerSize"] = int.Parse(residualMode.TextBox_FreeMemoryLowerSize.Text);
                toml["ResidualMode"]["IntervalTime"] = int.Parse(residualMode.TextBox_IntervalTime.Text);

                toml["TimeMode"]["IntervalTime"] = int.Parse(timeMode.TextBox_IntervalTime.Text);

                toml["Extras"]["AutoMinimized"] = CheckBox_AutoMinimizedAndStart.IsChecked!;
                toml["Extras"]["AutoStartOnUserLogon"] = CheckBox_LanuchOnUserLogon.IsChecked!;
                toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;

                toml.SaveTo(Global.configPath);
            }
            catch
            {
                management.Close();
                StopTask();
                ConfigLoadError();
            }
        }
        void SetTimerGetMemoryMetrics()
        {
            timerGetMemoryMetrics.Tick += new EventHandler(GetMemoryMetrics!);
            timerGetMemoryMetrics.Interval = TimeSpan.FromMilliseconds(1000);
            timerGetMemoryMetrics.Start();
            void GetMemoryMetrics(object sender, EventArgs e)
            {
                if (management.GetMemoryMetrics() is MemoryMetrics mm)
                {
                    TextBox_UsedMemory.Text = mm.Used.ToString();
                    TextBox_FreeMemory.Text = mm.Free.ToString();
                }
            }
        }
        public void ExecuteNow()
        {
            if (!rammpRun.IsAlive)
            {
                Progressbar_TaskProgress.Maximum = rammapModeCheckedSize * 30;
                rammpRun = new Thread(RammpRun);
                rammpRun.Start();
            }
        }
        void TaskRammpRun()
        {
            if (!residualTask.IsAlive)
            {
                residualTask = new Thread(ResidualTaskTimer);
                residualTask.Start();
                if (!rammpRun.IsAlive)
                    ExecuteNow();
            }
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void RammpRun()
        {
            Dispatcher.Invoke(() => GroupBox_RammapMode.IsEnabled = false);
            new Thread(() =>
            {
                for (int i = (int)Dispatcher.Invoke(() => Progressbar_TaskProgress.Maximum); i >= 0; i--)
                {
                    Dispatcher.Invoke(() => Progressbar_TaskProgress.Value += 1);
                    Thread.Sleep(100);
                }
            }).Start();
            foreach (var mode in rammapMode)
                if (Dispatcher.Invoke(() => ((CheckBox)mode.Value).IsChecked is true))
                    RammapRunnerRun(mode.Key);
            Dispatcher.Invoke(() => Label_TaskProgressInfo.Content = MainPage_I18n.Success);
            Thread.Sleep(3000);
            Dispatcher.Invoke(() =>
            {
                Progressbar_TaskProgress.Value = 0;
                Label_TaskProgressInfo.Content = MainPage_I18n.NoCurrentTasks;
                GroupBox_RammapMode.IsEnabled = true;
            });

            void RammapRunnerRun(RammapMode mode)
            {
                RammapRunner.Run(mode);
                Dispatcher.Invoke(() => Label_TaskProgressInfo.Content = modeContent[mode]);
                Thread.Sleep(3000);
            }
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void ResidualTaskTimer()
        {
            for (int i = int.Parse(Dispatcher.Invoke(() => residualMode.TextBox_IntervalTime.Text)) * 60; i > 0 && residualTask.ThreadState != ThreadState.Unstarted; i--)
                Thread.Sleep(1000);
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void TimeTaskTimer()
        {
            int min = int.Parse(Dispatcher.Invoke(() => timeMode.TextBox_IntervalTime.Text));
            Dispatcher.Invoke(() => timeMode.ProgresBar_TimeLeft.Maximum = min * 60);
            DateTime dt = new(0001, 01, 01, 00, 00, 00);
            dt = dt.AddMinutes(min);
            for (int i = 0; i < min * 60 && timeTask.ThreadState != ThreadState.Unstarted; i++)
            {
                Dispatcher.Invoke(() =>
                {
                    timeMode.ProgresBar_TimeLeft.Value = i;
                    timeMode.Label_TimeLeft.Content = dt.AddSeconds(-i).ToString("HH:mm:ss");
                });
                Thread.Sleep(1000);
            }
        }
        public void StartTask()
        {
            Button_StartTask.IsEnabled = false;
            Button_StopTask.IsEnabled = true;
            Button_ModeSwitch.IsEnabled = false;
            if (Frame_ModeSwitch.Content is ResidualModePage)
            {
                residualMode.IsEnabled = false;
                residualTask.Start();
            }
            else if (Frame_ModeSwitch.Content is TimeModePage)
            {
                timeMode.TextBox_IntervalTime.IsEnabled = false;
                timeTask.Start();
            }
        }
        public void StopTask()
        {
            Button_StopTask.IsEnabled = false;
            Button_StartTask.IsEnabled = true;
            Button_ModeSwitch.IsEnabled = true;
            if (Frame_ModeSwitch.Content is ResidualModePage)
            {
                residualMode.IsEnabled = true;
                if (residualTask.ThreadState != ThreadState.Unstarted)
                    residualTask.Join(1);
                residualTask = new Thread(ResidualTaskTimer);
            }
            else if (Frame_ModeSwitch.Content is TimeModePage)
            {
                timeMode.TextBox_IntervalTime.IsEnabled = true;
                if (timeTask.ThreadState != ThreadState.Unstarted)
                    timeTask.Join(1);
                timeTask = new Thread(TimeTaskTimer);
                Dispatcher.Invoke(() =>
                {
                    timeMode.Label_TimeLeft.Content = "00:00:00";
                    timeMode.ProgresBar_TimeLeft.Value = 0;
                });
            }
        }
    }
}
