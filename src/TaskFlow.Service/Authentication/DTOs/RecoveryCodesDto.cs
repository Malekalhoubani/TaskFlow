namespace TaskFlow.Service.Authentication.DTOs;

public class RecoveryCodesDto
{
    public IEnumerable<string> Codes { get; set; } = [];
}
