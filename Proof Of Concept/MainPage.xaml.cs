using System;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using WinRTXamlToolkit.Controls;

namespace Proof_Of_Concept
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();   
            initMQTT();
            TemperatureImage.Source = new BitmapImage(new Uri(base.BaseUri, "/assets/temp-1.png"));
            humidityGauge.Value = 0;
        }

        public void initMQTT()
        {
            // create client instance 
            MqttClient client = new MqttClient("server.drewes-webdesign.nl", 1883, false, MqttSslProtocols.None);
            
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, "inf2g", "!_pass12", true, 60);

            // subscribe to the topic "/home/temperature" with QoS 2 
            client.Subscribe(new string[] { "POC/temperature" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE});
            client.Subscribe(new string[] { "POC/humidity" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Topic == "POC/temperature")
            {
                try
                {
                    var temp = int.Parse(System.Text.Encoding.ASCII.GetString(e.Message));
                    var uri = "";
                    if (temp < 20)
                    {
                        uri = "/assets/temp-1.png";
                    }
                    else if (temp < 25)
                    {
                        uri = "/assets/temp-2.png";
                    }
                    else if (temp < 30)
                    {
                        uri = "/assets/temp-3.png";
                    }
                    else
                    {
                        uri = "/assets/temp-4.png";
                    }

                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                TemperatureImage.Source = new BitmapImage(new Uri(base.BaseUri, uri));
                                temperature.Text = temp + "C";
                            });
                }
                catch (Exception)
                {
                    Debug.WriteLine("error!");;
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var dialog = new MessageDialog("Error parsing temperature to int!");
                            dialog.Title = "Error";
                            dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });
                            dialog.ShowAsync();
                        });

                }
            }

            if (e.Topic == "POC/humidity")
            {
                try
                {
                    var hum = int.Parse(System.Text.Encoding.ASCII.GetString(e.Message));

                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                humidityGauge.Value = hum;
                            });
                }
                catch (Exception)
                {
                    Debug.WriteLine("error!"); ;
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var dialog = new MessageDialog("Error parsing temperature to int!");
                            dialog.Title = "Error";
                            dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });
                            dialog.ShowAsync();
                        });

                }
            }
        }
        
    }
}
