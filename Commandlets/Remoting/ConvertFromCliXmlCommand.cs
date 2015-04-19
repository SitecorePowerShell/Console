using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Xml;

namespace Cognifide.PowerShell.Commandlets.Remoting
{
    [Cmdlet(VerbsData.ConvertFrom, "CliXml", SupportsShouldProcess = false)]
    public class ConvertFromCliXmlCommand : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true), AllowNull]
        public string InputObject { get; set; }

        protected override void ProcessRecord()
        {
            const BindingFlags commonBindings = BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags methodBindings = BindingFlags.InvokeMethod | commonBindings;
            var type = typeof(PSObject).Assembly.GetType("System.Management.Automation.Deserializer");
            var ctor = type.GetConstructor(commonBindings, null, new[] { typeof(XmlReader) }, null);

            using (var sr = new StringReader(InputObject))
            {
                using (var xr = new XmlTextReader(sr))
                {
                    var deserializer = ctor.Invoke(new object[] { xr });
                    while (!(bool)type.InvokeMember("Done", methodBindings, null, deserializer, new object[] { }))
                    {
                        try
                        {
                            WriteObject(type.InvokeMember("Deserialize", methodBindings, null, deserializer, new object[] { }));
                        }
                        catch (Exception ex)
                        {
                            WriteWarning("Could not deserialize string. Exception: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}