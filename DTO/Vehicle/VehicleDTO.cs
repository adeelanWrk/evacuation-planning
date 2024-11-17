using Evacuation.DTO.EvacuationZones;
using Evacuation.DTO.LocationCoordinates;

namespace Evacuation.DTO.Vehicle
{
    public class VehicleDTO
    {
        public string? VehicleId { get; set; }
        public int Capacity { get; set; }
        public string? Type { get; set; }
        public LocationCoordinatesDTO? LocationCoordinates { get; set; }
        public double Speed { get; set; }
    }

}
