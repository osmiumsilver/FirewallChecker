using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FirewallChecker;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private bool _isAutoRefreshEnabled;
    private readonly ObservableCollection<ProcessInfo> _processes = new();
    private readonly DispatcherTimer _refreshTimer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();
       
        ProcessListView.ItemsSource = _processes;
        _cancellationTokenSource = new CancellationTokenSource();
      Task.Run(() => MonitorProcess(_cancellationTokenSource.Token));
        
        _ = RefreshProcess();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        }; // Set interval as needed
        _refreshTimer.Tick += RefreshTimer_Tick;
    }
   
    
    private async Task MonitorProcess(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested)
        {
            await RefreshProcess();
            await Task.Delay(5000,cancellationToken);
        }
    }
    
    
    private void RefreshProcess_Click(object sender, RoutedEventArgs e)
    {
        _ = RefreshProcess();
        _refreshTimer.Stop();
        if (_isAutoRefreshEnabled) _refreshTimer.Start();
    }

    private async void RefreshTimer_Tick(object s, EventArgs e)
    {
        await RefreshProcess();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
        Application.Current.Shutdown();
    }

    private void AutoRefreshMenuItem_Checked(object sender, RoutedEventArgs e)
    {
        _isAutoRefreshEnabled = true;
        _refreshTimer.Start();
    }

    private void AutoRefreshMenuItem_Unchecked(object sender, RoutedEventArgs e)
    {
        _isAutoRefreshEnabled = false;
        _refreshTimer.Stop();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Firewall Process Checker\nVersion 1.0\nBy windy", "About");
    }

    // --

    private async Task RefreshProcess()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LoadingIndicator.Visibility = Visibility.Visible;
        });
        var currentProcesses = await Task.Run(() => Process.GetProcesses()
            .GroupBy(p => p.Id)
            .Select(g => 
                new ProcessInfo
                {
                    ProcessName = g.First()
                        .ProcessName,
                    // FilePath =g.First().GetMainModuleFileName(),
                    Pid = g.First()
                        .Id,
                    FilePath = null
                }).ToList()
        );
        
        Console.WriteLine($"Retrieved {currentProcesses.Count} processes.");

        Application.Current.Dispatcher.Invoke(() =>
        {
            _processes.Clear();
            foreach (var proc in currentProcesses)
            {
                var existingProcess = _processes.FirstOrDefault(p => p.ProcessName == proc.ProcessName);
                if (existingProcess == null)
                {
                    
                    _processes.Add(proc);
                }
                else
                {
                    existingProcess.FilePath = proc.FilePath;
                    existingProcess.Pid = proc.Pid;
                }
            }

            foreach (var proc in _processes.ToList())
            {
                proc.IsTerminated = currentProcesses.All(p => p.ProcessName != proc.ProcessName);
            }

            LoadingIndicator.Visibility = Visibility.Collapsed;
            //     if (_processes.All(p => p.ProcessName != proc.ProcessName))
            //         
            //         ProcessListView.Items.Add(proc);
            // foreach (var proc in _processes.ToList())
            //     if (currentProcesses.All(p => p.ProcessName != proc.ProcessName))
            //         _processes.Remove(proc);
        });
        // ProcessListView.ItemsSource = displayedProcesses;
    }

    private void ProcessListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProcessListView.SelectedItem != null)
        {
            var selectedProcess = (ProcessInfo)ProcessListView.SelectedItem;
            string filePath = selectedProcess.FilePath;
            var processId = selectedProcess.Pid;
            var matchingRules = FirewallHelper.GetMatchingFirewallRules(filePath);
            FirewallRulesListView.ItemsSource = matchingRules;
        }
    }

 
    
}




