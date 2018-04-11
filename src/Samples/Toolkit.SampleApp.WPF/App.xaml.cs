using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Esri.ArcGISRuntime.Toolkit.Samples
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
        }


        private static bool _AuthHandlerInitialized;
        /// <summary>
        ///s Configure this app to use the Toolkit's Authentication Challenge Handler
        /// </summary>
        public static void InitializeAuthenticationHandlers(System.Windows.Threading.Dispatcher dispatcher)
        {
            if (_AuthHandlerInitialized)
                return;
            _AuthHandlerInitialized = true;

            var handler = new Esri.ArcGISRuntime.Toolkit.Authentication.ChallengeHandler(dispatcher);
            AuthenticationManager.Current.ChallengeHandler = handler;
            AuthenticationManager.Current.OAuthAuthorizeHandler = (Esri.ArcGISRuntime.Security.IOAuthAuthorizeHandler)handler;
        }
    }
}
