

using aspnetcore_redis_cache;
using Evacuation.Constant;
using Evacuation.DTO.JsonDataDTO;
using Evacuation.DTO.Vehicle;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Evacuation.Mediator.Vehicle;

public class AddVehicleCmd : IRequest<JsonDataDTO<List<VehicleDTO>>>
{
    public List<VehicleDTO> Vehicles { get; set; }
    public AddVehicleCmd(List<VehicleDTO> vehicles)
    {
        Vehicles = vehicles;
    }

    public class AddVehicleCmdHandler : IRequestHandler<AddVehicleCmd, JsonDataDTO<List<VehicleDTO>>>
    {
        private readonly IDistributedCache _cache;

        public AddVehicleCmdHandler(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<JsonDataDTO<List<VehicleDTO>>> Handle(AddVehicleCmd request, CancellationToken cancellationToken)
        {
            var newVehicles = request.Vehicles;

            var oldData = await _cache.GetObjectAsync<List<VehicleDTO>>(CacheKey.VEHICLE);
            if (oldData != null)
            {
                var newVehiclesId = newVehicles.Select(x => x.VehicleId).ToList();

                var vehiclesToKeep = oldData.Where(x => !newVehiclesId.Contains(x.VehicleId)).ToList();

                newVehicles.AddRange(vehiclesToKeep);

                await _cache.RemoveAsync(CacheKey.VEHICLE);
            }

            await _cache.SetObjectAsync(CacheKey.VEHICLE, newVehicles);

            return new JsonDataDTO<List<VehicleDTO>>()
            {
                Data = newVehicles,
                Desc = "Evacuation Zone added successfully"
            };
        }
    }
}