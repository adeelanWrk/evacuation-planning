public class Vehicle
{
    public string? VehicleId { get; set; }
    public int Capacity { get; set; }
    public string? Type { get; set; }
    public (double Latitude, double Longitude) Coordinates { get; set; }
    public double Speed { get; set; }
}
