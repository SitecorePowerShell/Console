using System;
using Sitecore.Pipelines.PreprocessRequest;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IObsoleter
    {
        Uri GetRequestUrl(PreprocessRequestArgs args);
    }
}
