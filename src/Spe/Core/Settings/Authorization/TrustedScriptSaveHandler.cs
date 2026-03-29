using System;
using System.Web;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Event handler for item:saved that invalidates security caches
    /// when remoting security items are created or modified.
    /// </summary>
    public class TrustedScriptSaveHandler
    {
        internal void OnItemSaved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            if (item == null) return;

            if (item.TemplateID == Templates.TrustedScript.Id)
            {
                ScriptTrustRegistry.Invalidate();
            }
            else if (item.TemplateID == Templates.RemotingApiKey.Id)
            {
                // Clear the cached API Key list so changes take effect immediately
                HttpRuntime.Cache.Remove("Spe.RemotingApiKeys");
            }
        }
    }
}
