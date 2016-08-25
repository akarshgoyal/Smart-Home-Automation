using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227
using Newtonsoft.Json;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

#if WINDOWS_PHONE_APP
       using Windows.Media.SpeechRecognition;
		
#endif
namespace BACH
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
#if WINDOWS_PHONE_APP
        private TransitionCollection transitions;
		
#endif

		/// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
#if WINDOWS_PHONE_APP
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
#endif

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }
		protected override void OnActivated(IActivatedEventArgs args)
		{
			base.OnActivated(args);
			if (args.Kind == ActivationKind.VoiceCommand)
			{
				var commandArgs = args as VoiceCommandActivatedEventArgs;
				if (commandArgs != null)
				{
					SpeechRecognitionResult speechRecognitionResult = commandArgs.Result;
					var voiceCommandName = speechRecognitionResult.RulePath[0];
					switch (voiceCommandName)
					{
						case "DeskLightsOff":
							SendSBMessage("0");
							break;
						case "DeskLightsOn":
							SendSBMessage("1");
							break;
					}
				}
			}
			Window.Current.Activate();
		}
#endif

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }
		private static void SendSBMessage(string message)
		{
			try
			{
				string baseUri = "https://CustomNamespace.servicebus.windows.net";
				using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
				{
					client.BaseAddress = new Uri(baseUri);
					client.DefaultRequestHeaders.Accept.Clear();

					string token = SASTokenHelper();
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", token);

					string json = JsonConvert.SerializeObject(message);
					HttpContent content = new StringContent(json, Encoding.UTF8);
					content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

					content.Headers.Add("led", message);
					string path = "/lighttopic/messages";

					var response = client.PostAsync(path, content).Result;
					if (response.IsSuccessStatusCode)
					{
						// Do something
						Debug.WriteLine("Success!");
					}
					else
					{
						Debug.WriteLine("Failure!" + response);
					}

				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("ERORR!" + ex.ToString());
			}
		}

		private static string SASTokenHelper()
		{
			//Endpoint=sb://CustomNamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=pWq4OwxD5Bjfrq14YYk0oZ6wird8LdIuitGZbTyop8Y=
			string keyName = "RootManageSharedAccessKey";
			string key = "<INSERT_YOU_KEY_HERE>";
			string uri = "CustomNamespace.servicebus.windows.net";

			int expiry = (int)DateTime.UtcNow.AddMinutes(20).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			string stringToSign = WebUtility.UrlEncode(uri) + "\n" + expiry.ToString();
			string signature = HmacSha256(key, stringToSign);
			string token = String.Format("sr={0}&sig={1}&se={2}&skn={3}", WebUtility.UrlEncode(uri), WebUtility.UrlEncode(signature), expiry, keyName);

			return token;
		}

		// Because Windows.Security.Cryptography.Core.MacAlgorithmNames.HmacSha256 doesn't
		// exist in WP8.1 context we need to do another implementation
		public static string HmacSha256(string key, string value)
		{
			var keyStrm = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
			var valueStrm = CryptographicBuffer.ConvertStringToBinary(value, BinaryStringEncoding.Utf8);

			var objMacProv = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
			var hash = objMacProv.CreateHash(keyStrm);
			hash.Append(valueStrm);

			return CryptographicBuffer.EncodeToBase64String(hash.GetValueAndReset());
		}
    }
}