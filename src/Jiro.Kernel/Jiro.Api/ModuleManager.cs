using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Jiro.ModularBase;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Serilog;

namespace Jiro.Api
{
    public class ModuleManager
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceCollection _services;
        private readonly ConcurrentBag<string> _modulePaths = new();
        private readonly List<Assembly> _assemblies = new();
        private readonly List<string> _moduleNames = new();

        public ModuleManager(IServiceCollection services, IConfiguration config)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveAssembly!;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly!;

            _services = services;
            _configuration = config;
        }

        /// <summary>
        /// Debug Builds modules included in appsettings
        /// </summary>
        public void BuildDevModules()
        {
            var modules = _configuration.GetSection("Modules").Get<string[]>();
            var debugPath = @"bin\Debug";

            if (modules is null) return;

            Log.Logger.Information($"Build {modules.Length} module(s)");

            Task[] buildTasks = new Task[modules.Length];
            for (int i = 0; i < buildTasks.Length; i++)
            {
                int localScope = i;
                buildTasks[i] = Task.Run(async () =>
                {
                    Log.Logger.Information($"{modules[localScope]} => build...");
                    var path = modules[localScope];

                    await RunBuildCommandAsync(path);

                    var completepath = Path.Combine(path, debugPath);
                    var outputFolder = Directory.GetDirectories(completepath, "net*").First();
                    Log.Logger.Information($"{modules[localScope]} => build done. Output folder: {outputFolder}");

                    _modulePaths.Add(outputFolder);
                });
            }

            Task.WaitAll(buildTasks);
        }

        /// <summary>
        /// Navigates to path and runs `dotnet build` command
        /// </summary>
        /// <param name="path">Path to navigate to</param>
        /// <returns></returns>
        private static async Task RunBuildCommandAsync(string path)
        {
            Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            await cmd.StandardInput.WriteLineAsync($"cd {path}");
            await cmd.StandardInput.WriteLineAsync("dotnet build");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            await cmd.WaitForExitAsync();

            Log.Logger.Information(cmd.StandardOutput.ReadToEnd());
        }

        public void LoadModuleAssemblies()
        {
            var modulePath = AppContext.BaseDirectory + "Modules";
            if (!Directory.Exists(modulePath))
                Directory.CreateDirectory(modulePath);

            List<string> paths = new() { modulePath };

            paths.AddRange(_modulePaths);

            List<string> dllFiles = new();
            foreach (var path in paths)
                dllFiles.AddRange(GetDllFiles(path).ToList());

            var folders = Directory.GetDirectories(modulePath);

            dllFiles.AddRange(folders.Select(folder => GetDllFiles(folder)).SelectMany(e => e));

            foreach (var dll in dllFiles)
            {
                Log.Logger.Information($"Loading {dll}");

                _assemblies.Add(Assembly.LoadFile(dll));
            }
        }
        /// <summary>
        /// Loads controllers from loaded modular assemblies
        /// </summary>
        public void LoadModuleControllers()
        {
            try
            {
                _services
                    .AddControllers()
                    .ConfigureApplicationPartManager((manager) =>
                    {
                        foreach (var asm in _assemblies)
                            manager.ApplicationParts.Add(new AssemblyPart(asm));

                    })
                    .AddControllersAsServices();
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new();
                foreach (var exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub!.Message);
                    if (exSub is FileNotFoundException exFileNotFound)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }

                string errorMessage = sb.ToString();
                Log.Logger.Error(errorMessage);
            }
        }

        /// <summary>
        /// Runs `IServiceConfigurator` of loaded modular assemblies
        /// </summary>
        public void RegisterModuleServices()
        {
            try
            {
                var serviceConfigurators = _assemblies
                    .SelectMany(e => e.GetTypes())
                        .Where(type => !type.IsInterface && typeof(IServiceConfigurator).IsAssignableFrom(type));

                foreach (var serviceConfigurator in serviceConfigurators)
                {
                    if (Activator.CreateInstance(serviceConfigurator) is not IServiceConfigurator configurator) continue;

                    Log.Logger.Information($"Running {configurator.ConfiguratorName} service configurator");

                    configurator.RegisterServices(_services);
                    _moduleNames.Add(configurator.ConfiguratorName);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new();
                foreach (var exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub!.Message);
                    if (exSub is FileNotFoundException exFileNotFound)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }

                string errorMessage = sb.ToString();
                Log.Logger.Error(errorMessage);
            }
        }

        /// <summary>
        /// Validates modules based on `IServiceConfigurator.ConfiguratorName` and appsettings RequiredModules 
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void ValidateModules()
        {
            var lackingModules = _configuration.GetSection("RequiredModules").Get<string[]>()
                ?.Where(m => !_moduleNames.Contains(m));

            if (lackingModules is not null && lackingModules.Any())
                throw new Exception($"Couldn't find {string.Join(',', lackingModules)} module");
        }

        /// <summary>
        /// Assembly resolver
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Assembly? ResolveAssembly(Object sender, ResolveEventArgs e)
        {
            Assembly? res = _assemblies.FirstOrDefault(asm => asm.FullName == e.Name);

            if (res is null)
            {
                var args = e.Name.Split(',');
                res = _assemblies.FirstOrDefault(asm => asm.FullName!.Contains(args[0]));

                // if (res is null)
                // Log.Logger.Warning("Asm: {reqAsm} couldn't find {dep} dependency", e.RequestingAssembly, e.Name);
            }

            return res;
        }

        /// <summary>
        /// Gets DLL files based on `*.dll` pattern
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string[] GetDllFiles(string path) => Directory.GetFiles(path, "*.dll");
    }
}