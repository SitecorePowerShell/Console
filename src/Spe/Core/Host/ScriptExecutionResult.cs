using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sitecore;
using Sitecore.Web.UI.Sheer;
using Spe.Client.Applications;

namespace Spe.Core.Host
{
    [Serializable]
    public class ScriptExecutionResult
    {
        public ScriptExecutionResult()
        {
        }

        public ScriptExecutionResult(RunnerOutput result)
        {
            ScriptResult = result.DialogResult;
            DeferredMessages = result.DeferredMessages.ToArray();
        }

        public string[] DeferredMessages { get; set; }

        public string ScriptResult { get; set; }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        
        public static ScriptExecutionResult Parse(string json)
        {
            return JsonConvert.DeserializeObject<ScriptExecutionResult>(json);
        }
        
        public void ExecuteResults()
        {
            foreach (var deferredMessage in DeferredMessages)
            {
                switch(deferredMessage)
                {
                    case string message when message.StartsWith("message:"):
                        Context.ClientPage.ClientResponse.Timer(UnpackMessage(message), 2);
                        break;
                    case string message when message.StartsWith("script:"):
                        SheerResponse.Eval(UnpackMessage(message));
                        break;
                    case string message when message.StartsWith("alert:"):
                        SheerResponse.Alert(UnpackMessage(message),false);
                        break;
                    default:
                        Context.ClientPage.ClientResponse.Timer(deferredMessage, 2);
                        break;
                }
            }
        }
        
        private string UnpackMessage(string message)
        {
            return message.Substring(message.IndexOf(":", StringComparison.Ordinal) + 1);
        }
        
        public IEnumerable<string> GetIseResult(bool closeWindow)
        {
            foreach (var deferredMessage in DeferredMessages)
            {
                switch(deferredMessage)
                {
                    case string message when message.StartsWith("message:"):
                        yield return
                            "<span class='deferred message'><span class='label'>Send shell message</span><span class='content'>" +
                            UnpackMessage(message) + "</span></span><br/>";
                        break;
                    case string message when message.StartsWith("script:"):
                        yield return
                            "<span class='deferred script'><span class='label'>Execute JavaScript</span><span class='content'>" +
                            UnpackMessage(message) + "</span></span><br/>";
                        break;
                    case string message when message.StartsWith("alert:"):
                        SheerResponse.Alert(UnpackMessage(message),false);
                        break;
                    default:
                        Context.ClientPage.ClientResponse.Timer(deferredMessage, 2);
                        break;
                }
            }
            if (closeWindow)
            {
                yield return "<span class='deferred close'><span class='label'>Close Runner Window Request</span></span><br/>";
            }
        } 
    }
}