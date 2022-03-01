/**********************************************************************
This file is part of the TJoy project.
Copyright Maxim Paperno; all rights reserved.
https://github.com/mpaperno/TJoy

This file may be used under the terms of the GNU
General Public License as published by the Free Software Foundation,
either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

A copy of the GNU General Public License is included with this project
and is available at <http://www.gnu.org/licenses/>.

This project may also use 3rd-party Open Source software under the terms
of their respective licenses. The copyright notice above does not apply
to any 3rd-party components used within.
************************************************************************/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
//using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
//using System.Threading.Tasks;
using TouchPortalSDK.Configuration;

namespace TJoy.TouchPortalPlugin
{
	public static class Program
	{
    //private static async Task Main(string[] args) {
    private static void Main(string[] args)
    {
      _ = args;
      var logFactory = new LoggerFactory();
      var logger = logFactory.CreateLogger("Program");

      //Build configuration:
      var configurationRoot = new ConfigurationBuilder()
        .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
        .AddJsonFile("appsettings.json", false, true)
        .Build();

      // Ensure only one running instance
      const string mutextName = "vJoyTouchPortalPlugin";
      _ = new Mutex(true, mutextName, out var createdNew);

      if (!createdNew) {
        logger.LogError($"{mutextName} is already running. Exiting application.");
        return;
      }

      try {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(configure =>
        {
          configure.ClearProviders();
          configure.AddSerilog(logger: new LoggerConfiguration().ReadFrom.Configuration(configurationRoot).CreateLogger(), dispose: true);
        });

        //Registering the Plugin to the IoC container:
        serviceCollection.AddTouchPortalSdk(configurationRoot);
        serviceCollection.AddSingleton<Plugin>();

        var serviceProvider = serviceCollection.BuildServiceProvider(true);

        //Use your IoC framework to resolve the plugin with it's dependencies:
        serviceProvider.GetRequiredService<Plugin>().Run();

      //CancellationTokenSource ctSource = new();
      //CancellationToken ct = ctSource.Token;
      //void ExitHandler() {
      //  // You can add any arbitrary global clean up
      //  logger.LogInformation("Cuught exit signal, exiting...");
      //  ctSource.Cancel();
      //}
      //AppDomain.CurrentDomain.ProcessExit += (sender, args) => ExitHandler();
      //Console.CancelKeyPress += (sender, args) => ExitHandler();

//#pragma warning disable CA1416 // Validate platform compatibility
        //await Host.CreateDefaultBuilder(args)
        //  .ConfigureLogging((_, loggingBuilder) => {
        //    loggingBuilder
        //      .ClearProviders()
        //      .AddSerilog(logger: new LoggerConfiguration().ReadFrom.Configuration(configurationRoot).CreateLogger(), dispose: true);
        //  })
        //  .ConfigureServices((_, services) => {
        //    services
        //      //.Configure<TJoyTouchPortalPlugin>((opt) => { })
        //      //.AddHostedService<PluginService>()
        //      .AddTouchPortalSdk(configurationRoot);
        //  })
        //  .RunConsoleAsync();
//#pragma warning restore CA1416 // Validate platform compatibility
      }
      catch (Exception ex) {
        logger.LogError($"COMException: {ex.Message}");
      }
    }
	}
}
