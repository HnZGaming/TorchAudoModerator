﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using InfluxDB.Client.Writes;
using Profiler.Basics;
using Profiler.Core;
using Torch.Server.InfluxDb;
using VRage.Game.ModAPI;

namespace Profiler.Database
{
    public sealed class DbFactionProfiler : IDbProfiler
    {
        const int SamplingSeconds = 10;
        readonly InfluxDbClient _dbClient;

        public DbFactionProfiler(InfluxDbClient dbClient)
        {
            _dbClient = dbClient;
        }

        public void StartProfiling(CancellationToken canceller)
        {
            while (!canceller.IsCancellationRequested)
            {
                var gameEntityMask = new GameEntityMask(null, null, null);
                using (var profiler = new FactionProfiler(gameEntityMask))
                using (ProfilerPatch.Profile(profiler))
                {
                    var startTick = ProfilerPatch.CurrentTick;

                    profiler.StartProcessQueue();
                    canceller.WaitHandle.WaitOne(TimeSpan.FromSeconds(SamplingSeconds));

                    var totalTicks = ProfilerPatch.CurrentTick - startTick;

                    var factionProfilerEntities = profiler.GetProfilerEntries();
                    OnProfilingFinished(totalTicks, factionProfilerEntities);
                }
            }
        }

        void OnProfilingFinished(ulong totalTicks, IEnumerable<(IMyFaction Faction, ProfilerEntry ProfilerEntry)> entities)
        {
            var points = new List<PointData>();

            var topResults = entities
                .OrderByDescending(r => r.ProfilerEntry.TotalTimeMs)
                .ToArray();

            foreach (var (faction, profilerEntry) in topResults)
            {
                var deltaTime = (float) profilerEntry.TotalTimeMs / totalTicks;

                var point = _dbClient.MakePointIn("profiler_factions")
                    .Tag("faction_tag", faction.Tag)
                    .Field("main_ms", deltaTime);

                points.Add(point);
            }

            _dbClient.WritePoints(points.ToArray());
        }
    }
}