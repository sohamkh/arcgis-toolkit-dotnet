using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class OAuthDialog : ContentControl
    {
        private WebBrowser _webbrowser;
        private Uri _authorizeUri;

        public OAuthDialog()
        {
            Content = _webbrowser = new WebBrowser();

            // Handle the navigation event for the browser to check for a response to the redirect URL
            _webbrowser.Navigating += WebBrowserOnNavigating;
            _webbrowser.Loaded += OAuthDialog_Loaded;
        }

        private void OAuthDialog_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_authorizeUri != null)
            {
                _webbrowser.Navigate(_authorizeUri);
            }
        }

        public Uri AuthorizeUri
        {
            get => _authorizeUri;

            set
            {
                _authorizeUri = value;
                if (_webbrowser.IsLoaded)
                {
                    if (value == null)
                    {
                        _webbrowser.NavigateToString("<html />");
                    }
                    else
                    {
                        _webbrowser.Navigate(_authorizeUri);
                    }
                }
            }
        }

        public Uri CallbackUri { get; set; }

        public event EventHandler<IDictionary<string, string>> SigninSucceeded;

        // Handle browser navigation (content changing)
        private void WebBrowserOnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            // Check for a response to the callback url
            const string portalApprovalMarker = "/oauth2/approval";
            var webBrowser = sender as WebBrowser;
            Uri uri = e.Uri;

            // If no browser, uri, or an empty url, return
            if (webBrowser == null || uri == null || CallbackUri == null || string.IsNullOrEmpty(uri.AbsoluteUri))
            {
                return;
            }

            // Check for redirect
            bool isRedirected = uri.AbsoluteUri.StartsWith(CallbackUri.OriginalString) || (CallbackUri.OriginalString.Contains(portalApprovalMarker) && uri.AbsoluteUri.Contains(portalApprovalMarker));

            if (isRedirected)
            {
                // Browser was redirected to the callbackUrl (success!)
                //    -close the window 
                //    -decode the parameters (returned as fragments or query)
                //    -return these parameters as result of the Task
                e.Cancel = true;

                // Call a helper function to decode the response parameters
                var authResponse = DecodeParameters(uri);
                SigninSucceeded?.Invoke(this, authResponse);
            }
        }

        private static IDictionary<string, string> DecodeParameters(Uri uri)
        {
            // Create a dictionary of key value pairs returned in an OAuth authorization response URI query string
            var answer = string.Empty;

            // Get the values from the URI fragment or query string
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                answer = uri.Fragment.Substring(1);
            }
            else
            {
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    answer = uri.Query.Substring(1);
                }
            }

            // Parse parameters into key / value pairs
            var keyValueDictionary = new Dictionary<string, string>();
            var keysAndValues = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var kvString in keysAndValues)
            {
                var pair = kvString.Split('=');
                string key = pair[0];
                string value = string.Empty;
                if (key.Length > 1)
                {
                    value = Uri.UnescapeDataString(pair[1]);
                }

                keyValueDictionary.Add(key, value);
            }

            // Return the dictionary of string keys/values
            return keyValueDictionary;
        }
    }
}
