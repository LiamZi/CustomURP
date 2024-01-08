using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class Command
    {
        private ScriptableRenderContext _context;
        private string _name;

        public Command(string name)
        {
            _name = name;
            Cmd = new CommandBuffer
            {
                name = _name
            };
        }

        public Command(CommandBuffer buffer, string name)
        {
            Cmd = buffer;
            _name = name;
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                Cmd.name = value;
            }
        }

        public ScriptableRenderContext Context
        {
            get => _context;
            set => _context = value;
        }

        public CustomRenderPipelineAsset Asset { get; set; }

        public CommandBuffer Cmd { get; }

        public void BeginSample()
        {
            Cmd.BeginSample(_name);
        }

        public void EndSampler()
        {
            Cmd.EndSample(_name);
        }

        public void Execute(ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
        }

        public void Execute()
        {
            _context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
        }

        public void ExecuteAsync(ScriptableRenderContext context, ComputeQueueType type)
        {
            context.ExecuteCommandBufferAsync(Cmd, type);
            Cmd.Clear();
        }

        public void ExecuteAsync(ComputeQueueType type)
        {
            _context.ExecuteCommandBufferAsync(Cmd, type);
            Cmd.Clear();
        }

        public void Submit()
        {
            _context.Submit();
        }

        public void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings)
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
            Cmd.GetTemporaryRT(nameID, width, height, depthBuffer, filter, format);
        }

        public void CopyTexture(RenderTargetIdentifier src, RenderTargetIdentifier dst)
        {
            Cmd.CopyTexture(src, dst);
        }

        public void SetRenderTarget(
            RenderTargetIdentifier color,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depth,
            RenderBufferLoadAction depthLoadAction,
            RenderBufferStoreAction depthStoreAction)
        {
            Cmd.SetRenderTarget(color, colorLoadAction, colorStoreAction, depth, depthLoadAction, depthStoreAction);
        }

        public void SetRenderTarget(
            RenderTargetIdentifier rt,
            RenderBufferLoadAction loadAction,
            RenderBufferStoreAction storeAction)
        {
            Cmd.SetRenderTarget(rt, loadAction, storeAction);
        }

        public void DrawGizmos(Camera camera, GizmoSubset gizmoSubset)
        {
            _context.DrawGizmos(camera, gizmoSubset);
        }

        public void SetGlobalInt(int nameID, int value)
        {
            Cmd.SetGlobalInt(nameID, value);
        }

        public void SetGlobalFloat(int nameID, float value)
        {
            Cmd.SetGlobalFloat(nameID, value);
        }

        public void SetGlobalVectorArray(int nameID, Vector4[] values)
        {
            Cmd.SetGlobalVectorArray(nameID, values);
        }

        public void ReleaseTemporaryRT(int nameID)
        {
            Cmd.ReleaseTemporaryRT(nameID);
        }

        public void SetGlobalVector(int nameID, Vector4 value)
        {
            Cmd.SetGlobalVector(nameID, value);
        }

        public void SetGlobalVector(string name, Vector4 value)
        {
            Cmd.SetGlobalVector(name, value);
        }

        public void SetGlobalTexture(string name, RenderTargetIdentifier value)
        {
            Cmd.SetGlobalTexture(name, value);
        }

        public void SetGlobalTexture(int nameID, RenderTargetIdentifier value)
        {
            Cmd.SetGlobalTexture(nameID, value);
        }

        public void SetGlobalBuffer(int nameID, ComputeBuffer value)
        {
            Cmd.SetGlobalBuffer(nameID, value);
        }
        
        public void EnableShaderKeyword(string keyword)
        {
            Cmd.EnableShaderKeyword(keyword);
        }

        public void DisableShaderKeyword(string keyword)
        {
            Cmd.DisableShaderKeyword(keyword);
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
            Cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
        }

        public void DrawProcedural(
            Matrix4x4 matrix,
            Material material,
            int shaderPass,
            MeshTopology topology,
            int vertexCount,
            int instanceCount)
        {
            Cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount);
        }

        public void DrawProcedural(
            Matrix4x4 matrix,
            Material material,
            int shaderPass,
            MeshTopology topology,
            int vertexCount)
        {
            Cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount);
        }

        public void SetViewport(Rect pixelRect)
        {
            Cmd.SetViewport(pixelRect);
        }

        public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            Cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor);
        }

        public void SetComputeVectorParam(ComputeShader computeShader, int nameID, Vector4 val)
        {
            Cmd.SetComputeVectorParam(computeShader, nameID, val);
        }

        public void SetComputeFloatParam(ComputeShader computeShader, int nameID, float val)
        {
            Cmd.SetComputeFloatParam(computeShader, nameID, val);
        }

        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex,
                                                int nameID, ComputeBuffer buffer)
        {
            Cmd.SetComputeBufferParam(computeShader, kernelIndex, nameID, buffer);
        }
        
        public void SetComputeIntParam(ComputeShader computeShader, int nameID, int val)
        {
            Cmd.SetComputeIntParam(computeShader, nameID, val);
        }

        public void DispatchCompute(ComputeShader computeShader, int kernelIndex,
                                    int threadGroupsX, int threadGroupsY, int threadGroupsZ)
        {
            Cmd.DispatchCompute(computeShader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ);
        }
        
        public void DispatchCompute(ComputeShader computeShader, int kernelIndex,
                                    ComputeBuffer indirectBuffer, uint argsOffset)
        {
            Cmd.DispatchCompute(computeShader, kernelIndex, indirectBuffer, argsOffset);
        }
        
        public void CopyTexture(RenderTargetIdentifier src, int srcElement, int srcMip,
                                RenderTargetIdentifier dst, int dstElement, int dstMip)
        {
            Cmd.CopyTexture(src, srcElement, srcMip, dst, dstElement, dstMip);
        }

        public void SetBufferCounterValue(ComputeBuffer buffer, uint counterValue)
        {
            Cmd.SetComputeBufferCounterValue(buffer, counterValue);
        }

        public void CopyCounterValue(ComputeBuffer src, ComputeBuffer dst, uint dstOffsetBytes)
        {
            Cmd.CopyCounterValue(src, dst, dstOffsetBytes);
        }

        public void Release()
        {
            Cmd.Release();
        }

        public void Clear()
        {
            Cmd.Clear();
        }
    };
}