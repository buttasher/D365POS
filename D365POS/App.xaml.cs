using D365POS.Services;
using System.IO;

namespace D365POS
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }
    }
}
