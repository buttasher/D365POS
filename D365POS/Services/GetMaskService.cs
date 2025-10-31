using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace D365POS.Services
{
    public class GetMasksService
    {
        private readonly AuthService _authService = new AuthService();
        private readonly HttpClient _client = new HttpClient();
        private static string Resource => Preferences.Get("Resource", null);
        private string _url => $"{Resource}/api/services/TBInventoryServices/TBPOSOperationService/getProductBarcodes";
        public async Task<List<MasksRequestResponse>?> GetMasksServiceAsync(string company, CancellationToken token = default)
        {

            try
            {
                // Step 1: Get access token
                var accessToken = await _authService.GetAccessTokenAsync();

                // Step 2: Build payload
                var payload = new MasksRequest
                {
                    company = company,
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

                return JsonSerializer.Deserialize<List<MasksRequestResponse>>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return new List<MasksRequestResponse>
                {
                    new MasksRequestResponse { MaskId = "Error", Description = ex.Message, Mask = "N/A" }
                };
            }
        }

        // DTOs inside the same class
        public class MasksRequest
        {
            public string company { get; set; }
        }
        public class MasksRequestResponse
        {
            public string MaskId { get; set; }
            public string Description { get; set; }
            public string Mask { get; set; }
            public string Prefix { get; set; }
            public int Length { get; set; }
            public List<BarcodeSegmentsDto> barcodeSegments { get; set; }

        }
        public class BarcodeSegmentsDto
        {
            public int SegmentNum { get; set; }
            public string Type { get; set; }
            public int Length { get; set; }
            public string Char { get; set; }
            public decimal Decimals { get; set; }
        }
    }
}
