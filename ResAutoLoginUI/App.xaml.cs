using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;
using ResAutoLogin.ViewModel;

namespace ResAutoLogin
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
    {
        public static MainViewModel ViewModel;

        static App()
        {
            ViewModel = new MainViewModel();
        }
	}
}