using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WirelessDisplayClient.ViewModels;
using WirelessDisplayClient.Views;
using WirelessDisplayClient.Services;
using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.IO;

namespace WirelessDisplayClient
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                //////////// Modified Code starts here //////////////////////////
                
                // Will contain the specific type implementing the IServiceprovider.
                // An IServiceProvider-Isntance is needed for the MainWindowViewModel.
                // The specific type depends on the operating system we are running on.
                Type serviceProviderType; 

                // This loads the configuration from App.config (during development)
                // WirelessDisplayClient.exe.config (when published)
                NameValueCollection config = ConfigurationManager.AppSettings;

                // specificConfig will contain the key-value-pairs passed
                // to the constructor of the IServiceprovider to instantiate.
                NameValueCollection specificConfig = new NameValueCollection();
                
                // Either "_Linux", "_Windows", or "_macOS"
                string osKeySuffix;

                // Find out the operating-System and according to it, specify
                // the concrete type for the needed IServiceProvider
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    osKeySuffix = "_Linux";
                    serviceProviderType = typeof(WDCSercviceProviderGeneric);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    osKeySuffix = "_macOS";
                    serviceProviderType = typeof(WDCSercviceProviderGeneric);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    osKeySuffix = "_Windows";
                    serviceProviderType = typeof(WDCSercviceProviderGeneric);
                }
                else
                {
                    throw new Exception("Operating System not supported");
                }

                // Load correct App.config-Parameters and strip the operating-
                // system-suffi. Put these Parametersx in the new key-value-
                // collection specificConfig.
                foreach(string key in config.AllKeys)
                {
                    if(key.EndsWith(osKeySuffix))
                    {
                        specificConfig[ key.Replace(osKeySuffix, "") ] = config[key];
                    }
                }

                // Create the IServiceProvider-instance, passing specificConfig
                // as arguments to its constructor.
                IWDCServciceProvider serviceProvider = 
                        (IWDCServciceProvider) Activator
                        .CreateInstance( serviceProviderType, specificConfig);
                

                // Finally create the MainWindowViewModel and pass the created
                // IServiceProvider-instance to it.
                desktop.MainWindow = new MainWindow // <-- This line is original code from Avlonia-template
                {
                    DataContext = new MainWindowViewModel(serviceProvider),
                };
                //////////// Modified Code ends here //////////////////////////
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}