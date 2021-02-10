using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace StreamDeckLib.Hosting
{
	public static class DependencyInjectionExtensions
	{
		public static IHostBuilder ConfigureStreamDeckToolkit(this IHostBuilder hostBuilder, string[] args)
		{
			hostBuilder
				.ConfigureAppConfiguration((context, config) =>
				{
					var switchMappings = new Dictionary<string, string>()
						 {
								 { "-port", "StreamDeck:Port" },
								 { "--port", "StreamDeck:Port" },
								 { "-pluginUUID", "StreamDeck:PluginUUID" },
								 { "-info", "StreamDeck:Info" },
								 { "-registerEvent", "StreamDeck:RegisterEvent" },
								 { "-break", "Break" },
						 };
					config.AddCommandLine(args, switchMappings);
				});
			return hostBuilder;
		}
	}
}
