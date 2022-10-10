using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryCleaner.Pages
{
    static public class PageFun
    {
        static public int totalMemory = 0;
        static public int MemorySizeParse(int size)
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
        static public int IntervalTimeParse(int size)
        {
            if (size.ToString().Length != 2)
            {
                size = 10;
                MessageBox.Show("The value range is 10~99");
            }
            return size;
        }
    }
}
