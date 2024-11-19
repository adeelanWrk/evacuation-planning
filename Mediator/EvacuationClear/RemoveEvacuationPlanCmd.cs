

using System.Text.Json;
using aspnetcore_redis_cache;
using Evacuation.Constant;
using Evacuation.DTO.EvacuationZones;
using Evacuation.DTO.JsonDataDTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Evacuation.Mediator.AddEvacuationZone;

public class RemoveEvacuationPlanCmd : IRequest<JsonDataDTO<object>>
{
    public RemoveEvacuationPlanCmd()
    {
    }

    public class RemoveEvacuationPlanCmdHandler : IRequestHandler<RemoveEvacuationPlanCmd, JsonDataDTO<object>>
    {
        private readonly IDistributedCache _cache;

        public RemoveEvacuationPlanCmdHandler(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<JsonDataDTO<object>> Handle(RemoveEvacuationPlanCmd request, CancellationToken cancellationToken)
        {

            await _cache.RemoveAsync(CacheKey.EVACUATION_PLAN);
            await _cache.RemoveAsync(CacheKey.EVACUATION_STATUS);

            return new JsonDataDTO<object>()
            {
                Data = null,
                Desc = "Clear data successfully"
            };
        }
    }
}