using Microsoft.Extensions.DependencyInjection;

namespace Jiro.ModularBase
{
    public interface IServiceConfigurator
    {
        string ConfiguratorName { get; }
        void RegisterServices(IServiceCollection services);
    }
}