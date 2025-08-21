using Achikobuddy.Memory;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Achikobuddy.UI
{
    public partial class Debug : Window
    {
        private readonly DispatcherTimer _updateTimer;
        private CancellationTokenSource _pipeCts;

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
            StartLogPipeServer();
        }

        private void Debug_Unloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            _pipeCts?.Cancel();
            Bugger.Log("Debug window unloaded [Critical]");
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            var logs = Bugger.GetLogMessages();
            logTextBox.Text = string.Join("\n", logs);
            // No ScrollToEnd to disable autoscrolling
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            Bugger.ClearLogs();
            Bugger.Log("Logs cleared [Critical]");
        }

        private async void StartLogPipeServer()
        {
            _pipeCts = new CancellationTokenSource();
            while (!_pipeCts.Token.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream("AchikobuddyLogPipe", PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        await pipe.WaitForConnectionAsync(_pipeCts.Token);
                        Bugger.Log("Pipe server connected to client [Critical]");
                        using (var reader = new StreamReader(pipe))
                        {
                            while (!pipe.IsConnected || !_pipeCts.Token.IsCancellationRequested)
                            {
                                var log = await reader.ReadLineAsync();
                                if (log == null) break;
                                Bugger.Log(log);
                            }
                        }
                        Bugger.Log("Pipe server disconnected [Critical]");
                    }
                }
                catch (OperationCanceledException)
                {
                    Bugger.Log("Pipe server cancelled [Critical]");
                    break;
                }
                catch (Exception ex)
                {
                    Bugger.Log($"Pipe server error: {ex.Message} [Critical]");
                    await Task.Delay(1000, _pipeCts.Token); // Prevent tight loop on failure
                }
            }
        }
    }
}