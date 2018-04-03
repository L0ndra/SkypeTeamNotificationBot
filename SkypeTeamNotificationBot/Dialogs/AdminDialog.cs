using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkypeTeamNotificationBot.DataAccess;
using SkypeTeamNotificationBot.DataModels;
using SkypeTeamNotificationBot.Utils;

namespace SkypeTeamNotificationBot.Dialogs
{
    [Serializable]
    public class AdminDialog : IDialog<object>
    {
        private ILogger _logger;

        public AdminDialog()
        {
            _logger = LoggerProxy.Logger<AdminDialog>();
        }
        
        private IDictionary<AdminOptions, string> AdminOptionsDescriptions()
        {
            return Enum.GetValues(typeof(AdminOptions)).Cast<AdminOptions>().ToDictionary(k => k,
                v => v.GetAttributeValue<DescriptionAttribute, string>(x => x.Description));
        }

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Welcome, what you want to do?");
            PromptDialog.Choice(context, HandleMainDialogAsync, AdminOptionsDescriptions().Keys, "Choice action", descriptions: AdminOptionsDescriptions().Values);
        }

        private async Task HandleMainDialogAsync(IDialogContext context, IAwaitable<AdminOptions> option)
        {
            switch (await option)
            {
                case AdminOptions.AddAdmin:
                    await AddAdminAsync(context);
                    break;
                case AdminOptions.BlockUser:
                    await BlockUserAsync(context);
                    break;
                case AdminOptions.RemoveAdmin:
                    await RemoveAdminAsync(context);
                    break;
                case AdminOptions.SendMessage:
                    await SendMessageAsync(context);
                    break;
                case AdminOptions.UnblockUser:
                    await UnblockUserAsync(context);
                    break;
                default:
                    await context.PostAsync("Unknown command, please try again");
                    break;
            }
        }

        private async Task UnblockUserAsync(IDialogContext context)
        {
            var users = (await UsersDal.GetUsersWithSpecificConditionAsync(x => x.Block)).ToList();

            if (users.Count == 0)
            {
                await context.PostAsync("Any blocked users found");
                await StartAsync(context);
            }
            else
            {
                PromptDialog.Choice(context, UnblockUserCallbackAsync, users.Select(x => x.Id),
                    "Select user which you want to unblock", descriptions: users.Select(x => x.Name));
            }
        }

        private async Task UnblockUserCallbackAsync(IDialogContext context, IAwaitable<string> userId)
        {
            var id = await userId;
            var user = await UsersDal.GetUserByIdAsync(id);
            if (user == null)
            {
                await context.PostAsync("Selected user not found");
                await StartAsync(context);
            }
            else
            {
                user.Block = false;
                await UsersDal.UpdateUserAsync(user);
                await context.PostAsync("Selected user unblocked");
                await StartAsync(context);
            }
        }

        private async Task SendMessageAsync(IDialogContext context)
        {
            PromptDialog.Text(context, CheckMessageToSendAsync, "Write text that you want to send");
        }

        private async Task CheckMessageToSendAsync(IDialogContext context, IAwaitable<string> result)
        {
            var text = await result;
            context.UserData.SetValue("text", text);
            PromptDialog.Confirm(context, CheckMessageToSendCallbackAsync, $"Are you sure that you want to send '{text}'");
        }

        private async Task CheckMessageToSendCallbackAsync(IDialogContext context, IAwaitable<bool> result)
        {
            if (await result)
            {
                var text = context.UserData.GetValue<string>("text");
                await SendMessagesForAllNotBlockedUsersAsync(text);
                await context.PostAsync("Message sent for all users");
                await StartAsync(context);
            }
        }

        private async Task RemoveAdminAsync(IDialogContext context)
        {
            var users = (await UsersDal.GetUsersWithSpecificConditionAsync(x => x.Role == Role.Admin))
                .Where(x => x.Id != context.Activity.From.Id).ToList();

            if (users.Count == 0)
            {
                await context.PostAsync("Any admins to demote");
                await StartAsync(context);
            }
            
            PromptDialog.Choice(context, RemoveAdminCallbackAsync, users.Select(x => x.Id),
                "Select admin which you want to demote", descriptions: users.Select(x => x.Name));
        }

        private async Task RemoveAdminCallbackAsync(IDialogContext context, IAwaitable<string> userId)
        {
            var id = await userId;
            var user = await UsersDal.GetUserByIdAsync(id);
            if (user == null)
            {
                await context.PostAsync("Selected user not found");
                await StartAsync(context);
            }
            else
            {
                user.Role = Role.User;
                await UsersDal.UpdateUserAsync(user);
                await context.PostAsync("Selected user not admin");
                context.Reset();
            }

        }

        private async Task BlockUserAsync(IDialogContext context)
        {
            var users = (await UsersDal.GetUsersWithSpecificConditionAsync(x => !x.Block)).ToList();

            PromptDialog.Choice(context, BlockUserCallbackAsync, users.Select(x => x.Id),
                "Select user which you want to block", descriptions: users.Select(x => x.Name));
        }

        private async Task BlockUserCallbackAsync(IDialogContext context, IAwaitable<string> userId)
        {
            var id = await userId;
            var user = await UsersDal.GetUserByIdAsync(id);
            if (user == null)
            {
                await context.PostAsync("Selected user not found");
                await StartAsync(context);
            }
            else
            {
                user.Block = true;
                await UsersDal.UpdateUserAsync(user);
                await context.PostAsync("Selected user blocked");
                await StartAsync(context);
            }

        }

        private async Task AddAdminAsync(IDialogContext context)
        {
            var users = (await UsersDal.GetUsersWithSpecificConditionAsync(x => x.Role != Role.Admin)).ToList();
            if (users.Count == 0)
            {
                await context.PostAsync("Any user to promote");
                await StartAsync(context);
            }

            PromptDialog.Choice(context, AddAdminCallbackAsync, users.Select(x => x.Id),
                "Select user which you want to promote", descriptions: users.Select(x => x.Name));
        }

        private async Task AddAdminCallbackAsync(IDialogContext context, IAwaitable<string> userId)
        {
            var id = await userId;
            var user = await UsersDal.GetUserByIdAsync(id);
            if (user == null)
            {
                await context.PostAsync("Selected user not found");
                await StartAsync(context);
            }
            else
            {
                user.Role = Role.Admin;
                await UsersDal.UpdateUserAsync(user);
                await context.PostAsync("Selected user is admin");
                await StartAsync(context);
            }

        }

        private async Task SendMessagesForAllNotBlockedUsersAsync(string text)
        {
            var users = await UsersDal.GetUsersWithSpecificConditionAsync(x => !x.Block);
            
            foreach (var user in users)
            {
                Activity activity = JsonConvert.DeserializeObject<Activity>(user.Activity);
                var reply = activity.CreateReply("");
                reply.Text = text;
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
        }
    }
}