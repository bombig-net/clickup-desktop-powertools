using System;
using System.Threading.Tasks;
using ClickUpDesktopPowerTools.Core;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Tools.CustomCssJs;

/// <summary>
/// Minimal Custom CSS/JS injection tool for platform validation.
/// Injects CSS via style tag and executes JavaScript when runtime is ready.
/// Re-applies on navigation and reconnect.
/// </summary>
public class CustomCssJsTool : IToolLifecycle
{
    private readonly ILogger<CustomCssJsTool> _logger;
    private readonly CustomCssJsSettings _settings;
    private RuntimeContext? _runtimeContext;
    private string? _lastInjectionResult;
    private string? _lastInjectionError;
    private bool _isEnabled;

    public CustomCssJsTool(ILogger<CustomCssJsTool> logger)
    {
        _logger = logger;
        _settings = CustomCssJsSettings.Load();
    }

    public string? LastInjectionResult => _lastInjectionResult;
    public string? LastInjectionError => _lastInjectionError;

    public void OnEnable()
    {
        _isEnabled = true;
        _logger.LogInformation("Custom CSS/JS tool enabled");
        
        // If runtime is already connected, apply immediately
        if (_runtimeContext != null)
        {
            _ = Task.Run(async () => await ApplyInjectionsAsync());
        }
    }

    public void OnDisable()
    {
        _isEnabled = false;
        _logger.LogInformation("Custom CSS/JS tool disabled");
        
        // Remove injected content
        if (_runtimeContext != null)
        {
            _ = Task.Run(async () => await RemoveInjectionsAsync());
        }
        
        _runtimeContext = null;
    }

    public void OnRuntimeReady(RuntimeContext ctx)
    {
        _runtimeContext = ctx;
        _logger.LogInformation("Runtime ready, applying Custom CSS/JS");
        
        // Subscribe to navigation events to re-apply on navigation
        ctx.NavigationOccurred += OnNavigationOccurred;
        
        // Apply injections
        _ = Task.Run(async () => await ApplyInjectionsAsync());
    }

    public void OnRuntimeDisconnected()
    {
        if (_runtimeContext != null)
        {
            _runtimeContext.NavigationOccurred -= OnNavigationOccurred;
        }
        _runtimeContext = null;
        _logger.LogInformation("Runtime disconnected");
    }

    private void OnNavigationOccurred(object? sender, string url)
    {
        _logger.LogDebug("Navigation occurred: {Url}, re-applying injections", url);
        if (_isEnabled && _runtimeContext != null)
        {
            _ = Task.Run(async () => await ApplyInjectionsAsync());
        }
    }

    private async Task ApplyInjectionsAsync()
    {
        if (_runtimeContext == null || !_isEnabled)
        {
            return;
        }

        try
        {
            // Inject CSS if provided
            if (!string.IsNullOrWhiteSpace(_settings.CustomCss))
            {
                var cssScript = $@"
(function() {{
    var styleId = '__clickup_powertools_custom_css';
    var existingStyle = document.getElementById(styleId);
    if (existingStyle) {{
        existingStyle.remove();
    }}
    var style = document.createElement('style');
    style.id = styleId;
    style.textContent = {System.Text.Json.JsonSerializer.Serialize(_settings.CustomCss)};
    document.head.appendChild(style);
}})();
";
                var cssResult = await _runtimeContext.ExecuteScriptWithResultAsync(cssScript);
                if (cssResult.Success)
                {
                    _lastInjectionResult = $"CSS injected successfully at {DateTime.Now:HH:mm:ss}";
                    _lastInjectionError = null;
                    _logger.LogDebug("CSS injection successful");
                }
                else
                {
                    _lastInjectionError = $"CSS injection failed: {cssResult.ExceptionMessage ?? "Unknown error"}";
                    _logger.LogWarning("CSS injection failed: {Error}", cssResult.ExceptionMessage);
                }
            }

            // Execute JavaScript if provided
            if (!string.IsNullOrWhiteSpace(_settings.CustomJavaScript))
            {
                var jsResult = await _runtimeContext.ExecuteScriptWithResultAsync(_settings.CustomJavaScript);
                if (jsResult.Success)
                {
                    var resultText = !string.IsNullOrEmpty(jsResult.Value) ? $", result: {jsResult.Value}" : "";
                    _lastInjectionResult = $"JS executed successfully at {DateTime.Now:HH:mm:ss}{resultText}";
                    _lastInjectionError = null;
                    _logger.LogDebug("JavaScript execution successful: {Result}", jsResult.Value ?? "(no return value)");
                }
                else
                {
                    _lastInjectionError = $"JavaScript execution failed: {jsResult.ExceptionMessage ?? "Unknown error"}";
                    _logger.LogWarning("JavaScript execution failed: {Error}", jsResult.ExceptionMessage);
                }
            }

            if (string.IsNullOrWhiteSpace(_settings.CustomCss) && string.IsNullOrWhiteSpace(_settings.CustomJavaScript))
            {
                _lastInjectionResult = "No CSS or JavaScript configured";
                _lastInjectionError = null;
            }
        }
        catch (Exception ex)
        {
            _lastInjectionError = $"Error: {ex.Message}";
            _lastInjectionResult = null;
            _logger.LogError(ex, "Failed to apply injections");
        }
    }

    private async Task RemoveInjectionsAsync()
    {
        if (_runtimeContext == null)
        {
            return;
        }

        try
        {
            // Remove injected style tag
            var removeScript = @"
(function() {
    var styleId = '__clickup_powertools_custom_css';
    var existingStyle = document.getElementById(styleId);
    if (existingStyle) {
        existingStyle.remove();
    }
})();
";
            // Use ExecuteScriptAsync for removal - we don't care about the result
            await _runtimeContext.ExecuteScriptAsync(removeScript);
            _logger.LogDebug("Removed injected CSS");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove injections");
        }
    }

    public void UpdateSettings(string? css, string? js)
    {
        _settings.CustomCss = css;
        _settings.CustomJavaScript = js;
        _settings.Save();
        _logger.LogInformation("Settings updated");

        // Re-apply if enabled and runtime is connected
        if (_isEnabled && _runtimeContext != null)
        {
            _ = Task.Run(async () => await ApplyInjectionsAsync());
        }
    }

    public (string? css, string? js) GetSettings()
    {
        return (_settings.CustomCss, _settings.CustomJavaScript);
    }
}

