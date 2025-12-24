using System;
using System.IO;
using System.Text;
using System.Threading;
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
    /// <summary>
    /// Represents a navigation event detected by the observer.
    /// </summary>
    private class NavigationEvent
    {
        public long Timestamp { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    private readonly ILogger<CustomCssJsTool> _logger;
    private readonly CustomCssJsSettings _settings;
    private RuntimeContext? _runtimeContext;
    private string? _lastInjectionResult;
    private string? _lastInjectionError;
    private bool _isEnabled;
    private CancellationTokenSource? _pollingCancellationTokenSource;
    private Task? _pollingTask;

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
        
        // If runtime is already connected, apply immediately and start polling
        if (_runtimeContext != null)
        {
            _ = Task.Run(async () => await ApplyInjectionsAsync());
            StartPolling();
        }
    }

    public void OnDisable()
    {
        _isEnabled = false;
        _logger.LogInformation("Custom CSS/JS tool disabled");
        
        // Stop polling
        StopPolling();
        
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
        
        // Start polling for SPA navigation flags
        StartPolling();
    }

    public void OnRuntimeDisconnected()
    {
        // Stop polling
        StopPolling();
        
        if (_runtimeContext != null)
        {
            _runtimeContext.NavigationOccurred -= OnNavigationOccurred;
        }
        _runtimeContext = null;
        _logger.LogInformation("Runtime disconnected");
    }

    private async Task InjectNavigationObserverAsync()
    {
        if (_runtimeContext == null) return;

        var cssJson = System.Text.Json.JsonSerializer.Serialize(_settings.CustomCss ?? "");
        var jsCode = System.Text.Json.JsonSerializer.Serialize(_settings.CustomJavaScript ?? "");

        var observerScript = $@"
(function() {{
    if (window.__clickupPowertoolsObserverInstalled) {{
        // Update existing observer with new CSS
        if (window.__clickupPowertoolsUpdateCSS) {{
            window.__clickupPowertoolsUpdateCSS({cssJson});
        }}
        // JavaScript is stored but execution is handled by C# via CDP
        // Update the stored JS code for reference (though it's not executed here)
        if (window.__clickupPowertoolsSetJS) {{
            window.__clickupPowertoolsSetJS({jsCode});
        }}
        if (window.__clickupPowertoolsApply) {{
            window.__clickupPowertoolsApply();
        }}
        return;
    }}
    window.__clickupPowertoolsObserverInstalled = true;
    
    var cssContent = {cssJson};
    var jsCode = {jsCode};
    var styleId = '__clickup_powertools_custom_css';
    
    function applyCSS() {{
        if (!cssContent) return;
        try {{
            console.log('[ClickUp PowerTools] Applying CSS at', new Date().toISOString());
            var existingStyle = document.getElementById(styleId);
            if (existingStyle) {{
                existingStyle.remove();
            }}
            var style = document.createElement('style');
            style.id = styleId;
            style.textContent = cssContent;
            if (document.head) {{
                document.head.appendChild(style);
                console.log('[ClickUp PowerTools] CSS style tag added to head');
            }} else {{
                // Wait for head to be available
                setTimeout(function() {{
                    if (document.head) {{
                        document.head.appendChild(style);
                        console.log('[ClickUp PowerTools] CSS style tag added to head (delayed)');
                    }}
                }}, 50);
            }}
        }} catch (e) {{
            console.error('ClickUp PowerTools CSS error:', e);
        }}
    }}
    
    function signalNavigation() {{
        // Signal C# that navigation occurred - C# will execute JavaScript via CDP (bypasses CSP)
        if (jsCode) {{
            try {{
                // Use queue instead of single flag to handle rapid navigations
                if (!window.__clickupPowertoolsNavigationQueue) {{
                    window.__clickupPowertoolsNavigationQueue = [];
                }}
                window.__clickupPowertoolsNavigationQueue.push({{
                    timestamp: Date.now(),
                    url: window.location.href
                }});
                console.log('[ClickUp PowerTools] Navigation queued for C# at', new Date().toISOString(), 'URL:', window.location.href, 'Queue length:', window.__clickupPowertoolsNavigationQueue.length);
            }} catch (e) {{
                console.error('[ClickUp PowerTools] Failed to signal navigation:', e);
            }}
        }}
    }}
    
    function applyAll() {{
        console.log('[ClickUp PowerTools] applyAll() called at', new Date().toISOString());
        applyCSS();
        signalNavigation();
        // Log for debugging
        try {{
            console.log('[ClickUp PowerTools] Finished applying CSS and signaling navigation at', new Date().toISOString());
        }} catch (e) {{}}
    }}
    
    // Wait for DOM to be ready, then apply
    function initObserver() {{
        if (document.readyState === 'loading') {{
            document.addEventListener('DOMContentLoaded', function() {{
                applyAll();
                setupObservers();
            }});
        }} else {{
            applyAll();
            setupObservers();
        }}
    }}
    
    function setupObservers() {{
        // Watch for URL changes (SPA navigation) - more aggressive polling
        var lastUrl = window.location.href;
        var lastPathname = window.location.pathname;
        var navigationCount = 0;
        var urlCheckInterval = setInterval(function() {{
            var currentUrl = window.location.href;
            var currentPathname = window.location.pathname;
            if (currentUrl !== lastUrl || currentPathname !== lastPathname) {{
                navigationCount++;
                try {{
                    console.log('[ClickUp PowerTools] NAVIGATION #' + navigationCount + ' detected: URL changed from', lastUrl, 'to', currentUrl);
                    console.log('[ClickUp PowerTools] Pathname changed from', lastPathname, 'to', currentPathname);
                }} catch (e) {{}}
                lastUrl = currentUrl;
                lastPathname = currentPathname;
                console.log('[ClickUp PowerTools] Scheduling applyAll() due to URL change (navigation #' + navigationCount + ')');
                setTimeout(function() {{
                    console.log('[ClickUp PowerTools] Executing applyAll() after URL change timeout (navigation #' + navigationCount + ')');
                    applyAll();
                }}, 200);
            }}
        }}, 200);
        
        // Watch for DOM mutations that might indicate navigation
        if (document.body) {{
            var observer = new MutationObserver(function(mutations) {{
                var shouldReapply = false;
                for (var i = 0; i < mutations.length; i++) {{
                    var mutation = mutations[i];
                    if (mutation.type === 'childList' && mutation.target === document.body) {{
                        shouldReapply = true;
                        break;
                    }}
                }}
                if (shouldReapply) {{
                    try {{
                        console.log('[ClickUp PowerTools] DOM mutation detected, reapplying');
                    }} catch (e) {{}}
                    setTimeout(function() {{
                        console.log('[ClickUp PowerTools] Executing applyAll() after DOM mutation timeout');
                        applyAll();
                    }}, 200);
                }}
            }});
            
            observer.observe(document.body, {{
                childList: true,
                subtree: false
            }});
        }} else {{
            // Wait for body to be available
            var bodyObserver = new MutationObserver(function() {{
                if (document.body) {{
                    bodyObserver.disconnect();
                    setupObservers();
                }}
            }});
            bodyObserver.observe(document.documentElement, {{
                childList: true,
                subtree: true
            }});
        }}
        
        // Also watch for popstate (back/forward navigation)
        window.addEventListener('popstate', function() {{
            try {{
                console.log('[ClickUp PowerTools] popstate event, reapplying');
            }} catch (e) {{}}
            setTimeout(function() {{
                console.log('[ClickUp PowerTools] Executing applyAll() after popstate timeout');
                applyAll();
            }}, 200);
        }});
        
        // Watch for pushState/replaceState (SPA navigation)
        var originalPushState = history.pushState;
        var originalReplaceState = history.replaceState;
        history.pushState = function() {{
            originalPushState.apply(history, arguments);
            try {{
                console.log('[ClickUp PowerTools] pushState called, reapplying');
            }} catch (e) {{}}
            setTimeout(function() {{
                console.log('[ClickUp PowerTools] Executing applyAll() after pushState timeout');
                applyAll();
            }}, 200);
        }};
        history.replaceState = function() {{
            originalReplaceState.apply(history, arguments);
            try {{
                console.log('[ClickUp PowerTools] replaceState called, reapplying');
            }} catch (e) {{}}
            setTimeout(function() {{
                console.log('[ClickUp PowerTools] Executing applyAll() after replaceState timeout');
                applyAll();
            }}, 200);
        }};
    }}
    
    // Store functions globally so they can be updated
    window.__clickupPowertoolsApply = applyAll;
    window.__clickupPowertoolsUpdateCSS = function(newCss) {{
        cssContent = newCss;
        applyCSS();
    }};
    window.__clickupPowertoolsSetJS = function(newJs) {{
        jsCode = newJs;
    }};
    // Note: JavaScript execution is handled by C# via CDP (bypasses CSP), not through observer
    
    // Initialize
    initObserver();
}})();
";
        try
        {
            var result = await _runtimeContext.ExecuteScriptWithResultAsync(observerScript);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to inject navigation observer: {Error}", result.ExceptionMessage);
            }
            else
            {
                // Verify observer is installed and get its state
                await Task.Delay(500); // Give observer time to initialize
                try
                {
                    var verifyScript = @"(function() {
                        return {
                            installed: !!window.__clickupPowertoolsObserverInstalled,
                            hasApply: !!window.__clickupPowertoolsApply,
                            currentUrl: window.location.href,
                            hasHead: !!document.head,
                            hasBody: !!document.body
                        };
                    })();";
                    var verifyResult = await _runtimeContext.ExecuteScriptWithResultAsync(verifyScript);
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inject navigation observer");
        }
    }

    private void OnNavigationOccurred(object? sender, string url)
    {
        _logger.LogDebug("Navigation occurred: {Url}, re-applying injections", url);
        if (_isEnabled && _runtimeContext != null)
        {
            _ = Task.Run(async () =>
            {
                // Try to trigger observer's apply function first
                try
                {
                    var triggerScript = @"(function() {
                        if (window.__clickupPowertoolsApply) {
                            window.__clickupPowertoolsApply();
                            return 'triggered';
                        }
                        return 'not_found';
                    })();";
                    var triggerResult = await _runtimeContext.ExecuteScriptWithResultAsync(triggerScript);
                }
                catch { }
                await ApplyInjectionsAsync();
            });
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
            // First, inject a persistent navigation observer that re-applies CSS/JS on SPA navigations
            await InjectNavigationObserverAsync();

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
            // Remove injected style tag and clean up observer
            var removeScript = @"
