using System.ComponentModel.DataAnnotations;

namespace ZDLISPlus.Web.Models;

public class Patient
{
    public int Id { get; set; }

    [Required, Display(Name = "MRN")]
    public string MedicalRecordNumber { get; set; } = "";

    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required]
    public string Gender { get; set; } = "F";

    [Range(0, 130)]
    public int Age { get; set; }

    [DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-30);

    public string? Phone { get; set; }
    public string? Address { get; set; }

    public List<LabOrder> Orders { get; set; } = new();

    public string Initials =>
        string.Concat(FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(p => char.ToUpper(p[0])));
}