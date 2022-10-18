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
using System.IO;
using Hardcodet.Wpf.TaskbarNotification;
//using System.Drawing;
using System.Globalization;
using System.Threading;
using MemoryCleaner.Langs.MessageBox;
using MemoryCleaner.Langs.MainWindow;
using MemoryCleaner.Pages;
using System.Windows.Threading;
using HKW.TomlParse;
using HKW.WindowAccent;
//using HKW.WindowCompositorN;


namespace MemoryCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainPage mainPage = null!;
        TaskbarIcon tbi = new();
        MenuItem NotifyIcon_Run = new();
        MenuItem NotifyIcon_Show = new();
        MenuItem NotifyIcon_Close = new();

        public MainWindow()
        {
            SetI18n();
            InitializeComponent();
            //WindowAccent.SetBlurBehind(this, Color.FromArgb(64, 0, 0, 0));
            //WindowAccent.SetAcrylicBlurBehind(this, 0x64000000);
            InterfaceInitialize();
            ButtonInitialize();
            NotifyIconInitialize();
            AutoMinimizedAndStart();
        }
        void SetI18n()
        {
            if (!File.Exists(MainPage.configPath))
            {
                string config;
                using StreamReader sr = new(Application.GetResourceStream(MainPage.resourcesConfigUri).Stream);
                config = sr.ReadToEnd();
                using StreamWriter sw = File.AppendText(MainPage.configPath);
                sw.Write(config);
                sw.Close();
                using TomlTable toml = TOML.Parse(MainPage.configPath);
                toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                toml.SaveTo(MainPage.configPath);
            }
            else
            {
                try
                {
                    using TomlTable toml = TOML.Parse(MainPage.configPath);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                }
                catch
                {
                    //删除配置文件并重启应用
                    MessageBox.Show("Settings loading error\nWill restore default settings and restart the application");
                    File.Delete(MainPage.configPath);
                    System.Windows.Forms.Application.Restart();
                    Application.Current.Shutdown(-1);
                }
            }
            mainPage = new();
            foreach (ComboBoxItem item in mainPage.ComboBox_I18n.Items)
            {
                if (item.ToolTip.ToString() == Thread.CurrentThread.CurrentUICulture.Name)
                {
                    mainPage.ComboBox_I18n.SelectedItem = item;
                    break;
                }
            }
        }
        void AutoMinimizedAndStart()
        {
            if (mainPage.CheckBox_AutoMinimizedAndStart.IsChecked is true)
            {
                Visibility = Visibility.Hidden;
                mainPage.StartTask();
            }
        }
        void InterfaceInitialize()
        {
            Frame_MainWindows.Content = mainPage;
            if (mainPage.autoMinimized == true)
                Button_TitleMin_Click(null!, null!);
        }
        void ButtonInitialize()
        {
            NotifyIcon_Run.Header = "Run";
            NotifyIcon_Run.Icon = "♻";
            NotifyIcon_Run.Click += (o, e) => mainPage.ExecuteNow();
            NotifyIcon_Show.Header = "Show";
            NotifyIcon_Show.Icon = "🔲";
            NotifyIcon_Show.Click += (o, e) => Visibility = Visibility.Visible;
            NotifyIcon_Close.Header = "Close";
            NotifyIcon_Close.Icon = "❌";
            NotifyIcon_Close.Click += (o, e) => CloseProgram();
        }
        void NotifyIconInitialize()
        {
            tbi.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("/Resources/recycling.ico", UriKind.Relative)).Stream);
            tbi.ToolTipText = MainWindow_I18n.MemoryCleaner;
            tbi.TrayMouseDoubleClick += (o, e) => { Visibility = Visibility.Visible; };
            ContextMenu contextMenu = new();
            contextMenu.Items.Add(NotifyIcon_Run);
            contextMenu.Items.Add(NotifyIcon_Show);
            contextMenu.Items.Add(NotifyIcon_Close);
            tbi.ContextMenu = contextMenu;
        }
        //窗体移动
        private void Grid_TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        //最小化
        private void Button_TitleMin_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            //WindowState = WindowState.Minimized;
        }
        //最大化
        private void Button_TitleMax_Click(object sender, RoutedEventArgs e)
        {
            ////限制最大化区域,不然会盖住任务栏
            //MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            //MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            ////检测当前窗口状态
            //if (WindowState == WindowState.Normal)
            //    WindowState = WindowState.Maximized;
            //else
            //    WindowState = WindowState.Normal;
        }
        //关闭
        private void Button_TitleClose_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to quit?", MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                CloseProgram();
        }
        private void CloseProgram()
        {
            mainPage.Close();
            Close();
        }
    }
}
