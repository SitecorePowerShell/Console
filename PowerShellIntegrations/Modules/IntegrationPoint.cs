namespace Cognifide.PowerShell.PowerShellIntegrations.Modules
{
    public class IntegrationPoint
    {
        public string Id { get; internal set; }
        public string Path { get; internal set; }
        public string Name { get; internal set; }
        public string CreationScript { get; internal set; }
    }
}