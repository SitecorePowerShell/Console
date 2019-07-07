using System;
using Sitecore.Pipelines.PreprocessRequest;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IObsoleter
    {
        Uri GetRequestUrl(PreprocessRequestArgs args);

        [Obsolete("This feature is no longer active after Sitecore 9.2.")]
        bool IndexingEnabled { get; set; }
    }
}
