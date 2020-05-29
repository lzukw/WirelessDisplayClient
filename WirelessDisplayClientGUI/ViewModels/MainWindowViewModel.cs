using System;
using System.Collections.Generic;
using WirelessDisplayClient.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace WirelessDisplayClient.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        // Summary:
        //     Constructor.
        // Parameters:
        //   serviceProvider:
        //     An object that does all the work, , like communicating
        //     with the REST-API, setting the remote screen-resolution, and
        //     starting/stopping the local VNC-Server or FFmpeg.
        //     The serviceProvider is "injected" to the constructor.
        public MainWindowViewModel( IWDCServciceProvider serviceProvider)
        {
            _wdcServiceProvider = serviceProvider;
        }

        // Here the serviceProvider-instance passed to the constructor is stored.
        private IWDCServciceProvider _wdcServiceProvider;

        /////////////////////////////////////////////////////////////////////////////
        // Boolean properties for enabling and disabling controls of the MainWindow
        /////////////////////////////////////////////////////////////////////////////

        // Backup-field for property ConnectionEstablished.
        private bool _connectionEstablished = false; 

        // Summary:
        //   ConnectionEstablished is used to enable/disable the "Connect"-
        //   and "Disconnect"-Button and the TextBox for the IP-Address.
        public bool ConnectionEstablished
        {
            get => _connectionEstablished;
            set => this.RaiseAndSetIfChanged(ref _connectionEstablished, value);
        }


        // Backup-field for property StreamStarted
        private bool _streamStarted = false;

        // Summary:
        //   StreamStarted is false after connecting and before starting the stream.
        //   It becomes true after starting the stream.
        public bool StreamStarted
        {
            get => _streamStarted;
            set => this.RaiseAndSetIfChanged(ref _streamStarted, value);
        }


        ///////////////////////////////////////////////////////////
        // Value-properties bound to controls 
        /////////////////////////////////////////////////////////// 

        //
        // Summary:
        //     Bound to TextBox with IP-Address
        private string _ipAddress="";
        public string IpAddress
        {
            get => _ipAddress;
            set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
        }

        //
        // Summary:
        //     Bound to TextBlock containing initial screen-resolution
        private string _initialRemoteScreenResolution="";
        public string InitialRemoteScreenResolution
        {
            get => _initialRemoteScreenResolution;
            set => this.RaiseAndSetIfChanged(ref _initialRemoteScreenResolution, value);
        }

        //
        // Summary:
        //     Bound to TextBlock containing current screen-resolution
        private string _currentRemoteScreenResolution="";
        public string CurrentRemoteScreenResolution
        {
            get => _currentRemoteScreenResolution;
            set => this.RaiseAndSetIfChanged(ref _currentRemoteScreenResolution, value);
        }

        //
        // Summary:
        //     Bound to items of the ComboBox with availabe screen-resolutions on
        //     the remote computer.
        public ObservableCollection<string> AvailableRemoteScreenResolutions { get; } = new ObservableCollection<string>();

        //
        // Summary:
        //     Bound to the index of the selected item in the ComboBox with availabe 
        //     screen-resolutions on the remote computer (-1 if no item is selected).
        private int _selectedRemoteScreenResolutionIndex;
        public int SelectedRemoteScreenResolutionIndex
        {
            get => _selectedRemoteScreenResolutionIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedRemoteScreenResolutionIndex, value);
        }

        //
        // Summary:
        //     Bound to the IsSelected-Attribute of the "VNC"-Radio-Button
        private bool _vncSelected = true;
        public bool VncSelected 
        {
            get => _vncSelected;
            set => this.RaiseAndSetIfChanged(ref _vncSelected, value);
        }

        //
        // Summary:
        //     Bound to the IsSelected-Attribute of the "FFmpeg"-Radio-Button
        private bool _ffmpegSelected = false;
        public bool FFmpegSelected 
        {
            get => _ffmpegSelected;
            set => this.RaiseAndSetIfChanged(ref _ffmpegSelected, value);
        }

        //
        // Summary:
        //     Bound to the value of the NumericUpDown for the port-number
        private UInt16 _portNo = 5500;
        public UInt16 PortNo
        {
            get => _portNo;
            set => this.RaiseAndSetIfChanged(ref _portNo, value);
        }

        //
        // Summary:
        //     Bound to the status-text
        private string _statusText="Ready to connect...\n";
        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

 
        ///////////////////////////////////////////////////////////
        // React on Button-Clicks and Window-Close-Button. 
        ///////////////////////////////////////////////////////////

        //
        // Summary: 
        //     React on Connect-Button. 
        public async Task ButtonConnect_Click()
        {
            // Sanity Check: Textbox for IP-Address emtpy?
            if (String.IsNullOrEmpty(IpAddress))
            {
                StatusText += "WARNING: Please enter the IP-Address of the projecting-computer before connecting!\n";
                return; // bail out
            }

            // Try to connect.
            try
            { 
                await _wdcServiceProvider.Connect(IpAddress);
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "Maybe wrong IP-Address? Or WirelessDisplayServer not started on projecting-computer?\n";
                return; // bail out
            }

            // Get the initial screen-resolution of the remote computer.
            try
            {
                InitialRemoteScreenResolution = await 
                        _wdcServiceProvider.GetInitialScreenResolution();
            }       
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not get initial screen-resolution! Connection Lost?\n";
            }

            // Get the current screen-resolution of the remote computer.
            try
            {
                CurrentRemoteScreenResolution = await 
                        _wdcServiceProvider.GetCurrentScreenResolution();
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not get current screen-resolution! Connection Lost?\n";
            }

            // Fill the ComboBox-items with the available screen-resolutions
            try 
            {
                List<string> resolutions = await 
                        _wdcServiceProvider.GetAvailableRemoteScreenResolutions();
                SelectedRemoteScreenResolutionIndex = -1;
                AvailableRemoteScreenResolutions.Clear();
                foreach (string res in resolutions)
                {
                    AvailableRemoteScreenResolutions.Add(res);
                }
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not get available screen-resolutions! Connection Lost?\n";
            }

            // Pre-select moderate screen-resolution for remote computer and streaming
            SelectedRemoteScreenResolutionIndex = indexOfNearestResolution(
                                        AvailableRemoteScreenResolutions, 1024);

            // Finally switch the Window-State:
            ConnectionEstablished = true;
            StatusText += $"Successfully connected to {_wdcServiceProvider.LastKnownRemoteIP}\n";
        }

        //
        // Summary:
        //     React on Start-Streaming-Button
        public async Task ButtonStartStreaming_click()
        {  
            // First set Screen Resolution:
            string receiverResolution = 
                    AvailableRemoteScreenResolutions[SelectedRemoteScreenResolutionIndex];
            try
            {
                await _wdcServiceProvider.SetRemoteScreenResolution(receiverResolution);
                StatusText += $"Successfully set screen-resolution to {receiverResolution}\n";
                CurrentRemoteScreenResolution = receiverResolution;
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not set remote screen-resolution. Connection lost?";
            }

            // Then stop eventually still ongoing streaming
            try {
                await _wdcServiceProvider.StopStreaming();
                StatusText += "Sucessfully stopped eventually still running streaming.\n";
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not stop eventually still running streaming.\n"; 
            }

            // Check if VNC or FFmpeg is desired:
            if (VncSelected)
            {
                // start VNC streaming
                // No typo: The resolution used for the stream is set equal to the
                // the resolution of the receiver. 
                try {
                    await _wdcServiceProvider.StartVNCStreaming(
                                     PortNo,
                                     senderResolution : null,
                                     streamResolution : receiverResolution,
                                     receiverResolution : receiverResolution);
                    StatusText += $"Successfully started VNC-connection to {_wdcServiceProvider.LastKnownRemoteIP}:{PortNo} ... stream-resolution={receiverResolution}\n";
                }
                catch (Exception e)
                {
                    StatusText += $"{e.Message}\n";
                    StatusText += $"ERROR: Could not start VNC-Streaming to {_wdcServiceProvider.LastKnownRemoteIP}:{PortNo}\n";
                    return; // bail out
                }
            }
            else if (FFmpegSelected)
            {
                //start FFmpeg streaming
                // No typo: The resolution used for the stream is set equal to the
                // the resolution of the receiver. 
                try {
                    await _wdcServiceProvider.StartFFmpegStreaming(
                                     PortNo,
                                     senderResolution : null,
                                     streamResolution : receiverResolution,
                                     receiverResolution : receiverResolution);
                    StatusText += $"Successfully started FFmpeg-streaming to {_wdcServiceProvider.LastKnownRemoteIP}:{PortNo} ... stream-resolution={receiverResolution}\n";
                }
                catch (Exception e)
                {
                    StatusText += $"{e.Message}\n";
                    StatusText += $"ERROR: Could not start FFmpeg-streaming to {_wdcServiceProvider.LastKnownRemoteIP}:{PortNo}\n";
                    return; // bail out
                }
              }
            else
            {
                throw new Exception("BUG: Neither VNC nor FFmpeg selected. This should not be possible.");
            }            

            // Last, preselect initial screen-resolution
            SelectedRemoteScreenResolutionIndex = 
                    indexOfResolution( AvailableRemoteScreenResolutions, 
                                       InitialRemoteScreenResolution);

            // and switch the window-state
            ConnectionEstablished = true;
            StreamStarted = true;
        }


        //
        // Summary:
        //     React on Stop-Streaming-Button
        public async Task ButtonStopStreaming_click()
        {
            // First stop streaming
            try 
            {
                await _wdcServiceProvider.StopStreaming();
                StatusText += "Successfully stopped streaming\n";
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText = "ERROR: Could not stop streaming.\n";
                return;
            }

            // Then set Screen Resolution:
            string receiverResolution = 
                    AvailableRemoteScreenResolutions[SelectedRemoteScreenResolutionIndex];
            try {
                await _wdcServiceProvider.SetRemoteScreenResolution(receiverResolution);
                StatusText += $"Successfully set screen-resolution to {receiverResolution}\n";
                CurrentRemoteScreenResolution = receiverResolution;
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += $"WARNING: Could not set screen-resolution to {receiverResolution}\n";
            }

            // Finally preselect moderate screen-resolution, for next streaming
            SelectedRemoteScreenResolutionIndex = 
                    indexOfNearestResolution( AvailableRemoteScreenResolutions, 1024);

            // and switch the window-state
            ConnectionEstablished = true;
            StreamStarted = false;
        }

        //
        // Summary:
        //     React on the Disconnect-Button
        public async Task ButtonDisconnect_Click()
        {
            // Normally the user first should click the "Stop-Streaming"-
            // Button, and then the "Disconnect"-Button. For a better
            // user-experience, allow the user just click "Disconnect" directly 
            // and perform the "Stop-Sreaming"-Click by program:
            if (StreamStarted)
            {
                await ButtonStopStreaming_click();
            }

            ConnectionEstablished = false;
            StreamStarted = false;
            StatusText += "Disconnected.\n";
        }

        //
        // Summary:
        //     Executed, when the user uses the (X)-Button in the title-bar
        //     of the Main-Window. Shut down the connection before closing
        //     the application.
        public async Task OnWindowClose()
        {
            if (ConnectionEstablished)
            {
                await ButtonDisconnect_Click();
            }
        }
 

        ///////////////////////////////////////////////////////////
        // Helper methods
        ///////////////////////////////////////////////////////////

        // Searches all screen-resolutions in the ComboBox (AvailableScreenResolutions)
        // and selects the one, whose width is nearest to 1024 pixels. The selection
        // is simply done by modifying SelectedScreenResolutionIndex.
        private int indexOfNearestResolution( IEnumerable<string> resolutions,
                                              int desiredWidth = 1024)
        {
            int index = 0;
            int indexToSelect=-1; // worst-case: select none
            int smallestDeviation = Int32.MaxValue;

            foreach (string res in resolutions)
            {
                int width = Convert.ToInt32(res.Split('x')[0]);
                if ( Math.Abs(width-desiredWidth) <= smallestDeviation)
                {
                    smallestDeviation = Math.Abs(width-desiredWidth);
                    indexToSelect = index;
                }
                index++;
            }

            return indexToSelect;
        }

        // Searches all screen-resolutions in the ComboBox (AvailableScreenResolutions)
        // and selects the one that is given by resolutionToSelect.
        private int indexOfResolution(IEnumerable<string> resolutions,
                                       string resolutionToSelect)
        {
            int index = 0;

            foreach (string res in resolutions)
            {
                if (res == resolutionToSelect)
                {
                    return index;
                }
                index++;
            }

            return -1; // not found
        }
        
    }
}
