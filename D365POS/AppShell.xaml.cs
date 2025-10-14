namespace D365POS
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
            Routing.RegisterRoute(nameof(PayCashPage), typeof(PayCashPage));
            Routing.RegisterRoute(nameof(ShowJournalPage), typeof(ShowJournalPage));
        }
    }
}
