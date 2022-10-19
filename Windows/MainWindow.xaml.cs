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
using System.IO;
using Hardcodet.Wpf.TaskbarNotification;
using System.Globalization;
using System.Threading;
using MemoryCleaner.Langs.MessageBox;
using MemoryCleaner.Pages;
using MemoryCleaner.Langs.MainWindow;
using MemoryCleaner.Lib;
using System.Windows.Threading;
using HKW.TomlParse;
using HKW.WindowAccent;


namespace MemoryCleaner.Windows
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
            SetMainPage();
            InitializeComponent();
            WindowAccent.SetBlurBehind(this, Color.FromArgb(64, 0, 0, 0));
            InterfaceInitialize();
            NotifyIconButtonInitialize();
            NotifyIconInitialize();
            AutoMinimizedAndStart();
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
            if (MessageBox.Show(MessageBoxText_I18n.ConfirmExit, MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                CloseProgram();
        }
    }
}
