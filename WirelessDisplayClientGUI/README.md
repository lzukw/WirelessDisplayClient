# WirelessDisplayServer

For a project decription, see [README.md](../README.md) 
one folder above.

# Technical Details for WirelessDisplayServerGUI

For this GUI-Program the platform-independent toolkit 
[Avalonia](http://avaloniaui.net/) is used. 

To be able to deveolop an Avalonia-program run:

```
git clone https://github.com/AvaloniaUI/avalonia-dotnet-templates.git
dotnet new --install [path-to-repository]
```

This program was created with :

```
mkdir WirelessDisplayClientGUI
cd WirelessDisplayClientGUI
dotnet new dotnet new avalonia.mvvm
```

From the created files, the following ones are important:

- [Porgram.cs]: This file was not modified. It starts the Avalonia-engine. This
  engine creates and uses an instance of the `App`-class.
- [App.xaml] and [App.xaml.cs]: Together they define the `App-class`.
- [Views/MainWindow.xaml] and [Views/MainWindow.xaml.cs]: Toghether the define 
  the `MainWindow`-class, which is the 'view' for the GUI-Main-Window. The file
  [Views/MainWindow.xaml.cs] is called 'code-behind' for the 'view'.
- [ViewModels/MainWindowViewModel.cs]: The so called 'viewmodel' for the view.
  See [Views and ViewModels](http://avaloniaui.net/docs/quickstart/mvvm#views-and-viewmodels)
  for a good explanation for the idea behind 'views' and  their 'viewmodel', 
  and so called 'bindings' between them.
- The [Models]-folder is not used in this project, since there is no data to
  be managed.

The following files were created:

- [App.config]: An xml-File containting key-value-pairs, which can easily be 
  read by `System.Configuration.ConfigurationManager.AppSettings`. This is 
  done in [App.xaml.cs].
- The folder [WDCServices] containing:
  * [WDCSercviceCommon.cs]
  * [IRestApiClientService.cs] and [RestApiClientService.cs] 
  * [IStreamSourceService.cs] and [StreamSourceService.cs] 
  * [ISreenResolutionService.cs] and [SreenResolutionService.cs]

## Services

The background-work is performed by the classes defined in the folder 
[WDCServicesServices].

In the file [WDCSercviceCommon.cs] are two things used more than one class:

- `WDCSercviceException` is the type of exceptions thrown by the classes in 
  the [WDCServicesServices]-folder.
- `StreamType` is ab enumeration with the possible values `StreamType.None`,
  `StreamType.VNC` and `StreamType.FFmpeg`. 

Each class offers its relevant properties and methods via a corresponding
interface, that the class implements. For example, the class 
`RestApiClientService` implements the interface `IRestApiClientService`.
Alle Files outside [WDCServicesServices] use only the interfaces.

The class `RestApiClientService` provides:

- Methods and properties to establish a connection to the remote computer 
  (projecting computer), that is running the WirelessDisplayServer.
- Methods to get and set screen-resolution of the remote computer
- Methods to start and stop the remote streaming-sink (VNC-viewer or ffplay).

The class `StreamSourceService` provides methods and properties to start
and stop the local streaming-source (either VNC-Server in reverse-connection).

The class `SreenResolutionService` provides methods and properties to
query and manipulate the screen-resolution of the local computer.

The `StreamSourceService`-class and the `SreenResolutionService`-class do
not start ffmpeg, VNC-server or a program to manipulate the screen-resolution
directly, but they start a process wich executes a script. On Linux the 
scripts are shell-script started with bash. On Windows batch-files started with 
cmd.exe start the external programs, and on macOS ??? TODO ???. Strings for the
command-name "bash" or "cmd.exe" and the file-paths to the scripts to execute 
are injected to the constructors of `StreamSourceService` and 
`SreenResolutionService`. Also templates for the command-line-arguments that
have to be passed to the scripts are injected with constructor-injection.

The scripts for each operating-system can be changed to needs of each user,
without the need to change the C#-code.

In [App.xaml.cs] one instance of each service-providing-class 
(`RestApiClientService`, `StreamSourceService` and `SreenResolutionService`) 
is created. These instances are dependency-injected into the 
`MainWindowViewModel` via the constructor of `MainWindowViewModel`.
(The `MainWindowViewModel`-instance is also created in [App.xaml.cs]).
The `MainWindowViewModel`-instance then uses the services provided by 
service-providing-classes to perform actions started by the user.

## Configuration with App.config and startup-code

Dotnet core provides a simple configuration-mechanism using an XML-file called
[App.config]. In the section `<appsettings>` key-value-pairs can be defined.
The `App`-class defined in [App.xaml.cs] contains the startup-code. (The file 
[App.xaml] has not been modified). In [App.xaml.cs] the configuration from 
[App.config`] is read into the variable `config` with the statement :

```
NameValueCollection config = ConfigurationManager.AppSettings;
```

[App.config] contains configuration-strings containing:

- The command-name to execute a shell ("bash", "cmd.exe")
- The filepath of the scripts that start ffmpeg or a VNC-Server on the local
  computer.
- Template-strings for command-line arguments for the scripts.
- The preferred screen-resolution for local and remote computer during 
  streaming.

There are configuration-strings for Linux, Windows and macOS. In 
[App.xaml.cs] the correct operating-system is found with 
`RuntimeInformation.IsOSPlatform(...)`-tests. Then the corresponding
configuration-strings are copied into a new `NameValueCollection`, which is 
passed to the constructor of the service-provinding classes (see above).

Note, that it is not necessary to change [App.config]. To change
the way, how ffmpeg or a VNC-Server are started, the corresponding
shell- or batch-sripts can be changed. The only reason to change [App.config]
is to change the preferred screen-reolsution of the local and the remote
computer during streaming.

Also note, that after publishing the program with `dotnet publish` the 
XML-configuration is not called [App.config] anymore, but it is called
[WirelessDisplayClient.exe.config]. But this file could still be modified
without the need to recompile or publish the program again.

Sumarrized, the custom startup-code in [App.xaml.cs] just creates 
instances of the service-providing classes (`RestApiClientService`, 
`StreamSourceService` and `SreenResolutionService`) and passes the correct
configuration-key-value-pairs to their constructors. The startup-code also
creates an instance of `MainWindowViewModel`, and references to the 
instances of the service-providing-classes are passed to the constructor of
the `MainWindowViewModel`. The `MainWindowViewModel` is therefore able to use
the services provided by the service-providing-classes.

## Window-elements and MVMM-pattern:

See [Views and ViewModels](http://avaloniaui.net/docs/quickstart/mvvm#views-and-viewmodels)
for a good explanation for the idea behind 'views' and  their 'viewmodel'
and so called 'bindings' between them.

This application only consists of one simple Main-window (`MainWindow`-view
and `MainWindowViewModel`). The most important elements in the Main-window are:

- A TextBox, where the user enters the IP-Address of the remote computer. The
  text in this TextBox is bound to the property `MainWindowViewModel.IpAddress`
- A connect- and a disconnect-button which execute the methods 
  `MainWindowViewModel.ButtonConnect_Click` and 
  `MainWindowViewModel.ButtonDisconnect_Click`.
- TextBlocks to display local and remote screen-resolutions (initial and 
  current resolutions), bound to the properties 
  * `MainWindowViewModel.InitialLocalScreenResolution`
  * `MainWindowViewModel.CurrentLocalScreenResolution`
  * `MainWindowViewModel.InitialRemoteScreenResolution`
  * `MainWindowViewModel.CurrentRemoteScreenResolution`
- Two ComboBoxes to select the local and remote screen-resolution. The 
  selected screen-resolutions are set, when the user starts the streaming.
  Note: There is a third screen-resolution: The resolution used for the
  stream. This resolution is always identical to the resoltion of the
  remote computer (because no scaling of the received stream is performed by
  the WirelessDisplayServer on the remote computer). The items of these
  ComboBoxes and the index of the selected item are bound to the properties
  * `MainWindowViewModel.AvailableRemoteScreenResolutions`
  * `MainWindowViewModel.SelectedRemoteScreenResolutionIndex`
  * `MainWindowViewModel.AvailableRLocalScreenResolutions`
  * `MainWindowViewModel.SelectedLocalScreenResolutionIndex`
- Two RadioButtons to selct the streaming-method. The `IsChecked`-Property 
  of the two RadioButtons is bound to the properties
  `MainWindowViewModel.VncSelected` and `MainWindowViewModel.FFmpegSelected`.
- A NumericUpDown to select the Port-Number of the stream. On this port-number
  the streaming-sink - either ffplay or a VNC-viewer in reverse connection - 
  on the reremote computer will listen. The local streaming-source will 
  send the stream to this port on the remote-computer.
  The value of the NumericUpDown is bound to the property
  `MainWindowViewModel.PortNo`.
- Two Buttons for Starting and stopping the streaming. These buttons
  execute the methods `MainWindowViewModel.ButtonStartStreaming_click` and
  `MainWindowViewModel.ButtonStopStreaming_click`.
- A TextBlock bound the the property `MainWindowViewModel.StatusText` for
  displaying status-information.

Furthermore, there are two boolean-properties 
`MainWindowViewModel.ConnectionEstablished` and 
`MainWindowViewModel.StreamStarted`, which are bound to the 
IsEnabled-Attribute of several of the above described window-elements. 

When theuser closes the window, the method 
`MainWindowViewModel.OnWindowClose` is called.

Remark: It is not necessary to understand the fololowing explanation,
but the bindings between `MainWindowViewModel` and the `MainWindow`-view are 
established by:

- assiging the `MainWindowViewModel`-instance to the `DataContext`-property of
  the `MainWindow`-instance (the view).
- Using a `ViewLocater`-class and the fact, that `MainWindowViewModel` must
  inherit from `ViewModelBase`. The files `ViewLocater.cs` and 
  `ViewModelBase.cs` have not been modified.

## Shutdown-code

In the code-behind ot the Main-Window-View (`MainWindow.xaml.cs`) the call to
`MainWindowViewModel.OnWindowClose()` is performed, when the user closes
the window using the (X)-Button in the title-bar of the window. This is
ensured by registration of a lambda to the `this.Closing`-event and by
the field:

```
private bool  _readyToCloseWindow = false;
``` 
