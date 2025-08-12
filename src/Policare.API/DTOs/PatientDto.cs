﻿namespace Policare.API.DTO

{

    public class PatientDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string FiscalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePatientDto
    {
        public Guid ClinicId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string FiscalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class UpdatePatientDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string FiscalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
