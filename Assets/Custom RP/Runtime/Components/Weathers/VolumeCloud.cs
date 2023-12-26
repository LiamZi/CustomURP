using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class VolumeCloud : CoreAction
    {
        // CustomRenderPipelineAsset _asset;
        public VolumeCloudSettings _setttings;
        public string _name;
        public RenderTargetIdentifier _cameraColorRT;
        public int[] _cloudTexRT;
        public int _width;
        public int _height;
        public int _frameCount;
        public int _rtSwicth;

        int[] _cloundTexGame = new int[2];
        int[] _cloundTexScene = new int[2];
        int _lastWidthGame;
        int _lastHeightGame;
        int _lastWidthScene;
        int _lastHeightScene;
        int _frameCountGame;
        int _frameCountScene;
        int _rtSwitchGame;
        int _rtSwitchScene;

        int _frameDebug = 1;

        public VolumeCloud(VolumeCloudSettings settings, string name = "VolumeCloud")
        {
            _setttings = settings;
            _name = name;
            _frameCount = 0;
        }
        
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            this._asset = asset;
            _name = name;
            _frameCount = 0;
        }
        public override bool InspectProperty()
        {
            return true;
        }
        
        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;

            _cmd.Name = "VolumeCloud";

            if (!(_setttings._material && camera._camera.cameraType == CameraType.Game || camera._camera.cameraType == CameraType.SceneView))
                return;

            // _setttings = _asset.VolumeCloudSettings;

            int width = (int)(camera._renderTarget._size.x * _setttings._rtScale);
            int height = (int)(camera._renderTarget._size.y * _setttings._rtScale);

            if (_setttings._frameBlock == FrameBlock._Off)
            {
                for (int i = 0; i < 2; ++i)
                {
                    _cmd.ReleaseTemporaryRT(_cloundTexGame[i]);
                    _cmd.ReleaseTemporaryRT(_cloundTexScene[i]);
                    
                    // Array.Clear(_cloundTexGame, i, _cloundTexGame.Length);
                    // Array.Clear(_cloundTexGame, i, _cloundTexScene.Length);
                }

                _width = width;
                _height = height;
                _cameraColorRT = new RenderTargetIdentifier(camera._renderTarget._colorAttachmentId);
                return;
            }

            FrameDebugging();

            if (camera._camera.cameraType == CameraType.Game)
            {
                for (int i = 0; i < _cloundTexGame.Length; ++i)
                {
                    if (_cloundTexGame[i] != null && _lastWidthGame == width && _lastHeightGame == height)
                        continue;
                    
                    if(width < _setttings._shieldWith)
                        continue;
                    
                     _cmd.GetTemporaryRT(_cloundTexGame[i], width, height, 0, FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

                     _lastWidthGame = width;
                     _lastHeightGame = height;
                }

                _cloudTexRT = _cloundTexGame;
                _width = _lastWidthGame;
                _height = _lastHeightGame;
                _frameCount = _frameCountGame;
                _rtSwicth = _rtSwitchGame;

                _rtSwitchGame = (++_rtSwitchGame) % 2;
                _frameCountGame = (++_frameCountGame) % (int)_setttings._frameBlock;
            }
            else
            {
                for (int i = 0; i < _cloundTexScene.Length; ++i)
                {
                    if (_cloundTexScene[i] != null && _lastWidthGame == width && _lastHeightGame == height)
                        continue;

                    _cmd.GetTemporaryRT(_cloundTexScene[i], width, height, 0, FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                    
                    _lastWidthScene = width;
                    _lastHeightScene = height;
                }
                
                _cloudTexRT = _cloundTexScene;
                _width = _lastWidthGame;
                _height = _lastHeightGame;
                _frameCount = _frameCountGame;
                _rtSwicth = _rtSwitchGame;
                
                _rtSwitchGame = (++_rtSwitchGame) % 2;
                _frameCountGame = (++_frameCountGame) % (int)_setttings._frameBlock;
            }

            _cameraColorRT = new RenderTargetIdentifier(_camera._renderTarget._colorAttachmentId);
        }
        
        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;

            var mat = _setttings._material;
            mat.SetTexture(ShaderParams._blueNoiseTexId, _setttings._blueNoise);
            mat.SetVector(ShaderParams._blueNoiseTexUVId, new Vector4((float)_width / _setttings._blueNoise.width, (float)_height / _setttings._blueNoise.height));
            mat.SetInt(ShaderParams._cloudWidthId, _width - 1);
            mat.SetInt(ShaderParams._cloudHeightId, _height - 1);
            mat.SetInt(ShaderParams._cloudFrameCountId, _frameCount);

            if (_setttings._frameBlock == FrameBlock._Off)
            {
                mat.EnableKeyword("_OFF");
                mat.DisableKeyword("_2X2");
                mat.DisableKeyword("_4X4");
            }
            
            if (_setttings._frameBlock == FrameBlock._2x2)
            {
                mat.DisableKeyword("_OFF");
                mat.EnableKeyword("_2X2");
                mat.DisableKeyword("_4X4");
            }
            
            if (_setttings._frameBlock == FrameBlock._4x4)
            {
                mat.DisableKeyword("_OFF");
                mat.DisableKeyword("_2X2");
                mat.EnableKeyword("_4X4");
            }

            if (_setttings._frameBlock == FrameBlock._Off)
            {
               _cmd.GetTemporaryRT(ShaderParams._cloudTexId,_width, _height, 0, FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                
               _cmd.Cmd.Blit(_cameraColorRT, ShaderParams._cloudTexId, _setttings._material, 0);
               _cmd.Cmd.Blit(ShaderParams._cloudTexId, _cameraColorRT, _setttings._material, 1);
               _cmd.Execute();
               _cmd.ReleaseTemporaryRT(ShaderParams._cloudTexId);
            }
            else
            {
                _cmd.Cmd.Blit(_cloudTexRT[_rtSwicth % 2], _cloudTexRT[(_rtSwicth + 1) % 2], _setttings._material, 0);
                _cmd.Cmd.Blit(_cloudTexRT[(_rtSwicth + 1) % 2], _cameraColorRT, _setttings._material, 1);
                _cmd.Execute();
            }
            
            _cmd.Name = "Geometry Pass";
        }



        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;
        }

        void FrameDebugging()
        {
            if (_setttings._isFrameDebug)
            {
                if (_setttings._frameDebug != _frameDebug)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        _cmd.ReleaseTemporaryRT(_cloundTexGame[i]);
                        _cmd.ReleaseTemporaryRT(_cloundTexScene[i]);
                    
                        Array.Clear(_cloundTexGame, i, _cloundTexGame.Length);
                        Array.Clear(_cloundTexGame, i, _cloundTexScene.Length);
                    }
                }

                _frameDebug = _setttings._frameDebug;
                _frameCountGame = _frameCountGame % _setttings._frameDebug;
                _frameCountScene = _frameCountScene % _setttings._frameDebug;
            }
        }
        
    }
}
