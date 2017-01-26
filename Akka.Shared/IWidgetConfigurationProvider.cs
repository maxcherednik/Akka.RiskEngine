using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akka.Shared
{
    public interface IWidgetConfigurationProvider
    {
        Task<List<long>> GetAllAsync();
    }
}
