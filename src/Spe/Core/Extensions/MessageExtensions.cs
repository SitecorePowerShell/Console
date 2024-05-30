using System.Linq;
using Sitecore.Web.UI.Sheer;

namespace Spe.Core.Extensions
{
    public static class MessageExtensions
    {
        public static string Serialize(this Message message)
        {
            if (message == null)
            {
                return string.Empty;
            }

            if (message.Arguments != null && message.Arguments.Count > 0)
            {
                var paramsArray = message.Arguments.AllKeys.Select<string, string>(key => $"{key}={message.Arguments[key]}").ToArray();
                var result = 
                    $"{message.Name}({string.Join(",", paramsArray)})";
                 return result;
            }

            return message.Name;
        }
    }
}