#region License and Terms
// MoreLINQ - Extensions to LINQ to Objects
// Copyright (c) 2017 Leandro F. Vieira (leandromoh). All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using System.Threading;

namespace MoreLinq.Test
{
    using System.Threading.Tasks;
    using Microsoft.Concurrency.TestTools.UnitTesting;
    using Assert = NUnit.Framework.Assert;

    [TestFixture]
    public class MemoizeTest
    {
        [Test]
        [DataRaceTestMethod]
        public void MemoizeIsThreadSafe()
        {
            var sequence = Enumerable.Range(1, 1000);
            var memoized = sequence.Memoize();

            Parallel.Invoke(
                () => memoized.ToArray(),
                () => memoized.ToArray());

            return;

            var runs = Enumerable.Range(1, 2/*Environment.ProcessorCount * 4*/)
                                 .Select(_ => new List<int>())
                                 .ToArray();

            var start  = new Barrier(runs.Length + 1);
            var finish = new Barrier(start.ParticipantCount);

            foreach (var thread in from xs in runs
                                   select new Thread(() =>
                                   {
                                       start.SignalAndWait();
                                       foreach (var x in memoized)
                                           xs.Add(x);
                                       finish.SignalAndWait();
                                   }))
            {
                thread.Start();
            }

            start.SignalAndWait();
            finish.SignalAndWait();

            Assert.That(sequence, Is.EquivalentTo(memoized));
            Array.ForEach(runs, xs => Assert.That(xs, Is.EquivalentTo(memoized)));
        }
    }
}
