using Jiro.Core.DTO;
using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices
{
    public interface IJiroInstanceService
    {
        Task<InstanceConfigDTO> GetConfigAsync();
        Task ConfigureAsync(InstanceConfigDTO instanceConfig);
        Task CreateJiroInstanceAsync(JiroInstance jiroInstance);
        Task<JiroInstance?> GetJiroInstanceAsync();
    }
}