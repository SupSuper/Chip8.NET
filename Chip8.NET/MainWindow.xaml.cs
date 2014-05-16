using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private BackgroundWorker _worker;
        private Button[] _inputButtons;
        private Key[] _inputKeys;

        public MainWindow()
        {
            InitializeComponent();
            _chip8 = new Chip8Interpreter(listDebug);
            this.DataContext = _chip8;
            screen.ItemsSource = _chip8.Screen;
            _worker = new BackgroundWorker();
            _worker.DoWork += worker_RunGame;

            _inputButtons = new Button[] { key0, key1, key2, key3, key4, key5, key6, key7, key8, key9, keyA, keyB, keyC, keyD, keyE, keyF };
            _inputKeys = new Key[] { Key.X, Key.D1, Key.D2, Key.D3, Key.Q, Key.W, Key.E, Key.A, Key.S, Key.D, Key.Z, Key.C, Key.D4, Key.R, Key.F, Key.V };
        }

        void worker_RunGame(object sender, DoWorkEventArgs e)
        {
            _chip8.Run();
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (_worker.IsBusy)
            {
                _chip8.Stop();
            }
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Chip-8 Binaries|*.ch8;*.c8|All Files|*.*";
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                _chip8.LoadProgram(dlg.FileName);
                buttonRun.IsEnabled = true;
                buttonStep.IsEnabled = true;
            }
        }

        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            if (_worker.IsBusy)
            {
                _chip8.Stop();
            }
            else
            {
                _worker.RunWorkerAsync();
            }
        }

        private void buttonStep_Click(object sender, RoutedEventArgs e)
        {
            _chip8.Step();
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            for (byte i = 0; i < _inputKeys.Length; i++)
            {
                if (_inputKeys[i] == e.Key)
                {
                    _chip8.InputPress(i);
                    break;
                }
            }
        }

        private void Input_KeyUp(object sender, KeyEventArgs e)
        {
            for (byte i = 0; i < _inputKeys.Length; i++)
            {
                if (_inputKeys[i] == e.Key)
                {
                    _chip8.InputRelease(i);
                    break;
                }
            }
        }

        private void Input_MouseDown(object sender, MouseButtonEventArgs e)
        {
            for (byte i = 0; i < _inputButtons.Length; i++)
            {
                if (_inputButtons[i] == sender)
                {
                    _chip8.InputPress(i);
                    break;
                }
            }
        }

        private void Input_MouseUp(object sender, MouseButtonEventArgs e)
        {
            for (byte i = 0; i < _inputButtons.Length; i++)
            {
                if (_inputButtons[i] == sender)
                {
                    _chip8.InputRelease(i);
                    break;
                }
            }
        }
    }
}
