using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

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

            _edges.Add(edge);
        }

        /// <inheritdoc cref="EdgePredecessorRecorderObserver{TVertex,TEdge}.AttachScope"/>
        public struct AttachScope : IDisposable
        {
            private readonly ITreeBuilderAlgorithm<TVertex, TEdge> _algorithm;
            private readonly EdgeRecorderObserver<TVertex, TEdge> _observer;

            internal AttachScope(
                ITreeBuilderAlgorithm<TVertex, TEdge> algorithm,
                EdgeRecorderObserver<TVertex, TEdge> observer
            )
            {
                Debug.Assert(algorithm != null);
                Debug.Assert(observer != null);

                _algorithm = algorithm;
                _observer = observer;

                _algorithm.TreeEdge += observer._onEdgeDiscovered;
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