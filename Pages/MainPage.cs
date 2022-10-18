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

namespace MemoryCleaner.Pages
{
    public partial class MainPage
    {
        Thread rammpRun = null!;
        void ConfigInitialize()
        {
            if (!File.Exists(configPath))
            {
                using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
                string config = sr.ReadToEnd();
                using StreamWriter sw = File.AppendText(configPath);
                sw.Write(config);
            }
            else
            {
                try
                {
                    using TomlTable toml = TOML.Parse(configPath);
                    CheckBox_EmptyWorkingSets.IsChecked = toml["RammapMode"]["EmptyWorkingSets"].AsBoolean;
                    CheckBox_EmptySystemWorkingSets.IsChecked = toml["RammapMode"]["EmptySystemWorkingSets"].AsBoolean;
                    CheckBox_EmptyModifiedPageList.IsChecked = toml["RammapMode"]["EmptyModifiedPageList"].AsBoolean;
                    CheckBox_EmptyStandbyList.IsChecked = toml["RammapMode"]["EmptyStandbyList"].AsBoolean;
                    CheckBox_EmptyPrioity0StandbyList.IsChecked = toml["RammapMode"]["EmptyPrioity0StandbyList"].AsBoolean;

                    while (currentMode.Value.GetType().Name == toml["TaskMode"]["Mode"].AsString)
                        currentMode = currentMode.Next!;
                    Frame_ModeSwitch.Content = currentMode.Value;
                    Label_ModeSwitch.Content = toml["TaskMode"]["Mode"].AsString;

                    residualMode.CheckBox_UsedMemoryMore.IsChecked = toml["ResidualMode"]["UsedMemoryMore"].AsBoolean;
                    residualMode.TextBox_UsedMemoryMoreSize.Text =
                        PageFun.MemorySizeParse(toml["ResidualMode"]["UsedMemoryMoreSize"].AsInteger).ToString();
                    residualMode.CheckBox_FreeMemoryLower.IsChecked = toml["ResidualMode"]["FreeMemoryLower"].AsBoolean;
                    residualMode.TextBox_FreeMemoryLowerSize.Text =
                        PageFun.MemorySizeParse(toml["ResidualMode"]["FreeMemoryLowerSize"].AsInteger).ToString();
                    residualMode.TextBox_IntervalTime.Text =
                        PageFun.IntervalTimeParse(toml["ResidualMode"]["IntervalTime"].AsInteger).ToString();

                    timeMode.TextBox_IntervalTime.Text =
                        PageFun.IntervalTimeParse(toml["TimeMode"]["IntervalTime"].AsInteger).ToString();

                    CheckBox_AutoMinimizedAndStart.IsChecked = toml["Extras"]["AutoMinimized"].AsBoolean;
                    autoMinimized = toml["Extras"]["AutoMinimized"].AsBoolean;
                    CheckBox_LanuchOnUserLogon.IsChecked = toml["Extras"]["AutoStartOnUserLogon"].AsBoolean;
                }
                catch
                {
                    MessageBox.Show("Settings loading error\nWill restore default settings and restart the application");
                    File.Delete(configPath);
                    System.Windows.Forms.Application.Restart();
                    Application.Current.Shutdown(-1);
                }
            }
            if (!File.Exists(rammapPath))
            {
                using Stream stream = Application.GetResourceStream(resourcesRAMMapUri).Stream;
                byte[] b = new byte[stream.Length];
                stream.Read(b, 0, b.Length);
                using FileStream fs = File.Create(rammapPath);
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
            ConfigSave();
        }
        void ConfigSave()
        {
            using TomlTable toml = TOML.Parse(configPath);
            toml["RammapMode"]["EmptyWorkingSets"] = CheckBox_EmptyWorkingSets.IsChecked!;
            toml["RammapMode"]["EmptySystemWorkingSets"] = CheckBox_EmptySystemWorkingSets.IsChecked!;
            toml["RammapMode"]["EmptyModifiedPageList"] = CheckBox_EmptyModifiedPageList.IsChecked!;
            toml["RammapMode"]["EmptyStandbyList"] = CheckBox_EmptyStandbyList.IsChecked!;
            toml["RammapMode"]["EmptyPrioity0StandbyList"] = CheckBox_EmptyPrioity0StandbyList.IsChecked!;

            toml["TaskMode"]["Mode"] = currentMode.Value.GetType().Name;

            toml["ResidualMode"]["UsedMemoryMore"] = residualMode.CheckBox_UsedMemoryMore.IsChecked!;
            toml["ResidualMode"]["UsedMemoryMoreSize"] = int.Parse(residualMode.TextBox_UsedMemoryMoreSize.Text);
            toml["ResidualMode"]["FreeMemoryLower"] = residualMode.CheckBox_FreeMemoryLower.IsChecked!;
            toml["ResidualMode"]["FreeMemoryLowerSize"] = int.Parse(residualMode.TextBox_FreeMemoryLowerSize.Text);
            toml["ResidualMode"]["IntervalTime"] = int.Parse(residualMode.TextBox_IntervalTime.Text);

            toml["TimeMode"]["IntervalTime"] = int.Parse(timeMode.TextBox_IntervalTime.Text);

            toml["Extras"]["AutoMinimized"] = CheckBox_AutoMinimizedAndStart.IsChecked!;
            toml["Extras"]["AutoStartOnUserLogon"] = CheckBox_LanuchOnUserLogon.IsChecked!;
            toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;

            toml.SaveTo(configPath);
        }
        void InterfaceInitialize()
        {
            Progressbar_MemoryMetrics.Maximum = PageFun.totalMemory;
            TextBox_TotalMemory.Text = PageFun.totalMemory.ToString();
            SetTimerGetMemoryMetrics();
            residualTask = new Thread(ResidualTaskTimer);
            timeTask = new Thread(TimeTaskTimer);
            rammpRun = new Thread(RammpRun);
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
            Dispatcher.Invoke(() => Label_TaskProgressInfo.Content = "Success");
            Thread.Sleep(3000);
            Dispatcher.Invoke(() =>
            {
                Progressbar_TaskProgress.Value = 0;
                Label_TaskProgressInfo.Content = "No current tasks";
                GroupBox_RammapMode.IsEnabled = true;
            });
            void RammapRunnerRun(RammapMode mode)
            {
                RammapRunner.Run(mode);
                Dispatcher.Invoke(() => Label_TaskProgressInfo.Content = mode.ToString());
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
            if (Frame_ModeSwitch.Content is ResidualMode)
            {
                residualMode.IsEnabled = false;
                residualTask.Start();
            }
            else if (Frame_ModeSwitch.Content is TimeMode)
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
            if (Frame_ModeSwitch.Content is ResidualMode)
            {
                residualMode.IsEnabled = true;
                if (residualTask.ThreadState != ThreadState.Unstarted)
                    residualTask.Join(1);
                residualTask = new Thread(ResidualTaskTimer);
            }
            else if (Frame_ModeSwitch.Content is TimeMode)
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
