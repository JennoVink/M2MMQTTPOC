using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using WinRTXamlToolkit.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Proof_Of_Concept
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string CryptoKey = "QbRFpLYP7x:BdtK64JrPe;a6}X^$@j`_";
        private Dictionary<string, string>  _teacherDictionary = new Dictionary<string, string>();

        public MainPage()
        {
            this.InitializeComponent();   
            initMQTT();
            TemperatureImage.Source = new BitmapImage(new Uri(base.BaseUri, "/assets/temp-1.png"));
            humidityGauge.Value = 0;
        }

        private Boolean light;

        private MqttClient client;

        public void initMQTT()
        {
            // create client instance 
            client = new MqttClient("server.drewes-webdesign.nl", 1883, false, MqttSslProtocols.None);
            
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, "inf2g", "!_pass12", true, 60);

            // subscribe to the topic "/home/temperature" with QoS 2 
            client.Subscribe(new string[] { "POC/temperature" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE});
            client.Subscribe(new string[] { "POC/humidity" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { "POC/humidity" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { "/app/present/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { "/app/pair/request" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        }

        private async void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);

            if (e.Topic == "POC/temperature")
            {
                try
                {
                    var temp = int.Parse(message);
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

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                TemperatureImage.Source = new BitmapImage(new Uri(base.BaseUri, uri));
                                temperature.Text = temp + "C";
                            });
                }
                catch (Exception)
                {
                    Debug.WriteLine("error!"); ;
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            var dialog = new MessageDialog("Error parsing temperature to int!") {Title = "Error"};
                            dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });
                            await dialog.ShowAsync();
                        });

                }
            }

            if (e.Topic == "POC/humidity")
            {
                try
                {
                    var hum = int.Parse(message);

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                humidityGauge.Value = hum;
                            });
                }
                catch (Exception)
                {
                    Debug.WriteLine("error!"); ;
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            var dialog = new MessageDialog("Error parsing temperature to int!") {Title = "Error"};
                            dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });
                            await dialog.ShowAsync();
                        });

                }
            }

            if (e.Topic == "/app/pair/request")
            {
                var name = Decrypt(message, CryptoKey);

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var dialog = new MessageDialog("Accept pair request for: " + name) { Title = "Pair request" };
                    dialog.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                    dialog.Commands.Add(new UICommand { Label = "No", Id = 1 });

                    IUICommand result = await dialog.ShowAsync();

                    string returnStr = name + "|" + result.Label;
                    string encryptedStr = Encrypt(returnStr, CryptoKey);

                    client.Publish("/app/pair", Encoding.UTF8.GetBytes(encryptedStr));

                    _teacherDictionary.Add(returnStr, name);
                });
            }

            if (e.Topic == "/app/present/yes")
            {
                string decrypted = Decrypt(message, CryptoKey);

                if (_teacherDictionary.ContainsKey(decrypted))
                {
                    this.teachersListView.Items?.Add(new TextBlock() {Text = _teacherDictionary[decrypted]});
                }
            }

            if (e.Topic == "/app/present/no")
            {
                string decrypted = Decrypt(message, CryptoKey);

                if (_teacherDictionary.ContainsKey(decrypted) && this.teachersListView.Items.Contains(new TextBlock() { Text = _teacherDictionary[decrypted] }))
                {
                    this.teachersListView.Items.Remove(new TextBlock() { Text = _teacherDictionary[decrypted] });
                }
            }
        }

        private void buttonBlink_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!light)
            {
                client.Publish("POC/light", Encoding.UTF8.GetBytes("true"));               
            }
            else
            {
                client.Publish("POC/light", Encoding.UTF8.GetBytes("false"));
            }
            light = !light;
        }

        private string Decrypt(string str, string key)
        {
            IBuffer toDecryptBuffer = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);

            SymmetricKeyAlgorithmProvider aes = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);

            CryptographicKey symetricKey = aes.CreateSymmetricKey(ComputeMD5(key));

            IBuffer buffDecrypted = CryptographicEngine.Decrypt(symetricKey, toDecryptBuffer, null);

            return CryptographicBuffer.EncodeToBase64String(buffDecrypted);
        }

        private string Encrypt(string str, string key)
        {
            IBuffer toDecryptBuffer = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);

            SymmetricKeyAlgorithmProvider aes = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);

            CryptographicKey symetricKey = aes.CreateSymmetricKey(ComputeMD5(key));

            IBuffer buffDecrypted = CryptographicEngine.Decrypt(symetricKey, toDecryptBuffer, null);

            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8 ,buffDecrypted);
        }

        private IBuffer ComputeMD5(string str)
        {
            try
            {
                var alg = HashAlgorithmProvider.OpenAlgorithm("MD5");

                IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);

                IBuffer hash = alg.HashData(buff);

                byte[] hashBytes = hash.ToArray();

                Array.Resize(ref hashBytes, 16);

                return CryptographicBuffer.CreateFromByteArray(hashBytes);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
