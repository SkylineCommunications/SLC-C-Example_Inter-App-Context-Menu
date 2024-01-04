using System;
using System.Collections.Generic;
using Skyline.DataMiner.Scripting;
using Skyline.Protocol;

/// <summary>
/// DataMiner QAction Class: After Startup.
/// </summary>
public static class QAction
{
    /// <summary>
    /// The QAction entry point.
    /// </summary>
    /// <param name="protocol">Link with SLProtocol process.</param>
    public static void Run(SLProtocolExt protocol)
    {
        try
        {
            PopulatePeopleTable(protocol);
        }
        catch (Exception ex)
        {
            protocol.Log($"QA{protocol.QActionID}|{protocol.GetTriggerParameter()}|Run|Exception thrown:{Environment.NewLine}{ex}", LogType.Error, LogLevel.NoLogging);
        }
    }

    private static void PopulatePeopleTable(SLProtocolExt protocol)
    {
        var firstNames = new List<string> { "Alice", "Bob", "Charlie", "David", "Emma", "Frank", "Grace", "Henry" };

        var lastNames = new List<string> { "Adams", "Brown", "Clark", "Davis", "Edwards", "Fisher", "Garcia", "Harris" };

        var genders = new List<string> { "M", "F", "X" };

        var random = new Random();

        var rows = new List<QActionTableRow>();

        for (int i = 0; i < 10; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Count)];

            var lastName = lastNames[random.Next(lastNames.Count)];

            var gender = genders[random.Next(genders.Count)];

            var row = new PersonsQActionRow
            {
                Personsindex_1001 = Generic.GetSocialSecurityNumber(),
                Personsfirstname_1002 = firstName,
                Personslastname_1003 = lastName,
                Personsgender_1004 = gender,
                Personsage_1005 = random.Next(1, 100),
            };

            rows.Add(row);
        }

        protocol.persons.FillArray(rows);
    }
}
