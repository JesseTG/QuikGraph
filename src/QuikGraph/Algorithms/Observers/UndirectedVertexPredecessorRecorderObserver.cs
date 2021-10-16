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
    public sealed class UndirectedVertexPredecessorRecorderObserver<TVertex, TEdge> :
        IObserver<IUndirectedTreeBuilderAlgorithm<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private readonly UndirectedEdgeAction<TVertex, TEdge> _onEdgeDiscovered;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndirectedVertexPredecessorRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        public UndirectedVertexPredecessorRecorderObserver()
            : this(new Dictionary<TVertex, TEdge>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndirectedVertexPredecessorRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="verticesPredecessors">Vertices predecessors.</param>
        public UndirectedVertexPredecessorRecorderObserver(
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
        IDisposable IObserver<IUndirectedTreeBuilderAlgorithm<TVertex, TEdge>>.Attach(
            IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> algorithm
        )
        {
            return Attach(algorithm);
        }

        /// <inheritdoc cref="Attach(QuikGraph.Algorithms.IUndirectedTreeBuilderAlgorithm{TVertex,TEdge})"/>
        public AttachScope Attach(IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            return new AttachScope(algorithm, this);
        }

        #endregion

        private void OnEdgeDiscovered([NotNull] object sender, [NotNull] UndirectedEdgeEventArgs<TVertex, TEdge> args)
        {
            Debug.Assert(sender != null);
            Debug.Assert(args != null);

            VerticesPredecessors[args.Target] = args.Edge;
        }

        /// <summary>
        /// Tries to get the predecessor path, if reachable.
        /// </summary>
        /// <param name="vertex">Path ending vertex.</param>
        /// <param name="path">Path to the ending vertex.</param>
        /// <returns>True if a path was found, false otherwise.</returns>
        [Pure]
        public bool TryGetPath(TVertex vertex, out IEnumerable<TEdge> path)
        {
            return VerticesPredecessors.TryGetPath(vertex, out path);
        }

        /// <inheritdoc cref="EdgePredecessorRecorderObserver{TVertex,TEdge}.AttachScope"/>
        public struct AttachScope : IDisposable
        {
            private readonly IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> _algorithm;
            private readonly UndirectedVertexPredecessorRecorderObserver<TVertex, TEdge> _observer;

            internal AttachScope(
                IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> algorithm,
                UndirectedVertexPredecessorRecorderObserver<TVertex, TEdge> observer
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