using Microsoft.Extensions.Logging;
using UniversalFeeder.Mobile.Services;
using UniversalFeeder.Mobile.ViewModels;
using UniversalFeeder.Mobile.Views;

namespace UniversalFeeder.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Services (singletons — shared across the app)
		builder.Services.AddSingleton<MqttService>();
		builder.Services.AddSingleton<BleService>();
		builder.Services.AddSingleton<FeederStorageService>();
		builder.Services.AddSingleton<LogRepository>();

		// ViewModels
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<ScheduleViewModel>();
		builder.Services.AddTransient<ProvisioningViewModel>();

		// Pages
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<SchedulePage>();
		builder.Services.AddTransient<ProvisioningPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
