using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Achikobuddy.Memory;
using Achikobuddy.Core;

namespace Achikobuddy.UI
{
    public partial class Launcher : Window
    {
        private DispatcherTimer _updateTimer;
        private int? _lastSelectedPid;
        private Debug _debugWindow;

        public Launcher()
        {
            InitializeComponent();
            Loaded += Launcher_Loaded;
            Unloaded += Launcher_Unloaded;
        }

        private void Launcher_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePidList();
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _updateTimer.Tick += (s, args) => UpdatePidList();
            _updateTimer.Start();
            Bugger.Log("Launcher loaded, timer started [Critical]");
        }

        private void Launcher_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
                Bugger.Log("Launcher unloaded, timer stopped [Critical]");
            }
            if (_debugWindow != null)
            {
                _debugWindow.Close();
                _debugWindow = null;
            }
        }

        private void UpdatePidList()
        {
            _lastSelectedPid = GetSelectedPid();
            var wowProcesses = Process.GetProcessesByName("Wow").OrderBy(p => p.Id).ToArray();
            Bugger.Log($"Found {wowProcesses.Length} WoW processes [Critical]");

            var newItems = new List<string>();
            foreach (var proc in wowProcesses)
            {
                string item = $"WoW.exe (PID:{proc.Id})";
                newItems.Add(item);
            }

            var currentItems = selectPID.Items.Cast<string>().ToList();
            if (!newItems.SequenceEqual(currentItems))
            {
                selectPID.Items.Clear();
                foreach (var item in newItems)
                    selectPID.Items.Add(item);

                if (_lastSelectedPid.HasValue)
                {
                    var matchingItem = newItems.FirstOrDefault(i => ParsePidFromItem(i) == _lastSelectedPid.Value);
                    if (matchingItem != null)
                        selectPID.SelectedItem = matchingItem;
                }
                else if (selectPID.Items.Count > 0)
                {
                    selectPID.SelectedIndex = 0;
                }
            }
        }

        private int? GetSelectedPid()
        {
            if (selectPID.SelectedItem is string selectedItem)
            {
                int pid = ParsePidFromItem(selectedItem);
                if (pid == 0)
                    Bugger.Log("Failed to parse PID from selected item [Critical]");
                return pid;
            }
            return null;
        }

        private static int ParsePidFromItem(string item)
        {
            var match = Regex.Match(item, @"PID:(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private void launchMain_Click(object sender, RoutedEventArgs e)
        {
            var pid = GetSelectedPid();
            if (!pid.HasValue || pid.Value == 0)
            {
                Bugger.Log("No valid PID selected [Critical]");
                MessageBox.Show("Please select a WoW process first.", "Achikobuddy", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AchikobuddyDll.dll");
                if (!Injector.InjectDll(pid.Value, dllPath))
                    throw new Exception("Failed to inject DLL");

                Bugger.Log($"Launching Main for PID {pid.Value} [Critical]");
                var mainWindow = new Main(pid.Value);
                mainWindow.Show();
                _updateTimer?.Stop();
                Bugger.Log("Launcher closing, launching Main [Critical]");
                Close();
            }
            catch (Exception ex)
            {
                Bugger.Log($"Failed to launch Main for PID {pid.Value}: {ex.Message} [Critical]");
                MessageBox.Show($"Failed to launch: {ex.Message}", "Achikobuddy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableDebugCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Bugger.EnableLogging(true);
            if (_debugWindow == null)
            {
                _debugWindow = new Debug();
                _debugWindow.Closed += (s, args) =>
                {
                    _debugWindow = null;
                    enableDebugCheckBox.IsChecked = false;
                    Bugger.Log("Debug window closed from Launcher [Critical]");
                };
                _debugWindow.Show();
                Bugger.Log("Debug window opened from Launcher [Critical]");
            }
        }

        private void EnableDebugCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Bugger.EnableLogging(false);
            if (_debugWindow != null)
            {
                _debugWindow.Close();
                _debugWindow = null;
                Bugger.Log("Debug window closed from Launcher [Critical]");
            }
        }
    }
}