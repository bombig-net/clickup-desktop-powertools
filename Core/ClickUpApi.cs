using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ClickUpDesktopPowerTools.Core;

public class ClickUpApi
{
    private readonly HttpClient _httpClient;
    private readonly ITokenProvider _tokenProvider;
    private const string BaseUrl = "https://api.clickup.com/api/v2";

    public ClickUpApi(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
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
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        SetAuthHeader(request);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(data)
        };
        SetAuthHeader(request);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
        {
            Content = JsonContent.Create(data)
        };
        SetAuthHeader(request);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task DeleteAsync(string endpoint)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        SetAuthHeader(request);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}

