using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace D365POS.Services
{
    public class AuthService
    {
        private static readonly string TenantId = "1d30208b-6f34-41ab-be26-f8b037cded0c";
        private static readonly string ClientId = "329ddef5-e60b-444b-b252-bafc7329bb42";
        private static readonly string ClientSecret = "gwE8Q~dA3kTSwD1LSECijJFOuBMuyyoPAH8a_cPr"; 
        private static readonly string Resource = "https://tbd365deve8cbf0eb94119fe1devaos.cloudax.uae.dynamics.com";

        private static DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
        private static string? _cachedToken;

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiry)
                    return _cachedToken;

                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"https://login.windows.net/{TenantId}/oauth2/token");

                var formData = new Dictionary<string, string>
                {
                    {"client_id", ClientId},
                    {"client_secret", ClientSecret},
                    {"grant_type", "client_credentials"},
                    {"resource", Resource}
                };

                request.Content = new FormUrlEncodedContent(formData);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Token request failed ({response.StatusCode}): {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

                if (tokenResponse != null)
                {
                    _cachedToken = tokenResponse.access_token;

                    if (int.TryParse(tokenResponse.expires_in, out var seconds))
                        _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(seconds - 60);

                    return _cachedToken;
                }

                throw new Exception("Failed to parse token response");
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP problem
                throw new Exception("Network error while getting access token", ex);
            }
            catch (Exception ex)
            {
                // General problem
                throw new Exception("Unexpected error while getting access token", ex);
            }
        }

    }

    public class TokenResponse
    {
        public string? token_type { get; set; }
        public string? access_token { get; set; }
        public string? expires_in { get; set; }
    }
}
