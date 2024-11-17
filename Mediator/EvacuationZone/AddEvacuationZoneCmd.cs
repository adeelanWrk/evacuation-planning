

using System.Text.Json;
using aspnetcore_redis_cache;
using Evacuation.Constant;
using Evacuation.DTO.EvacuationZones;
using Evacuation.DTO.JsonDataDTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Evacuation.Mediator.AddEvacuationZone;

public class AddEvacuationZoneCmd : IRequest<JsonDataDTO<List<EvacuationZonesDTO>>>
{
    public List<EvacuationZonesDTO> EvacuationZones { get; set; }
    public AddEvacuationZoneCmd(List<EvacuationZonesDTO> evacuationZones)
    {
        EvacuationZones = evacuationZones;
    }

    public class AddEvacuationZoneCmdHandler : IRequestHandler<AddEvacuationZoneCmd, JsonDataDTO<List<EvacuationZonesDTO>>>
    {
        private readonly IDistributedCache _cache;

        public AddEvacuationZoneCmdHandler(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<JsonDataDTO<List<EvacuationZonesDTO>>> Handle(AddEvacuationZoneCmd request, CancellationToken cancellationToken)
        {
            var newZones = request.EvacuationZones;

            var oldData = await _cache.GetObjectAsync<List<EvacuationZonesDTO>>(CacheKey.EVACUATION_ZONE);
            if (oldData != null)
            {
                var newZonesId = newZones.Select(x => x.ZoneId).ToList();

                var zonesToKeep = oldData.Where(x => !newZonesId.Contains(x.ZoneId)).ToList();

                newZones.AddRange(zonesToKeep);

                await _cache.RemoveAsync(CacheKey.EVACUATION_ZONE);
            }

            await _cache.SetObjectAsync(CacheKey.EVACUATION_ZONE, newZones);

            return new JsonDataDTO<List<EvacuationZonesDTO>>()
            {
                Data = newZones,
                Desc = "Evacuation Zone added successfully"
            };
        }
    }
}