namespace Evacuation.DTO.EvacuationPlan
{
    public class EvacuationPlanDTO
    {
        public string? ZoneId { get; set; }
        public string? VehicleId { get; set; }
        public DateTime EstimatedTimeOfArrival { get; set; }
        public int PeopleToEvacuate { get; set; }
    }
}