public class EvacuationZone
{
    public string? ZoneId { get; set; }
    public (double Latitude, double Longitude) Coordinates { get; set; }
    public int NumberOfPeople { get; set; }
    public int UrgencyLevel { get; set; }
}
