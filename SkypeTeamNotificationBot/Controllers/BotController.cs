using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Configuration;
using SkypeTeamNotificationBot.DataAccess;
using SkypeTeamNotificationBot.DataModels;

namespace SkypeTeamNotificationBot.Controllers
{
    [Route("bot")]
    public class BotController : Controller
    {
        private readonly IConfigurationRoot _configuration;
        private readonly UsersDal UserDal;

        public BotController(IConfigurationRoot configuration, UsersDal userdal)
        {
            this._configuration = configuration;
            UserDal = userdal;
        }

        [Authorize(Roles = "Bot")]
        // POST api/values
        [HttpPost]
        public virtual async Task<OkResult> Post([FromBody]Activity activity)
        {
            var appCredentials = new MicrosoftAppCredentials(this._configuration);
            var client = new ConnectorClient(new Uri(activity.ServiceUrl), appCredentials);
                  if (activity.Type == ActivityTypes.Message || activity.Type == ActivityTypes.ConversationUpdate)
            {
                await ExecuteAction(activity, client);
            }
            else
            {
                HandleSystemMessage(activity);
            }

            return Ok();
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private async Task ExecuteAction(Activity activity, ConnectorClient client)
        {
            var user = await UserDal.GetUserByNameAsync(activity.From.Id);
            if (user == null)
            {
                user = new UserModel()
                {
                    Name = activity.From.Id,
                    Activity = activity
                };
                await UserDal.AddNewUserAsync(user);
                var reply = activity.CreateReply();
                reply.Text = $"Hello, {activity.Recipient.Name}. You will receive first notification soon";
                await client.Conversations.ReplyToActivityAsync(reply);
            }

            if (user.Role == Role.Admin && activity.Type == ActivityTypes.Message)
            {
            }
}

    }
}