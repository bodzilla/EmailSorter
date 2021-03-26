using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace EmailSorter
{
    public class Program
    {
        private static ICollection<string> _excludedEmails;
        private static bool _removeExcludedEmails;

        private static void Main()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("settings.json").Build();
            _excludedEmails = configuration.GetSection("excluded-emails").GetChildren().Select(x => x.Value).ToList();
            _removeExcludedEmails = bool.Parse(configuration.GetSection("remove-excluded-emails").Value);

            var rawEmails = File.ReadAllLines($@"{Directory.GetCurrentDirectory()}\raw-emails.txt");
            var cleanedEmails = Clean(rawEmails);
            var fileName = Export(cleanedEmails);
            new Process { StartInfo = new ProcessStartInfo { UseShellExecute = true, FileName = fileName } }.Start();
        }

        private static ICollection<string> Clean(IEnumerable<string> emails)
        {
            var validEmailsRegex = new Regex(@"(.*?)([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)(.*?)");
            var escapedEmails = emails.Select(email => email.Replace(@"\n", " "))
                .Select(escapedEmail => validEmailsRegex.Matches(escapedEmail))
                .Where(matches => matches.Any())
                .SelectMany(x => x, (matches, match) => new { matches, match })
                .SelectMany(x => x.match.Captures, (_, escaped) => escaped.Value);

            var validEmailRegex = new Regex(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+");
            var cleanedEmails = escapedEmails.Select(email => validEmailRegex.Match(email).Value).Where(email => !String.IsNullOrWhiteSpace(email) && !IsExcludedEmail(email));
            return cleanedEmails.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static bool IsExcludedEmail(string email) => _removeExcludedEmails && _excludedEmails.Any(excludedEmail => email.Contains(excludedEmail, StringComparison.CurrentCultureIgnoreCase));

        private static string Export(ICollection<string> finalEmails)
        {
            var directoryName = $@"{Directory.GetCurrentDirectory()}\Sorted Emails";
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);
            var fileName = $@"{directoryName}\{DateTime.Now:yyyyMMdd-HHmmss} - {finalEmails.Count}.txt";
            File.WriteAllText(fileName, String.Join("\r\n", finalEmails));
            return fileName;
        }
    }
}
