using System.Net.Http;
using System.Threading.Tasks;

namespace UniversalFeeder.Server.Services
{
    public interface IFeederClient
    {
        Task<bool> TriggerFeedAsync(string ipAddress, int durationMs);
        Task<bool> TriggerChimeAsync(string ipAddress, float volume);
    }

    public class FeederClient : IFeederClient
    {
        private readonly HttpClient _httpClient;

        public FeederClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> TriggerFeedAsync(string ipAddress, int durationMs)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://{ipAddress}/feed?ms={durationMs}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TriggerChimeAsync(string ipAddress, float volume)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://{ipAddress}/chime?vol={volume}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
