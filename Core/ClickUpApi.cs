using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

public enum ApiVersion { V2, V3 }

public class ClickUpApi
{
    private readonly HttpClient _httpClient;
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<ClickUpApi> _logger;
    
    private const string BaseUrl = "https://api.clickup.com/";

    public ClickUpApi(ITokenProvider tokenProvider, ILogger<ClickUpApi> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    private static string GetVersionPath(ApiVersion version) => version switch
    {
        ApiVersion.V2 => "api/v2",
        ApiVersion.V3 => "api/v3",
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };

    private static bool IsAbsoluteUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUrl(string endpoint, ApiVersion version)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));
        }
        
        // Absolute URLs pass through unchanged
        if (IsAbsoluteUrl(endpoint))
        {
            return endpoint;
        }
        
        var path = endpoint.TrimStart('/');
        return $"{GetVersionPath(version)}/{path}";
    }

    private void SetAuthHeader(HttpRequestMessage request)
    {
        var token = _tokenProvider.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.TryAddWithoutValidation("Authorization", token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint, ApiVersion version = ApiVersion.V2)
    {
        var url = BuildUrl(endpoint, version);
        var isAbsolute = IsAbsoluteUrl(url);
        
        _logger.LogDebug("GET {Url} (IsAbsolute: {IsAbsolute})", url, isAbsolute);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        SetAuthHeader(request);
        
        var response = await _httpClient.SendAsync(request);
        LogResponse("GET", url, response);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, ApiVersion version = ApiVersion.V2)
    {
        var url = BuildUrl(endpoint, version);
        var isAbsolute = IsAbsoluteUrl(url);
        
        _logger.LogDebug("POST {Url} (IsAbsolute: {IsAbsolute})", url, isAbsolute);
        
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data)
        };
        SetAuthHeader(request);
        
        var response = await _httpClient.SendAsync(request);
        LogResponse("POST", url, response);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, ApiVersion version = ApiVersion.V2)
    {
        var url = BuildUrl(endpoint, version);
        var isAbsolute = IsAbsoluteUrl(url);
        
        _logger.LogDebug("PUT {Url} (IsAbsolute: {IsAbsolute})", url, isAbsolute);
        
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(data)
        };
        SetAuthHeader(request);
        
        var response = await _httpClient.SendAsync(request);
        LogResponse("PUT", url, response);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task DeleteAsync(string endpoint, ApiVersion version = ApiVersion.V2)
    {
        var url = BuildUrl(endpoint, version);
        var isAbsolute = IsAbsoluteUrl(url);
        
        _logger.LogDebug("DELETE {Url} (IsAbsolute: {IsAbsolute})", url, isAbsolute);
        
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        SetAuthHeader(request);
        
        var response = await _httpClient.SendAsync(request);
        LogResponse("DELETE", url, response);
        response.EnsureSuccessStatusCode();
    }

    private void LogResponse(string method, string url, HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("{Method} {Url} -> {StatusCode}", method, url, (int)response.StatusCode);
        }
        else
        {
            _logger.LogError("{Method} {Url} -> {StatusCode}", method, url, (int)response.StatusCode);
        }
    }
}
