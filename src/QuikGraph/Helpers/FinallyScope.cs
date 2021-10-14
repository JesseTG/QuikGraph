using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace QuikGraph.Utils
{
    /// <summary>
    ///
    /// </summary>
    public struct FinallyScope : IDisposable
    {
        private Action _action;

        /// <summary>
        ///
        /// </summary>
        /// <param name="action">The action to call upon disposal of this scope.</param>
        public FinallyScope([NotNull] Action action)
        {
            Debug.Assert(action != null);

            _action = action;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
        }
    }
}