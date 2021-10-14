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
    /// Recorder of encountered edges.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class EdgeRecorderObserver<TVertex, TEdge> : IObserver<ITreeBuilderAlgorithm<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        public EdgeRecorderObserver()
        {
            _edges = new List<TEdge>();
            _onEdgeDiscovered = OnEdgeDiscovered;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="edges">Set of edges.</param>
        public EdgeRecorderObserver([NotNull, ItemNotNull] IEnumerable<TEdge> edges)
        {
            if (edges is null)
                throw new ArgumentNullException(nameof(edges));

            _edges = edges.ToList();
            _onEdgeDiscovered = OnEdgeDiscovered;
        }

        [NotNull, ItemNotNull]
        private readonly List<TEdge> _edges;

        private readonly EdgeAction<TVertex, TEdge> _onEdgeDiscovered;

        /// <summary>
        /// Encountered edges.
        /// </summary>
        [NotNull, ItemNotNull]
        public IEnumerable<TEdge> Edges => _edges;

        #region IObserver<TAlgorithm>

        /// <inheritdoc />
        IDisposable IObserver<ITreeBuilderAlgorithm<TVertex, TEdge>>.Attach(ITreeBuilderAlgorithm<TVertex, TEdge> algorithm)
        {
            return Attach(algorithm);
        }

        /// <inheritdoc cref="Attach(QuikGraph.Algorithms.ITreeBuilderAlgorithm{TVertex,TEdge})"/>
        public FinallyScope Attach(ITreeBuilderAlgorithm<TVertex, TEdge> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            algorithm.TreeEdge += _onEdgeDiscovered;
            return Finally(() => algorithm.TreeEdge -= _onEdgeDiscovered);
        }

        #endregion

        private void OnEdgeDiscovered([NotNull] TEdge edge)
        {
            Debug.Assert(edge != null);

            _edges.Add(edge);
        }
    }
}