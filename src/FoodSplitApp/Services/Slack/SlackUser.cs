using System;
using System.Text.RegularExpressions;

namespace FoodSplitApp.Services.Slack
{
    /// <summary>
    /// Slack user identified by username and unique identifier.
    /// </summary>
    public class SlackUser
    {
        public SlackUser(string username, string id)
        {
            Username = username;
            Id = id;
        }

        public SlackUser()
        {
            // Deserialization purposes
        }

        public string Username { get; set; }

        public string Id { get; set; }

        /// <summary>
        /// Create an user from slack-encoded user string such as "<@1337|batman>" or "<@1337>".
        /// </summary>
        public static SlackUser Parse(string eaterStr)
        {
            var singleRegex = new Regex(@"<@(\w+)>");
            var doubleRegex = new Regex(@"<@(\w+)\|(\w+)>");

            var singleMatch = singleRegex.Match(eaterStr);
            var doubleMatch = doubleRegex.Match(eaterStr);

            if (doubleMatch.Success)
            {
                return new SlackUser(
                    doubleMatch.Groups[2].Value,
                    doubleMatch.Groups[1].Value);
            }
            else if (singleMatch.Success)
            {
                return new SlackUser(null, doubleMatch.Groups[1].Value);
            }
            else
            {
                throw new FormatException("Invalid slack user string.");
            }
        }
    }
}