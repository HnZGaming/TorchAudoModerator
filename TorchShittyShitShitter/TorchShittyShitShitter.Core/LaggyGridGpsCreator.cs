﻿using System;
using System.Linq;
using System.Threading;
using NLog;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using VRage;
using VRage.Game;
using VRageMath;

namespace TorchShittyShitShitter.Core
{
    /// <summary>
    /// Create GPS entities for laggy grids.
    /// </summary>
    public sealed class LaggyGridGpsCreator
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public MyGps CreateGpsOrNull(LaggyGridReport gridReport, int ranking)
        {
            // must be called in the game loop
            if (Thread.CurrentThread.ManagedThreadId !=
                MySandboxGame.Static.UpdateThread.ManagedThreadId)
            {
                throw new Exception("Can be called in the game loop only");
            }

            Log.Trace($"laggy grid report to be broadcast: {gridReport}");

            // this method fails outside the game loop
            if (!MyEntityIdentifier.TryGetEntity(gridReport.GridId, out var entity, true))
            {
                Log.Warn($"Grid not found by EntityId: {gridReport}");
                return null;
            }

            if (entity.Closed)
            {
                Log.Warn($"Grid found but closed: {gridReport}");
                return null;
            }

            var grid = (MyCubeGrid) entity;

            var gps = new MyGps(new MyObjectBuilder_Gps.Entry
            {
                name = grid.DisplayName,
                DisplayName = grid.DisplayName,
                coords = grid.PositionComp.GetPosition(),
                showOnHud = true,
                color = Color.Purple,
                description = $"The {RankingToString(ranking)} laggiest grid. Get 'em!",
            });

            gps.SetEntity(grid);
            gps.UpdateHash();

            return gps;
        }

        static string RankingToString(int ranking)
        {
            switch ($"{ranking}".Last())
            {
                case '1': return $"{ranking}st";
                case '2': return $"{ranking}nd";
                case '3': return $"{ranking}rd";
                default: return $"{ranking}th";
            }
        }
    }
}