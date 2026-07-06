namespace backend.DTOs;

public class GeometryResponse
{
    public int Id { get; set; }

    public string Type { get; set; } = "";

    public string Endpoint { get; set; } = "";

    public string Name { get; set; } = "";

    public string Color { get; set; } = "";

    public DateTime ModifiedDate { get; set; }

    public string Wkt { get; set; } = "";
}
