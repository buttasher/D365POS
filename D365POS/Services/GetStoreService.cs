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
            try
            {
                // Step 1: Get access token
                var token = await _authService.GetAccessTokenAsync();

                // Step 2: Build request
                var url = "https://aduat.sandbox.operations.uae.dynamics.com/api/services/TBGetWarehouseGroup/TBGetWarehouseService/getStore";
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

                if (!response.IsSuccessStatusCode)
                {
                    return new StoreResponse
                    {
                        Status = "Error",
                        Message = $"Request failed with status code {response.StatusCode}"
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();

                // Step 4: Deserialize
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var storeResponse = JsonSerializer.Deserialize<StoreResponse>(responseJson, options);


                return storeResponse;
            }
            catch (Exception ex)
            {
                return new StoreResponse
                {
                    Status = "Error",
                    Message = "Unexpected error occurred while getting store data."
                };
            }
        }

        public class StoreResponse
        {
            public string? Status { get; set; }
            public string? Message { get; set; }
            public List<string>? Warehouse { get; set; }
        }
    }
}
