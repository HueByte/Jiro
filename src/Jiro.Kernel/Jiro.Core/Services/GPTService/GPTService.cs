using Jiro.Core.Attributes;
using Jiro.Core.Interfaces.IServices;

namespace Jiro.Core.Services.GPTService
{
    public class GPTService : IGPTService
    {

        public GPTService()
        {

        }

        public async Task ChatAsync()
        {
            await Task.Delay(1000);
        }
    }
}