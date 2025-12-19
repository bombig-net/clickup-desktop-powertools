using System;
using System.Linq;
using System.Threading.Tasks;
using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

public class TimeTrackingService
{
    private readonly ClickUpApi _api;
    private DateTime? _startTime;
    private bool _isRunning = false;
    private string? _currentTimeEntryId;
    private string _currentTaskName = "No task";
    private TimeSpan _elapsedTime = TimeSpan.Zero;

    public TimeTrackingService(ClickUpApi api)
    {
        _api = api;
    }

    public string CurrentTaskName => _currentTaskName;
    public TimeSpan ElapsedTime => _elapsedTime;
    public bool IsRunning => _isRunning;

    public async Task LoadCurrentTimeEntry()
    {
        try
        {
            var response = await _api.GetAsync<ClickUpTimeEntryResponseDto>("/time_entries/current");
            if (response?.Data != null && response.Data.Length > 0)
            {
                var entry = response.Data[0];
                _currentTimeEntryId = entry.Id;
                _currentTaskName = entry.Task?.Name ?? "Unknown task";
                _isRunning = true;
                
                // Calculate elapsed time
                var startTime = DateTimeOffset.FromUnixTimeMilliseconds(entry.Start).DateTime;
                _startTime = startTime;
                _elapsedTime = DateTime.Now - startTime;
            }
            else
            {
                _isRunning = false;
                _currentTimeEntryId = null;
                _currentTaskName = "No task";
            }
        }
        catch
        {
            // API call failed, keep current state
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
                    Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds(),
                    Description = "Time tracking from PowerTools"
                };

                var response = await _api.PostAsync<ClickUpStartTimeEntryRequestDto, ClickUpStartTimeEntryResponseDto>(
                    "/time_entries", request);

                if (response?.Data != null)
                {
                    _currentTimeEntryId = response.Data.Id;
                    _currentTaskName = response.Data.Task?.Name ?? "Unknown task";
                    _startTime = DateTime.Now;
                    _isRunning = true;
                    _elapsedTime = TimeSpan.Zero;
                }
            }
            catch
            {
                // API call failed
            }
        }
    }

    public async Task Stop()
    {
        if (_isRunning && !string.IsNullOrEmpty(_currentTimeEntryId))
        {
            try
            {
                var endTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
                var updateRequest = new { end = endTime };
                await _api.PutAsync<object, object>($"/time_entries/{_currentTimeEntryId}", updateRequest);
                
                if (_startTime.HasValue)
                {
                    _elapsedTime += DateTime.Now - _startTime.Value;
                }
                _startTime = null;
                _isRunning = false;
                _currentTimeEntryId = null;
                _currentTaskName = "No task";
            }
            catch
            {
                // API call failed, but update local state
                if (_startTime.HasValue)
                {
                    _elapsedTime += DateTime.Now - _startTime.Value;
                }
                _startTime = null;
                _isRunning = false;
            }
        }
    }

    public TimeSpan GetCurrentElapsedTime()
    {
        if (_isRunning && _startTime.HasValue)
        {
            return _elapsedTime + (DateTime.Now - _startTime.Value);
        }
        return _elapsedTime;
    }
}

