using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    MaterialEditor _editor;
    Object[] _materials;
    MaterialProperty[] _properties;
    bool _showPresets;

    enum ShadowMode
    {
        On,
        Clip,
        Dither,
        Off
    };

    ShadowMode shadows
    {
        set 
        {
			if (SetProperty("_Shadows", (float)value)) 
            {
				SetKeyWord("_SHADOWS_CLIP", value == ShadowMode.Clip);
				SetKeyWord("_SHADOWS_DITHER", value == ShadowMode.Dither);
			}
        }
    }

    bool Clipping
    {
        set => SetProperty("_Clipping", value, "_CLIPPING");
    }

    bool PremultipyAlpha
    {
        set => SetProperty("_PremulAlpha", value, "_PREMULTIPLY_ALPHA");
    }

    BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }

    BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }

    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    bool HasProperty(string name) => FindProperty(name, _properties, false) !=  null;

    bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");


    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) 
    {
        EditorGUI.BeginChangeCheck();

        base.OnGUI(materialEditor, properties);
        this._editor = materialEditor;
        this._materials = materialEditor.targets;
        this._properties = properties;

        EditorGUILayout.Space();
        _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
        if(_showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }



        if(EditorGUI.EndChangeCheck())
        {
            SetShadowCasterPass();
        }
    }

    RenderQueue RenderQueue
    {
        set 
        {
            foreach(Material m in _materials)
            {
                m.renderQueue = (int)value;
            }
        }
    }
    bool SetProperty(string name, float value)
    {
       MaterialProperty property = FindProperty(name, _properties, false);
       if(property != null)
       {
            property.floatValue = value;
            return true;
       }

       return false;
    }

    void SetProperty(string name, bool value, string keyword)
    {
        if(SetProperty(name, value ? 1f : 0f))
        {
            SetKeyWord(keyword, value);
        }
        
    }

    void SetKeyWord(string keyword, bool enabled)
    {
        if(enabled)
        {
            foreach(Material m in _materials)
            {
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach(Material m in _materials)
            {
                m.DisableKeyword(keyword);
            }
        }
    }

    bool PresetButton(string name)
    {
        if(GUILayout.Button(name))
        {
            _editor.RegisterPropertyChangeUndo(name);
            return true;
        }

        return false;
    }

    void OpaquePreset()
    {
        if(PresetButton("Opaque"))
        {
            Clipping = false;
            PremultipyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }

    void ClipPreset()
    {
        if(PresetButton("Clip"))
        {
            Clipping = true;
            PremultipyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }

    void FadePreset()
    {
        if(PresetButton("Fade"))
        {
            Clipping = false;
            PremultipyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    void TransparentPreset()
    {
        if(HasPremultiplyAlpha && PresetButton("Transparent"))
        {
            Clipping = false;
            PremultipyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    void SetShadowCasterPass()
    {
        MaterialProperty shadows = FindProperty("_Shadows", _properties, false);
        if(shadows == null || shadows.hasMixedValue)
        {
            return;
        }

        bool enabled = shadows.floatValue < (float)ShadowMode.Off;
        foreach(Material m in _materials)
        {
            m.SetShaderPassEnabled("ShadowCaster", enabled);
        }

    }
};