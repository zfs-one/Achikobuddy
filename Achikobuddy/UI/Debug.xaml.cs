using System;
using System.Windows;
using System.Windows.Threading;
using Achikobuddy.Memory;

namespace Achikobuddy.UI
{
    public partial class Debug : Window
    {
        private readonly DispatcherTimer _updateTimer;

        public Debug()
        {
            InitializeComponent();
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _updateTimer.Tick += UpdateTimer_Tick;
            Loaded += Debug_Loaded;
            Unloaded += Debug_Unloaded;
        }

        private void Debug_Loaded(object sender, RoutedEventArgs e)
        {
            _updateTimer.Start();
            Bugger.Log("Debug window loaded [Critical]");
        }

        private void Debug_Unloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            Bugger.Log("Debug window unloaded [Critical]");
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            var logs = Bugger.GetLogMessages();
            logTextBox.Text = string.Join("\n", logs);
            logTextBox.ScrollToEnd();
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            Bugger.ClearLogs();
            Bugger.Log("Logs cleared [Critical]");
        }
    }
}