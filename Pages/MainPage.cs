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
                string config = "";
                using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
                config = sr.ReadToEnd();
                using StreamWriter sw = File.AppendText(configPath);
                sw.Write(config);
            }
            else
            {
                FileIni fini = new(configPath);
                try
                {
                    string temp = "";
                    fini = new(configPath);

                    CheckBox_EmptyWorkingSets.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyWorkingSets"].First());
                    CheckBox_EmptySystemWorkingSets.IsChecked = bool.Parse(fini["RammapMode"]!["EmptySystemWorkingSets"].First());
                    CheckBox_EmptyModifiedPageList.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyModifiedPageList"].First());
                    CheckBox_EmptyStandbyList.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyStandbyList"].First());
                    CheckBox_EmptyPrioity0StandbyList.IsChecked = bool.Parse(fini["RammapMode"]!["EmptyPrioity0StandbyList"].First());

                    CheckBox_UsedMemoryMore.IsChecked = bool.Parse(fini["ProgramTasks"]!["UsedMemoryMore"].First());
                    temp = fini["ProgramTasks"]!["UsedMemoryMoreSize"].First();
                    if (temp.All(c => char.IsNumber(c)))
                        TextBox_UsedMemoryMoreSize.Text = temp;
                    MemorySizeSave(TextBox_UsedMemoryMoreSize);
                    CheckBox_FreeMemoryLower.IsChecked = bool.Parse(fini["ProgramTasks"]!["FreeMemoryLower"].First());
                    temp = fini["ProgramTasks"]!["FreeMemoryLowerSize"].First();
                    if (temp.All(c => char.IsNumber(c)))
                        TextBox_FreeMemoryLowerSize.Text = temp;
                    MemorySizeSave(TextBox_FreeMemoryLowerSize);
                    temp = fini["ProgramTasks"]!["TaskTimer"].First();
                    if (temp.All(c => char.IsNumber(c)))
                        TextBox_TaskTimer.Text = temp;
                    MinimumIntervalSizeSave(TextBox_TaskTimer);

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
            if (CheckBox_EmptyWorkingSets.IsChecked == true)
                rammapModeCheckedSize += 1;
            if (CheckBox_EmptySystemWorkingSets.IsChecked == true)
                rammapModeCheckedSize += 1;
            if (CheckBox_EmptyModifiedPageList.IsChecked == true)
                rammapModeCheckedSize += 1;
            if (CheckBox_EmptyStandbyList.IsChecked == true)
                rammapModeCheckedSize += 1;
            if (CheckBox_EmptyPrioity0StandbyList.IsChecked == true)
                rammapModeCheckedSize += 1;
        }
        public void Close()
        {
            management.Close();
            ConfigSave();
        }
        void ConfigSave()
        {
            FileIni fini = new(configPath);
            fini["RammapMode"]!["EmptyWorkingSets"].Replace(CheckBox_EmptyWorkingSets.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptySystemWorkingSets"].Replace(CheckBox_EmptySystemWorkingSets.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptyModifiedPageList"].Replace(CheckBox_EmptyModifiedPageList.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptyStandbyList"].Replace(CheckBox_EmptyStandbyList.IsChecked.ToString()!);
            fini["RammapMode"]!["EmptyPrioity0StandbyList"].Replace(CheckBox_EmptyPrioity0StandbyList.IsChecked.ToString()!);

            fini["ProgramTasks"]!["UsedMemoryMore"].Replace(CheckBox_UsedMemoryMore.IsChecked.ToString()!);
            fini["ProgramTasks"]!["UsedMemoryMoreSize"].Replace(TextBox_UsedMemoryMoreSize.Text);
            fini["ProgramTasks"]!["FreeMemoryLower"].Replace(CheckBox_FreeMemoryLower.IsChecked.ToString()!);
            fini["ProgramTasks"]!["FreeMemoryLowerSize"].Replace(TextBox_FreeMemoryLowerSize.Text);
            fini["ProgramTasks"]!["TaskTimer"].Replace(TextBox_TaskTimer.Text);

            fini["Extras"]!["AutoMinimized"].Replace(CheckBox_AutoMinimizedAndStart.IsChecked.ToString()!);
            fini["Extras"]!["AutoStartOnUserLogon"].Replace(CheckBox_LanuchOnUserLogon.IsChecked.ToString()!);
            fini["Extras"]!["Lang"].Replace((ComboBox_I18n.SelectedItem as ComboBoxItem)!.ToolTip.ToString()!);

            fini.Save();
            fini.Close();
        }
        void DateInitialize()
        {
            if (management.GetMemoryMetrics() is MemoryMetrics mm)
                totalMemory = mm.Total;
        }
        void InterfaceInitialize()
        {
            Progressbar_MemoryMetrics.Maximum = totalMemory;
            TextBox_TotalMemory.Text = totalMemory.ToString();
            SetTimerGetMemoryMetrics();
            taskTimer = new Thread(() => TaskTimer(taskTime));
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
                Button_ExecuteNow.IsEnabled = false;
                CheckBox_EmptyWorkingSets.IsEnabled = false;
                CheckBox_EmptySystemWorkingSets.IsEnabled = false;
                CheckBox_EmptyModifiedPageList.IsEnabled = false;
                CheckBox_EmptyStandbyList.IsEnabled = false;
                CheckBox_EmptyPrioity0StandbyList.IsEnabled = false;
                Progressbar_TaskProgress.Maximum = rammapModeCheckedSize * 30;
                rammpRun = new Thread(RammpRun);
                rammpRun.Start();
            }
        }
        void TaskRammpRun()
        {
            if (!taskTimer.IsAlive)
            {
                taskTimer = new Thread(() => TaskTimer(taskTime));
                taskTimer.Start();
                if (!rammpRun.IsAlive)
                    ExecuteNow();
            }
        }
        bool Checked(CheckBox? checkBox)
        {
            if (checkBox?.IsChecked is true)
                return true;
            return false;
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void ThreadSleepOfMinutes(int minutes)
        {
            Thread.Sleep(minutes * 60 * 1000);
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void RammpRun()
        {
            new Thread(() =>
            {
                for (int i = (int)Dispatcher.Invoke(() => Progressbar_TaskProgress.Maximum); i >= 0; i--)
                {
                    Dispatcher.Invoke(() => Progressbar_TaskProgress.Value += 1);
                    Thread.Sleep(100);
                }
            }).Start();
            if (Dispatcher.Invoke(Checked, CheckBox_EmptyWorkingSets) is true)
                RammapRunnerRun(RammapMode.EmptyWorkingSets);
            if (Dispatcher.Invoke(Checked, CheckBox_EmptySystemWorkingSets) is true)
                RammapRunnerRun(RammapMode.EmptySystemWorkingSets);
            if (Dispatcher.Invoke(Checked, CheckBox_EmptyModifiedPageList) is true)
                RammapRunnerRun(RammapMode.EmptyModifiedPageList);
            if (Dispatcher.Invoke(Checked, CheckBox_EmptyStandbyList) is true)
                RammapRunnerRun(RammapMode.EmptyStandbyList);
            if (Dispatcher.Invoke(Checked, CheckBox_EmptyPrioity0StandbyList) is true)
                RammapRunnerRun(RammapMode.EmptyPrioity0StandbyList);
            Dispatcher.Invoke(() =>
            {
                Label_TaskProgressInfo.Content = "Success";
            });
            Thread.Sleep(3000);
            Dispatcher.Invoke(() =>
            {
                Progressbar_TaskProgress.Value = 0;
                Label_TaskProgressInfo.Content = "No current tasks";
                Button_ExecuteNow.IsEnabled = true;
                CheckBox_EmptyWorkingSets.IsEnabled = true;
                CheckBox_EmptySystemWorkingSets.IsEnabled = true;
                CheckBox_EmptyModifiedPageList.IsEnabled = true;
                CheckBox_EmptyStandbyList.IsEnabled = true;
                CheckBox_EmptyPrioity0StandbyList.IsEnabled = true;
            });
            void RammapRunnerRun(RammapMode mode)
            {
                RammapRunner.Run(mode);
                Dispatcher.Invoke(() => Label_TaskProgressInfo.Content = mode.ToString());
                Thread.Sleep(3000);
            }
        }
        void MemorySizeSave(TextBox textBox)
        {
            if (!Regex.IsMatch(textBox.Text, "^[0-9]+$"))
            {
                MessageBox.Show("Only numeric input");
                textBox.Text = "1024";
                return;
            }
            int size = int.Parse(textBox.Text);
            if (size < 1024)
            {
                MessageBox.Show("Minimum value is 1024");
                textBox.Text = "1024";
            }
            else if (size > (int)totalMemory / 2)
            {
                MessageBox.Show("The maximum value cannot exceed half of the current memory maximum");
                textBox.Text = ((int)totalMemory / 2).ToString();
            }
        }
        void MinimumIntervalSizeSave(TextBox textBox)
        {
            if (!Regex.IsMatch(textBox.Text, "^[0-9]+$"))
            {
                MessageBox.Show("Only numeric input");
                textBox.Text = "10";
                return;
            }
            if (textBox.Text.Length != 2)
            {
                textBox.Text = "10";
                MessageBox.Show("The value range is 10~99");
            }
            taskTime = int.Parse(textBox.Text);
            ProgresBar_TimeLeft.Value = 0;
            ProgresBar_TimeLeft.Maximum = taskTime * 60;
            if (taskTimer != null && taskTimer.ThreadState != ThreadState.Unstarted)
                taskTimer.Join(1);
            taskTimer = new Thread(() => TaskTimer(taskTime));
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void TaskTimer(int min)
        {
            if (min > 0)
            {
                DateTime dt = new(0001, 01, 01, 00, min, 00);
                for (int i = 0; i < min * 60; i++)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        ProgresBar_TimeLeft.Value = i;
                        Label_TimeLeft.Content = dt.AddSeconds(-i).ToString("mm:ss");
                    });
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
