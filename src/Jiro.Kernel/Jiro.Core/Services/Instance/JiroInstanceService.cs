using System.Text.Json;
using System.Text.Json.Serialization;
using Jiro.Core.DTO;
using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.Instance
{
    public class JiroInstanceService : IJiroInstanceService
    {
        private readonly ILogger _logger;
        private readonly IJiroInstanceRepository _jiroInstanceRepository;
        private readonly ICurrentInstanceService _currentInstanceService;
        private readonly IConfiguration _configuration;
        public JiroInstanceService(ILogger<JiroInstanceService> logger, IJiroInstanceRepository jiroInstanceRepository, ICurrentInstanceService currentInstanceService, IConfiguration configuration)
        {
            _logger = logger;
            _jiroInstanceRepository = jiroInstanceRepository;
            _currentInstanceService = currentInstanceService;
            _configuration = configuration;
        }

        public async Task<JiroInstance?> GetJiroInstanceAsync()
        {
            return await _jiroInstanceRepository.AsQueryable()
                .FirstOrDefaultAsync(instance => instance.IsActive);
        }

        public async Task<InstanceConfigDTO> GetConfigAsync()
        {
            var jiroConfig = await GetConfigAsyncInternal();

            if (jiroConfig is null)
                return new InstanceConfigDTO();

            return jiroConfig;
        }

        public async Task ConfigureAsync(InstanceConfigDTO instanceConfig)
        {
            // TODO
            // validation here...

            _logger.LogInformation("Fetching current Jiro instance...");
            var jiroInstance = await _jiroInstanceRepository.AsQueryable()
                .FirstOrDefaultAsync(instance => instance.IsActive);

            if (jiroInstance is null)
            {
                _logger.LogError("No active Jiro instance found! Attempting to create new instance...");

                jiroInstance = new JiroInstance()
                {
                    InstanceName = "Default Jiro Instance",
                    IsActive = true,
                    IsConfigured = false
                };

                await _jiroInstanceRepository.AddAsync(jiroInstance);
                await _jiroInstanceRepository.SaveChangesAsync();

                _logger.LogInformation("New Jiro instance created.");
            }

            jiroInstance.IsConfigured = true;
            await _jiroInstanceRepository.UpdateAsync(jiroInstance);

            var otherJiroInstancesQuery = await _jiroInstanceRepository.AsQueryable()
                .Where(instance => instance.Id != jiroInstance.Id)
                .ToListAsync();

            otherJiroInstancesQuery.ForEach(e => e.IsActive = false);

            await _jiroInstanceRepository.UpdateRange(otherJiroInstancesQuery);
            await _jiroInstanceRepository.SaveChangesAsync();
            _currentInstanceService.SetCurrentInstance(jiroInstance);
            await WriteConfigAsyncInternal(instanceConfig);

            _logger.LogInformation("Jiro instance configuration complete. Using {InstanceName} as current instance.", jiroInstance.InstanceName);
        }

        public async Task CreateJiroInstanceAsync(JiroInstance jiroInstance)
        {
            await _jiroInstanceRepository.AddAsync(jiroInstance);
            await _jiroInstanceRepository.SaveChangesAsync();
        }

        private async Task WriteConfigAsyncInternal(InstanceConfigDTO instanceConfig)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
            };

            var json = JsonSerializer.Serialize(instanceConfig, options);
            await File.WriteAllTextAsync(GetConfigPath(), json);
        }

        private async Task<InstanceConfigDTO?> GetConfigAsyncInternal()
        {
            var json = await File.ReadAllBytesAsync(GetConfigPath());

            return JsonSerializer.Deserialize<InstanceConfigDTO>(json);
        }

        private string GetConfigPath()
        {
            var path = _configuration.GetValue<string>("CONFIG_PATH") ?? Path.Join(AppContext.BaseDirectory, "appsettings.json");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new JiroException("No configuration file found!");

            _logger.LogInformation("Found configuration file at {Path}", path);

            return path;
        }
    }
}