using System.ComponentModel.DataAnnotations;

namespace ImageShare.Authentication;

public class ApiKeyEntry
{
    [Required]
    public string Key { get; set; } = "";

    [Required]
    public string Filter { get; set; } = "";

    public bool IsAdmin { get; set; }
}
