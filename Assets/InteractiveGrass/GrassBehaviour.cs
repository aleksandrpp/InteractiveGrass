using AK.CG.Common;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace AK.InteractiveGrass
{
    public class GrassBehaviour : MonoBehaviour
    {
        private const int VertexCount = 6;
        private const int WriteTarget = 1;

        private const int BatchCount = 64;
        private const int CommandPerJob = 32;

        [SerializeField] private GrassConfig _config;
        [SerializeField] private Material _material;
        [SerializeField] private Camera _camera;
        [SerializeField] private Light _light;
        [SerializeField] private Transform _input;

        private CompBuffer<Blade> _blades;
        private Vector3 _previousInputPosition;

        private readonly int 
            _inputPositionId = Shader.PropertyToID("_InputPosition"),
            _inputDirectionId = Shader.PropertyToID("_InputDirection");

        private void Start()
        {
            var count = PropagateBlades();

            _material.SetBuffer("_BladeBuffer", _blades.Buffer);
            _material.SetFloat("_SpringForce", _config.SpringForce);
            _material.SetFloat("_SpringDamping", _config.SpringDamping);
            _material.SetFloat("_BendForce", _config.BendForce);
            _material.SetInteger("_VertexCount", VertexCount);

            GeometryBuffer(count);
            DepthBuffer(count);
            ShadowCasterBuffer(count);
            ShadowTextureBuffer();
        }

        private int PropagateBlades()
        {
            var size = _config.Bounds.y - _config.Bounds.x;
            var count = size * _config.Density * size * _config.Density;

            var raycastCommands = new NativeArray<RaycastCommand>(count, Allocator.TempJob);
            var hitResults = new NativeArray<RaycastHit>(count, Allocator.TempJob);

            using var blades = new NativeList<Blade>(Allocator.TempJob);

            var raycast = new RaycastJob()
            {
                Bounds = _config.Bounds,
                LayerMask = _config.GroundLayers,
                Commands = raycastCommands
            };

            var place = new PlaceJob()
            {
                Blades = blades,
                Results = hitResults
            };

            var jh = raycast.Schedule(count, BatchCount);
            jh = RaycastCommand.ScheduleBatch(raycastCommands, hitResults, CommandPerJob, 1, jh);
            jh = raycastCommands.Dispose(jh);
            jh = place.Schedule(jh);
            jh = hitResults.Dispose(jh);
            jh.Complete();

            _blades = new CompBuffer<Blade>(blades.AsArray());
            _blades.SetWriteTarget(WriteTarget);

            return blades.Length;
        }

        private void ShadowTextureBuffer()
        {
            using var shadowTextureBuffer = new CommandBuffer();
            shadowTextureBuffer.SetGlobalTexture("_ShadowTexture", BuiltinRenderTextureType.CurrentActive);
            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, shadowTextureBuffer);
        }

        private void ShadowCasterBuffer(int count)
        {
            using var shadowCasterBuffer = new CommandBuffer();
            shadowCasterBuffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, VertexCount * count, 1);
            _light.AddCommandBuffer(LightEvent.BeforeShadowMapPass, shadowCasterBuffer);
        }

        private void DepthBuffer(int count)
        {
            using var depthBuffer = new CommandBuffer();
            depthBuffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, VertexCount * count, 1);
            _camera.AddCommandBuffer(CameraEvent.BeforeDepthTexture, depthBuffer);
        }

        private void GeometryBuffer(int count)
        {
            using var geometryBuffer = new CommandBuffer();
            geometryBuffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, VertexCount * count, 1);
            _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, geometryBuffer);
        }

        private void Update()
        {
            var position = _input.position;
            var direction = position - _previousInputPosition;

            if (direction.magnitude > 0)
            {
                _material.SetVector(_inputPositionId, position);
                _material.SetVector(_inputDirectionId, direction * 10);
                _previousInputPosition = position;
            }
        }

        private void OnDestroy()
        {
            _blades.Dispose();

            if (_camera)
                _camera.RemoveAllCommandBuffers();

            if (_light)
                _light.RemoveAllCommandBuffers();
        }

        #region GUI

        public void OnGUI()
        {
            var stl = new GUIStyle(GUI.skin.label)
            {
                padding = new RectOffset(150, 125, 195, 100),
                fontSize = 24
            };

            var text =
                $"Grass blades: {_blades.Buffer.count}";

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text, stl);
        }

        #endregion
    }
}

