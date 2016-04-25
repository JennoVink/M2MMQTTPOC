using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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
            Debug.WriteLine("Hello");
            init();
        }

        public void init()
        {
            // create client instance 
            MqttClient client = new MqttClient("server.drewes-webdesign.nl", 1883, false, MqttSslProtocols.None);
            
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, "inf2g", "!_pass12", true, 60);
            Debug.WriteLine("Is connected? " + client.IsConnected);

            // subscribe to the topic "/home/temperature" with QoS 2 
            client.Subscribe(new string[] { "test/testPOC" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE});
            client.Subscribe(new string[] { "power/solar/temp" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.WriteLine("------------ You've recieved a message! ----------");
            Debug.WriteLine("Topic: " + e.Topic);
            Debug.WriteLine("vertaald: " + System.Text.Encoding.ASCII.GetString(e.Message));    
            Debug.WriteLine("------------------ End of message ----------------");
        }
        
    }
}
