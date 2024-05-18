using System.ComponentModel.DataAnnotations;

namespace XeonPwm.Api.Models.Db;

public class AuthToken
{
    [Key]
    public int Id { get; set; }
    public required int UserId { get; set; }
    public required string Token { get; set; }
    public required DateTime ExpirationDate { get; set; }
    public required bool IsForHub { get; set; }
    
    public virtual User User { get; set; }
}