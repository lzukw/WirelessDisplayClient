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

- `Porgram.cs`: This file was not modified. It starts the Avalonia-engine. This
  engine creates and uses an instance of the `App`-class.
- `App.xaml` and `App.xaml.cs`: Together they define the `App-class`.
- `Views/MainWindow.xaml` and `Views/MainWindow.xaml.cs`: Toghether the define 
  the `MainWindow`-class, which is the 'view' for the GUI-Main-Window. The file
  `Views/MainWindow.xaml.cs` is called 'code-behind' for the 'view'.
- `ViewModels/MainWindowViewModel.cs`: The so called 'viewmodel' for the view.
  See [Views and ViewModels](http://avaloniaui.net/docs/quickstart/mvvm#views-and-viewmodels)
  for a good explanation for the idea behind 'views' and  their 'viewmodel', and
  co called 'bindings' between them.
- The `Models`-folder is not used in this project, since there is no data to
  be managed.

The following files were created:

- `App.config`: An xml-File containting key-value-pairs, that can easily be 
  read by `System.Configuration.ConfigurationManager.AppSettings`. This is 
  done in `App.xaml.cs`.
- The folder `Services` containing:
  * `IWDCServciceProvider.cs`
  * `WDCServiceProviderBase.cs`
  * Concrete classes inheriting from `WDCServiceProviderBase.cs`. (for now 
    only `WDCSercviceProviderGeneric.cs`).
  * `WDCSercviceException.cs`

## Service

The background-work is performed by the classes defined in the folder 
`Services`.

`WDCSercviceException` is the type of exceptions thrown by the classes in 
the `Services`-folder.

The provided services are declared as methods and properties of the interface 
`IWDCServciceProvider`:

- Services to establish a connection to the remote computer (projecting 
  computer), that is running the WirelessDisplayServer.
- Methods to get and set screen-resolution of the remote computer
- Methods to start and stop VNC- or ffmpeg-streaming.

The abstract base-class `WDCServiceProviderBase` implements 
`IWDCServciceProvider`. This class provides non-abstract methods for the 
communication via the REST-API with the remote computer. 
Platform-dependent local services (for example starting ffmpeg as local 
streaming-source)  are defined as abstract methods and therfore must be 
implemented by child-classes from `WDCServiceProviderBase`.

The idea behind this hirarchy is, that there could be some platform-dependent 
concrete classes like `WDCServiceProviderLinux`, `WDCServiceProviderMacOS`
and `WDCServiceProviderWindows`. These classes could do the platform-dpendent
work to provide the services. But for now, there is only one concrete 
child-class, called `WDCSercviceProviderGeneric`. Hopefully, this class 
can be used on Linux, Windows and macOS.

The `WDCSercviceProviderGeneric`-class does not start ffmpeg or a VNC-server
directly, but it starts a process wich executes a script. On Linux the script 
is a shell-script started with bash on Linux, a batch-file started with 
cmd.exe on Windows, and ??? TODO ??? on macOS. The command-name "bash" or 
"cmd.exe" and the file-paths to the scripts to execute are passed to the 
constructor of `WDCSercviceProviderGeneric`.

One instance of a concrete class (for now an instance of 
`WDCSercviceProviderGeneric`) is instantiated in `App.xaml.cs` and
"injected" in the constructor of the `MainWindowViewModel`. The
`MainWindowViewModel`-instance uses the services provided by the 
`WDCSercviceProviderGeneric`-instance.

## Configuration with App.config and startup-code

Dotnet core provides a simple configuration-mechanism using an XML-file called
`App.config`. In the section `<appsettings>` key-value-pairs can be defined.
The `App`-class defined in `App.xaml.cs` contains the startup-code. (The file 
`App.xaml` has not been modified). Here the configuration from `App.config` 
is read with the statement into the variable `config`:

```
NameValueCollection config = ConfigurationManager.AppSettings;
```

`App.config` contains configuration-strings containing:

- The command-name to execute a shell ("bash", "cmd.exe")
- The filepath of the scripts that start ffmpeg or a VNC-Server on the local
  computer.
- Template-strings for command-line arguments.

There are configuration-strings for Linux, Windows and macOS. In 
`App.xaml.cs` the correct operating-system is found with 
`RuntimeInformation.IsOSPlatform(...)`-tests. Then the corresponding
configuration-strings are copied into a new `NameValueCollection`, which is 
passed to the constructor of `WDCSercviceProviderGeneric`.

Note, that it should not be necessary to change `App.config`. To change
the way, how ffmpeg or a VNC-Server are started, the corresponding
shell- or batch-sripts can be changed.

Also note, that after publishing the program with `dotnet publish` the 
XML-configuration is not called `App.config` anymore, but it is called
`WirelessDisplayClient.exe.config`.

Sumarrized, the custom startup-code in `App.xaml.cs` just creates an
instance of `WDCSercviceProviderGeneric` and passes the correct
configuration-key-value-pairs to its constructor. Also an instance of
`MainWindowViewModel` is created, and the 
`WDCSercviceProviderGeneric`-instance is passed to the constructor of
the `MainWindowViewModel`.

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
  current resolutions).
- Two ComboBoxes to select the local and remote screen-resolution. The 
  selected screen-resolutions are set, when the user starts the streaming.
  Note: There is a third screen-resolution: The resolution used for the
  stream. This resolution is always identical to the resoltion of the
  remote computer (because no scaling of the received stream is performed by
  the WirelessDisplayServer on the remote computer). The items of these
  ComboBoxes and the index of the selected item are bound to the properties
  * AvailableRemoteScreenResolutions
  * SelectedRemoteScreenResolutionIndex
  * AvailableRLocalScreenResolutions
  * SelectedLocalScreenResolutionIndex
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
`CMainWindowViewModel.onnectionEstablished` and 
`MainWindowViewModel.StreamStarted`, which are bound to the 
IsEnabled-Attribute of the above described window-elements. 

When theuser closes the window, the method  
`MainWindowViewModel.OnWindowClose` is called.

Remark: It is not necessary to understand the code of this application,
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
ensured with the field

```
private bool  _readyToCloseWindow = false;
```

and by registration of a lambda to the `this.Closing`-event, where `this` is
the `MainWindow`-View.