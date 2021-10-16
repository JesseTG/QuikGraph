using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace QuikGraph.Algorithms.Observers
{
    /// <summary>
    /// Recorder of vertices discover timestamps.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class VertexTimeStamperObserver<TVertex> : IObserver<IVertexTimeStamperAlgorithm<TVertex>>
    {
        private readonly VertexAction<TVertex> _onVertexDiscovered;
        private readonly VertexAction<TVertex> _onVertexFinished;
        private int _currentTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexTimeStamperObserver{TVertex}"/> class.
        /// </summary>
        public VertexTimeStamperObserver()
            : this(new Dictionary<TVertex, int>(), new Dictionary<TVertex, int>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexTimeStamperObserver{TVertex}"/> class.
        /// </summary>
        /// <param name="discoverTimes">Vertices discover times.</param>
        public VertexTimeStamperObserver([NotNull] IDictionary<TVertex, int> discoverTimes)
        {
            DiscoverTimes = discoverTimes ?? throw new ArgumentNullException(nameof(discoverTimes));
            FinishTimes = null;
            _onVertexDiscovered = OnVertexDiscovered;
            _onVertexFinished = OnVertexFinished;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexTimeStamperObserver{TVertex}"/> class.
        /// </summary>
        /// <param name="discoverTimes">Vertices discover times.</param>
        /// <param name="finishTimes">Vertices fully treated times.</param>
        public VertexTimeStamperObserver(
            [NotNull] IDictionary<TVertex, int> discoverTimes,
            [NotNull] IDictionary<TVertex, int> finishTimes)
        {
            DiscoverTimes = discoverTimes ?? throw new ArgumentNullException(nameof(discoverTimes));
            FinishTimes = finishTimes ?? throw new ArgumentNullException(nameof(finishTimes));
            _onVertexDiscovered = OnVertexDiscovered;
            _onVertexFinished = OnVertexFinished;
        }

        /// <summary>
        /// Times of vertices discover.
        /// </summary>
        [NotNull]
        public IDictionary<TVertex, int> DiscoverTimes { get; }

        /// <summary>
        /// Times of vertices fully treated.
        /// </summary>
        [CanBeNull]
        public IDictionary<TVertex, int> FinishTimes { get; }

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

            DiscoverTimes[vertex] = _currentTime++;
        }

        private void OnVertexFinished([NotNull] TVertex vertex)
        {
            Debug.Assert(vertex != null);

            // ReSharper disable once PossibleNullReferenceException, Justification: Not null if the handler is attached
            FinishTimes[vertex] = _currentTime++;
        }

        /// <inheritdoc cref="EdgePredecessorRecorderObserver{TVertex,TEdge}.AttachScope"/>
        public struct AttachScope : IDisposable
        {
            private readonly IVertexTimeStamperAlgorithm<TVertex> _algorithm;
            private readonly VertexTimeStamperObserver<TVertex> _observer;

            internal AttachScope(
                IVertexTimeStamperAlgorithm<TVertex> algorithm,
                VertexTimeStamperObserver<TVertex> observer
            )
            {
                Debug.Assert(algorithm != null);
                Debug.Assert(observer != null);

                _algorithm = algorithm;
                _observer = observer;

                algorithm.DiscoverVertex += _observer._onVertexDiscovered;
                if (observer.FinishTimes != null)
                    algorithm.FinishVertex += _observer._onVertexFinished;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (_algorithm != null)
                {
                    _algorithm.DiscoverVertex -= _observer._onVertexDiscovered;
                    if (_observer?.FinishTimes != null)
                        _algorithm.FinishVertex -= _observer._onVertexFinished;
                }
            }
        }
    }
}