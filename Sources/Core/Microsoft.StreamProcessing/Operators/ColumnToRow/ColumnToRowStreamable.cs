﻿// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Diagnostics.Contracts;
using Microsoft.StreamProcessing.Internal.Collections;

namespace Microsoft.StreamProcessing
{
    internal sealed class ColumnToRowStreamable<TKey, TPayload> : UnaryStreamable<TKey, TPayload, TPayload>
    {
        private static readonly SafeConcurrentDictionary<Tuple<Type, string>> cachedPipes
                          = new SafeConcurrentDictionary<Tuple<Type, string>>();

        public ColumnToRowStreamable(IStreamable<TKey, TPayload> source)
            : base(source, source.Properties.ToRowBased())
        {
            Contract.Requires(source != null);
        }

        internal override IStreamObserver<TKey, TPayload> CreatePipe(IStreamObserver<TKey, TPayload> observer)
        {
            var lookupKey = CacheKey.Create();

            var generatedPipeType = cachedPipes.GetOrAdd(lookupKey, key => ColumnToRowTemplate.Generate(this));
            Func<PlanNode, IQueryObject, PlanNode> planNode = ((PlanNode p, IQueryObject o) => new ColumnToRowPlanNode(p, o, typeof(TKey), typeof(TPayload)));

            var instance = Activator.CreateInstance(generatedPipeType.Item1, this, observer, planNode);
            var returnValue = (UnaryPipe<TKey, TPayload, TPayload>)instance;
            return returnValue;
        }

        protected override bool CanGenerateColumnar() => true;
    }
}