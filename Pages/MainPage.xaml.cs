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
using System.Globalization;
using MemoryCleaner.Langs.Code;

namespace MemoryCleaner.Pages
{
    /// <summary>
    /// MainPage.xaml 的交互逻辑
    /// </summary>
    public partial class MainPage : Page
    {
        public bool autoMinimized = false;
        Management management = new();
        int taskTime = 0;
        Thread taskTimer = null!;
        int rammapModeCheckedSize = 0;
        double totalMemory = 0;
        DispatcherTimer timerGetMemoryMetrics = new();
        public const string configPath = @"Config.ini";
        public const string rammapPath = @"RAMMap.exe";
        public readonly static Uri resourcesConfigUri = new("/Resources/Config.ini", UriKind.Relative);
        public readonly static Uri resourcesRAMMapUri = new("/Resources/RAMMap.exe", UriKind.Relative);
        bool isFirst = true;
        public MainPage()
        {
            DateInitialize();
            InitializeComponent();
            InterfaceInitialize();
            ConfigInitialize();
            GetRammapModeCheckedSize();
        }
        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        private void TextBox_UsedMemory_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double usedMemory = double.Parse(textBox.Text);
            Progressbar_MemoryMetrics.Value = usedMemory;
            Label_MemoryMetrics.Content = (usedMemory / totalMemory).ToString("0%");
            int size = int.Parse(textBox.Text);
            if (!Button_StartTask.IsEnabled && CheckBox_UsedMemoryMore.IsChecked is true && size > int.Parse(TextBox_UsedMemoryMoreSize.Text))
                TaskRammpRun();
        }
        private void TextBox_FreeMemory_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            int size = int.Parse(textBox.Text);
            if (!Button_StartTask.IsEnabled && CheckBox_FreeMemoryLower.IsChecked is true && size < int.Parse(TextBox_FreeMemoryLowerSize.Text))
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
        public void StartTask()
        {
            Button_StartTask.IsEnabled = false;
            Button_StopTask.IsEnabled = true;
            CheckBox_UsedMemoryMore.IsEnabled = false;
            CheckBox_FreeMemoryLower.IsEnabled = false;
            TextBox_UsedMemoryMoreSize.IsEnabled = false;
            TextBox_FreeMemoryLowerSize.IsEnabled = false;
            TextBox_TaskTimer.IsEnabled = false;
            //if(CheckBox_)
            taskTimer.Start();
        }

        private void Button_StartTask_Click(object sender, RoutedEventArgs e)
        {
            StartTask();
        }

        private void Button_StopTask_Click(object sender, RoutedEventArgs e)
        {
            Button_StopTask.IsEnabled = false;
            Button_StartTask.IsEnabled = true;
            CheckBox_UsedMemoryMore.IsEnabled = true;
            CheckBox_FreeMemoryLower.IsEnabled = true;
            TextBox_UsedMemoryMoreSize.IsEnabled = true;
            TextBox_FreeMemoryLowerSize.IsEnabled = true;
            TextBox_TaskTimer.IsEnabled = true;
            if (taskTimer.ThreadState != ThreadState.Unstarted)
                taskTimer.Join(1);
            taskTimer = new Thread(() => TaskTimer(taskTime));
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

        private void TextBox_MemoryMoreSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Keyboard.ClearFocus();
        }
        private void TextBox_MemorySizeChange_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            MemorySizeSave(textBox);
        }
        private void TextBox_MinimumIntervalSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Keyboard.ClearFocus();
        }
        private void TextBox_MinimumIntervalSize_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            MinimumIntervalSizeSave(textBox);
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
            if (MessageBox.Show("Are you sure you want to quit?", Code_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                Close();
                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown(-1);
            }
        }
    }
}
