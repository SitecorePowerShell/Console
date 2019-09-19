using System;
using Sitecore.Pipelines.PreprocessRequest;
using Sitecore.Shell.Applications.WebEdit;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeObsoletor : IObsoletor
    {
        public Uri GetRequestUrl(PreprocessRequestArgs args)
        {
            return args.Context.Request.Url;
        }

        public void SetPageEditorValues(string handle)
        {
            PageEditFieldEditorOptions.Parse(handle).SetPageEditorFieldValues();
        }
    }
}
