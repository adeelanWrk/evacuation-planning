namespace Evacuation.DTO.EvacuationUpdate
{
    public class EvacuationUpdateDTO
    {
        public string? ZoneId { get; set; }
        public string? VehicleId { get; set; }
        public int PeopleToEvacuated { get; set; }
        public DateTime? TimeToEvacuated { get; set; }
    }
}