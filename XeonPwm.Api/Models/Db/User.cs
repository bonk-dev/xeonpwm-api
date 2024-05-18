using System.ComponentModel.DataAnnotations;

namespace XeonPwm.Api.Models.Db;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [MaxLength(30)]
    public required string Username { get; set; }
    
    public required string Hash { get; set; }
}