using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Security;
using System.Web.Services;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Jobs;
using Sitecore.Security.Accounts;
using LicenseManager = Sitecore.SecurityModel.License.LicenseManager;

namespace Cognifide.PowerShell.Console.Services
{
	/// <summary>
	///     Summary description for PowerShellWebService
	/// </summary>
	[WebService(Namespace = "http://cognifide.powershell.com")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	[ScriptService]
	public class PowerShellWebService : WebService
	{
		public const string StatusComplete = "complete";
		public const string StatusPartial = "partial";
		public const string StatusWorking = "working";
		public const string StatusError = "error";

		[WebMethod(EnableSession = true)]
		public void LoginUser(string userName, string password)
		{
			if (!userName.Contains("\\"))
			{
				userName = "sitecore\\" + userName;
			}
			if (Sitecore.Context.IsLoggedIn)
			{
				if (Sitecore.Context.User.Name.Equals(userName, StringComparison.OrdinalIgnoreCase))
					return;
				Sitecore.Context.Logout();
			}
			if (!LicenseManager.HasContentManager && !LicenseManager.HasExpress)
				throw new AccessDeniedException("A required license is missing");
			Assert.IsTrue(Membership.ValidateUser(userName, password), "Unknown username or password.");
			var user = Sitecore.Security.Accounts.User.FromName(userName, true);
			UserSwitcher.Enter(user);
		}

		[WebMethod(EnableSession = true)]
		public object ExecuteRocksCommand(string guid, string command, string username, string password)
		{
			LoginUser(username, password);
			return ExecuteCommand(guid, command, "text");
		}

		[WebMethod(EnableSession = true)]
		public object ExecuteCommand(string guid, string command, string stringFormat)
		{
			var serializer = new JavaScriptSerializer();
			var output = new StringBuilder();

			if (!HttpContext.Current.Request.IsAuthenticated &&
			    !command.StartsWith("login-user", StringComparison.OrdinalIgnoreCase))
			{
				return serializer.Serialize(
					new
						{
							result = "You need to be authenticated to use the PowerShell console. Please login to Sitecore first.",
							prompt = "PS >"
						});
			}

			if (command.StartsWith("recycle-session", StringComparison.OrdinalIgnoreCase))
			{
				RecycleSession(guid);
				return serializer.Serialize(
					new {result = "Session recycled.", prompt = "PS >"});
			}

			var session = GetScriptSession(guid);
			try
			{
				var handle = ID.NewID.ToString();
				var jobOptions = new JobOptions(GetJobID(guid, handle), "PowerShell", "shell", this, "RunJob",
				                                new object[] {session, command})
					{
						AfterLife = new TimeSpan(0, 10, 0),
						ContextUser = Sitecore.Context.User
					};
				JobManager.Start(jobOptions);
				Thread.Sleep(WebServiceSettings.CommandWaitMillis);
				return PollCommandOutput(guid, handle, stringFormat);
			}
			catch (Exception ex)
			{
				return
					serializer.Serialize(
						new Result
							{
								status = StatusError,
								result = output +
								         "\r\n[[;#f00;#000]Ooops, something went wrong... Do you need assistance?]\r\n" +
								         "[[;#f00;#000]Send an email with the stack trace to adam@najmanowicz.com or contact me on Twitter @AdamNaj]\r\n\r\n" +
								         session.GetExceptionConsoleString(ex) + "\r\n",
								prompt = string.Format("PS {0}>", session.CurrentLocation)
							});
			}
		}

		private static ScriptSession GetScriptSession(string guid)
		{
			return ScriptSession.GetScriptSession(ApplicationNames.AjaxConsole, guid);
		}

		[WebMethod(EnableSession = true)]
		protected void RunJob(ScriptSession session, string command)
		{
			try
			{
				session.ExecuteScriptPart(command);
			}
			catch (Exception ex)
			{
				var job = Sitecore.Context.Job;
				if (job != null)
				{
					job.Status.Failed = true;
					job.Status.Messages.Add("Ooops, something went wrong... Do you need assistance?");
					job.Status.Messages.Add("Send an email with the stack trace to adam@najmanowicz.com or contact me on Twitter @AdamNaj");
					job.Status.LogException(ex);
				}
				else
				{
					Log.Error("Script execution failed. Could not find command job.", ex, this);
				}
			}
		}

		[WebMethod(EnableSession = true)]
		[ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
		public object PollCommandOutput(string guid, string handle, string stringFormat)
		{
			var serializer = new JavaScriptSerializer();

			var session = GetScriptSession(guid);
			var result = new Result();
			var scriptJob = JobManager.GetJob(GetJobID(guid, handle));
			if (scriptJob == null)
			{
				result.status = StatusError;
				result.result = "Sorry, can't find your command result. Well this is embarassing. Try again?";
				result.prompt = string.Format("PS {0}>", session.CurrentLocation);
				session.Output.Clear();
				return serializer.Serialize(result);
			}
			if (!scriptJob.IsDone)
			{
				result.status = StatusWorking;
				result.handle = handle;
				return serializer.Serialize(result);
			}
			if (scriptJob.Status.Failed)
			{
				result.status = StatusError;
				var message = string.Join(Environment.NewLine, scriptJob.Status.Messages.Cast<string>().ToArray());
				result.result = "[[;#f00;#000]" + (message.Length > 0 ? message : "Command failed") + "]";
				result.prompt = string.Format("PS {0}>", session.CurrentLocation);
				session.Output.Clear();
				return serializer.Serialize(result);
			}
			result.status = StatusComplete;
			var lines = 0;
			var buffer = WebServiceSettings.SerializationSizeBuffer;
			var partial = false;
			var temp = new StringBuilder();
			var output = new StringBuilder();
			foreach (var outputLine in session.Output)
			{
				outputLine.GetLine(temp, stringFormat);
				if ((output.Length + temp.Length + buffer) > 131072)
				{
					session.Output.RemoveRange(0, lines);
					partial = true;
					break;
				}
				lines++;
				output.Append(temp);
				temp.Remove(0, temp.Length);
			}
			result.result = output.ToString().TrimEnd(new[] {'\r', '\n'});
			result.prompt = string.Format("PS {0}>", session.CurrentLocation);
			if (partial)
			{
				result.status = StatusPartial;
			}
			else
			{
				session.Output.Clear();
			}
			HttpContext.Current.Response.ContentType = "application/json";
			return serializer.Serialize(result);
		}

		[WebMethod(EnableSession = true)]
		[ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
		public string[] CompleteRocksCommand(string guid, string command, string username, string password)
		{
			LoginUser(username, password);
			return GetTabCompletionOutputs(guid, command, false);
		}

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object CompleteAceCommand(string guid, string command)
        {
            var serializer = new JavaScriptSerializer();
            var result = serializer.Serialize(GetTabCompletionOutputs(guid, command, true));
            return result;
        }

        [WebMethod(EnableSession = true)]
		[ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
		public object CompleteCommand(string guid, string command)
		{
			var serializer = new JavaScriptSerializer();
			var result = serializer.Serialize(GetTabCompletionOutputs(guid, command, false));
			return result;
		}

		public static string[] GetTabCompletionOutputs(string guid, string command, bool lastTokenOnly)
		{
			var session = GetScriptSession(guid);
			var result = CommandCompletion.FindMatches(session, command, lastTokenOnly);
			return result.ToArray();
		}


		[WebMethod(EnableSession = true)]
		[ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
		public object GetHelpForCommand(string guid, string command)
		{
			var serializer = new JavaScriptSerializer();
			var result = serializer.Serialize(GetHelpOutputs(guid, command));
			return result;
		}

		public static string[] GetHelpOutputs(string guid, string command)
		{
			var session = GetScriptSession(guid);
			var result = CommandHelp.GetHelp(session, command);
			return result.ToArray();
		}


		protected string GetJobID(string sessionGuid, string handle)
		{
			return "PowerShell-" + sessionGuid + "-" + handle;
		}

		private static void RecycleSession(string guid)
		{
			var session = HttpContext.Current.Session[guid] as ScriptSession;
			if (session != null)
			{
				HttpContext.Current.Session.Remove(guid);
				session.Dispose();
			}
		}

		public class Result
		{
			public string handle;
			public string prompt;
			public string result;
			public string status;
		}
	}
}