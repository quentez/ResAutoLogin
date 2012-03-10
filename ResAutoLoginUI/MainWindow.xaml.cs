using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ResAutoLogin.Properties;
using ResAutoLogin.Network;
using ResAutoLogin.ViewModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Drawing;

namespace ResAutoLogin
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
            this.InitializeComponent();
            this.Hide();

            // Initiate the ResLogin.
            resLog = new ResLogin();
            resLog.StatusChanged += (sender, e) =>
            {
                UpdateStatus(true);
            };

            locator = new ViewModelLocator();

			// Insert code required on object creation below this point.
            GrPageSettings.Visibility = System.Windows.Visibility.Collapsed;

            storyShowSettings = (Storyboard)this.Resources["GoToSettings"];
            storyShowSettings.Completed += (sender, e) =>
            {
                GrPageHome.Visibility = System.Windows.Visibility.Collapsed;
            };

            storyShowHome = (Storyboard)this.Resources["GoToHome"];
            storyShowHome.Completed += (sender, e) =>
            {
                GrPageSettings.Visibility = System.Windows.Visibility.Collapsed;
                this.LayoutRoot.Focus();
            };

            storyStatusGoGreen = (Storyboard)this.Resources["StatusGoGreen"];
            storyStatusGoRed = (Storyboard)this.Resources["StatusGoRed"];
            storyStatusGoGray = (Storyboard)this.Resources["StatusGoGray"];

            // Tray icon settings.
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = "Insa House Connector";
            trayIcon.Icon = new Icon(Application.GetResourceStream(new Uri("/Icons/DefaultTrayIcon.ico", UriKind.Relative)).Stream);
            trayIcon.Visible = true;
            trayIcon.Click += (sender, e) =>
            {
                ShowWindow();
            };
            trayIcon.BalloonTipClicked += (sender, e) =>
            {
                ShowWindow();
            };

            // We setup the contextMenu of the TrayIcon.
            var openMenuItem = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "Open"
            };
            openMenuItem.Click += (sender, e) =>
            {
                ShowWindow();
            };

            var separatorMenuItem = new System.Windows.Forms.MenuItem
            {
                Index = 1,
                Text = "-"
            };

            var exitMenuItem = new System.Windows.Forms.MenuItem
            {
                Index = 2,
                Text = "Exit"
            };
            exitMenuItem.Click += (sender, e) =>
            {
                closeByTray = true;
                this.Close();
            };

            // We add the contextMenu to the trayIcon.
            trayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] { openMenuItem, separatorMenuItem, exitMenuItem });

            // Loading settings.
            App.ViewModel.Username = Settings.Default.Username;
            App.ViewModel.Password = Settings.Default.Password;

            UpdateStatus();
		}

        private Storyboard storyShowSettings;
        private Storyboard storyShowHome;

        private Storyboard storyStatusGoGreen;
        private Storyboard storyStatusGoRed;
        private Storyboard storyStatusGoGray;

        private ResLogin resLog;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private ViewModelLocator locator;
        private bool closeByTray = false;

        // Loading.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        // UI Refresh logic.
        private void UpdateStatus()
        {
            UpdateStatus(false);
        }

        private void UpdateStatus(bool fromNetwork)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrEmpty(App.ViewModel.Username) || string.IsNullOrEmpty(App.ViewModel.Password))
                {
                    TbStatus.Text = "no settings";
                    storyStatusGoRed.Begin();

                    if (!fromNetwork)
                    {
                        this.Show();
                    }
                }
                else
                {
                    if (resLog.Status == ResLogin.ResLoginStatus.Stopped)
                    {
                        Task.Factory.StartNew(() => resLog.StartLogin());
                    }
                    else if (resLog.Status == ResLogin.ResLoginStatus.Checking)
                    {
                        TbStatus.Text = "checking";
                    }
                    else if (resLog.Status == ResLogin.ResLoginStatus.Authenticating)
                    {
                        TbStatus.Text = "authenticating";
                        storyStatusGoGray.Begin();
                    }
                    else if (resLog.Status == ResLogin.ResLoginStatus.Authenticated)
                    {
                        if (fromNetwork && this.Visibility != Visibility.Visible)
                        {
                            trayIcon.ShowBalloonTip(2000, "Insa House Connector", "You are now connected.", System.Windows.Forms.ToolTipIcon.Info);
                        }

                        TbStatus.Text = "authenticated";
                        storyStatusGoGreen.Begin();
                    }
                    else
                    {
                        if (fromNetwork && this.Visibility != Visibility.Visible)
                        {
                            trayIcon.ShowBalloonTip(2000, "Insa House Connector", "You are not connected anymore.", System.Windows.Forms.ToolTipIcon.Error);
                        }

                        TbStatus.Text = "error, will retry";
                        storyStatusGoRed.Begin();
                    }
                }
            }));
        }

        // Button events.
		private void BtSettings_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            GrPageSettings.Visibility = System.Windows.Visibility.Visible;
            storyShowSettings.Begin();

            TbUsername.Focus();
            TbUsername.Select(TbUsername.Text.Length, 0);
		}

        private void BtSaveSettings_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SaveSettings();
        }

        private void BtSaveSettings_Down(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VisualStateManager.GoToState(BtSaveSettings, "Down", false);
        }

        private void Settings_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            GrPageHome.Visibility = System.Windows.Visibility.Visible;
            storyShowHome.Begin();

            // Applying settings.
            Task.Factory.StartNew(() =>
            {
                Settings.Default.Username = App.ViewModel.Username;
                Settings.Default.Password = App.ViewModel.Password;
                Settings.Default.Save();
            });

            UpdateStatus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible && !closeByTray)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
                {
                    this.Hide();
                    return null;
                }, null);

                e.Cancel = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // We free the resources associated with the TrayIcon.
            trayIcon.Dispose();
            trayIcon = null;
        }

        private void ShowWindow()
        {
            if (this.Visibility != System.Windows.Visibility.Visible)
            {
                this.Show();
            }
        }
	}
}