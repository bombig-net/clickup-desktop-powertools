using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClickUpDesktopPowerTools.Core;
using Microsoft.Extensions.Logging;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

public class TimeTrackingService : IDisposable
{
    private readonly ClickUpApi _api;
    private readonly ILogger<TimeTrackingService> _logger;
    private readonly string? _teamId;
    private DateTime? _startTime;
    private bool _isRunning = false;
    private string? _currentTimeEntryId;
    private string _currentTaskName = "No task";
    private TimeSpan _elapsedTime = TimeSpan.Zero;
    private bool _hasError = false;
    private System.Threading.Timer? _pollingTimer;
    private readonly object _lockObject = new object();
    private readonly SemaphoreSlim _pollingSemaphore = new SemaphoreSlim(1, 1);

    public TimeTrackingService(ClickUpApi api, ILogger<TimeTrackingService> logger)
    {
        _api = api;
        _logger = logger;
        _teamId = TimeTrackingSettings.Load().TeamId;
        
        if (_teamId != null)
        {
            _logger.LogInformation("Team ID loaded: {TeamId}", _teamId);
            StartPolling();
        }
        else
        {
            _logger.LogInformation("Team ID not configured");
        }
    }

    public string CurrentTaskName => _currentTaskName;
    public TimeSpan ElapsedTime => _elapsedTime;
    public bool IsRunning => _isRunning;
    public bool IsTeamIdConfigured => _teamId != null;
    public bool HasError => _hasError;

    private void StartPolling()
    {
        if (_teamId == null)
        {
            return;
        }

        // Dispose existing timer if any (defensive guard against multiple calls)
        _pollingTimer?.Dispose();

        _pollingTimer = new System.Threading.Timer(
            _ => _ = PollTimerAsync(), // Fire-and-forget async work
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10));
        
