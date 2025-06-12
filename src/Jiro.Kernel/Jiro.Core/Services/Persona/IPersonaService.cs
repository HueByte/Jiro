namespace Jiro.Core.Services.Persona;

public interface IPersonaService
{
    Task AddSummaryAsync(string updateMessage);
    Task<string> GetPersonaAsync(ulong instanceId = 0);
}
