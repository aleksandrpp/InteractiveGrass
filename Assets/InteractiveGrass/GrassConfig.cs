using Unity.Mathematics;
using UnityEngine;

namespace AK.InteractiveGrass
{
    [CreateAssetMenu(fileName = "SO_GrassConfig", menuName = "AK.InteractiveGrass/GrassConfig")]
    public sealed class GrassConfig : ScriptableObject
    {
        public int2
            Bounds = new(0, 100);

        public float
            SpringForce = 8,
            SpringDamping = 8,
            BendForce = 18;

        public int
            Density = 8;

        public LayerMask
            GroundLayers = 1 << 0;
    }
}

