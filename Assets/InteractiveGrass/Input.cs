using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace AK.InteractiveGrass
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Input
    {
        public float3 Position;
        public float3 Direction;
    }
}

