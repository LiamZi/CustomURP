using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class Command
    {
        private string _name;
        private UnityEngine.Rendering.CommandBuffer _cmd;
        private ScriptableRenderContext _context;
        private CustomRenderPipelineAsset _asset;

        public Command(string name)
        {
            _name = name;
            _cmd = new UnityEngine.Rendering.CommandBuffer() { name = _name };
        }

        public Command(UnityEngine.Rendering.CommandBuffer buffer, string name) 
        {
            _cmd = buffer;
            _name = name;
        }

        public void BeginSample()
        {
            _cmd.BeginSample(_name);
        }

        public void EndSampler()
        {
            _cmd.EndSample(_name);
        }

        public void Execute(ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        public void Execute()
        {
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        public void ExecuteAsync(ScriptableRenderContext context, ComputeQueueType type)
        {
            context.ExecuteCommandBufferAsync(_cmd, type);
            _cmd.Clear();
        }

        public void ExecuteAsync(ComputeQueueType type)
        {
            _context.ExecuteCommandBufferAsync(_cmd, type);
            _cmd.Clear();
        }

        public void Submit()
        {
            _context.Submit();
        }
        
        public void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings)
        {
            _context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        public void DrawSkybox(CustomRenderPipelineCamera camera)
        {
            _context.DrawSkybox(camera._camera);
        }

        public void GetTemporaryRT(
            int nameID,
            int width,
            int height,
            int depthBuffer,
            FilterMode filter,
            RenderTextureFormat format)
        {
            _cmd.GetTemporaryRT(nameID, width, height, depthBuffer, filter, format);
        }

        public void CopyTexture(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            _cmd.CopyTexture(src, dst);
        }

        public void SetRenderTarget(
            RenderTargetIdentifier color,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depth,
            RenderBufferLoadAction depthLoadAction,
            RenderBufferStoreAction depthStoreAction)
        {
            _cmd.SetRenderTarget(color, colorLoadAction, colorStoreAction, depth, depthLoadAction, depthStoreAction);
        }

        public void SetRenderTarget(
            RenderTargetIdentifier rt,
            RenderBufferLoadAction loadAction,
            RenderBufferStoreAction storeAction)
        {
            _cmd.SetRenderTarget(rt, loadAction, storeAction);
        }

        public void DrawGizmos(Camera camera, GizmoSubset gizmoSubset)
        {
            _context.DrawGizmos(camera, gizmoSubset);
        }

        public void SetGlobalInt(int nameID, int value)
        {
            _cmd.SetGlobalInt(nameID, value);
        }

        public void SetGlobalFloat(int nameID, float value)
        {
            _cmd.SetGlobalFloat(nameID, value);
        }

        public void SetGlobalVectorArray(int nameID, Vector4[] values)
        {
            _cmd.SetGlobalVectorArray(nameID, values);
        }
        
        public void ReleaseTemporaryRT(int nameID)
        {
            _cmd.ReleaseTemporaryRT(nameID);
        }

        public void SetGlobalVector(int nameID, Vector4 value)
        {
            _cmd.SetGlobalVector(nameID, value);
        }

        public void SetGlobalVector(string name, Vector4 value)
        {
            _cmd.SetGlobalVector(name, value);
        }

        public void SetGlobalTexture(string name, RenderTargetIdentifier value)
        {
            _cmd.SetGlobalTexture(name, value);
        }

        public void SetGlobalTexture(int nameID, RenderTargetIdentifier value)
        {
            _cmd.SetGlobalTexture(nameID, value);
        }

        public void DrawProcedural(
            Matrix4x4 matrix,
            Material material,
            int shaderPass,
            MeshTopology topology,
            int vertexCount,
            int instanceCount,
            MaterialPropertyBlock properties)
        {
            _cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
        }

        public void DrawProcedural(
            Matrix4x4 matrix,
            Material material,
            int shaderPass,
            MeshTopology topology,
            int vertexCount,
            int instanceCount)
        {
            _cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount);
        }

        public void DrawProcedural(
            Matrix4x4 matrix,
            Material material,
            int shaderPass,
            MeshTopology topology,
            int vertexCount)
        {
            _cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount);
        }
        
        public string Name
        {
            get => _name;
            set 
            { 
                _name = value;
                _cmd.name = value;
            }
        }

        public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            _cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor);
        }

        public ScriptableRenderContext Context
        {
            get => _context;
            set => _context = value;
        }

        public CustomRenderPipelineAsset Asset
        {
            get => _asset;
            set => _asset = value;
        }
        
        public UnityEngine.Rendering.CommandBuffer Cmd => _cmd;

    };
    
};
