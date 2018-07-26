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
using System.Threading;
using System.IO.Ports;

namespace QuadVisualizer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort port;
        bool portOpened;
        Thread readThread;

        public MainWindow()
        {
            InitializeComponent();
            port = new SerialPort();
            Refresh();
        }

        private void Refresh()
        {
            String[] names = SerialPort.GetPortNames();
            Array.Sort(names);
            cbSerials.Items.Clear();
            foreach (String name in names)
                cbSerials.Items.Add(name);
            if (cbSerials.HasItems)
                cbSerials.SelectedIndex = 0;
        }

        private void btPort_Click(object sender, RoutedEventArgs e)
        {
            if (!portOpened)
            {
                if (cbSerials.SelectedIndex == -1)
                    return;
                string portName = (string)cbSerials.Items[cbSerials.SelectedIndex];
                try
                {
                    port.PortName = portName;
                    port.BaudRate = 38400;
                    port.DataBits = 8;
                    port.Parity = Parity.None;
                    port.StopBits = StopBits.One;
                    port.ReadTimeout = 500;
                    port.WriteTimeout = 500;
                    port.Open();
                    cbSerials.IsEnabled = false;
                    btPort.Content = "Close";
                    portOpened = true;
                    readThread = new Thread(Read);
                    readThread.Start();
                }
                catch (Exception)
                {
                    return;
                }
            }
            else
            {
                new Thread(() =>
                {
                    portOpened = false;
                    readThread.Join();
                    port.Close();
                    btPort.Dispatcher.Invoke(new Action(() => { btPort.Content = "Open"; }));
                    cbSerials.Dispatcher.Invoke(new Action(() => { cbSerials.IsEnabled = true; }));
                }).Start();
            }
        }

        private void btRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Read()
        {
            while (portOpened)
            {
                bool isBreak = false;
                try
                {
                    string msg = port.ReadLine();
                    string[] stringValues = msg.Split(' ');
                    if (stringValues.Length != 4)
                        continue;
                    int[] values = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        if (!int.TryParse(stringValues[i], out values[i]))
                        {
                            isBreak = true;
                            break;
                        }
                    }
                    if (isBreak)
                        continue;
                    fl.Dispatcher.Invoke(new Action(() => { fl.Value = values[0]; }));
                    fr.Dispatcher.Invoke(new Action(() => { fr.Value = values[1]; }));
                    bl.Dispatcher.Invoke(new Action(() => { bl.Value = values[2]; }));
                    br.Dispatcher.Invoke(new Action(() => { br.Value = values[3]; }));
                }
                catch (TimeoutException) { }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (portOpened)
            {
                new Thread(() =>
                {
                    portOpened = false;
                    readThread.Join();
                    port.Close();
                }).Start();
            }
        }
    }
}
