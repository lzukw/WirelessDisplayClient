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
using Microsoft.Extensions.Logging;

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
                ( ILogger<MainWindowViewModel> mainWindowViewModelLogger,
                  IScreenResolutionService screenResolutionService, 
                  IStreamSourceService streamSourceService, 
                  IRestApiClientService restApiClientService, 
                  int preferredScreenWidth ) =  instantiateWDCServices();

                // Create the MainWindowViewModel and pass the created
                // service-providers and the preferred screen-width to it.
                desktop.MainWindow = new MainWindow // <-- This line is original code from Avlonia-template
                {
                    DataContext = new MainWindowViewModel(  mainWindowViewModelLogger, 
                                                            screenResolutionService,
                                                            streamSourceService,
                                                            restApiClientService,
                                                            preferredScreenWidth ),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }


        //
        // Summary:
        //     Creates and returns necessary instances of WirelessDisplayClient-serviceProviders.
        //     These instances are later passed to the constructor of the MainWindowViewModel,
        //     which uses the provided services.
        //     Also the preferred screen-reslution given in App.Config is returned.
        private Tuple<
                    ILogger<MainWindowViewModel>, 
                    IScreenResolutionService, 
                    IStreamSourceService, 
                    IRestApiClientService, 
                    int> 
                instantiateWDCServices()
        {
            // Create typed loggers.
            // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1#non-host-console-app
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("Default", LogLevel.Information)
                    .AddConsole();
            });

            var screenResolutionServiceLogger = loggerFactory.CreateLogger<ScreenResolutionService>();
            var streamSourceServiceLogger = loggerFactory.CreateLogger<StreamSourceService>();
            var restApiClientServiceLogger = loggerFactory.CreateLogger<RestApiClientService>();
            var mainWindowViewModelLogger = loggerFactory.CreateLogger<MainWindowViewModel>();

            // Find out the operating-System and according to it, specify
            // the concrete type for the needed IServiceProvider
            // Either "Linux", "Windows", or "macOS"
            string operatingSystem;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                operatingSystem = "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                operatingSystem = "macOS";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                operatingSystem = "Windows";
            }
            else
            {
                throw new Exception("Operating System not supported");
            }

            // Extract necessary strings from configuration (App.config)
            NameValueCollection config = ConfigurationManager.AppSettings;

            string shell = config[$"shell_{operatingSystem}"];
            string shellArgsTemplate = config[$"shell_Args_Template_{operatingSystem}"];
            string startStreamingSourceScriptPath = config[$"Start_Streaming_Source_Script_Path_{operatingSystem}"];
            string startStreamingSourceScriptArgsTemplate = config[$"Start_Streaming_Source_Script_Args_Template_{operatingSystem}"];
            string manageScreenResolutionsScriptPath = config[$"Manage_Screen_Resolutions_Script_Path_{operatingSystem}"];
            string manageScreenResolutionsScriptArgsTemplate = config[$"Manage_Screen_Resolutions_Script_Args_Template_{operatingSystem}"];
            int preferredScreenWidth = Convert.ToInt32(config[$"Preferred_Screen_Width_{operatingSystem}"]);

            IScreenResolutionService screenResolutionService = new ScreenResolutionService(
                    logger : screenResolutionServiceLogger,
                    shell : shell,
                    shellArgsTemplate : shellArgsTemplate,
                    manageScreenResolutionsScriptPath : manageScreenResolutionsScriptPath,
                    manageScreenResolutionsScriptArgsTemplate : manageScreenResolutionsScriptArgsTemplate
                    );

            IStreamSourceService streamSourceService = new StreamSourceService(
                    logger : streamSourceServiceLogger,
                    shell : shell,
                    shellArgsTemplate : shellArgsTemplate,
                    startStreamingSourceScriptPath : startStreamingSourceScriptPath,
                    startStreamingSourceScriptArgsTemplate : startStreamingSourceScriptArgsTemplate
                    );

            IRestApiClientService restApiClientService = new RestApiClientService(
                    logger : restApiClientServiceLogger
                    );

            return Tuple.Create( mainWindowViewModelLogger,
                                 screenResolutionService, 
                                 streamSourceService, 
                                 restApiClientService, 
                                 preferredScreenWidth);
        }
    }
}