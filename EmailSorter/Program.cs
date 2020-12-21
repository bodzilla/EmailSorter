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
        private static IList<string> _forbiddenEmails;
        private static bool _removeForbiddenEmails;
        private static void Main()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("settings.json").Build();
            _forbiddenEmails = configuration.GetSection("forbidden-emails").GetChildren().Select(x => x.Value).ToList();
            _removeForbiddenEmails = bool.Parse(configuration.GetSection("remove-forbidden-emails").Value);

            var rawEmails = File.ReadAllLines($@"{Directory.GetCurrentDirectory()}\raw-emails.txt");
            var cleanedEmails = Clean(rawEmails);
            var fileName = Export(cleanedEmails);
            new Process { StartInfo = new ProcessStartInfo { UseShellExecute = true, FileName = fileName } }.Start();
        }


        private static IList<string> Clean(IEnumerable<string> emails)
        {
            var validEmailsRegex = new Regex(@"(.*?)([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)(.*?)");
            var escapedEmails = (emails.Select(email => email.Replace(@"\n", " "))
                .Select(escapedEmail => validEmailsRegex.Matches(escapedEmail))
                .Where(matches => matches.Any())
                .SelectMany(x => x, (matches, match) => new { matches, match })
                .SelectMany(x => x.match.Captures, (x, clean) => clean.Value)).ToList();

            var validEmailRegex = new Regex(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+");
            var cleanedEmails = escapedEmails.Select(email => validEmailRegex.Match(email).Value).Where(match => !String.IsNullOrWhiteSpace(match) && !IsForbiddenEmail(match)).ToList();
            return cleanedEmails.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static bool IsForbiddenEmail(string email)
        {
            if (!_removeForbiddenEmails) return false;
            bool forbidden = false;
            foreach (var forbiddenEmail in _forbiddenEmails)
            {
                if (email.Contains(forbiddenEmail, StringComparison.CurrentCultureIgnoreCase)) forbidden = true;
            }
            return forbidden;
        }

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
