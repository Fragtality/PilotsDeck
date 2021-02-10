using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamDeckLib.HostedServices;
using StreamDeckLib.Models;
using System.Reflection;

namespace StreamDeckLib.DependencyInjection
{
	public static class DependencyInjectionExtensions
	{
		public static IServiceCollection AddStreamDeck(this IServiceCollection services, IConfiguration configuration, Assembly registrationAssembly)
		{
			services.Configure<StreamDeckToolkitOptions>(configuration.GetSection("StreamDeck"));
			services.Configure<StreamDeckRegistrationOptions>(config => {
				config.Assembly = registrationAssembly;
			});
			services.AddSingleton<IStreamDeckProxy, StreamDeckProxy>();
			services.AddSingleton<ConnectionManager>();
			services.AddSingleton<ActionManager>();
			services.AddHostedService<StreamDeckHostedService>();
			return services;
		}
	}
}

