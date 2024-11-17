namespace Evacuation.DTO.EvacuationPlan
{
    public class DraftEvacuationPlanDTO
    {
        public string? ZoneId { get; set; }
        public string? VehicleId { get; set; }
        public double ArriveInMinute { get; set; }
        public int PeopleToEvacuate { get; set; }
        public int VehicleCapacity { get; set; }
        public int UrgencyLevel { get; set; }
    }
}