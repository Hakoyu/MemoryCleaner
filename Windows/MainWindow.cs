using HKW.TomlParse;
using MemoryCleaner.Langs.Windows.MainWindow;
using MemoryCleaner.Langs.MessageBox;
using MemoryCleaner.Langs.NotifyIcon;
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
using System.Drawing;

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
            mainPage.SetBlurEffect += SetBlurEffect;
            mainPage.RemoveBlurEffect += RemoveBlurEffect;
            GC.Collect();
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
            SetBlurEffect();
            MessageBox.Show(MessageBoxText_I18n.ConfigLoadingError, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK);
            File.Delete(Global.configPath);
            Global.CreateConfigFile();
            SaveI18n2Config();
            RemoveBlurEffect();
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
        void DisplayInitialisation()
        {
            Frame_MainWindows.Content = mainPage;
            if (mainPage.autoMinimized == true)
                Button_TitleMin_Click(null!, null!);
        }
        void InitializeNotifyIcon()
        {
            SetNotifyIconButton();
            raskbarIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("/Resources/recycling.ico", UriKind.Relative)).Stream);
            raskbarIcon.ToolTipText = MainWindow_I18n.MemoryCleaner;
            raskbarIcon.TrayMouseDoubleClick += (o, e) => { Visibility = Visibility.Visible; };
            ContextMenu contextMenu = new();
            contextMenu.Items.Add(NotifyIcon_Run);
            contextMenu.Items.Add(NotifyIcon_Show);
            contextMenu.Items.Add(NotifyIcon_Close);
            raskbarIcon.ContextMenu = contextMenu;
        }
        void SetNotifyIconButton()
        {
            NotifyIcon_Run.Header = NotifyIcon_I18n.Run;
            NotifyIcon_Run.Icon = "♻";
            NotifyIcon_Run.Click += (o, e) => mainPage.ExecuteNow();
            NotifyIcon_Show.Header = NotifyIcon_I18n.Show;
            NotifyIcon_Show.Icon = "🔲";
            NotifyIcon_Show.Click += (o, e) => Visibility = Visibility.Visible;
            NotifyIcon_Close.Header = NotifyIcon_I18n.Close;
            NotifyIcon_Close.Icon = "❌";
            NotifyIcon_Close.Click += (o, e) => CloseProgram();
        }
        private void CloseProgram()
        {
            Effect = new System.Windows.Media.Effects.BlurEffect();
            if (MessageBox.Show(this, MessageBoxText_I18n.ConfirmExit, MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                mainPage.Close();
                Close();
            }
            Effect = null;
        }
        private void SetBlurEffect()
        {
            Effect = new System.Windows.Media.Effects.BlurEffect();
        }
        private void RemoveBlurEffect()
        {
            Effect = null;
        }
    }
}
