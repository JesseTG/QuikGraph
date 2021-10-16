using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace QuikGraph.Algorithms.Observers
{
    /// <summary>
    /// Recorder of vertices predecessors (undirected).
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class VertexPredecessorRecorderObserver<TVertex, TEdge> : IObserver<ITreeBuilderAlgorithm<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private readonly EdgeAction<TVertex, TEdge> _onEdgeDiscovered;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexPredecessorRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        public VertexPredecessorRecorderObserver()
            : this(new Dictionary<TVertex, TEdge>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexPredecessorRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="verticesPredecessors">Vertices predecessors.</param>
        public VertexPredecessorRecorderObserver(
            [NotNull] IDictionary<TVertex, TEdge> verticesPredecessors)
        {
            VerticesPredecessors = verticesPredecessors ?? throw new ArgumentNullException(nameof(verticesPredecessors));
            _onEdgeDiscovered = OnEdgeDiscovered;
        }

        /// <summary>
        /// Vertices predecessors.
        /// </summary>
        [NotNull]
        public IDictionary<TVertex, TEdge> VerticesPredecessors { get; }

        #region IObserver<TAlgorithm>

        /// <inheritdoc />
        IDisposable IObserver<ITreeBuilderAlgorithm<TVertex, TEdge>>.Attach(
            ITreeBuilderAlgorithm<TVertex, TEdge> algorithm
        )
        {
            return Attach(algorithm);
        }

        /// <inheritdoc cref="Attach(QuikGraph.Algorithms.ITreeBuilderAlgorithm{TVertex,TEdge})"/>
        public AttachScope Attach(ITreeBuilderAlgorithm<TVertex, TEdge> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            return new AttachScope(algorithm, this);
        }

        #endregion

        private void OnEdgeDiscovered([NotNull] TEdge edge)
        {
            Debug.Assert(edge != null);

            VerticesPredecessors[edge.Target] = edge;
        }

        /// <summary>
        /// Tries to get the predecessor path, if reachable.
        /// </summary>
        /// <param name="vertex">Path ending vertex.</param>
        /// <param name="path">Path to the ending vertex.</param>
        /// <returns>True if a path was found, false otherwise.</returns>
        [Pure]
        public bool TryGetPath([NotNull] TVertex vertex, out IEnumerable<TEdge> path)
        {
            return VerticesPredecessors.TryGetPath(vertex, out path);
        }

        /// <inheritdoc cref="EdgePredecessorRecorderObserver{TVertex,TEdge}.AttachScope"/>
        public struct AttachScope : IDisposable
        {
            private readonly ITreeBuilderAlgorithm<TVertex, TEdge> _algorithm;
            private readonly VertexPredecessorRecorderObserver<TVertex, TEdge> _observer;

            internal AttachScope(
                ITreeBuilderAlgorithm<TVertex, TEdge> algorithm,
                VertexPredecessorRecorderObserver<TVertex, TEdge> observer
            )
            {
                Debug.Assert(algorithm != null);
                Debug.Assert(observer != null);

                _algorithm = algorithm;
                _observer = observer;

                _algorithm.TreeEdge += _observer._onEdgeDiscovered;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (_algorithm != null && _observer != null)
                {
                    _algorithm.TreeEdge -= _observer._onEdgeDiscovered;
                }
            }
        }
    }
}