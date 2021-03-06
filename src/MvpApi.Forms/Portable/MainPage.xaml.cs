using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MvpApi.Common.Extensions;
using MvpApi.Forms.Portable.Common;
using MvpApi.Forms.Portable.Models;
using MvpApi.Forms.Portable.Views;
using MvpApi.Services.Apis;
using MvpApi.Services.Utilities;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace MvpApi.Forms.Portable
{
    public partial class MainPage : ContentPage, INavigationHandler
    {
        private readonly WebView _webView;

        public MainPage()
        {
            InitializeComponent();

            ViewModel.NavigationHandler = this;

            this._webView = new WebView();
            this._webView.Navigated += WebView_OnNavigated;
        }
        
        private async void SignOutButton_OnClicked(object sender, EventArgs e)
        {
            try
            {
                this.SideDrawer.MainContent = this._webView;
                this._webView.Source = _signOutUri;

                StorageHelpers.Instance.DeleteToken("access_token");
                StorageHelpers.Instance.DeleteToken("refresh_token");
            }
            catch (Exception ex)
            {
                await ex.LogExceptionAsync();
                await this.DisplayAlert("Error", "something went wrong signing you out, try again.", "ok");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.IsBusy = true;
            ViewModel.IsBusyMessage = "authenticating...";

            var refreshToken = StorageHelpers.Instance.LoadToken("refresh_token");

            if (!string.IsNullOrEmpty(refreshToken))
            {
                // there is a token stored, let's try to use it and not even have to show UI
                var authorization = await this.RequestAuthorizationAsync(refreshToken, true);

                if (string.IsNullOrEmpty(authorization))
                {
                    // no token available, show dialog to get user to signin and accept
                    SideDrawer.MainContent = _webView;
                    this._webView.Source = _signInUrl;
                }
                else
                {
                    App.ApiService = new MvpApiService(authorization);

                    ViewModel.IsLoggedIn = true;

                    ViewModel.IsBusyMessage = "downloading profile info...";
                    ViewModel.Mvp = await App.ApiService.GetProfileAsync();

                    ViewModel.IsBusyMessage = "downloading profile image...";
                    ViewModel.ProfileImagePath = await App.ApiService.DownloadAndSaveProfileImage();
                    
                    // Using ViewModel method in order to trigger appropriate data downloads for that view
                    this.ViewModel.LoadView(ViewType.Home);
                }
            }
            else
            {
                // no token available, show dialog to get user to signin and accept
                SideDrawer.MainContent = _webView;
                this._webView.Source = _signInUrl;
            }

            ViewModel.IsBusyMessage = "";
            ViewModel.IsBusy = false;
        }

        #region Global Navigation
        
        public void LoadView(ViewType viewType)
        {
            switch (viewType)
            {
                case ViewType.Home:
                    SideDrawer.MainContent = new HomeView();
                    break;
                case ViewType.Profile:
                    SideDrawer.MainContent = new ProfileView();
                    break;
                case ViewType.Detail:
                    SideDrawer.MainContent = new DetailView();
                    break;
                case ViewType.About:
                    SideDrawer.MainContent = new AboutView();
                    break;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (SideDrawer.MainContent.GetType() != typeof(HomeView))
            {
                SideDrawer.MainContent = new HomeView();
                return true;
            }

            return base.OnBackButtonPressed();
        }

        #endregion
        
        #region Global Authentication

        private static readonly string _scope = "wl.emails%20wl.basic%20wl.offline_access%20wl.signin";
        private static readonly string _clientId = "090fa1d9-3d6f-4f6f-a733-a8b8a3fe16ff";
        private readonly string _redirectUrl = "https://login.live.com/oauth20_desktop.srf";
        private readonly string _accessTokenUrl = "https://login.live.com/oauth20_token.srf";
        private readonly Uri _signInUrl = new Uri($"https://login.live.com/oauth20_authorize.srf?client_id={_clientId}&redirect_uri=https:%2F%2Flogin.live.com%2Foauth20_desktop.srf&response_type=code&scope={_scope}");
        private readonly Uri _signOutUri = new Uri($"https://login.live.com/oauth20_logout.srf?client_id={_clientId}&redirect_uri=https:%2F%2Flogin.live.com%2Foauth20_desktop.srf");
        
        private async void WebView_OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            switch (e.Result)
            {
                case WebNavigationResult.Success:
                    if (e.Url.Contains("code="))
                    {
                        var myUri = new Uri(e.Url);

                        var authCode = myUri.ExtractQueryValue("code");

                        await this.RequestAuthorizationAsync(authCode);
                    }
                    else if (e.Url.Contains("lc="))
                    {
                        // Redirect to signin page if there's a bounce
                        this._webView.Source = _signInUrl;
                    }
                    break;
                case WebNavigationResult.Failure:
                    break;
                case WebNavigationResult.Timeout:
                    break;
                case WebNavigationResult.Cancel:
                    break;
                default:
                    break;
            }

        }
        
        private async Task<string> RequestAuthorizationAsync(string authCode, bool isRefresh = false)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Construct the Form content, this is where I add the OAuth token (could be access token or refresh token)
                    var postContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("client_id", _clientId),
                        new KeyValuePair<string, string>("grant_type", isRefresh ? "refresh_token" : "authorization_code"),
                        new KeyValuePair<string, string>(isRefresh ? "refresh_token" : "code", authCode.Split('&')[0]),
                        new KeyValuePair<string, string>("redirect_uri", _redirectUrl)
                    });

                    // Variable to hold the response data
                    var responseTxt = "";

                    // post the Form data
                    using (var response = await client.PostAsync(new Uri(_accessTokenUrl), postContent))
                    {
                        // Read the response
                        responseTxt = await response.Content.ReadAsStringAsync();
                    }

                    // Deserialize the parameters from the response
                    var tokenData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseTxt);

                    if (tokenData.ContainsKey("access_token"))
                    {
                        StorageHelpers.Instance.StoreToken("access_token", tokenData["access_token"]);
                        StorageHelpers.Instance.StoreToken("refresh_token", tokenData["refresh_token"]);

                        // We need to prefix the access token with the token type for the auth header. 
                        // Currently this is always "bearer", doing this to be more future proof
                        var tokenType = tokenData["token_type"];
                        var cleanedAccessToken = tokenData["access_token"].Split('&')[0];

                        // set public property that is "returned"
                        return $"{tokenType} {cleanedAccessToken}";
                    }
                }
            }
            catch (HttpRequestException e)
            {
                await e.LogExceptionAsync();
                await this.DisplayAlert("Error", $"Something went wrong signing you in, try again. {e.Message}", "ok");
            }
            catch (Exception e)
            {
                await e.LogExceptionAsync();
                await this.DisplayAlert("Error", $"Something went wrong signing you in, try again. {e.Message}", "ok");
            }

            return null;
        }

        #endregion
    }
}
