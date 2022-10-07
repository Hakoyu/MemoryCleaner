using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MemoryCleaner.Windows
{
    /// <summary>
    /// InfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
        }
        //窗体移动
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            Close();
        }

        private void Hyperlink_RAMMap_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)sender;
            ProcessStartInfo info = new(link.NavigateUri.AbsoluteUri);
            info.UseShellExecute = true;
            Process.Start(info);
        }
    }
}
