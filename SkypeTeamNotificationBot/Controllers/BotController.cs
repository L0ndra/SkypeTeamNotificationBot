using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Configuration;

namespace SkypeTeamNotificationBot.Controllers
{
    [Route("bot")]
    public class BotController : Controller
    {
        private readonly IConfigurationRoot _configuration;

        public BotController(IConfigurationRoot configuration)
        {
            this._configuration = configuration;
        }

        [Authorize(Roles = "Bot")]
        // POST api/values
        [HttpPost]
        public virtual async Task<OkResult> Post([FromBody]Activity activity)
        {
            var appCredentials = new MicrosoftAppCredentials(this._configuration);
            var client = new ConnectorClient(new Uri(activity.ServiceUrl), appCredentials);
            var reply = activity.CreateReply();
            if (activity.Type == ActivityTypes.Message)
            {
                reply.Text = $"echo: {activity.Text}";
            }
            else
            {
                reply.Text = $"activity type: {activity.Type}";
            }
            await client.Conversations.ReplyToActivityAsync(reply);
            return Ok();
        }

    }
}