using System.ComponentModel;

namespace FirewallChecker;

public class ProcessInfo : INotifyPropertyChanged
{
    private bool _isTerminated;
    public required string ProcessName { get; set; }
    public required string FilePath { get; set; }
    public int Pid { get; set; }

    public bool IsTerminated
    {
        get => _isTerminated;
        set
        {
            _isTerminated = value;
            OnPropertyChanged(nameof(IsTerminated));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
