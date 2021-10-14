using System;
using JetBrains.Annotations;

namespace QuikGraph.Utils
{
    /// <summary>
    /// Helpers to work with <see cref="IDisposable"/>.
    /// </summary>
    public static class DisposableHelpers
    {
        /// <summary>
        /// Calls an action when going out of scope.
        /// </summary>
        /// <param name="action">The action to call.</param>
        /// <returns>A <see cref="IDisposable"/> object to give to a using clause.</returns>
        [Pure]
        public static FinallyScope Finally([NotNull] Action action)
        {
            return new FinallyScope(action);
        }
    }
}