using System;
using System.Collections.Generic;

namespace Q.WebAPI.Models.DbData;

public partial class Appointment
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public int? Duration { get; set; }

    public string? AdditionalInfo { get; set; }

    public int UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
