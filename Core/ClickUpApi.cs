using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Core;

public class ClickUpApi
{
    private readonly HttpClient _httpClient;
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<ClickUpApi> _logger;
    private const string BaseUrl = "https://api.clickup.com/api/v2";

    public ClickUpApi(ITokenProvider tokenProvider, ILogger<ClickUpApi> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    private void SetAuthHeader(HttpRequestMessage request)
    {
        var token = _tokenProvider.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add("Authorization", token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        _logger.LogInformation("HTTP request: GET {Endpoint}", endpoint);
        
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            SetAuthHeader(request);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("HTTP success: GET {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            else
            {
                _logger.LogError("HTTP failure: GET {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: GET {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during HTTP request: GET {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        _logger.LogInformation("HTTP request: POST {Endpoint}", endpoint);
        
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(data)
            };
            SetAuthHeader(request);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("HTTP success: POST {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            else
            {
                _logger.LogError("HTTP failure: POST {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: POST {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during HTTP request: POST {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        _logger.LogInformation("HTTP request: PUT {Endpoint}", endpoint);
        
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
            {
                Content = JsonContent.Create(data)
            };
            SetAuthHeader(request);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("HTTP success: PUT {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            else
            {
                _logger.LogError("HTTP failure: PUT {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: PUT {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during HTTP request: PUT {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task DeleteAsync(string endpoint)
    {
        _logger.LogInformation("HTTP request: DELETE {Endpoint}", endpoint);
        
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            SetAuthHeader(request);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("HTTP success: DELETE {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            else
            {
                _logger.LogError("HTTP failure: DELETE {Endpoint} {StatusCode}", endpoint, (int)response.StatusCode);
            }
            
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: DELETE {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during HTTP request: DELETE {Endpoint}", endpoint);
            throw;
        }
    }
}

