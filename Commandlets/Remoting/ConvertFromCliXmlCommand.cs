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
        private MethodInfo method;
        private MethodInfo done;
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true), AllowNull]
        public string InputObject { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var reader = new StringReader(InputObject);
                var xmlReader = XmlReader.Create(reader);
                Type type = typeof(PSObject).Assembly.GetType("System.Management.Automation.Deserializer");
                var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(XmlReader) }, null);
                var deserializer = ctor.Invoke(new object[] { xmlReader });
                method = type.GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new Type[] {}, null);
                done = type.GetMethod("Done", BindingFlags.Instance | BindingFlags.NonPublic);
                try
                {
                    while (!(bool) done.Invoke(deserializer, new object[] {}))
                    {
                        WriteObject(method.Invoke(deserializer, new object[] {}));
                    }
                }
                finally
                {
                    xmlReader.Close();
                    reader.Close();
                }
            }
            catch(Exception ex)
            {
                WriteWarning("Could not deserialize string. Exception: " + ex.Message);
            }
        }
    }
}