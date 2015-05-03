using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Cognifide.PowerShell.Commandlets.Remoting
{
    [Cmdlet(VerbsData.ConvertTo, "CliXml", SupportsShouldProcess = false)]
    [OutputType(typeof(string))]
    public class ConvertToCliXmlCommand : PSCmdlet
    {
        private StringBuilder builder;
        private MethodInfo done;
        private MethodInfo method;
        private object serializer;
        private XmlWriter xmlWriter;

        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true), AllowNull]
        public PSObject InputObject { get; set; }

        protected override void BeginProcessing()
        {
            const BindingFlags commonBindings = BindingFlags.NonPublic | BindingFlags.Instance;

            builder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                CloseOutput = true,
                Encoding = Encoding.UTF8,
                Indent = false,
                OmitXmlDeclaration = true
            };
            xmlWriter = XmlWriter.Create(builder, settings);
            var type = typeof(PSObject).Assembly.GetType("System.Management.Automation.Serializer");
            var ctor = type.GetConstructor(commonBindings, null, new[] { typeof(XmlWriter) }, null);
            serializer = ctor.Invoke(new object[] { xmlWriter });
            method = type.GetMethod("Serialize", commonBindings, null, new[] { typeof(object) }, null);
            done = type.GetMethod("Done", commonBindings);
        }

        protected override void ProcessRecord()
        {
            try
            {
                method.Invoke(serializer, new object[] { InputObject });
            }
            catch
            {
                WriteWarning("Could not serialize $($InputObject.gettype()): $_");
            }
        }

        protected override void EndProcessing()
        {
            done.Invoke(serializer, new object[] { });
            WriteObject(builder.ToString());
            xmlWriter.Close();
        }
    }
}