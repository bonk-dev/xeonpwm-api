namespace XeonPwm.Data.Payloads.ToApi;

public record SetDtCycleRequest
{
    public required int DutyCycle { get; set; } 
}