﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace QuickGraph.Predicates
{
    [Serializable]
    public sealed class InDictionaryVertexPredicate<TVertex, TValue>
    {
        private readonly IDictionary<TVertex, TValue> dictionary;

        public InDictionaryVertexPredicate(
            IDictionary<TVertex,TValue> dictionary)
        {
            CodeContract.Requires(dictionary != null);
            this.dictionary = dictionary;
        }

        [Pure]
        public bool Test(TVertex v)
        {
            CodeContract.Requires(v != null);

            return this.dictionary.ContainsKey(v);
        }
    }
}