using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace D365POS.Services
{
    public class GetActiveProductsService
    {
        private readonly AuthService _authService = new AuthService();
        private readonly HttpClient _client = new HttpClient();

        private static string Resource => Preferences.Get("Resource", null);
        private string _url => $"{Resource}/api/services/TBInventoryServices/TBPOSOperationService/getActiveProducts";
        public async Task<List<ActiveProductsResponse>?> GetActiveProductsAsync(string company, string storeId, CancellationToken token = default)
        {
          
            try
            {
                // Step 1: Get access token
                var accessToken = await _authService.GetAccessTokenAsync();

                // Step 2: Build payload
                var payload = new ActiveProductsRequest
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

                return JsonSerializer.Deserialize<List<ActiveProductsResponse>>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return new List<ActiveProductsResponse>
                {
                    new ActiveProductsResponse { ItemId = "Error", Description = ex.Message, ItemBarcode = "N/A" }
                };
            }
        }

        // DTOs inside the same class
        public class ActiveProductsRequest
        {
            public string company { get; set; }
            public string storeId { get; set; }
        }
        public class ActiveProductsResponse
        {
            public string ItemId { get; set; }
            public string Description { get; set; }
            public string DescriptionAr { get; set; }
            public string UnitId { get; set; }
            public string PLUCode { get; set; }
            public string ItemBarcode { get; set; }
            public string SalesTaxGroup { get; set; }
            public string ItemSalesTaxGroup { get; set; }
            public decimal TaxFactor { get; set; }

        }
    }

}
