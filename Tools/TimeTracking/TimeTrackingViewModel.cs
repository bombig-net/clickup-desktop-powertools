using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

public class TimeTrackingViewModel : INotifyPropertyChanged
{
    private readonly TimeTrackingService _service;
    private readonly DispatcherTimer _timer;
    private string _currentTaskName = "No task";
    private string _elapsedTime = "00:00:00";
    private bool _isRunning = false;

    public TimeTrackingViewModel(TimeTrackingService service)
    {
        _service = service;
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        
        StartStopCommand = new RelayCommand(ExecuteStartStop);
        
        UpdateFromService();
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

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateFromService();
    }

    private void UpdateFromService()
    {
        CurrentTaskName = _service.CurrentTaskName;
        ElapsedTime = _service.GetCurrentElapsedTime().ToString(@"hh\:mm\:ss");
        IsRunning = _service.IsRunning;
        
        if (IsRunning && !_timer.IsEnabled)
        {
            _timer.Start();
        }
        else if (!IsRunning && _timer.IsEnabled)
        {
            _timer.Stop();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        _execute();
    }
}

