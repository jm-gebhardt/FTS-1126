using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace AgvSimulator
{
    #region WPF Mock Data
    public class ListViewMockData
    {
        public ObservableCollection<LogData> ListViewItems { get; set; }
        public ListViewMockData()
        {
            ListViewItems = new ObservableCollection<LogData>();
            ListViewItems.Add(new LogData() { Timestamp = DateTime.Parse("13:36:30"), Message = "Left node 10" });
            ListViewItems.Add(new LogData() { Timestamp = DateTime.Parse("13:37:42"), Message = "Status message sent" });
            ListViewItems.Add(new LogData() { Timestamp = DateTime.Parse("13:38:12"), Message = "Arrived at Node 11" });
        }
    }
    #endregion

    public class LogData
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isConnected = false;
        private bool _isRunning = false;
        private ObservableCollection<LogData> _listViewItems = new();

        public MainWindow()
        {
            InitializeComponent();

            lsvLog.ItemsSource = _listViewItems;

            _listViewItems.Add(new LogData() { Timestamp = DateTime.Parse("13:36:30"), Message = "Left node 10" });
            _listViewItems.Add(new LogData() { Timestamp = DateTime.Parse("13:37:42"), Message = "Status message sent" });
            _listViewItems.Add(new LogData() { Timestamp = DateTime.Parse("13:38:12"), Message = "Arrived at Node 11" });
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            _isConnected = !_isConnected;
            var button = (Button)sender;
            button.Content = _isConnected ? "Disconnect" : "Connect";
        }

        private void RunSim_Click(object sender, RoutedEventArgs e)
        {
            _isRunning = !_isRunning;
            var button = (Button)sender;
            button.Content = _isRunning ? "Pause Simulation" : "Run Simulation";
        }

        private void Command_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                _listViewItems.Add(new LogData() { Timestamp = DateTime.Now, Message = textBox.Text });
            }
        }
    }
}
