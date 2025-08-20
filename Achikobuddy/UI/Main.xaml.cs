using System;
using System.Windows;
using Achikobuddy.Memory;

namespace Achikobuddy.UI
{
    public partial class Main : Window
    {
        private readonly int _pid;
        private Debug _debugWindow;

        public Main(int pid)
        {
            InitializeComponent();
            _pid = pid;
            Title = $"Achikobuddy - Attached to PID {_pid}";
            statusText.Text = "Bot Status: Initializing...";
            Bugger.Log($"Main initialized for PID {_pid} [Critical]");
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Placeholder: Check DLL injection status later
                statusText.Text = "Bot Status: Attached";
                Bugger.Log($"Successfully attached to PID {_pid} [Critical]");
                UpdateStatus();
            }
            catch (Exception ex)
            {
                statusText.Text = $"Bot Status: Failed - {ex.Message}";
                Bugger.Log($"Failed to attach to PID {_pid}: {ex.Message} [Critical]");
                MessageBox.Show($"Failed to initialize: {ex.Message}");
                Close();
            }
        }

        private void UpdateStatus()
        {
            // Placeholder: Will read from DLL (e.g., via named pipes)
            statusText.Text = $"Bot Status: Attached, PID: {_pid}";
            playerNameText.Text = "Player: Unknown";
            playerPosText.Text = "Position: Unknown";
            playerHealthText.Text = "Health: Unknown";
            spellIdText.Text = "Spell ID: Unknown";
            targetNameText.Text = "Target: None";
            targetGuidText.Text = "Target GUID: None";
            zoneTextLabel.Text = "Zone: Unknown";
            minimapZoneTextLabel.Text = "Minimap Zone: Unknown";
            Bugger.Log($"Status updated for PID {_pid} [Critical]");
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
                    Bugger.Log("Debug window closed from Main [Critical]");
                };
                _debugWindow.Show();
                Bugger.Log("Debug window opened from Main [Critical]");
            }
        }

        private void EnableDebugCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Bugger.EnableLogging(false);
            if (_debugWindow != null)
            {
                _debugWindow.Close();
                _debugWindow = null;
                Bugger.Log("Debug window closed from Main [Critical]");
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            statusText.Text = "Bot Status: Started";
            Bugger.Log("Start button clicked [Critical]");
            UpdateStatus();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            statusText.Text = "Bot Status: Stopped";
            Bugger.Log("Stop button clicked [Critical]");
        }

        private void ClickToMoveButton_Click(object sender, RoutedEventArgs e)
        {
            statusText.Text = "Bot Status: Move Initiated";
            Bugger.Log("Move button clicked [Critical]");
            // Placeholder: Will signal DLL for CTM
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_debugWindow != null)
            {
                _debugWindow.Close();
                _debugWindow = null;
            }
            Bugger.Log($"Main closed, detached from PID {_pid} [Critical]");
            base.OnClosed(e);
        }
    }
}