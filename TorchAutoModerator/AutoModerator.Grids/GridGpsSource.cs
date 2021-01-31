﻿using System;
using System.Threading;
using AutoModerator.Core;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Utils.Torch;
using VRage;
using VRage.Game;
using VRageMath;

namespace AutoModerator.Grids
{
    // this class shouldn't hold onto any game entities so it won't mess with the game's GC
    public sealed class GridGpsSource : IEntityGpsSource
    {
        public interface IConfig
        {
            string GridGpsNameFormat { get; }
            string GridGpsDescriptionFormat { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        readonly IConfig _config;
        readonly string _gridName;
        readonly string _factionTagOrNull;
        readonly string _playerNameOrNull;
        readonly TimeSpan _remainingTime;

        public GridGpsSource(IConfig config, GridLagProfileResult profileResult, TimeSpan remainingTime)
        {
            _config = config;
            EntityId = profileResult.GridId;
            LagNormal = profileResult.ThresholdNormal;
            _gridName = profileResult.GridName;
            _factionTagOrNull = profileResult.FactionTagOrNull;
            _playerNameOrNull = profileResult.PlayerNameOrNull;
            _remainingTime = remainingTime;
        }

        public long EntityId { get; }
        public double LagNormal { get; }

        public bool TryCreateGps(int rank, out MyGps gps)
        {
            if (!Thread.CurrentThread.IsSessionThread())
            {
                throw new Exception("Can be called in the game loop only");
            }

            Log.Trace($"laggy grid report to be broadcast: {EntityId}");

            gps = null;

            if (!MyEntityIdentifier.TryGetEntity(EntityId, out var entity, true))
            {
                Log.Warn($"Grid not found by EntityId: {EntityId}");
                return false;
            }

            if (entity.Closed)
            {
                Log.Warn($"Grid found but closed: {EntityId}");
                return false;
            }

            var grid = (MyCubeGrid) entity;
            var rankStr = RankToString(rank);
            var name = ToString(_config.GridGpsNameFormat).Replace("{rank}", rankStr);
            var description = ToString(_config.GridGpsDescriptionFormat).Replace("{rank}", rankStr);

            gps = new MyGps(new MyObjectBuilder_Gps.Entry
            {
                name = name,
                DisplayName = name,
                coords = grid.PositionComp.GetPosition(),
                showOnHud = true,
                color = Color.Purple,
                description = description,
            });

            gps.SetEntity(grid);
            gps.UpdateHash();

            return true;
        }

        string ToString(string format)
        {
            var str = format
                .Replace("{grid}", _gridName)
                .Replace("{player}", _playerNameOrNull ?? "<none>")
                .Replace("{faction}", _factionTagOrNull ?? "<none>")
                .Replace("{ratio}", $"{LagNormal * 100:0}%");

            var remainingTimeStr = RemainingTimeToString(_remainingTime);
            return $"{str} ({remainingTimeStr})";
        }

        static string RemainingTimeToString(TimeSpan remainingTime)
        {
            if (remainingTime.TotalHours >= 1)
            {
                return $"{remainingTime.TotalHours:0} hours";
            }

            if (remainingTime.TotalMinutes >= 1)
            {
                return $"{remainingTime.TotalMinutes:0} minutes";
            }

            return $"{remainingTime.TotalSeconds:0} seconds";
        }

        static string RankToString(int rank)
        {
            switch (rank % 10)
            {
                case 1: return $"{rank}st";
                case 2: return $"{rank}nd";
                case 3: return $"{rank}rd";
                default: return $"{rank}th";
            }
        }

        public override string ToString()
        {
            var normal = $"{LagNormal * 100f:0.00}%";
            var remainingTime = $"{_remainingTime.TotalMinutes:0.0}m";
            var factionTag = _factionTagOrNull ?? "<single>";
            var playerName = _playerNameOrNull ?? "<none>";
            return $"\"{_gridName}\" ({EntityId}) {normal} for {remainingTime} [{factionTag}] {playerName}";
        }
    }
}