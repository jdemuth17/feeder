using System.Threading.Tasks;

namespace UniversalFeeder.Server.Services
{
    public interface IFeederClient
    {
        Task<bool> TriggerFeedAsync(string identifier, int durationMs);
        Task<bool> TriggerChimeAsync(string identifier, float volume);
    }
}
