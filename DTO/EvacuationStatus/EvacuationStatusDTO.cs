namespace Evacuation.DTO.EvacuationStatus
{
    public class EvacuationStatusDTO
    {
        public string? ZoneId { get; set; }
        public string? LastVehicleId { get; set; }
        public int PeopleToEvacuate { get; set; }
        public int PeopleToEvacuated { get; set; }
        public string? Status { get; set; }
        public DateTime? LastTimeToCheck { get; set; }
    }
}