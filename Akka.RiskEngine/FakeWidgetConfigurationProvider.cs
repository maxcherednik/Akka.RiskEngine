using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Shared;

namespace Akka.RiskEngine
{
    class FakeWidgetConfigurationProvider: IWidgetConfigurationProvider
    {
        public Task<List<long>> GetAllAsync()
        {
            return Task.FromResult(new List<long> {0, 1, 2, 3, 4, 5, 6, 7, 8, 9});
        }
    }
}
