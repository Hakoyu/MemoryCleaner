﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using MemoryCleaner.Lib;

namespace MemoryCleaner.Pages
{
    /// <summary>
    /// TimeModePage.xaml 的交互逻辑
    /// </summary>
    public partial class TimeModePage : Page
    {
        public TimeModePage()
        {
            InitializeComponent();
        }
        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        private void TextBox_IntervalTime_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Keyboard.ClearFocus();
        }
        private void TextBox_IntervalTime_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.Text = Global.IntervalTimeParse(int.Parse(textBox.Text)).ToString();
        }
    }
}
