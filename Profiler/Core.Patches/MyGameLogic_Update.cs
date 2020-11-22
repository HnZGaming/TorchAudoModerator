﻿using System;
using NLog;
using Profiler.TorchUtils;
using Torch.Managers.PatchManager;
using VRage.Game.Entity;

namespace Profiler.Core.Patches
{
    public static class MyGameLogic_Update
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        static readonly Type Type = typeof(MyGameLogic);

        public static void Patch(PatchContext ctx)
        {
            var UpdateOnceBeforeFrameMethod = Type.StaticMethod(nameof(MyGameLogic.UpdateOnceBeforeFrame));
            MyEntity_Transpile.Patch(ctx, UpdateOnceBeforeFrameMethod);

            if (MyDistributedUpdater_Iterate.ApiExists())
            {
                var UpdateBeforeSimulationMethod = Type.StaticMethod(nameof(MyGameLogic.UpdateBeforeSimulation));
                foreach (var updateMethod in MyDistributedUpdater_Iterate.FindUpdateMethods(UpdateBeforeSimulationMethod))
                {
                    MyEntity_Transpile.Patch(ctx, updateMethod);
                }

                var UpdateAfterSimulationMethod = Type.StaticMethod(nameof(MyGameLogic.UpdateAfterSimulation));
                foreach (var updateMethod in MyDistributedUpdater_Iterate.FindUpdateMethods(UpdateAfterSimulationMethod))
                {
                    MyEntity_Transpile.Patch(ctx, updateMethod);
                }
            }
            else
            {
                Log.Error("Unable to find MyDistributedUpdater.Iterate(Delegate) method.  Some profiling data will be missing.");
            }
        }
    }
}