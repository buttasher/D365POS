using D365POS.Services;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace D365POS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddTransient<SignInPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<SalesPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}