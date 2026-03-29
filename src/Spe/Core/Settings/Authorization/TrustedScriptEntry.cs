using System;
using System.Collections.Generic;
using Sitecore.Data;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Represents a trusted script item in the trust registry.
    /// Trust is based on the script item's Sitecore GUID (immutable identity)
    /// and a SHA256 content hash (integrity verification).
    /// </summary>
    public class TrustedScriptEntry
    {
        public string Name { get; }
        public ID ItemId { get; }
        public string ContentHash { get; }
        public ScriptTrustLevel Trust { get; }
        public bool AllowTopLevel { get; }
        public HashSet<string> Exports { get; }
        public HashMismatchAction OnHashMismatch { get; }

        public TrustedScriptEntry(
            string name,
            ID itemId,
            string contentHash,
            ScriptTrustLevel trust,
            bool allowTopLevel,
            HashSet<string> exports,
            HashMismatchAction onHashMismatch)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ItemId = itemId;
            ContentHash = contentHash;
            Trust = trust;
            AllowTopLevel = allowTopLevel;
            Exports = exports ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            OnHashMismatch = onHashMismatch;
        }
    }

    public enum ScriptTrustLevel
    {
        /// <summary>
        /// Runs under the caller's language mode and command restrictions.
        /// </summary>
        Untrusted,

        /// <summary>
        /// Can use .NET types and CLM bypass. No reflection access.
        /// </summary>
        Trusted,

        /// <summary>
        /// Reflection and private member access allowed. Config-only assignment.
        /// </summary>
        System
    }

    public enum HashMismatchAction
    {
        /// <summary>
        /// Run the script under the caller's constrained mode (safe default).
        /// </summary>
        Constrain,

        /// <summary>
        /// Refuse to execute entirely.
        /// </summary>
        Block,

        /// <summary>
        /// Log a warning but allow execution with trust (for upgrades).
        /// </summary>
        Warn
    }
}
