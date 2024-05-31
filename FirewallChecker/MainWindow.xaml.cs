using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FirewallChecker;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _isAutoRefreshEnabled;
    private readonly ObservableCollection<ProcessInfo> _processes = new();
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow()
    {
        InitializeComponent();
        ProcessListView.ItemsSource = _processes;
        RefreshProcess();
        _refreshTimer = new DispatcherTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(10); // Set interval as needed
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private void RefreshProcess_Click(object sender, RoutedEventArgs e)
    {
        RefreshProcess();
        _refreshTimer.Stop();
        if (_isAutoRefreshEnabled) _refreshTimer.Start();
    }

    private async void RefreshTimer_Tick(object s, EventArgs e)
    {
        await RefreshProcess();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
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
        var currentProcesses = Process.GetProcesses()
            .GroupBy(p => p.ProcessName)
            .Select(g => new ProcessInfo
            {
                ProcessName = g.First().ProcessName,
                FilePath = g.Key,
                Pid = g.First().Id
            }).ToList();
        foreach (var proc in currentProcesses)
            if (_processes.All(p => p.FilePath != proc.FilePath))
                ProcessListView.Items.Add(proc);
        foreach (var proc in _processes.ToList())
            if (currentProcesses.All(p => p.FilePath != proc.FilePath))
                _processes.Remove(proc);
        // ProcessListView.ItemsSource = displayedProcesses;
    }

    private List<FirewallRule> GetFirewallRules()
    {
        List<FirewallRule> rules;
        var processInfo = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = "advfirewall firewall show rule name=all",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        using (var reader = process.StandardOutput)
        {
            string output = reader.ReadToEnd();
            rules = ParseFirewallRules(output);
        }

        return rules;
    }

    private List<FirewallRule> ParseFirewallRules(string output)
    {
        var rules = new List<FirewallRule>();
        var ruleSections = Regex.Split(output, @"(?=\s*Rule Name:)").Skip(1);

        foreach (var section in ruleSections)
        {
            var rule = new FirewallRule();
            var lines = section.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.StartsWith("Rule Name:"))
                {
                    rule.Name = line.Split(new[] { ':' }, 2)[1].Trim();
                }
                else if (line.StartsWith("Direction:"))
                {
                    rule.Direction = line.Split(new[] { ':' }, 2)[1].Trim();
                }
                else if (line.StartsWith("Action:"))
                {
                    rule.Action = line.Split(new[] { ':' }, 2)[1].Trim();
                }
                else if (line.StartsWith("Program:"))
                {
                    rule.ApplicationPath = line.Split(new[] { ':' }, 2)[1].Trim();
                }
                // Add more properties as needed
            }

            rules.Add(rule);
        }

        return rules;
    }

    private void ProcessListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProcessListView.SelectedItem != null)
        {
            var selectedProcess = (ProcessInfo)ProcessListView.SelectedItem;
            string filePath = selectedProcess.FilePath;
            // var processId = selectedProcess.Pid;
            var matchingRules = GetMatchingFirewallRules(filePath);
            FirewallRulesListView.ItemsSource = matchingRules;
        }
    }

    // private List<FirewallRule> GetMatchingFirewallRules(int processId)
    // {
    //     var allRules = GetFirewallRules();
    //  
    //
    //     return allRules;
    // }
       private List<FirewallRule> GetMatchingFirewallRules(string filePath)
    {
        var allRules = GetFirewallRules();
        // var matchingRules = new List<FirewallRule>();
        var matchingRules = allRules
            .Where(e => e.ApplicationPath?.Equals(filePath, StringComparison.OrdinalIgnoreCase) == true).ToList();
        // Implement logic to match firewall rules to the specific process based on your criteria.
        // For example, you can use ports, application path, etc.

        return matchingRules;
    }
}

public class ProcessInfo
{
    public string ProcessName { get; set; }
    public string FilePath { get; set; }
    public int Pid { get; set; }
}

public class FirewallRule
{
    public string Name { get; set; }
    public string Direction { get; set; }
    public string Action { get; set; }
    public string ApplicationPath { get; set; }
}