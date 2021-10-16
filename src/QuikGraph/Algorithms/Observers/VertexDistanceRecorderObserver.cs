using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace QuikGraph.Algorithms.Observers
{
    /// <summary>
    /// A distance recorder for <see cref="ITreeBuilderAlgorithm{TVertex,TEdge}"/> algorithms.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
    public sealed class VertexDistanceRecorderObserver<TVertex, TEdge> : IObserver<ITreeBuilderAlgorithm<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private readonly EdgeAction<TVertex, TEdge> _onEdgeDiscovered;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDistanceRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="edgeWeights">Function that computes the weight for a given edge.</param>
        public VertexDistanceRecorderObserver([NotNull] Func<TEdge, double> edgeWeights)
            : this(edgeWeights, DistanceRelaxers.EdgeShortestDistance, new Dictionary<TVertex, double>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDistanceRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="edgeWeights">Function that computes the weight for a given edge.</param>
        /// <param name="distanceRelaxer">Distance relaxer.</param>
        /// <param name="distances">Distances per vertex.</param>
        public VertexDistanceRecorderObserver(
            [NotNull] Func<TEdge, double> edgeWeights,
            [NotNull] IDistanceRelaxer distanceRelaxer,
            [NotNull] IDictionary<TVertex, double> distances)
        {
            EdgeWeights = edgeWeights ?? throw new ArgumentNullException(nameof(edgeWeights));
            DistanceRelaxer = distanceRelaxer ?? throw new ArgumentNullException(nameof(distanceRelaxer));
            Distances = distances ?? throw new ArgumentNullException(nameof(distances));
            _onEdgeDiscovered = OnEdgeDiscovered;
        }

        /// <summary>
        /// Distance relaxer.
        /// </summary>
        [NotNull]
        public IDistanceRelaxer DistanceRelaxer { get; }

        /// <summary>
        /// Function that computes the weight for a given edge.
        /// </summary>
        [NotNull]
        public Func<TEdge, double> EdgeWeights { get; }

        /// <summary>
        /// Distances per vertex.
        /// </summary>
        [NotNull]
        public IDictionary<TVertex, double> Distances { get; }

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

            if (!Distances.TryGetValue(edge.Source, out double sourceDistance))
                Distances[edge.Source] = sourceDistance = DistanceRelaxer.InitialDistance;
            Distances[edge.Target] = DistanceRelaxer.Combine(sourceDistance, EdgeWeights(edge));
        }

        /// <inheritdoc cref="EdgePredecessorRecorderObserver{TVertex,TEdge}.AttachScope"/>
        public struct AttachScope : IDisposable
        {
            private readonly ITreeBuilderAlgorithm<TVertex, TEdge> _algorithm;
            private readonly VertexDistanceRecorderObserver<TVertex, TEdge> _observer;

            internal AttachScope(
                ITreeBuilderAlgorithm<TVertex, TEdge> algorithm,
                VertexDistanceRecorderObserver<TVertex, TEdge> observer
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