using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using QuikGraph.Utils;
using static QuikGraph.Utils.DisposableHelpers;

namespace QuikGraph.Algorithms.Observers
{
    /// <summary>
    /// Recorder of vertices predecessors paths.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class VertexPredecessorPathRecorderObserver<TVertex, TEdge> :
        IObserver<IVertexPredecessorRecorderAlgorithm<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private readonly EdgeAction<TVertex, TEdge> _onEdgeDiscovered;
        private readonly VertexAction<TVertex> _onVertexFinished;
        private readonly List<TVertex> _endPathVertices;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexPredecessorPathRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        public VertexPredecessorPathRecorderObserver()
            : this(new Dictionary<TVertex, TEdge>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexPredecessorPathRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="verticesPredecessors">Vertices predecessors.</param>
        public VertexPredecessorPathRecorderObserver(
            [NotNull] IDictionary<TVertex, TEdge> verticesPredecessors)
        {
            VerticesPredecessors = verticesPredecessors ?? throw new ArgumentNullException(nameof(verticesPredecessors));
            _onEdgeDiscovered = OnEdgeDiscovered;
            _onVertexFinished = OnVertexFinished;
            _endPathVertices = new List<TVertex>(verticesPredecessors.Count);
        }

        /// <summary>
        /// Vertices predecessors.
        /// </summary>
        [NotNull]
        public IDictionary<TVertex, TEdge> VerticesPredecessors { get; }

        /// <summary>
        /// Path ending vertices.
        /// </summary>
        [NotNull, ItemNotNull]
        public ICollection<TVertex> EndPathVertices => _endPathVertices;

        /// <summary>
        /// Gets all paths.
        /// </summary>
        /// <returns>Enumerable of paths.</returns>
        [Pure]
        [NotNull, ItemNotNull]
        public IEnumerable<IEnumerable<TEdge>> AllPaths()
        {
            return _endPathVertices
                .Select(vertex =>
                {
                    if (VerticesPredecessors.TryGetPath(vertex, out IEnumerable<TEdge> path))
                        return path;
                    return null;
                })
                .Where(path => path != null);
        }

        #region IObserver<TAlgorithm>

        /// <inheritdoc />
        IDisposable IObserver<IVertexPredecessorRecorderAlgorithm<TVertex, TEdge>>.Attach(
            IVertexPredecessorRecorderAlgorithm<TVertex, TEdge> algorithm
        )
        {
            return Attach(algorithm);
        }

        /// <inheritdoc cref="Attach(QuikGraph.Algorithms.IVertexPredecessorRecorderAlgorithm{TVertex,TEdge})"/>
        public FinallyScope Attach(IVertexPredecessorRecorderAlgorithm<TVertex, TEdge> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            algorithm.TreeEdge += _onEdgeDiscovered;
            algorithm.FinishVertex += _onVertexFinished;
            return Finally(() =>
            {
                algorithm.TreeEdge -= _onEdgeDiscovered;
                algorithm.FinishVertex -= _onVertexFinished;
            });
        }

        #endregion

        private void OnEdgeDiscovered([NotNull] TEdge edge)
        {
            Debug.Assert(edge != null);

            VerticesPredecessors[edge.Target] = edge;
        }

        private void OnVertexFinished([NotNull] TVertex vertex)
        {
            Debug.Assert(vertex != null);

            foreach (TEdge edge in VerticesPredecessors.Values)
            {
                if (EqualityComparer<TVertex>.Default.Equals(edge.Source, vertex))
                    return;
            }

            _endPathVertices.Add(vertex);
        }
    }
}