using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Samples.Authentication
{
    /// <summary>
    /// Interaction logic for PortalSignin.xaml
    /// </summary>
    public partial class PortalSigninSample : UserControl
    {
        ServerInfo serverInfo;
        public PortalSigninSample()
        {
            InitializeComponent();

            // Configure this app to use the Toolkit's Authentication Challenge Handler
            App.InitializeAuthenticationHandlers(this.Dispatcher);

            Loaded += PortalSignin_Loaded;
            Unloaded += PortalSignin_Unloaded;
        }

        private void PortalSignin_Unloaded(object sender, RoutedEventArgs e)
        {
            //AuthenticationManager.Current.
        }

        private void PortalSignin_Loaded(object sender, RoutedEventArgs e)
        {
            // Register a portal that uses OAuth authentication with the AuthenticationManager 
            serverInfo = new ServerInfo
            {
                ServerUri = new Uri("https://www.arcgis.com/sharing/rest"),
                TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit,
                OAuthClientInfo = new OAuthClientInfo { ClientId = "q244Lb8gDRgWQ8hM", RedirectUri = new Uri("https://developers.arcgis.com/javascript") }
            };
            AuthenticationManager.Current.RegisterServer(serverInfo);
        }


        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var portal = await Portal.ArcGISPortal.CreateAsync(new Uri("https://www.arcgis.com/sharing/rest"), loginRequired: true);
            }
            catch(System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Login failed");
            }
        }
    }
}
