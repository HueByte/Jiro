using Jiro.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Services.Instance
{
    public class CurrentInstanceService : ICurrentInstanceService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public JiroInstance? CurrentInstance { get; private set; }

        public CurrentInstanceService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task SetCurrentInstance()
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var _jiroInstanceService = scope.ServiceProvider.GetRequiredService<IJiroInstanceService>();

            var instance = await _jiroInstanceService.GetJiroInstanceAsync();
            CurrentInstance = instance;
        }

        public void SetCurrentInstance(JiroInstance jiroInstance)
        {
            CurrentInstance = jiroInstance;
        }

        public bool IsConfigured() => CurrentInstance?.IsConfigured ?? false;
    }
}