using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace QuikGraph.Algorithms.Observers
{
    /// <summary>
    /// A distance recorder for <see cref="IUndirectedTreeBuilderAlgorithm{TVertex,TEdge}"/> algorithms.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
    public sealed class UndirectedVertexDistanceRecorderObserver<TVertex, TEdge>
        : IObserver<IUndirectedTreeBuilderAlgorithm<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private readonly UndirectedEdgeAction<TVertex, TEdge> _onEdgeDiscovered;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndirectedVertexDistanceRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="edgeWeights">Function that computes the weight for a given edge.</param>
        public UndirectedVertexDistanceRecorderObserver([NotNull] Func<TEdge, double> edgeWeights)
            : this(edgeWeights, DistanceRelaxers.EdgeShortestDistance, new Dictionary<TVertex, double>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndirectedVertexDistanceRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="edgeWeights">Function that computes the weight for a given edge.</param>
        /// <param name="distanceRelaxer">Distance relaxer.</param>
        /// <param name="distances">Distances per vertex.</param>
        public UndirectedVertexDistanceRecorderObserver(
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

        /// <inheritdoc cref="Attach" />
        public AttachScope Attach(IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            return new AttachScope(algorithm, this);
        }

        /// <inheritdoc />
        IDisposable IObserver<IUndirectedTreeBuilderAlgorithm<TVertex, TEdge>>.Attach(
            IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> algorithm
        )
        {
            return Attach(algorithm);
        }

        #endregion

        private void OnEdgeDiscovered([NotNull] object sender, [NotNull] UndirectedEdgeEventArgs<TVertex, TEdge> args)
        {
            Debug.Assert(sender != null);
            Debug.Assert(args != null);

            if (!Distances.TryGetValue(args.Source, out double sourceDistance))
                Distances[args.Source] = sourceDistance = DistanceRelaxer.InitialDistance;
            Distances[args.Target] = DistanceRelaxer.Combine(sourceDistance, EdgeWeights(args.Edge));
        }

        /// <inheritdoc cref="EdgePredecessorRecorderObserver{TVertex,TEdge}.AttachScope"/>
        public struct AttachScope : IDisposable
        {
            private readonly IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> _algorithm;
            private readonly UndirectedVertexDistanceRecorderObserver<TVertex, TEdge> _observer;

            internal AttachScope(
                IUndirectedTreeBuilderAlgorithm<TVertex, TEdge> algorithm,
                UndirectedVertexDistanceRecorderObserver<TVertex, TEdge> observer
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