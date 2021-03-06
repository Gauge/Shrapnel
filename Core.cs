﻿using Sandbox.Definitions;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Shrapnel
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public const float ReductionMult = 1.0f;

        private Queue<ShrapnelData> queue = new Queue<ShrapnelData>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(9, ProcessDamage);
        }

        public void ProcessDamage(object target, ref MyDamageInformation info)
        {
            if (info.Type != MyDamageType.Bullet) return; // missle and collision damage does not need shrapnel
            if (!(target is IMySlimBlock)) return;

            IMySlimBlock slim = target as IMySlimBlock;
            if (slim.Integrity >= info.Amount) return;

            float overkill = info.Amount - slim.Integrity;
            info.Amount = slim.Integrity;

            queue.Enqueue(new ShrapnelData()
            {
                Neighbours = slim.Neighbours,
                OverKill = overkill
            });
        }

        public override void UpdateBeforeSimulation()
        {
            while (queue.Count > 0)
            {
                ShrapnelData data = queue.Dequeue();
                int count = data.Neighbours.Count;
                foreach (IMySlimBlock neighbour in data.Neighbours)
                {
                    // get block resistance
                    float generalMult = 1;
                    if (neighbour.BlockDefinition is MyCubeBlockDefinition)
                    {
                        generalMult = ((MyCubeBlockDefinition)neighbour.BlockDefinition).GeneralDamageMultiplier;
                    }

                    // total over kill damage devided by the number of neighbours
                    // that times the reduction multiplier 0 to 1
                    // that times the blocks general reduction multiplier 0 - 1
                    float damage = ((data.OverKill / (float)count) * ReductionMult * generalMult);
                    neighbour.DoDamage(damage, MyDamageType.Bullet, true);
                }
            }
        }
    }

    internal class ShrapnelData
    {
        public float OverKill { get; set; }
        public List<IMySlimBlock> Neighbours { get; set; }
    }
}
