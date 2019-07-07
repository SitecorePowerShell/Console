using System;
using Sitecore.Pipelines.PreprocessRequest;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeObsoleter : IObsoleter
    {
        public Uri GetRequestUrl(PreprocessRequestArgs args)
        {
            return args.Context.Request.Url;
        }

        public bool IndexingEnabled
        {
            get => Sitecore.Configuration.Settings.Indexing.Enabled;
            set => Sitecore.Configuration.Settings.Indexing.Enabled = value;
        }
    }
}
