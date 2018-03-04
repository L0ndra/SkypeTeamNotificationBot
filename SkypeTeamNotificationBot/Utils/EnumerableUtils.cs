using System;

namespace SkypeTeamNotificationBot.Utils
{
    public static class EnumerableUtils
    {
        public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            if (memInfo.Length <= 0) { return null; }
            var attributes = memInfo[0].GetCustomAttributes(typeof (T), false);
            return (attributes.Length > 0) ? (T) attributes[0] : null;
        }

        public static TV GetAttributeValue<TA, TV>(this Enum enumVal, Func<TA, TV> valueProvider, TV defaultValue = default(TV)) where TA : Attribute
        {
            var attr = enumVal.GetAttributeOfType<TA>();
            return attr == null ? defaultValue : valueProvider(attr);
        }
    }
}