        _logger.LogInformation("Polling timer started, interval: 10s");
    }

    private async Task PollTimerAsync()
    {
        // Prevent overlapping executions using SemaphoreSlim
        if (!await _pollingSemaphore.WaitAsync(0))
        {
            // Previous execution still running, skip this tick
            return;
        }

        try
        {
            _logger.LogDebug("Polling timer tick");
            await LoadCurrentTimeEntry();
        }
        catch (Exception ex)
        {
            // Exceptions in async Timer callbacks are swallowed by thread pool
            // Preserve error state by setting _hasError (done in LoadCurrentTimeEntry)
            _logger.LogError(ex, "Polling exception in timer callback");
        }
        finally
        {
            _pollingSemaphore.Release();
        }
    }

    public async Task LoadCurrentTimeEntry()
    {
        if (_teamId == null)
        {
            _hasError = false;
            _currentTaskName = "No task";
            _isRunning = false;
            return;
        }

        try
        {
            var response = await _api.GetAsync<ClickUpTimeEntryResponseDto>($"/team/{_teamId}/time_entries/current");
            
            // Verified: data == null means no timer
            if (response?.Data == null)
            {
                lock (_lockObject)
                {
                    var wasRunning = _isRunning;
                    _isRunning = false;
                    _currentTimeEntryId = null;
                    _currentTaskName = "No task";
                    _startTime = null;
                    _elapsedTime = TimeSpan.Zero;
                    _hasError = false;
                    
                    if (wasRunning)
                    {
                        _logger.LogInformation("Timer state changed: running -> stopped");
                    }
                }
                return;
            }

            var entry = response.Data;
            
            // Verified via manual API testing: duration < 0 means running, duration >= 0 means stopped
            bool isRunning = false;
            if (!string.IsNullOrEmpty(entry.Duration) && long.TryParse(entry.Duration, out var duration))
            {
                isRunning = duration < 0;
            }

            lock (_lockObject)
            {
                var oldTaskName = _currentTaskName;
                var wasRunning = _isRunning;
                var hadError = _hasError;
                
                _currentTimeEntryId = entry.Id;
                _currentTaskName = entry.Task?.Name ?? "Unknown task";
                _isRunning = isRunning;
                _hasError = false;

                // Parse start time (string to UTC DateTime)
                if (!string.IsNullOrEmpty(entry.Start) && long.TryParse(entry.Start, out var startMs))
                {
                    var startTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(startMs).UtcDateTime;
                    _startTime = startTimeUtc;
                    
                    if (isRunning)
                    {
                        // Calculate elapsed time from start
                        _elapsedTime = TimeSpan.Zero; // Will be calculated dynamically
                    }
                }
                
                // Log state changes
                if (oldTaskName != _currentTaskName)
                {
                    _logger.LogInformation("Task name changed: \"{OldTaskName}\" -> \"{NewTaskName}\"", oldTaskName, _currentTaskName);
                }
                
                if (wasRunning != _isRunning)
                {
                    _logger.LogInformation("Timer state changed: {OldState} -> {NewState}", 
                        wasRunning ? "running" : "stopped", 
                        _isRunning ? "running" : "stopped");
                }
                
                if (hadError && !_hasError)
                {
                    _logger.LogInformation("Error state cleared");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP errors - preserve state on all errors
            // 404/401/403 indicate configuration/API errors, but we preserve state
            // Network errors also preserve state (user might have timer running)
            _logger.LogError(ex, "API call failed: GET /team/{TeamId}/time_entries/current", _teamId);
            lock (_lockObject)
            {
                var hadError = _hasError;
                _hasError = true;
                if (!hadError)
                {
                    _logger.LogInformation("Error state set");
                }
                // Don't reset to "no timer" - preserve current state
            }
        }
        catch (TaskCanceledException ex)
        {
            // Timeout - preserve state
            _logger.LogError(ex, "API call timeout: GET /team/{TeamId}/time_entries/current", _teamId);
            lock (_lockObject)
            {
                var hadError = _hasError;
                _hasError = true;
                if (!hadError)
                {
                    _logger.LogInformation("Error state set");
                }
            }
        }
        catch (Exception ex)
        {
            // Other errors - preserve state
            _logger.LogError(ex, "Unexpected error in LoadCurrentTimeEntry");
            lock (_lockObject)
            {
                var hadError = _hasError;
                _hasError = true;
                if (!hadError)
                {
                    _logger.LogInformation("Error state set");
                }
            }
        }
    }

    public async Task Start(string taskId)
    {
        if (!_isRunning)
        {
            try
            {
                var request = new ClickUpStartTimeEntryRequestDto
                {
                    TaskId = taskId,
                    Start = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds(),
                    Description = "Time tracking from PowerTools"
                };

                var response = await _api.PostAsync<ClickUpStartTimeEntryRequestDto, ClickUpStartTimeEntryResponseDto>(
                    "/time_entries", request);

                if (response?.Data != null)
                {
                    lock (_lockObject)
                    {
                        _currentTimeEntryId = response.Data.Id;
                        _currentTaskName = response.Data.Task?.Name ?? "Unknown task";
                        _startTime = DateTime.UtcNow;
                        _isRunning = true;
                        _elapsedTime = TimeSpan.Zero;
                        _hasError = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API call failed: POST /time_entries (Start timer)");
            }
        }
    }

    public async Task Stop()
    {
        if (_isRunning && !string.IsNullOrEmpty(_currentTimeEntryId))
        {
            try
            {
                var endTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
                var updateRequest = new { end = endTime };
                await _api.PutAsync<object, object>($"/time_entries/{_currentTimeEntryId}", updateRequest);
                
                lock (_lockObject)
                {
                    if (_startTime.HasValue)
                    {
                        _elapsedTime += DateTime.UtcNow - _startTime.Value;
                    }
                    _startTime = null;
                    _isRunning = false;
                    _currentTimeEntryId = null;
                    _currentTaskName = "No task";
                    _hasError = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API call failed: PUT /time_entries/{TimeEntryId} (Stop timer)", _currentTimeEntryId);
                // API call failed, but update local state
                lock (_lockObject)
                {
                    if (_startTime.HasValue)
                    {
                        _elapsedTime += DateTime.UtcNow - _startTime.Value;
                    }
                    _startTime = null;
                    _isRunning = false;
                }
            }
        }
    }

    public TimeSpan GetCurrentElapsedTime()
    {
        lock (_lockObject)
        {
            if (_isRunning && _startTime.HasValue)
            {
                return _elapsedTime + (DateTime.UtcNow - _startTime.Value);
            }
            return _elapsedTime;
        }
    }

    public void Dispose()
    {
        _pollingTimer?.Dispose();
        _pollingSemaphore?.Dispose();
    }
}

