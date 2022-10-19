using HKW.TomlParse;
using MemoryCleaner.Langs.MainWindow;
using MemoryCleaner.Langs.MessageBox;
using MemoryCleaner.Lib;
using MemoryCleaner.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;
using System.IO;

namespace MemoryCleaner.Windows
{
    public partial class MainWindow
    {
        void SetI18n()
        {
            if (Global.CreateConfigFile())
            {
                SaveI18n2Config();
            }
            else
            {
                try
                {
                    using TomlTable toml = TOML.Parse(Global.configPath);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                }
                catch
                {
                    ConfigLoadError();
                }
            }
        }
        void SetMainPage()
        {
            mainPage = new();
            mainPage.ChangeI18n += ChangeI18n;
            mainPage.ConfigLoadError += ConfigLoadError;
        }
        void ChangeI18n()
        {
            Label_Title.Content = MainWindow_I18n.MemoryCleaner;
            SetMainPage();
            Frame_MainWindows.Content = null!;
            Frame_MainWindows.Content = mainPage;
        }
        void ConfigLoadError()
        {
            MessageBox.Show(MessageBoxText_I18n.ConfigLoadingError,MessageBoxCaption_I18n.Warn,MessageBoxButton.OK);
            File.Delete(Global.configPath);
            Global.CreateConfigFile();
            SaveI18n2Config();
        }
        static void SaveI18n2Config()
        {
            using TomlTable toml = TOML.Parse(Global.configPath);
            toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
            toml.SaveTo(Global.configPath);
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
        void NotifyIconButtonInitialize()
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
        private void CloseProgram()
        {
            mainPage.Close();
            Close();
        }
    }
}
