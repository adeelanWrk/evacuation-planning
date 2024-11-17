

using aspnetcore_redis_cache;
using Evacuation.Constant;
using Evacuation.DTO.EvacuationStatus;
using Evacuation.DTO.JsonDataDTO;
using Evacuation.DTO.Vehicle;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Evacuation.Mediator.EvacuationPlan;

public class GetEvacuationStatusQuery : IRequest<JsonDataDTO<List<EvacuationStatusDTO>>>
{

    public class GetEvacuationStatusQueryHandler : IRequestHandler<GetEvacuationStatusQuery, JsonDataDTO<List<EvacuationStatusDTO>>>
    {
        private readonly IDistributedCache _cache;

        public GetEvacuationStatusQueryHandler(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<JsonDataDTO<List<EvacuationStatusDTO>>> Handle(GetEvacuationStatusQuery request, CancellationToken cancellationToken)
        {
            var evacuationsZones = await _cache.GetObjectAsync<List<EvacuationStatusDTO>>(CacheKey.EVACUATION_STATUS);
      
            return new JsonDataDTO<List<EvacuationStatusDTO>>()
            {
                Data = evacuationsZones,
                Desc = "Evacuation Plan Successfully Retrieved",
            };
        }
        // private static async Task CheckStatus(List<EvacuationStatusDTO> plan, IDistributedCache _cache)
        // {
        //     var newData = plan;
        //     var oldData = await _cache.GetObjectAsync<List<EvacuationStatusDTO>>(CacheKey.EVACUATION_PLAN);
        //     if (oldData != null)
        //     {
        //         var newDataId = newData.Select(x => x.ZoneId).ToList();

        //         var dataKeepToKeep = oldData.Where(x => !newDataId.Contains(x.VehicleId)).ToList();

        //         newData.AddRange(dataKeepToKeep);

        //         await _cache.RemoveAsync(CacheKey.EVACUATION_PLAN);
        //     }
        //     var newZones = new List<EvacuationStatusDTO>();
        //     await _cache.SetObjectAsync(CacheKey.EVACUATION_ZONE, newZones);
        // }
    }
}