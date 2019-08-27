using System;
using Sitecore.Pipelines.PreprocessRequest;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeObsoletor : IObsoletor
    {
        public Uri GetRequestUrl(PreprocessRequestArgs args)
        {
            return args.HttpContext.Request.Url;
        }
    }
}
