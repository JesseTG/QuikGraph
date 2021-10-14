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
    /// Recorder of edges predecessors.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class EdgePredecessorRecorderObserver<TVertex, TEdge> : IObserver<IEdgePredecessorRecorderAlgorithm<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private readonly EdgeEdgeAction<TVertex, TEdge> _onEdgeDiscovered;
        private readonly EdgeAction<TVertex, TEdge> _onEdgeFinished;
        private readonly Func<TEdge, ICollection<TEdge>> _path;
        private readonly List<TEdge> _endPathEdges;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgePredecessorRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        public EdgePredecessorRecorderObserver()
            : this(new Dictionary<TEdge, TEdge>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgePredecessorRecorderObserver{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="edgesPredecessors">Edges predecessors.</param>
        public EdgePredecessorRecorderObserver(
            [NotNull] IDictionary<TEdge, TEdge> edgesPredecessors)
        {
            EdgesPredecessors = edgesPredecessors ?? throw new ArgumentNullException(nameof(edgesPredecessors));
            _endPathEdges = new List<TEdge>();
            _onEdgeDiscovered = OnEdgeDiscovered;
            _onEdgeFinished = OnEdgeFinished;
            _path = Path;
        }

        /// <summary>
        /// Edges predecessors.
        /// </summary>
        [NotNull]
        public IDictionary<TEdge, TEdge> EdgesPredecessors { get; }

        /// <summary>
        /// Path ending edges.
        /// </summary>
        [NotNull]
        public ICollection<TEdge> EndPathEdges => _endPathEdges;

        #region IObserver<TAlgorithm>

        /// <inheritdoc />
        IDisposable IObserver<IEdgePredecessorRecorderAlgorithm<TVertex, TEdge>>.Attach(
            IEdgePredecessorRecorderAlgorithm<TVertex, TEdge> algorithm
        )
        {
            return Attach(algorithm);
        }

        /// <inheritdoc cref="Attach(QuikGraph.Algorithms.IEdgePredecessorRecorderAlgorithm{TVertex,TEdge})"/>
        public FinallyScope Attach(IEdgePredecessorRecorderAlgorithm<TVertex, TEdge> algorithm)
        {
            if (algorithm is null)
                throw new ArgumentNullException(nameof(algorithm));

            algorithm.DiscoverTreeEdge += _onEdgeDiscovered;
            algorithm.FinishEdge += _onEdgeFinished;

            return Finally(() =>
            {
                algorithm.DiscoverTreeEdge -= _onEdgeDiscovered;
                algorithm.FinishEdge -= _onEdgeFinished;
            });
        }

        #endregion

        /// <summary>
        /// Gets a path starting with <paramref name="startingEdge"/>.
        /// </summary>
        /// <param name="startingEdge">Starting edge.</param>
        /// <returns>Edge path.</returns>
        [Pure]
        [NotNull, ItemNotNull]
        public ICollection<TEdge> Path([NotNull] TEdge startingEdge)
        {
            if (startingEdge == null)
                throw new ArgumentNullException(nameof(startingEdge));

            var path = new List<TEdge>(EdgesPredecessors.Count) { startingEdge };

            TEdge currentEdge = startingEdge;
            while (EdgesPredecessors.TryGetValue(currentEdge, out TEdge edge))
            {
                path.Add(edge);
                currentEdge = edge;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Gets all paths.
        /// </summary>
        /// <returns>Enumerable of paths.</returns>
        [Pure]
        [NotNull, ItemNotNull]
        public IEnumerable<ICollection<TEdge>> AllPaths()
        {
            return _endPathEdges.Select(_path);
        }

        /// <summary>
        /// Merges the path starting at <paramref name="startingEdge"/> with remaining edges.
        /// </summary>
        /// <param name="startingEdge">Starting edge.</param>
        /// <param name="colors">Edges colors mapping.</param>
        /// <returns>Merged path.</returns>
        [Pure]
        [NotNull, ItemNotNull]
        public ICollection<TEdge> MergedPath(
            [NotNull] TEdge startingEdge,
            [NotNull] IDictionary<TEdge, GraphColor> colors)
        {
            if (startingEdge == null)
                throw new ArgumentNullException(nameof(startingEdge));
            if (colors is null)
                throw new ArgumentNullException(nameof(colors));

            var path = new List<TEdge>();

            TEdge currentEdge = startingEdge;
            GraphColor color = colors[currentEdge];
            if (color != GraphColor.White)
                return path;
            colors[currentEdge] = GraphColor.Black;

            path.Add(currentEdge);
            while (EdgesPredecessors.TryGetValue(currentEdge, out TEdge edge))
            {
                color = colors[edge];
                if (color != GraphColor.White)
                {
                    path.Reverse();
                    return path;
                }

                colors[edge] = GraphColor.Black;

                path.Add(edge);
                currentEdge = edge;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Gets all merged path.
        /// </summary>
        /// <returns>Enumerable of merged paths.</returns>
        [Pure]
        [NotNull, ItemNotNull]
        public IEnumerable<ICollection<TEdge>> AllMergedPaths()
        {
            var colors = new Dictionary<TEdge, GraphColor>();

            foreach (KeyValuePair<TEdge, TEdge> pair in EdgesPredecessors)
            {
                colors[pair.Key] = GraphColor.White;
                colors[pair.Value] = GraphColor.White;
            }

            return _endPathEdges.Select(edge => MergedPath(edge, colors));
        }

        private void OnEdgeDiscovered([NotNull] TEdge edge, [NotNull] TEdge targetEdge)
        {
            Debug.Assert(edge != null);
            Debug.Assert(targetEdge != null);

            if (!EqualityComparer<TEdge>.Default.Equals(edge, targetEdge))
                EdgesPredecessors[targetEdge] = edge;
        }

        private void OnEdgeFinished([NotNull] TEdge finishedEdge)
        {
            Debug.Assert(finishedEdge != null);

            foreach (TEdge edge in EdgesPredecessors.Values)
            {
                if (EqualityComparer<TEdge>.Default.Equals(finishedEdge, edge))
                    return;
            }

            _endPathEdges.Add(finishedEdge);
        }
    }
}