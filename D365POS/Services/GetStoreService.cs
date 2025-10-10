using System.Text;
using System.Text.Json;

namespace D365POS.Services
{
    public class GetStoreService
    {
        private readonly AuthService _authService = new AuthService();
        private readonly HttpClient _client = new HttpClient();
        public async Task<StoreResponse?> GetStoreAsync(string userId, string company)
        {
            // Step 1: Get access token
            var token = await _authService.GetAccessTokenAsync();

            // Step 2: Build request
            var url = "https://tbd365deve8cbf0eb94119fe1devaos.cloudax.uae.dynamics.com/api/services/TBGetWarehouseGroup/TBGetWarehouseService/getStore";
            var payload = new
            {
                _userId = userId,
                _company = company
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Step 3: Send request
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            // Step 4: Deserialize
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<StoreResponse>(responseJson, options);
        }
        public class StoreResponse
        {
            public string? Status { get; set; }
            public string? Message { get; set; }
            public List<string>? Warehouse { get; set; }
        }

    }
}


