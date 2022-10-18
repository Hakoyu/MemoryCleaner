﻿using System;
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
using System.Reflection;
using System.Windows.Resources;
using System.Globalization;
using MemoryCleaner.Langs.MessageBox;
using MemoryCleaner.Pages;

namespace MemoryCleaner.Pages
{
    /// <summary>
    /// MainPage.xaml 的交互逻辑
    /// </summary>
    public partial class MainPage : Page
    {
        public bool autoMinimized = false;
        Management management = new();
        int rammapModeCheckedSize = 0;
        DispatcherTimer timerGetMemoryMetrics = new();
        public const string configPath = @"Config.toml";
        public const string rammapPath = @"RAMMap.exe";
        public readonly static Uri resourcesConfigUri = new("/Resources/Config.toml", UriKind.Relative);
        public readonly static Uri resourcesRAMMapUri = new("/Resources/RAMMap.exe", UriKind.Relative);
        ResidualMode residualMode = new();
        Thread residualTask = null!;
        TimeMode timeMode = new();
        Thread timeTask = null!;
        LinkedList<object> taskMode = new();
        LinkedListNode<object> currentMode = null!;
        Dictionary<RammapMode, object> rammapMode = new();
        bool isFirst = true;
        public MainPage()
        {
            taskMode.AddLast(timeMode);
            taskMode.AddLast(residualMode);
            currentMode = taskMode.First!;
            //DateInitialize();
            PageFun.totalMemory = management.GetMemoryMetrics()!.Total;
            InitializeComponent();
            rammapMode.Add(RammapMode.EmptyWorkingSets, CheckBox_EmptyWorkingSets);
            rammapMode.Add(RammapMode.EmptySystemWorkingSets, CheckBox_EmptySystemWorkingSets);
            rammapMode.Add(RammapMode.EmptyModifiedPageList, CheckBox_EmptyModifiedPageList);
            rammapMode.Add(RammapMode.EmptyStandbyList, CheckBox_EmptyStandbyList);
            rammapMode.Add(RammapMode.EmptyPrioity0StandbyList, CheckBox_EmptyPrioity0StandbyList);
            InterfaceInitialize();
            ConfigInitialize();
            GetRammapModeCheckedSize();
        }
        private void TextBox_UsedMemory_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double usedMemory = double.Parse(textBox.Text);
            Progressbar_MemoryMetrics.Value = usedMemory;
            Label_MemoryMetrics.Content = (usedMemory / PageFun.totalMemory).ToString("0%");
            if (!Button_StartTask.IsEnabled && residualMode.CheckBox_UsedMemoryMore.IsChecked is true && usedMemory > int.Parse(residualMode.TextBox_UsedMemoryMoreSize.Text))
                TaskRammpRun();
        }
        private void TextBox_FreeMemory_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            int size = int.Parse(textBox.Text);
            if (!Button_StartTask.IsEnabled && residualMode.CheckBox_FreeMemoryLower.IsChecked is true && size < int.Parse(residualMode.TextBox_FreeMemoryLowerSize.Text))
                TaskRammpRun();
        }
        private void Combobox_DataSamplingRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            timerGetMemoryMetrics.Stop();
            if (e.AddedItems[0] is ComboBoxItem item)
                timerGetMemoryMetrics.Interval = TimeSpan.FromMilliseconds(int.Parse(item.Content.ToString()![..^2]));
            timerGetMemoryMetrics.Start();
        }

        private void Button_ExecuteNow_Click(object sender, RoutedEventArgs e)
        {
            ConfigSave();
            ExecuteNow();
        }
        private void RammapModeStateChanges(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox.IsChecked == true)
                rammapModeCheckedSize += 1;
            else
                rammapModeCheckedSize -= 1;
        }
        private void Button_StartTask_Click(object sender, RoutedEventArgs e)
        {
            StartTask();
        }
        private void Button_StopTask_Click(object sender, RoutedEventArgs e)
        {
            StopTask();
        }
        private void CheckBox_LanuchOnUserLogon_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (checkBox.IsChecked == true)
                rk.SetValue("MemoryCleaner", @$"""{Environment.CurrentDirectory}\MemoryCleaner.exe""");
            else
                rk.DeleteValue("MemoryCleaner");
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }
        private void Button_Info_Click(object sender, RoutedEventArgs e)
        {
            Windows.InfoWindow infoWindow = new();
            infoWindow.ShowDialog();
        }
        private void ComboBox_I18n_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isFirst)
            {
                isFirst = false;
                return;
            }
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo((ComboBox_I18n.SelectedItem as ComboBoxItem)!.ToolTip.ToString()!);
            if (MessageBox.Show("Are you sure you want to quit?", MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                Close();
                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown(-1);
            }
        }
        private void Button_ModeSwitch_Click(object sender, RoutedEventArgs e)
        {
            Frame_ModeSwitch.Content = (currentMode = currentMode.Next ?? taskMode.First!).Value;
            Label_ModeSwitch.Content = currentMode.Value.GetType().Name;
            new Thread(() =>
            {
                Dispatcher.Invoke(() => Button_ModeSwitch.IsEnabled = false);
                Thread.Sleep(3000);
                Dispatcher.Invoke(() => Button_ModeSwitch.IsEnabled = true);
            }).Start();
        }
    }
}
