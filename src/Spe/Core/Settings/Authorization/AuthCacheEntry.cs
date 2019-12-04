using System;

namespace Spe.Core.Settings.Authorization
{
    internal class AuthCacheEntry
    {
        internal bool Authorized { get; set; }
        internal DateTime ExpirationDate { get; set; }
    }
}