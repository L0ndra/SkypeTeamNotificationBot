using System.ComponentModel;

namespace SkypeTeamNotificationBot.DataModels
{
    public enum AdminOptions
    {
        [Description("Send message to all users")]
        SendMessage,
        [Description("Exclude user from dialog receivers")]
        BlockUser,
        [Description("Include user to dialog receivers")]
        UnblockUser,
        [Description("Promote user to admin role")]
        AddAdmin,
        [Description("Demote user from admin role")]
        RemoveAdmin
    }
}