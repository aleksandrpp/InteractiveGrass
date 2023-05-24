using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AK.InteractiveGrass
{
    [BurstCompile]
    public struct PlaceJob : IJob
    {
        private const float NoiseScale = .2f;
        private const float NoiseClamp = .1f;

        [WriteOnly] public NativeList<Blade> Blades;
        [ReadOnly] public NativeArray<RaycastHit> Results;

        public void Execute()
        {
            for (int i = 0; i < Results.Length; i++)
            {
                if (Results[i].colliderInstanceID == 0) continue;

                float color = noise.cnoise(Results[i].point * NoiseScale);
                if (color < NoiseClamp) continue;

                float height = math.saturate(noise.cnoise(Results[i].point) * 2);
                Blades.Add(new Blade()
                {
                    Position = Results[i].point,
                    Size = new float2(.3f, height),
                    Bend = 0,
                    Direction = Vector3.right,
                    BendVelocity = 0,
                    Color = color
                });
            }
        }
    }
}

