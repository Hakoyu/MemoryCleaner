using MemoryCleaner.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryCleaner.Lib
{
    public static class Global
    {
        public static int totalMemory = 0;
        public const string configPath = @"Config.toml";
        public const string rammapPath = @"RAMMap.exe";
        public readonly static Uri resourcesConfigUri = new("/Resources/Config.toml", UriKind.Relative);
        public readonly static Uri resourcesRAMMapUri = new("/Resources/RAMMap.exe", UriKind.Relative);
        public static int MemorySizeParse(int size)
        {
            if (size < 1024)
            {
                MessageBox.Show("Minimum value is 1024");
                size = 1024;
            }
            else if (size > totalMemory / 2)
            {
                MessageBox.Show("The maximum value cannot exceed half of the current memory maximum");
                size = totalMemory / 2;
            }
            return size;
        }
        public static int IntervalTimeParse(int size)
        {
            if (size.ToString().Length != 2)
            {
                size = 10;
                MessageBox.Show("The value range is 10~99");
            }
            return size;
        }
        /// <summary>
        /// 判断配置文件是否存在
        /// </summary>
        /// <returns>存在返回true,不存在则新建配置文件并返回false</returns>
        public static bool CreateConfigFile()
        {
            if (File.Exists(configPath))
                return true;
            string config;
            using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
            config = sr.ReadToEnd();
            using StreamWriter sw = File.AppendText(configPath);
            sw.Write(config);
            sw.Close();
            return false;
        }
    }
}
