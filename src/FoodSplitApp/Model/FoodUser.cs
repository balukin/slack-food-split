using FoodSplitApp.Services.Slack;

namespace FoodSplitApp.Model
{
    public class FoodUser
    {
        /// <summary>
        /// Identifier used to distinguish users.
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// Optional name to display instead of Id.
        /// </summary>
        public string FriendlyName { get; set; }

        public FoodUser()
        {
        }

        public FoodUser(string uniqueId, string friendlyName = null)
        {
            UniqueId = uniqueId;
            FriendlyName = friendlyName;
        }

        public FoodUser(SlackUser slacker)
        {
            UniqueId = slacker.Id;
            FriendlyName = slacker.Username;
        }

        public static FoodUser CreateFromString(string eaterStr)
        {
            if (eaterStr.StartsWith("<@"))
            {
                // Slack mention
                var slacker = SlackUser.Parse(eaterStr);
                return new FoodUser(slacker.Id, slacker.Username);
            }
            else
            {
                // External guest-eater
                return new FoodUser(eaterStr);
            }
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(FriendlyName) ? UniqueId : FriendlyName;
        }

        public string ToSlackMention()
        {
            return $"<@{UniqueId}>";
        }
    }
}