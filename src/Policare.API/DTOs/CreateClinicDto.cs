namespace Policare.API.DTOs;

public class CreateClinicDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? VatNumber { get; set; }
}