using System.Windows;
using FloatingDock.Services;

namespace FloatingDock
{
    public partial class App : Application
    {
        private DockManager? _dockManager;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var settings = ConfigService.Load();
            _dockManager = new DockManager(settings);
            _dockManager.LoadAll();
        }
    }
}
