using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chip8.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Chip8Interpreter _chip8;

        public MainWindow()
        {
            InitializeComponent();
            _chip8 = new Chip8Interpreter();
            this.DataContext = _chip8;
            screen.ItemsSource = _chip8.LCD;
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                _chip8.LoadProgram(dlg.FileName);
            }
        }

        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            _chip8.Run();
        }

        private void buttonStep_Click(object sender, RoutedEventArgs e)
        {
            _chip8.Step();
        }
    }
}
