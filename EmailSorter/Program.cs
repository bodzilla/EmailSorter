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
        private static void Main()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("settings.json").Build();
            var separationCriteria = configuration.GetSection("separation-criteria").GetChildren().Select(x => x.Value);
            var forbiddenEmails = configuration.GetSection("forbidden-emails").GetChildren().Select(x => x.Value);
            var removeForbiddenEmails = bool.Parse(configuration.GetSection("remove-forbidden-emails").Value);

            var rawEmails = File.ReadAllLines($@"{Directory.GetCurrentDirectory()}\raw-emails.txt");
            var separatedEmails = Separate(separationCriteria, rawEmails);
            var emptyLinesRemovedEmails = RemoveEmptyLines(separatedEmails);
            var cleanedEmails = Clean(emptyLinesRemovedEmails);
            var distinctEmails = cleanedEmails.Distinct(StringComparer.CurrentCultureIgnoreCase);

            var fileName = Export(removeForbiddenEmails ? RemoveForbidden(forbiddenEmails, distinctEmails).ToList() : distinctEmails.ToList());
            new Process { StartInfo = new ProcessStartInfo { UseShellExecute = true, FileName = fileName } }.Start();
        }

        private static IEnumerable<string> Separate(IEnumerable<string> separationCriteria, IEnumerable<string> emails) =>
            separationCriteria
                .SelectMany(separationCriterion => emails, (separationCriterion, email) => new { separationCriterion, email })
                .SelectMany(x => x.email.Split(x.separationCriterion));

        private static IEnumerable<string> RemoveEmptyLines(IEnumerable<string> emails) => emails.Where(x => !String.IsNullOrWhiteSpace(x));

        private static IEnumerable<string> Clean(IEnumerable<string> emails)
        {
            var validEmail = new Regex(@"(.*?)([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)(.*?)");
            return emails.SelectMany(email => validEmail.Match(email).Groups[2].Captures, (email, clean) => clean.Value);
        }

        private static IEnumerable<string> RemoveForbidden(IEnumerable<string> forbiddenEmails, IEnumerable<string> emails)
        {
            foreach (var forbiddenEmail in forbiddenEmails)
            {
                var subset = emails.Where(x => !x.Contains(forbiddenEmail, StringComparison.CurrentCultureIgnoreCase));
                emails = subset;
            }
            return emails;
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
