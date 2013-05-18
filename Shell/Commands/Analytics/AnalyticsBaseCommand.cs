using System.Configuration;
using System.Data.EntityClient;
using System.Data.Metadata.Edm;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Cognifide.PowerShell.Shell.Commands.Analytics
{
    public class AnalyticsBaseCommand : BaseCommand
    {
        private static analytics _context;

        protected static analytics Context
        {
            get
            {
                if (_context == null)
                {
                    var workspace = new MetadataWorkspace(
                        new[] {"res://*/"}, new[] {Assembly.GetExecutingAssembly()});
                    string connString = ConfigurationManager.ConnectionStrings["analytics"].ConnectionString;
                    var sqlConnection = new SqlConnection(connString);
                    var entityConnection = new EntityConnection(workspace, sqlConnection);
                    _context = new analytics(entityConnection);
                }
                return _context;
            }
        }

        protected virtual void PipeQuery<T>(IQueryable<T> query)
        {
            if (query != null)
            {
                foreach (T result in query)
                {
                    WriteObject(result);
                }
            }
        }
    }
}