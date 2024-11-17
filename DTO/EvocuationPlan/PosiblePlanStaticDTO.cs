namespace Evacuation.DTO.EvacuationPlan
{
    public class PosiblePlanStaticDTO
    {
        public int index { get; set; }
        public double ArriveInMinute { get; set; }
        public int DiffCapacityAndPeopleToEvacuate { get; set; }
        public int TotalVehicle { get; set; }
        public int? UrgencyLevel { get; set; }
    }
}