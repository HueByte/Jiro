using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices
{
    public interface ICurrentInstanceService
    {
        JiroInstance? CurrentInstance { get; }
        Task SetCurrentInstance();
        void SetCurrentInstance(JiroInstance jiroInstance);
        bool IsConfigured();
    }
}