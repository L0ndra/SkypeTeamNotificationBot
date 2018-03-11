using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using MongoDB.Bson;
using Newtonsoft.Json;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using SkypeTeamNotificationBot.DataAccess;
using SkypeTeamNotificationBot.DataModels;
using SkypeTeamNotificationBot.Utils;

namespace SkypeTeamNotificationBot.Dialogs
{
    [Serializable]
    public class AdminDialog : IDialog<object>
    {
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
                    context.Reset();
                    break;
            }
        }

        private async Task UnblockUserAsync(IDialogContext context)
        {
            var users = await UsersDal.GetUsersWithSpecificConditionAsync(x => x.Block);

            PromptDialog.Choice(context, async (IDialogContext innerContext, IAwaitable<string> userId) =>
                {
                    var id = await userId;
                    var user = users.FirstOrDefault(x => x.Id == id);
                    if (user == null)
                    {
                        await context.PostAsync("Selected user not found");
                        await StartAsync(context);
                    }
                    else
                    {
                        user.Block = false;
                        await UsersDal.InsertUserAsync(user);
                        await context.PostAsync("Selected user unblocked");
                        context.Reset();
                    }
                    
                }, users.Select(x => x.Id),
                "Select user which you want to unblock", descriptions: users.Select(x => x.Name));
        }

        private async Task SendMessageAsync(IDialogContext context)
        {
            PromptDialog.Text(context, async (dialogContext, result) =>
            {
                PromptDialog.Confirm(dialogContext, async (context1, awaitable) =>
                {
                    if (await awaitable)
                    {
                        await SendMessagesForAllNotBlockedUsersAsync(await result);
                        await context.PostAsync("Message sent for all users");
                    }
                }, $"Are you sure that you want to send '{await result}'");
            }, "Write text that you want to send");
        }

        private async Task RemoveAdminAsync(IDialogContext context)
        {
            var users = await UsersDal.GetUsersWithSpecificConditionAsync(x => x.Role == Role.Admin);

            PromptDialog.Choice(context, async (IDialogContext innerContext, IAwaitable<string> userId) =>
                {
                    var id = await userId;
                    var user = users.FirstOrDefault(x => x.Id == id);
                    if (user == null)
                    {
                        await context.PostAsync("Selected user not found");
                        await StartAsync(context);
                    }
                    else
                    {
                        user.Role = Role.User;
                        await UsersDal.InsertUserAsync(user);
                        await context.PostAsync("Selected user not admin");
                        context.Reset();
                    }
                    
                }, users.Select(x => x.Id),
                "Select user which you want to unblock", descriptions: users.Select(x => x.Name));
        }

        private async Task BlockUserAsync(IDialogContext context)
        {
            var users = await UsersDal.GetUsersWithSpecificConditionAsync(x => !x.Block);

            PromptDialog.Choice(context, async (IDialogContext innerContext, IAwaitable<string> userId) =>
                {
                    var id = await userId;
                    var user = users.FirstOrDefault(x => x.Id == id);
                    if (user == null)
                    {
                        await context.PostAsync("Selected user not found");
                        await StartAsync(context);
                    }
                    else
                    {
                        user.Block = true;
                        await UsersDal.InsertUserAsync(user);
                        await context.PostAsync("Selected user blocked");
                        context.Reset();
                    }
                    
                }, users.Select(x => x.Id),
                "Select user which you want to unblock", descriptions: users.Select(x => x.Name));
        }

        private async Task AddAdminAsync(IDialogContext context)
        {
            var users = await UsersDal.GetUsersWithSpecificConditionAsync(x => x.Block);

            PromptDialog.Choice(context, async (IDialogContext innerContext, IAwaitable<string> userId) =>
                {
                    var id = await userId;
                    var user = users.FirstOrDefault(x => x.Id == id);
                    if (user == null)
                    {
                        await context.PostAsync("Selected user not found");
                        await StartAsync(context);
                    }
                    else
                    {
                        user.Role = Role.Admin;
                        await UsersDal.InsertUserAsync(user);
                        await context.PostAsync("Selected user is admin");
                        context.Reset();
                    }
                    
                }, users.Select(x => x.Id),
                "Select user which you want to unblock", descriptions: users.Select(x => x.Name));
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