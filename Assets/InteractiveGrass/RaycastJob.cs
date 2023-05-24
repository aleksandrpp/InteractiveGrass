using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AK.InteractiveGrass
{
    [BurstCompile]
    public struct RaycastJob : IJobParallelFor
    {
        [ReadOnly] public int2 Bounds;
        [ReadOnly] public int LayerMask;
        [WriteOnly] public NativeArray<RaycastCommand> Commands;

        public void Execute(int index)
        {
            var rnd = new Random((uint)index + 1);
            var position = rnd.NextFloat3(Bounds.x, Bounds.y);

            Commands[index] = new RaycastCommand(position, math.down(), new QueryParameters(LayerMask));
        }
    }
}

