using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using ClickUpDesktopPowerTools.Core;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

public class TimeTrackingViewModel : INotifyPropertyChanged, IToolLifecycle
{
    private readonly TimeTrackingService _service;
    private readonly DispatcherTimer _uiRefreshTimer;
    private RuntimeContext? _runtimeContext;
    private string _currentTaskName = "No task";
    private string _elapsedTime = "00:00:00";
    private bool _isRunning = false;
    private bool _isTeamIdConfigured = false;
    private bool _hasError = false;

    public TimeTrackingViewModel(TimeTrackingService service)
    {
        _service = service;
        
        _uiRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
        
        StartStopCommand = new RelayCommand(ExecuteStartStop);
        
        UpdateFromService();
        _uiRefreshTimer.Start();
    }

    public string CurrentTaskName
    {
        get => _currentTaskName;
        set
        {
            if (_currentTaskName != value)
            {
                _currentTaskName = value;
                OnPropertyChanged();
            }
        }
    }

    public string ElapsedTime
    {
        get => _elapsedTime;
        set
        {
            if (_elapsedTime != value)
            {
                _elapsedTime = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StartStopButtonText));
            }
        }
    }

    public string StartStopButtonText => IsRunning ? "Stop" : "Start";

    public bool IsTeamIdConfigured
    {
        get => _isTeamIdConfigured;
        set
        {
            if (_isTeamIdConfigured != value)
            {
                _isTeamIdConfigured = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            if (_hasError != value)
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand StartStopCommand { get; }

    private async void ExecuteStartStop()
    {
        if (IsRunning)
        {
            await _service.Stop();
        }
        else
        {
            // Note: Starting requires a task ID. For now, this will fail gracefully.
            // In a full implementation, we would need task selection UI.
            // For the initial version, we'll handle the error gracefully.
            try
            {
                await _service.Start("dummy-task-id");
            }
            catch
            {
                // API call failed - this is expected without a valid task ID
            }
        }
        UpdateFromService();
    }

    private void UiRefreshTimer_Tick(object? sender, EventArgs e)
    {
        UpdateFromService();
    }

    private void UpdateFromService()
    {
        CurrentTaskName = _service.CurrentTaskName;
        ElapsedTime = _service.GetCurrentElapsedTime().ToString(@"hh\:mm\:ss");
        IsRunning = _service.IsRunning;
        IsTeamIdConfigured = _service.IsTeamIdConfigured;
        HasError = _service.HasError;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // IToolLifecycle implementation
    public void OnEnable()
    {
        // Tool enabled - no action needed, service already running if configured
    }

    public void OnDisable()
    {
        // Tool disabled - clear runtime context
        _runtimeContext = null;
    }

    public void OnRuntimeReady(RuntimeContext ctx)
    {
        // Runtime connected - store context and start polling task ID
        _runtimeContext = ctx;
        
        // Start background task to poll task ID from runtime
        Task.Run(async () =>
        {
            while (_runtimeContext != null)
            {
                try
                {
                    var taskId = await _runtimeContext.GetTaskIdAsync();
                    if (!string.IsNullOrEmpty(taskId))
                    {
                        // Task ID available from runtime - could be used for auto-starting timer
                        // For now, just log it
                        System.Diagnostics.Debug.WriteLine($"Current task ID from runtime: {taskId}");
                    }
                }
                catch
                {
                    // Ignore errors in background polling
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5)); // Poll every 5 seconds
            }
        });
    }

    public void OnRuntimeDisconnected()
    {
        // Runtime disconnected - clear context
        _runtimeContext = null;
    }
}

#pragma warning disable CS0067 // Event is never used but required by ICommand interface
public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        _execute();
    }
}

