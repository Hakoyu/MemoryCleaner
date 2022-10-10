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
using HKW.FileIni;
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
                FileIni fini = new(configPath);
                try
                {
                    fini = new(configPath);

                    CheckBox_EmptyWorkingSets.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyWorkingSets"].First());
                    CheckBox_EmptySystemWorkingSets.IsChecked = bool.Parse(fini["RammapMode"]!["EmptySystemWorkingSets"].First());
                    CheckBox_EmptyModifiedPageList.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyModifiedPageList"].First());
                    CheckBox_EmptyStandbyList.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyStandbyList"].First());
                    CheckBox_EmptyPrioity0StandbyList.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyPrioity0StandbyList"].First());

                    while (currentMode != null)
                    {
                        if (currentMode.Value.GetType().Name == fini["TaskMode"]!["Mode"].First())
                        {
                            Frame_ModeSwitch.Content = currentMode.Value;
                            Label_ModeSwitch.Content = fini["TaskMode"]!["Mode"].First();
                            break;
                        }
                        currentMode = currentMode.Next!;
                    }

                    residualMode.CheckBox_UsedMemoryMore.IsChecked = bool.Parse(fini["ResidualMode"]!["UsedMemoryMore"].First());
                    residualMode.TextBox_UsedMemoryMoreSize.Text =
                        PageFun.MemorySizeParse(int.Parse(fini["ResidualMode"]!["UsedMemoryMoreSize"].First())).ToString();
                    residualMode.CheckBox_FreeMemoryLower.IsChecked = bool.Parse(fini["ResidualMode"]!["FreeMemoryLower"].First());
                    residualMode.TextBox_FreeMemoryLowerSize.Text =
                        PageFun.MemorySizeParse(int.Parse(fini["ResidualMode"]!["FreeMemoryLowerSize"].First())).ToString();
                    residualMode.TextBox_IntervalTime.Text =
                        PageFun.IntervalTimeParse(int.Parse(fini["ResidualMode"]!["IntervalTime"].First())).ToString();

                    timeMode.TextBox_IntervalTime.Text =
                        PageFun.IntervalTimeParse(int.Parse(fini["TimeMode"]!["IntervalTime"].First())).ToString();

                    CheckBox_AutoMinimizedAndStart.IsChecked = bool.Parse(fini["Extras"]!["AutoMinimized"].First());
                    autoMinimized = bool.Parse(fini["Extras"]!["AutoMinimized"].First());
                    CheckBox_LanuchOnUserLogon.IsChecked = bool.Parse(fini["Extras"]!["AutoStartOnUserLogon"].First());
                }
                catch
                {
                    MessageBox.Show("Settings loading error\nWill restore default settings and restart the application");
                    File.Delete(configPath);
                    System.Windows.Forms.Application.Restart();
                    Application.Current.Shutdown(-1);
                }
                fini.Close();
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
            ConfigSave();
        }
        void ConfigSave()
        {
            using FileIni fini = new(configPath);
            fini["RammapMode"]!["EmptyWorkingSets"].Replace(CheckBox_EmptyWorkingSets.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptySystemWorkingSets"].Replace(CheckBox_EmptySystemWorkingSets.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptyModifiedPageList"].Replace(CheckBox_EmptyModifiedPageList.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptyStandbyList"].Replace(CheckBox_EmptyStandbyList.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptyPrioity0StandbyList"].Replace(CheckBox_EmptyPrioity0StandbyList.IsChecked.ToString()!);

            fini["TaskMode"]!["Mode"].Replace(currentMode.Value.GetType().Name);

            fini["ResidualMode"]!["UsedMemoryMore"].Replace(residualMode.CheckBox_UsedMemoryMore.IsChecked.ToString()!);
            fini["ResidualMode"]!["UsedMemoryMoreSize"].Replace(residualMode.TextBox_UsedMemoryMoreSize.Text);
            fini["ResidualMode"]!["FreeMemoryLower"].Replace(residualMode.CheckBox_FreeMemoryLower.IsChecked.ToString()!);
            fini["ResidualMode"]!["FreeMemoryLowerSize"].Replace(residualMode.TextBox_FreeMemoryLowerSize.Text);
            fini["ResidualMode"]!["IntervalTime"].Replace(residualMode.TextBox_IntervalTime.Text);

            fini["TimeMode"]!["IntervalTime"].Replace(timeMode.TextBox_IntervalTime.Text);

            fini["Extras"]!["AutoMinimized"].Replace(CheckBox_AutoMinimizedAndStart.IsChecked.ToString()!);
            fini["Extras"]!["AutoStartOnUserLogon"].Replace(CheckBox_LanuchOnUserLogon.IsChecked.ToString()!);
            fini["Extras"]!["Lang"].Replace((ComboBox_I18n.SelectedItem as ComboBoxItem)!.ToolTip.ToString()!);
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
