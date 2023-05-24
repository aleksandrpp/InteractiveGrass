using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace AK.InteractiveGrass
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Blade
    {
        public float3 Position;
        public float2 Size;
        public float Bend;
        public float BendVelocity;
        public float3 Direction;
        public float Color;
    }
}