(function() {
    var styleId = '__clickup_powertools_custom_css';
    var existingStyle = document.getElementById(styleId);
    if (existingStyle) {
        existingStyle.remove();
    }
    // Clean up observer globals
    if (window.__clickupPowertoolsObserverInstalled) {
        delete window.__clickupPowertoolsObserverInstalled;
        delete window.__clickupPowertoolsApply;
        delete window.__clickupPowertoolsUpdateCSS;
        delete window.__clickupPowertoolsSetJS;
        delete window.__clickupPowertoolsNavigationQueue;
    }
})();
";
            // Use ExecuteScriptAsync for removal - we don't care about the result
            await _runtimeContext.ExecuteScriptAsync(removeScript);
            _logger.LogDebug("Removed injected CSS and observer");
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
            _ = Task.Run(async () =>
            {
                try
                {
                    // Update CSS via observer
                    var cssJson = System.Text.Json.JsonSerializer.Serialize(css ?? "");
                    var jsJson = System.Text.Json.JsonSerializer.Serialize(js ?? "");
                    var updateScript = $@"
(function() {{
    if (window.__clickupPowertoolsUpdateCSS) {{
        window.__clickupPowertoolsUpdateCSS({cssJson});
    }}
    if (window.__clickupPowertoolsSetJS) {{
        window.__clickupPowertoolsSetJS({jsJson});
    }}
    if (window.__clickupPowertoolsApply) {{
        window.__clickupPowertoolsApply();
    }}
}})();
";
                    await _runtimeContext.ExecuteScriptAsync(updateScript);
                    
                    // Execute JavaScript directly via CDP (bypasses CSP)
                    if (!string.IsNullOrWhiteSpace(js))
                    {
                        var jsResult = await _runtimeContext.ExecuteScriptWithResultAsync(js);
                        if (jsResult.Success)
                        {
                            _lastInjectionResult = $"JS executed successfully at {DateTime.Now:HH:mm:ss}";
                            _lastInjectionError = null;
                            _logger.LogDebug("JavaScript executed successfully via CDP after settings update");
                        }
                        else
                        {
                            _lastInjectionError = $"JavaScript execution failed: {jsResult.ExceptionMessage ?? "Unknown error"}";
                            _logger.LogWarning("JavaScript execution failed after settings update: {Error}", jsResult.ExceptionMessage);
                        }
                    }
                }
                catch
                {
                    // If update fails, re-inject everything
                    await ApplyInjectionsAsync();
                }
            });
        }
    }

    public (string? css, string? js) GetSettings()
    {
        return (_settings.CustomCss, _settings.CustomJavaScript);
    }

    private void StartPolling()
    {
        // Stop any existing polling
        StopPolling();

        if (!_isEnabled || _runtimeContext == null)
        {
            return;
        }

        _pollingCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _pollingCancellationTokenSource.Token;

        _pollingTask = Task.Run(async () =>
        {
            _logger.LogInformation("Started polling for navigation flags (interval: 200ms)");
            var iterationCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(200, cancellationToken); // Poll every 200ms for faster detection

                    if (!_isEnabled || _runtimeContext == null)
                    {
                        _logger.LogDebug("Polling stopped: tool disabled or runtime disconnected");
                        break;
                    }

                    iterationCount++;
                    
                    // Check for navigation queue - use top window to ensure we're in the right context
                    var checkScript = @"(function() {
                        try {
                            // Get the top window (main frame) to ensure we're checking the right context
                            var topWindow = window.top || window;
                            var queue = topWindow.__clickupPowertoolsNavigationQueue;
                            
                            // Debug: Check if window and queue exist
                            var debug = {
                                hasWindow: typeof window !== 'undefined',
                                hasTopWindow: typeof topWindow !== 'undefined',
                                hasQueue: typeof topWindow.__clickupPowertoolsNavigationQueue !== 'undefined',
                                queueType: typeof topWindow.__clickupPowertoolsNavigationQueue,
                                queueIsArray: Array.isArray(topWindow.__clickupPowertoolsNavigationQueue),
                                queueLength: topWindow.__clickupPowertoolsNavigationQueue ? topWindow.__clickupPowertoolsNavigationQueue.length : 0
                            };
                            
                            if (queue && Array.isArray(queue) && queue.length > 0) {
                                topWindow.__clickupPowertoolsNavigationQueue = [];
                                return JSON.stringify({ success: true, queue: queue, debug: debug });
                            }
                            
                            // Return debug info even if queue is empty
                            return JSON.stringify({ success: false, queue: null, debug: debug });
                        } catch (e) {
                            return JSON.stringify({ success: false, error: e.message, queue: null });
                        }
                    })();";

                    var checkResult = await _runtimeContext.ExecuteScriptWithResultAsync(checkScript);
                    
                    if (!checkResult.Success)
                    {
                        _logger.LogWarning("Polling iteration #{Iteration}: Failed to check navigation queue: {Error}", iterationCount, checkResult.ExceptionMessage);
                        continue;
                    }
                    
                    // Handle null/empty values - CDP might return "null" as string or actual null
                    var queueJson = checkResult.Value;
                    if (string.IsNullOrEmpty(queueJson) || queueJson == "null" || queueJson == "\"null\"")
                    {
                        // No navigation detected in this iteration
                        if (iterationCount % 25 == 0) // Log every 5 seconds (25 * 200ms)
                        {
                            _logger.LogDebug("Polling iteration #{Iteration}: No navigation queue detected", iterationCount);
                        }
                        continue;
                    }
                    
                    // Parse the response (now includes debug info)
                    try
                    {
                        // Parse the response object (contains success, queue, and debug)
                        var responseObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(queueJson);
                        var success = responseObj.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                        
                        if (!success || !responseObj.TryGetProperty("queue", out var queueProp))
                        {
                            if (iterationCount % 25 == 0)
                            {
                                _logger.LogDebug("Polling iteration #{Iteration}: Queue check returned no queue", iterationCount);
                            }
                            continue;
                        }
                        
                        // Deserialize the queue array
                        var navigations = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<NavigationEvent>>(queueProp.GetRawText());
                        if (navigations != null && navigations.Count > 0)
                        {
                            _logger.LogInformation("Polling iteration #{Iteration}: Detected {Count} navigation(s) in queue", iterationCount, navigations.Count);
                            
                            // Execute JavaScript for each navigation
                            foreach (var nav in navigations)
                            {
                                _logger.LogDebug("Processing navigation: URL={Url}, Timestamp={Timestamp}", nav.Url, nav.Timestamp);
                                
                                if (!string.IsNullOrWhiteSpace(_settings.CustomJavaScript))
                                {
                                    _logger.LogDebug("Executing JavaScript via CDP for navigation to {Url}", nav.Url);
                                    var jsResult = await _runtimeContext.ExecuteScriptWithResultAsync(_settings.CustomJavaScript);
                                    if (jsResult.Success)
                                    {
                                        _lastInjectionResult = $"JS executed on navigation to {nav.Url} at {DateTime.Now:HH:mm:ss}";
                                        _lastInjectionError = null;
                                        _logger.LogInformation("JavaScript executed successfully on navigation via CDP (URL: {Url})", nav.Url);
                                    }
                                    else
                                    {
                                        _lastInjectionError = $"JavaScript execution failed on navigation: {jsResult.ExceptionMessage ?? "Unknown error"}";
                                        _logger.LogWarning("JavaScript execution failed on navigation (URL: {Url}): {Error}", nav.Url, jsResult.ExceptionMessage);
                                    }
                                }
                                else
                                {
                                    _logger.LogDebug("No JavaScript code configured, skipping execution");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Polling iteration #{Iteration}: Queue deserialized but empty or null", iterationCount);
                        }
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "Polling iteration #{Iteration}: Failed to parse navigation queue JSON: {Json}", iterationCount, queueJson);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    _logger.LogDebug("Polling cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Polling iteration #{Iteration}: Unexpected error during navigation polling", iterationCount);
                    // Continue polling despite errors
                }
            }
            _logger.LogInformation("Stopped polling for navigation flags (total iterations: {Count})", iterationCount);
        }, cancellationToken);
    }

    private void StopPolling()
    {
        if (_pollingCancellationTokenSource != null)
        {
            _pollingCancellationTokenSource.Cancel();
            _pollingCancellationTokenSource.Dispose();
            _pollingCancellationTokenSource = null;
        }

        if (_pollingTask != null)
        {
            try
            {
                _pollingTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error waiting for polling task to complete");
            }
            _pollingTask = null;
        }
    }
}

