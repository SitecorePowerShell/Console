using System;
using Sitecore.Pipelines.PreprocessRequest;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IObsoletor
    {
        Uri GetRequestUrl(PreprocessRequestArgs args);
    }
}
