using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace D365POS.Services
{
    public class GetActiveProductPrices
    {
        private readonly AuthService _authService = new AuthService();
        private readonly HttpClient _client = new HttpClient();
        private static string Resource => Preferences.Get("Resource", null);

        private string _url => $"{Resource}/api/services/TBInventoryServices/TBPOSOperationService/getActiveProductPrices";
        public async Task<List<ActiveProductPricesResponse>?> GetActiveProductPricesAsync(string company, string storeId, CancellationToken token = default)
        {
            
            try
            {
                // Step 1: Get access token
                var accessToken = await _authService.GetAccessTokenAsync();

                // Step 2: Build payload
                var payload = new ActiveProductPricesRequest
                {
                    company = company,
                    storeId = storeId,
                };

                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, _url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromSeconds(15));

                var response = await _client.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cts.Token);

                return JsonSerializer.Deserialize<List<ActiveProductPricesResponse>>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return new List<ActiveProductPricesResponse>
                {
                    new ActiveProductPricesResponse { ItemId = "Error", UnitId = ex.Message}
                };
            }
        }

        // DTOs inside the same class
        public class ActiveProductPricesRequest
        {
            public string company { get; set; }
            public string storeId { get; set; }
        }
        public class ActiveProductPricesResponse
        {
            public string ItemId { get; set; }
            public string UnitId { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal PriceIncludeTax { get; set; }

        }
    }

}

