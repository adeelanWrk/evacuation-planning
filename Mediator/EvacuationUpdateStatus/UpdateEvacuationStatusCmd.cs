

using System.Text;
using System.Text.Json;
using aspnetcore_redis_cache;
using Evacuation.Constant;
using Evacuation.DTO.EvacuationStatus;
using Evacuation.DTO.EvacuationUpdate;
using Evacuation.DTO.EvacuationZones;
using Evacuation.DTO.JsonDataDTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Evacuation.Mediator.EvacuationUpateStatus;

public class UpdateEvacuationStatusCmd : IRequest<JsonDataDTO<object>>
{
    public List<EvacuationUpdateDTO> EvacuationUpdate { get; set; }
    public UpdateEvacuationStatusCmd(List<EvacuationUpdateDTO> evacuationUpdate)
    {
        EvacuationUpdate = evacuationUpdate;
    }

    public class UpdateEvacuationStatusCmdHandler : IRequestHandler<UpdateEvacuationStatusCmd, JsonDataDTO<object>>
    {
        private readonly IDistributedCache _cache;

        public UpdateEvacuationStatusCmdHandler(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<JsonDataDTO<object>> Handle(UpdateEvacuationStatusCmd request, CancellationToken cancellationToken)
        {
            var errorMessage = await ValidateEvacuationUpdate(request.EvacuationUpdate, _cache);
            if (errorMessage != string.Empty)
                return new JsonDataDTO<object>()
                { IsError = true, ErrorMessage = errorMessage };

            await _cache.SetObjectAsync(CacheKey.EVACUATION_UPDATE_LOG, request.EvacuationUpdate);

            await EvacuationUpdate(request.EvacuationUpdate, _cache);

            return new JsonDataDTO<object>()
            {
                Desc = "Updated Evacuation successfully"
            };
        }
        private static async Task<string> ValidateEvacuationUpdate(List<EvacuationUpdateDTO> updateEvacauationStatus, IDistributedCache _cache)
        {
            var errors = new StringBuilder();

            var evacuationStatuses = await _cache.GetObjectAsync<List<EvacuationStatusDTO>>(CacheKey.EVACUATION_STATUS);

            foreach (var item in updateEvacauationStatus)
            {
                if (string.IsNullOrEmpty(item.ZoneId))
                {
                    errors.AppendLine("ZoneId is required at Row: {updateEvacauationStatus.IndexOf(item)} <br>");
                }
                if (string.IsNullOrEmpty(item.VehicleId))
                {
                    errors.AppendLine($"VehicleId is required at Row: {updateEvacauationStatus.IndexOf(item)}<br>");
                }
                if (item.PeopleToEvacuated <= 0)
                {
                    errors.AppendLine($"PeopleToEvacuated is required at Row: {updateEvacauationStatus.IndexOf(item)}<br>");
                }
                if (evacuationStatuses.Any(x => x.ZoneId == item.ZoneId) == false)
                {
                    errors.AppendLine($"ZoneId {item.ZoneId} is not found in Evacuation Status<br>");
                }
            }
            return errors.ToString();
        }
        private static async Task EvacuationUpdate(List<EvacuationUpdateDTO> updateEvacauationStatus, IDistributedCache _cache)
        {
            var dateNow = DateTime.Now;

            var evacuationZones = await _cache.GetObjectAsync<List<EvacuationZonesDTO>>(CacheKey.EVACUATION_ZONE);

            var evacuationStatuses = await _cache.GetObjectAsync<List<EvacuationStatusDTO>>(CacheKey.EVACUATION_STATUS);

            var findEvacuationStatus = updateEvacauationStatus.GroupBy(x => x.ZoneId).ToList();

            foreach (var item in findEvacuationStatus)
            {
                var peopleToEvacuate = evacuationZones.FirstOrDefault(x => x.ZoneId == item.Key)?.NumberOfPeople ?? 0;
                var evacuationStatus = evacuationStatuses.FirstOrDefault(x => x.ZoneId == item.Key);
                var PeopleToEvacuated = item.Sum(x => x.PeopleToEvacuated);

                if (evacuationStatus == null)
                {
                    evacuationStatuses.Add(new EvacuationStatusDTO()
                    {
                        ZoneId = item.Key,
                        LastVehicleId = item.Last().VehicleId,
                        PeopleToEvacuate = peopleToEvacuate,
                        PeopleToEvacuated = PeopleToEvacuated,
                        LastTimeToCheck = dateNow,
                    });
                    continue;
                }
                evacuationStatus.LastVehicleId = item.Last().VehicleId;
                evacuationStatus.PeopleToEvacuated = PeopleToEvacuated;
                evacuationStatus.LastTimeToCheck = dateNow;
                evacuationStatus.Status = PeopleToEvacuated == peopleToEvacuate ? "Completed" : "In Progress";
            }
            await _cache.SetObjectAsync(CacheKey.EVACUATION_STATUS, evacuationStatuses);
        }
    }
}