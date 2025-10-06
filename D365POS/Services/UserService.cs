using SISWindowsApp.Services;
using System.Text;
using System.Text.Json;

public class UserService
{
    private readonly AuthService _authService = new AuthService();
    private readonly HttpClient _client = new HttpClient();

    public async Task<SignInResponse> SignInAsync(string userId, string password, CancellationToken token = default)
    {
        // Step 1: Get access token
        var accessToken = await _authService.GetAccessTokenAsync();

        // Step 2: Build request
        var url = "https://tbd365deve8cbf0eb94119fe1devaos.cloudax.uae.dynamics.com/api/services/TBGetGlobalUserGroup/TBGetGlobalUserService/getUser";
        var payload = new
        {
            _userId = userId,
            _passwordHash = password
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Step 3: Send request with timeout via CancellationToken
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 seconds timeout

            var response = await _client.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cts.Token);

            return JsonSerializer.Deserialize<SignInResponse>(responseJson);
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation occurred
            return new SignInResponse
            {
                Status = "Error",
                Message = "Server is not responding. Please try again."
            };
        }
        catch (Exception ex)
        {
            // Other errors
            return new SignInResponse
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public class SignInResponse
    {
        public List<string>? CompanyList { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
    }
}