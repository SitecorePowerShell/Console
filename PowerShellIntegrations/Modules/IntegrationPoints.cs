using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.PowerShellIntegrations.Modules
{
    public static class IntegrationPoints
    {
        private static SortedList<string,string> libraries = null;

        public static SortedList<string, string> Libraries
        {
            get
            {
                if (libraries == null)
                {
                    libraries = new SortedList<string, string>();
                    var ipNode = Factory.GetConfigNode("powershell/integrationPoints");
                    if (ipNode == null)
                    {
                        return libraries;
                    }

                    var allIntegrationPoints = ipNode.Cast<XmlNode>().ToList();

                    foreach (var integrationPoint in allIntegrationPoints)
                    {
                        libraries.Add(integrationPoint.Name, integrationPoint.InnerText);
                    }
                }
                return libraries;
            }
        }
    }
}