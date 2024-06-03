using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace FirewallChecker;

public static class FirewallHelper
{
     private static List<FirewallRule> GetFirewallRules()
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

    private static List<FirewallRule> ParseFirewallRules(string output)
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



    // private List<FirewallRule> GetMatchingFirewallRules(int processId)
    // {
    //     var allRules = GetFirewallRules();
    //  
    //
    //     return allRules;
    // }
       public static List<FirewallRule> GetMatchingFirewallRules(string filePath)
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