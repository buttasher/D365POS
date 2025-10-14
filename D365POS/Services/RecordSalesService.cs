using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace D365POS.Services
{
    public class RecordSalesService
    {
        private readonly AuthService _authService = new AuthService();
        private readonly HttpClient _client = new HttpClient();

        private readonly string _url =
            "https://tbd365deve8cbf0eb94119fe1devaos.cloudax.uae.dynamics.com/api/services/TBInventoryServices/TBPOSOperationService/recordSales";

        public async Task<bool> RecordSalesAsync(string company, List<SaleItemDto> saleItems, CancellationToken token = default)
        {
            try
            {
                // 1️⃣ Get access token
                var accessToken = await _authService.GetAccessTokenAsync();

                // 2️⃣ Build payload
                var payload = new RecordSalesRequest
                {
                    company = company,
                    saleItems = saleItems
                };

                var json = JsonSerializer.Serialize(payload,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });

                // 3️⃣ Create request
                var request = new HttpRequestMessage(HttpMethod.Post, _url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // 4️⃣ Send request
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                var response = await _client.SendAsync(request, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // ---------------- DTOs ----------------

        public class RecordSalesRequest
        {
            public string company { get; set; }
            public List<SaleItemDto> saleItems { get; set; }
        }

        public class SaleItemDto
        {
            public string StoreId { get; set; }
            public DateOnly TransDate { get; set; }
            public string TerminalId { get; set; }
            public string StaffId { get; set; }
            public string ShiftId { get; set; }
            public string ReceiptId { get; set; }

            public List<PaymentDto> Payments { get; set; }
            public List<TaxDto> Taxes { get; set; }
            public List<ItemDto> Items { get; set; }
        }

        public class PaymentDto
        {
            public DateTime PaymentDateTime { get; set; }
            public string PaymentMethod { get; set; }
            public string PaymentType { get; set; }
            public string Currency { get; set; }
            public string PaymentAmount { get; set; }
        }

        public class TaxDto
        {
            public string TaxName { get; set; }
            public double TaxRate { get; set; }
            public string TaxValue { get; set; }
        }

        public class ItemDto
        {
            public string ItemId { get; set; }
            public string UnitId { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Qty { get; set; }
            public decimal LineAmount { get; set; }
            public decimal TaxAmount { get; set; }
            public int Action { get; set; }
            public DateTime ActionDateTime { get; set; }
        }
    }
}
