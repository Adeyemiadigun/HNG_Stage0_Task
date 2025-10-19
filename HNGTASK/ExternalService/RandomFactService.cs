using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HNGTASK.ResponseModels;

namespace HNGTASK.ExternalService
{
    public class RandomFactService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RandomFactService> _logger;

        public RandomFactService(HttpClient httpClient, ILogger<RandomFactService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

       public async Task<string> GetRandomFact()
        {
            try
            {
                _logger.LogInformation("Requesting random cat fact from catfact.ninja");
                var request = new HttpRequestMessage(HttpMethod.Get, "https://catfact.ninja/fact?max_length=200");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<CatFactResponse>();
                var fact = result?.Fact ?? "No fact available";
                _logger.LogInformation("Received cat fact (len={Length})", result?.Length ?? 0);
                return fact;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching cat fact");
                return "Unable to reach the cat facts service. Please try again later.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Request for cat fact timed out");
                return "The request timed out. Please try again shortly.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching cat fact");
                return "Something went wrong while fetching a cat fact. Please try again later.";
            }
        }
    }
}