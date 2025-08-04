using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace PoliCare.Core.Entities;

public class Clinic : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string VatNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string Settings { get; set; } = "{}";

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    public virtual ICollection<Professional> Professionals { get; set; } = new List<Professional>();
}