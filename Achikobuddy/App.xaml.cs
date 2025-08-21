using System.Windows;
using Achikobuddy.Memory;

namespace Achikobuddy
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Enable logging globally
            Bugger.EnableLogging(true);

            // ✅ Start pipe server once for the whole application
            Bugger.StartPipeServer();

            Bugger.Log("App started, pipe server running [Critical]");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Bugger.Log("App exiting, stopping pipe server [Critical]");
            Bugger.StopPipeServer(); // Graceful cleanup
            base.OnExit(e);
        }
    }
}
