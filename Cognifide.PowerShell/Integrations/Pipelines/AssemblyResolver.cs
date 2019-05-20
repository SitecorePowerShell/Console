using System;
using System.Reflection;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Pipelines;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class AssemblyResolver
    {
        public void Process(PipelineArgs args)
        {
            var assemblyNamespace = Assembly.GetExecutingAssembly().GetName().Name;
            var assemblyName = $"{assemblyNamespace}.VersionSpecific";
            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                if (eventArgs.Name.Contains(","))
                {
                    if (eventArgs.Name.Substring(0, eventArgs.Name.IndexOf(",", StringComparison.Ordinal)) != assemblyName)
                        return null;
                }
                else
                {
                    if (string.Compare(eventArgs.Name, assemblyName, StringComparison.OrdinalIgnoreCase) != 0)
                        return null;
                }

                var resourceName = CurrentVersion.IsAtLeast(SitecoreVersion.V92) ? 
                    $"{assemblyNamespace}.Resources.Version92." + assemblyName + ".dll" : 
                    $"{assemblyNamespace}.Resources.Version8." + assemblyName + ".dll";

                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream == null) return null;

                using (stream)
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return Assembly.Load(data);
                }
            };
        }
    }
}