using System.IO;
using System.Windows;
using WinIsland.UI.Services;

namespace WinIsland.UI
{
    public partial class App : Application
    {
        private BackendService? _backend;

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, args) =>
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), args.Exception.ToString());
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_domain.log"), args.ExceptionObject.ToString());
            };

            base.OnStartup(e);

            try
            {
                _backend = new BackendService();
                if (_backend.CanStart)
                    _backend.Start();
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend_err.log"), ex.ToString());
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _backend?.Dispose();
            base.OnExit(e);
        }
    }
}
