using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResAutoLogin.Network
{
    public class ResLogin
    {
        public enum ResLoginStatus
        {
            Stopped = 4,
            Checking = 0,
            Authenticating = 1,
            Authenticated = 2,
            Error = 3
        }

        public ResLogin()
        {
            // Disable certificate validation.
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };
            ServicePointManager.Expect100Continue = false;
        }

        private ResLoginStatus _status = ResLoginStatus.Stopped;
        public event EventHandler StatusChanged;

        public ResLoginStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status == value)
                {
                    return;
                }
                _status = value;
                if (StatusChanged != null)
                {
                    StatusChanged(this, null);
                }
            }
        }

        public async Task StartLogin()
        {
            while (!(string.IsNullOrEmpty(App.ViewModel.Username) || string.IsNullOrEmpty(App.ViewModel.Password)))
            {
                int timing = 5000;
                if (await CheckAuth())
                {
                    this.Status = ResLoginStatus.Authenticated;
                }
                else
                {
                    if (this.Status != ResLoginStatus.Error)
                    {
                        this.Status = ResLoginStatus.Authenticating;
                    }

                    if (await PerformLogin(App.ViewModel.Username, App.ViewModel.Password))
                    {
                        this.Status = ResLoginStatus.Authenticated;
                    }
                    else
                    {
                        this.Status = ResLoginStatus.Error;
                        timing = 1000;
                    }
                }

                Thread.Sleep(timing);
            }

            this.Status = ResLoginStatus.Stopped;
        }

        public async Task<bool> CheckAuth()
        {
            try
            {
                // Create and configure the HttpClientHandler
                var httpHandler = new HttpClientHandler();

                httpHandler.AllowAutoRedirect = false;

                // Create the HttpClient.
                var http = new HttpClient(httpHandler);
                var response = await http.GetAsync("http://www.google.fr/", HttpCompletionOption.ResponseHeadersRead);
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PerformLogin(string userName, string password)
        {
            try
            {
                // Create and configure the HttpClientHandler
                var httpHandler = new HttpClientHandler();

                httpHandler.AllowAutoRedirect = false;

                httpHandler.UseCookies = true;
                httpHandler.CookieContainer.Add(new Uri("https://securelogin.arubanetworks.com/"), new Cookie("url", "http://www.google.com/"));

                // Create the HttpClient.
                var http = new HttpClient(httpHandler);

                // Message content.
                var strContent = string.Format("user={0}&fqdn=insa-lyon.fr&password={1}&submit=Envoyer", userName, password);

                var content = new StringContent(strContent);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var message = new HttpRequestMessage(HttpMethod.Post, new Uri("https://securelogin.arubanetworks.com/cgi-bin/login"));

                // Message headers.
                message.Headers.Referrer = new Uri("https://securelogin.arubanetworks.com/upload/custom/CP_FILAIRE/centre.htm");
                message.Headers.CacheControl = new CacheControlHeaderValue
                {
                    MaxAge = TimeSpan.Zero
                };

                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("sdch"));

                message.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
                message.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

                message.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("ISO-8859-1"));
                message.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

                message.Headers.Add("Origin", "https://securelogin.arubanetworks.com");
                message.Content = content;

                // Send the request and process the response.
                var response = await http.SendAsync(message);
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
        }
    }
}
