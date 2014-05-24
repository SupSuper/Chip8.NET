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
using WinForms = System.Windows.Forms;
using WinDraw = System.Drawing;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Chip8.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Chip8Interpreter chip8;
        private BackgroundWorker worker;
        private DispatcherTimer delay, sound;
        private Button[] inputButtons;
        private Key[] inputKeys;

        public MainWindow()
        {
            InitializeComponent();
            chip8 = new Chip8Interpreter(listDebug);
            this.DataContext = chip8;
            screen.ItemsSource = chip8.Screen;
            worker = new BackgroundWorker();
            worker.DoWork += worker_RunGame;
            delay = new DispatcherTimer();
            delay.Interval = new TimeSpan(0, 0, 0, 0, 16);
            delay.Tick += delay_Tick;
            sound = new DispatcherTimer();
            sound.Interval = new TimeSpan(0, 0, 0, 0, 16);
            sound.Tick += sound_Tick;

            inputButtons = new Button[] { key0, key1, key2, key3, key4, key5, key6, key7, key8, key9, keyA, keyB, keyC, keyD, keyE, keyF };
            inputKeys = new Key[] { Key.X, Key.D1, Key.D2, Key.D3, Key.Q, Key.W, Key.E, Key.A, Key.S, Key.D, Key.Z, Key.C, Key.D4, Key.R, Key.F, Key.V };
        }

        private void delay_Tick(object sender, EventArgs e)
        {
            chip8.DelayTick();
        }

        private void sound_Tick(object sender, EventArgs e)
        {
            chip8.SoundTick();
        }

        private void worker_RunGame(object sender, DoWorkEventArgs e)
        {
            chip8.Run();
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            buttonPause_Click(sender, e);
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Chip-8 Binaries|*.ch8;*.c8|All Files|*.*";
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                chip8.LoadProgram(dlg.FileName);
                Title = System.IO.Path.GetFileName(dlg.FileName) + " - Chip8.NET";
                buttonRun.IsEnabled = true;
                buttonPause.IsEnabled = false;
                buttonStep.IsEnabled = true;
            }
        }

        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
                delay.Start();
                sound.Start();
                buttonRun.IsEnabled = false;
                buttonPause.IsEnabled = true;
            }
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy)
            {
                chip8.Stop();
                delay.Stop();
                sound.Stop();
                buttonRun.IsEnabled = true;
                buttonPause.IsEnabled = false;
            }
        }

        private void buttonStep_Click(object sender, RoutedEventArgs e)
        {
            chip8.Step();
        }

        private Color Win32ToWPF(WinDraw.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private WinDraw.Color WPFToWin32(Color color)
        {
            return WinDraw.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void buttonBackColor_Click(object sender, RoutedEventArgs e)
        {
            SolidColorBrush brush = Resources["screenColor"] as SolidColorBrush;
            WinForms.ColorDialog dlg = new WinForms.ColorDialog();
            dlg.Color = WPFToWin32(brush.Color);
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
            {
                Resources["screenColor"] = new SolidColorBrush(Win32ToWPF(dlg.Color));
            }
        }

        private void buttonForeColor_Click(object sender, RoutedEventArgs e)
        {
            SolidColorBrush brush = Resources["pixelColor"] as SolidColorBrush;
            WinForms.ColorDialog dlg = new WinForms.ColorDialog();
            dlg.Color = WPFToWin32(brush.Color);
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
            {
                Resources["pixelColor"] = new SolidColorBrush(Win32ToWPF(dlg.Color));
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            for (byte i = 0; i < inputKeys.Length; i++)
            {
                if (inputKeys[i] == e.Key)
                {
                    chip8.InputPress(i);
                    break;
                }
            }
        }

        private void Input_KeyUp(object sender, KeyEventArgs e)
        {
            for (byte i = 0; i < inputKeys.Length; i++)
            {
                if (inputKeys[i] == e.Key)
                {
                    chip8.InputRelease(i);
                    break;
                }
            }
        }

        private void Input_MouseDown(object sender, MouseButtonEventArgs e)
        {
            for (byte i = 0; i < inputButtons.Length; i++)
            {
                if (inputButtons[i] == sender)
                {
                    chip8.InputPress(i);
                    break;
                }
            }
        }

        private void Input_MouseUp(object sender, MouseButtonEventArgs e)
        {
            for (byte i = 0; i < inputButtons.Length; i++)
            {
                if (inputButtons[i] == sender)
                {
                    chip8.InputRelease(i);
                    break;
                }
            }
        }
    }
}
