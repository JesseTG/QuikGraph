using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using QuikGraph.Utils;
using static QuikGraph.Utils.DisposableHelpers;

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
        public FinallyScope Attach(IVertexTimeStamperAlgorithm<TVertex> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            algorithm.DiscoverVertex += _onVertexDiscovered;
            if (FinishTimes != null)
                algorithm.FinishVertex += _onVertexFinished;

            return Finally(() =>
            {
                algorithm.DiscoverVertex -= _onVertexDiscovered;
                if (FinishTimes != null)
                    algorithm.FinishVertex -= _onVertexFinished;
            });
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
    }
}