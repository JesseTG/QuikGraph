using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace QuikGraph.Algorithms.Observers
{
    /// <summary>
    /// Recorder of encountered vertices.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class VertexRecorderObserver<TVertex> : IObserver<IVertexTimeStamperAlgorithm<TVertex>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VertexRecorderObserver{TVertex}"/> class.
        /// </summary>
        public VertexRecorderObserver()
        {
            _vertices = new List<TVertex>();
            _onVertexDiscovered = OnVertexDiscovered;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexRecorderObserver{TVertex}"/> class.
        /// </summary>
        /// <param name="vertices">Set of vertices.</param>
        public VertexRecorderObserver([NotNull, ItemNotNull] IEnumerable<TVertex> vertices)
        {
            if (vertices is null)
                throw new ArgumentNullException(nameof(vertices));

            _vertices = vertices.ToList();
            _onVertexDiscovered = OnVertexDiscovered;
        }

        [NotNull, ItemNotNull]
        private readonly List<TVertex> _vertices;

        [NotNull]
        private readonly VertexAction<TVertex> _onVertexDiscovered;

        /// <summary>
        /// Encountered vertices.
        /// </summary>
        [NotNull, ItemNotNull]
        public IEnumerable<TVertex> Vertices => _vertices;

        #region IObserver<TAlgorithm>

        /// <inheritdoc />
        IDisposable IObserver<IVertexTimeStamperAlgorithm<TVertex>>.Attach(
            IVertexTimeStamperAlgorithm<TVertex> algorithm
        )
        {
            return Attach(algorithm);
        }

        /// <inheritdoc cref="Attach(QuikGraph.Algorithms.IVertexTimeStamperAlgorithm{TVertex})"/>
        public AttachScope Attach(IVertexTimeStamperAlgorithm<TVertex> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            return new AttachScope(algorithm, this);
        }

        #endregion

        private void OnVertexDiscovered([NotNull] TVertex vertex)
        {
            Debug.Assert(vertex != null);

            _vertices.Add(vertex);
        }

        /// <inheritdoc cref="EdgePredecessorRecorderObserver{TVertex,TEdge}.AttachScope"/>
        public struct AttachScope : IDisposable
        {
            private readonly IVertexTimeStamperAlgorithm<TVertex> _algorithm;
            private readonly VertexRecorderObserver<TVertex> _observer;

            internal AttachScope(
                IVertexTimeStamperAlgorithm<TVertex> algorithm,
                VertexRecorderObserver<TVertex> observer
            )
            {
                Debug.Assert(algorithm != null);
                Debug.Assert(observer != null);

                _algorithm = algorithm;
                _observer = observer;

                _algorithm.DiscoverVertex += _observer._onVertexDiscovered;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (_algorithm != null && _observer != null)
                {
                    _algorithm.DiscoverVertex -= _observer._onVertexDiscovered;
                }
            }
        }
    }
}