using Evacuation.DTO.LocationCoordinates;

namespace Evacuation.DTO.EvacuationZones
{
    public class EvacuationZonesDTO
    {
        public string? ZoneId { get; set; }
        public LocationCoordinatesDTO? LocationCoordinates { get; set; }
        public int NumberOfPeople { get; set; }
        public int UrgencyLevel { get; set; }
    }

}
