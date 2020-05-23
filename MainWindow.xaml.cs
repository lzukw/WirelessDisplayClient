using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System.Diagnostics;

using System.Collections.Generic;

namespace WirelessDisplayClient
{
    public class MainWindow : Window
    {

        private TextBox textBoxIP;
        private Button buttonConnect;
        private Button buttonDisconnect;
        private TextBlock textBlockInitialResolution;
        private TextBlock textBlockCurrentResolution;
        private ComboBox comboBoxScreenResolutions;
        private RadioButton radioButtonVNC;
        private RadioButton radioButtonFFmpeg;
        private NumericUpDown numericUpDownPort;
        private Button buttonStartStreaming;
        private Button buttonStopStreaming;

        private List<string> screenResolutions = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Find all relevant Controls from the xaml-File
            textBoxIP = this.FindControl<TextBox>("textBoxIP");
            buttonConnect = this.FindControl<Button>("buttonConnect");
            buttonDisconnect = this.FindControl<Button>("buttonDisconnect");
            textBlockInitialResolution = this.FindControl<TextBlock>("textBlockInitialResolution");
            textBlockCurrentResolution = this.FindControl<TextBlock>("textBlockCurrentResolution");
            comboBoxScreenResolutions = this.FindControl<ComboBox>("comboBoxScreenResolutions");
            buttonStartStreaming = this.FindControl<Button>("buttonStartStreaming");
            buttonStopStreaming = this.FindControl<Button>("buttonStopStreaming");
            radioButtonVNC = this.FindControl<RadioButton>("radioButtonVNC");
            radioButtonFFmpeg = this.FindControl<RadioButton>("radioButtonFFmpeg");
            numericUpDownPort = this.FindControl<NumericUpDown>("numericUpDownPort");

            buttonDisconnect.IsEnabled = false;
            comboBoxScreenResolutions.IsEnabled = false;
            
            //textBlockCurrentResolution.FormattedText.Text = "Hallo Mühlviertel!";
            comboBoxScreenResolutions.Items = screenResolutions;
            screenResolutions.Add("HalloA");
            screenResolutions.Add("HalloB");
    	    screenResolutions.Add("HalloC");
            screenResolutions.Add("HalloD");
            screenResolutions.Add("HalloE");

        }


        public void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Button Connect Clicked!");
            buttonConnect.IsEnabled = false;
            buttonDisconnect.IsEnabled = true;
        }

        public void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Button Disonnect Clicked!");
            buttonConnect.IsEnabled = true;
            buttonDisconnect.IsEnabled = false;
        }

        public void ButtonStartStreaming_Click(object sender, RoutedEventArgs e)
        {

        }

        public void ButtonStopStreaming_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}