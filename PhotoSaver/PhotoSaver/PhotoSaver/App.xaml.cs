using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PhotoSaver
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            (MainPage as MainPage).LoadState();
        }

        protected override void OnSleep()
        {
            (MainPage as MainPage).SaveState();
        }

        protected override void OnResume()
        {   
        }
    }
}
