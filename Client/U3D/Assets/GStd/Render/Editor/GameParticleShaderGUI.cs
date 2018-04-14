﻿//------------------------------------------------------------------------------
// This file is part of MistLand project in GStd.
// Copyright © 2016-2016 GStd Technology Co., Ltd.
// All Right Reserved.
//------------------------------------------------------------------------------

using System;
using GStd;
using GStd.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The custom editor for shader: "Game/Particle".
/// </summary>
public class GameParticleShaderGUI : GStdShaderGUI
{
    private static readonly string[] BlendNames =
        Enum.GetNames(typeof(RenderingMode));

    private MaterialProperty renderingMode;
    private MaterialProperty cullMode;
    private MaterialProperty cutoff;
    private MaterialProperty customRenderQueue;

    private MaterialProperty mainTex;
    private MaterialProperty tintColor;

    private MaterialProperty decalTex;

    private MaterialProperty dissloveTex;
    private MaterialProperty dissloveAmount;

    private MaterialProperty uvNoise;
    private MaterialProperty uvNoiseBias;
    private MaterialProperty uvNoiseIntensity;
    private MaterialProperty uvNoiseSpeed;

    private MaterialProperty rimColor;
    private MaterialProperty rimIntensity;
    private MaterialProperty rimFresnel;

    private Texture2D mainViewTex;
    private Texture2D decalViewTex;

    /// <summary>
    /// The rendering mode enumeration.
    /// </summary>
    private enum RenderingMode
    {
        /// <summary>
        /// The transparent object without semi-transparent areas.
        /// </summary>
        Cutout = 2,

        /// <summary>
        /// The soft edge material for transparent.
        /// </summary>
        AlphaBlend = 0,

        /// <summary>
        /// The transparent object like glass, the diffuse color will fade out
        /// but the specular will maintain.
        /// </summary>
        Additive = 1,
    }

    /// <inheritdoc/>
    protected override void FindProperties(MaterialProperty[] props)
    {
        this.renderingMode = ShaderGUI.FindProperty("_RenderingMode", props);
        this.cullMode = ShaderGUI.FindProperty("_CullMode", props);
        this.cutoff = ShaderGUI.FindProperty("_Cutoff", props);
        this.customRenderQueue = ShaderGUI.FindProperty("_CustomRenderQueue", props);

        this.mainTex = ShaderGUI.FindProperty("_MainTex", props);
        this.tintColor = ShaderGUI.FindProperty("_TintColor", props);

        this.decalTex = ShaderGUI.FindProperty("_DecalTex", props);

        this.dissloveTex = ShaderGUI.FindProperty("_DissloveTex", props);
        this.dissloveAmount = ShaderGUI.FindProperty("_DissloveAmount", props);

        this.uvNoise = ShaderGUI.FindProperty("_UVNoise", props);
        this.uvNoiseBias = ShaderGUI.FindProperty("_UVNoiseBias", props);
        this.uvNoiseIntensity = ShaderGUI.FindProperty("_UVNoiseIntensity", props);
        this.uvNoiseSpeed = ShaderGUI.FindProperty("_UVNoiseSpeed", props);

        this.rimColor = ShaderGUI.FindProperty("_RimColor", props);
        this.rimIntensity = ShaderGUI.FindProperty("_RimIntensity", props);
        this.rimFresnel = ShaderGUI.FindProperty("_RimFresnel", props);
    }

    /// <inheritdoc/>
    protected override void OnShaderGUI(
        MaterialEditor materialEditor, Material[] materials)
    {
        this.BlendModeGUI(materialEditor, materials);
        this.ColorGUI(materialEditor, materials);
        this.FogGUI(materialEditor, materials);
        this.DecalGUI(materialEditor, materials);
        this.DissolveGUI(materialEditor, materials);
        this.UVNoiseGUI(materialEditor, materials);
        this.RimGUI(materialEditor, materials);

        // Render Queue
        if (materials.Length == 1)
        {
            var customRenderQueue = EditorGUILayout.ToggleLeft(
                "Custom Render Queue", 
                this.customRenderQueue.floatValue > 0.0f);
            if (customRenderQueue)
            {
                this.customRenderQueue.floatValue = 1.0f;
                materials[0].renderQueue = EditorGUILayout.IntField(
                    "RenderQueue", materials[0].renderQueue);
            }
            else
            {
                this.customRenderQueue.floatValue = 0.0f;
            }
        }

        // Check the gray scale alpha support.
        this.CheckGrayscaleAlpha(materialEditor, materials);
    }

    /// <inheritdoc/>
    protected override void MaterialChanged(Material material)
    {
        if (this.renderingMode != null)
        {
            var renderingMode = (RenderingMode)this.renderingMode.floatValue;
            this.UpdateRenderingMode(renderingMode, material);
        }
    }

    private void UpdateRenderingMode(
        RenderingMode renderingMode, Material material)
    {
        switch (renderingMode)
        {
        case RenderingMode.Cutout:
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.EnableKeyword("_ALPHA_TEST");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            if (this.customRenderQueue.floatValue <= 0.0f)
            {
                material.renderQueue = -1;
            }

            break;
        case RenderingMode.AlphaBlend:
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHA_BLEND");
            material.DisableKeyword("_ALPHA_PREMULTIPLY");
            material.SetOverrideTag("RenderType", "Transparent");
            if (this.customRenderQueue.floatValue <= 0.0f)
            {
                material.renderQueue = 3000;
            }

            break;
        case RenderingMode.Additive:
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHA_BLEND");
            material.DisableKeyword("_ALPHA_PREMULTIPLY");
            material.SetOverrideTag("RenderType", "Transparent");
            if (this.customRenderQueue.floatValue <= 0.0f)
            {
                material.renderQueue = 3100;
            }

            break;
        }
    }

    private void BlendModeGUI(
        MaterialEditor materialEditor, Material[] materials)
    {
        EditorGUI.showMixedValue = this.renderingMode.hasMixedValue;
        var renderingMode = (RenderingMode)this.renderingMode.floatValue;
        EditorGUI.BeginChangeCheck();
        renderingMode = (RenderingMode)EditorGUILayout.Popup(
            "Rendering Mode", (int)renderingMode, BlendNames);
        if (EditorGUI.EndChangeCheck())
        {
            materialEditor.RegisterPropertyChangeUndo("Rendering Mode");
            this.renderingMode.floatValue = (float)renderingMode;
            foreach (var mat in materials)
            {
                this.UpdateRenderingMode(renderingMode, mat);
            }
        }

        EditorGUI.showMixedValue = false;

        if (this.CheckOption(
            materials,
            "Enable Alpha Test",
            "_ALPHA_TEST"))
        {
            materialEditor.RangeProperty(this.cutoff, "Cutoff");
        }

        EditorGUI.BeginChangeCheck();
        int cullMode = (int)this.cullMode.floatValue;
        var cullEnum = (CullMode)EditorGUILayout.EnumPopup(
            "Cull Mode", (CullMode)cullMode);
        if (EditorGUI.EndChangeCheck())
        {
            this.cullMode.floatValue = (float)cullEnum;
        }
    }

    private void ColorGUI(
        MaterialEditor materialEditor, Material[] materials)
    {
        var options = new GUIContent[] {
            new GUIContent("All"),
            new GUIContent("R"),
            new GUIContent("G"),
            new GUIContent("B"),
            new GUIContent("A")
        };
        var keys = new string[] {
            "_",
            "_CHANNEL_R",
            "_CHANNEL_G",
            "_CHANNEL_B",
            "_CHANNEL_A",
        };

        EditorGUI.BeginChangeCheck();
        int channel = this.ListOptions(materials, options, keys, true);
        if (channel != 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            materialEditor.TexturePropertySingleLine(
                new GUIContent("Main Texture"), this.mainTex);
            materialEditor.TextureScaleOffsetProperty(this.mainTex);
            GUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck() || this.mainViewTex == null)
            {
                EditorApplication.delayCall += () =>
                {
                    this.mainViewTex = BuildPreview(this.mainTex, channel);
                    materialEditor.Repaint();
                };
            }

            GUILayout.Box(
                this.mainViewTex, 
                GUILayout.Width(64), 
                GUILayout.Height(64));
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUI.EndChangeCheck();
            materialEditor.TextureProperty(this.mainTex, "Main Texture");
        }
        
        if (this.CheckOption(
                materials,
                "Enable Tint Color",
                "ENABLE_TINT_COLOR"))
        {
            EditorGUI.indentLevel = 1;
            materialEditor.ColorProperty(this.tintColor, "Tint Color");
            EditorGUI.indentLevel = 0;
        }
    }

    private void DecalGUI(MaterialEditor materialEditor, Material[] materials)
    {
        if (!this.CheckOption(
                materials,
                "Enable Decal",
                "ENABLE_DECAL"))
        {
            return;
        }

        EditorGUI.indentLevel = 1;
        var options = new GUIContent[] {
            new GUIContent("All"),
            new GUIContent("R"),
            new GUIContent("G"),
            new GUIContent("B"),
            new GUIContent("A")
        };
        var keys = new string[] {
            "_",
            "_DECAL_CHANNEL_R",
            "_DECAL_CHANNEL_G",
            "_DECAL_CHANNEL_B",
            "_DECAL_CHANNEL_A",
        };

        EditorGUI.BeginChangeCheck();
        int channel = this.ListOptions(materials, options, keys, true);
        if (channel != 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            materialEditor.TexturePropertySingleLine(
                new GUIContent("Decal Texture"), this.decalTex);
            materialEditor.TextureScaleOffsetProperty(this.decalTex);
            GUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck() || this.decalViewTex == null)
            {
                EditorApplication.delayCall += () =>
                {
                    this.decalViewTex = BuildPreview(this.decalTex, channel);
                    materialEditor.Repaint();
                };
            }

            GUILayout.Box(
                this.decalViewTex,
                GUILayout.Width(64),
                GUILayout.Height(64));
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUI.EndChangeCheck();
            materialEditor.TextureProperty(this.decalTex, "Decal Texture");
        }
        
        EditorGUI.indentLevel = 0;
    }

    private void DissolveGUI(
        MaterialEditor materialEditor, Material[] materials)
    {
        if (!this.CheckOption(
                materials,
                "Enable Dissolve",
                "ENABLE_DISSLOVE"))
        {
            return;
        }

        EditorGUI.indentLevel = 1;
        materialEditor.TextureProperty(
            this.dissloveTex, "Dissolve Texture");
        materialEditor.RangeProperty(this.dissloveAmount, "Dissolve Amount");
        EditorGUI.indentLevel = 0;
    }

    private void FogGUI(
        MaterialEditor materialEditor, Material[] materials)
    {
        this.CheckOption(materials, "Enable Fog", "ENABLE_FOG");
    }

    private void UVNoiseGUI(
        MaterialEditor materialEditor, Material[] materials)
    {
        if (this.CheckOption(
                materials,
                "Enable UV Noise",
                "ENABLE_UV_NOISE"))
        {
            EditorGUI.indentLevel = 1;
            materialEditor.TextureProperty(this.uvNoise, "UV Noise");
            materialEditor.FloatProperty(this.uvNoiseBias, "Noise Bias");
            materialEditor.FloatProperty(this.uvNoiseIntensity, "Noise Intensity");
            materialEditor.VectorProperty(this.uvNoiseSpeed, "UV Speed");
            EditorGUI.indentLevel = 0;
        }
    }

    private void RimGUI(
        MaterialEditor materialEditor, Material[] materials)
    {
        if (this.CheckOption(
                materials,
                "Enable Rim",
                "ENABLE_RIM"))
        {
            EditorGUI.indentLevel = 1;
            materialEditor.ColorProperty(this.rimColor, "Color");
            materialEditor.RangeProperty(this.rimIntensity, "Intensity");
            materialEditor.RangeProperty(this.rimFresnel, "Fresnel");
            EditorGUI.indentLevel = 0;
        }
    }

    private void CheckGrayscaleAlpha(MaterialEditor materialEditor, Material[] materials)
    {
        var renderingMode = (RenderingMode)this.renderingMode.floatValue;
        var mainTexture = (Texture2D)this.mainTex.textureValue;
        if (renderingMode == RenderingMode.Additive &&
            mainTexture != null &&
            !mainTexture.HasAlpha())
        {
            foreach (var material in materials)
            {
                material.EnableKeyword("ENABLE_GRAYSCALE_ALPHA");
            }
        }
        else
        {
            foreach (var material in materials)
            {
                material.DisableKeyword("ENABLE_GRAYSCALE_ALPHA");
            }
        }
    }

    private static Texture2D RedChannel(Texture2D texture)
    {
        var gray = new Texture2D(
            texture.width,
            texture.height,
            texture.format,
            texture.mipmapCount > 0);

        for (int i = 0; i < texture.width; ++i)
        {
            for (int j = 0; j < texture.height; ++j)
            {
                var c = texture.GetPixel(i, j);
                c.g = c.r;
                c.b = c.r;
                c.a = 1.0f;
                gray.SetPixel(i, j, c);
            }
        }

        gray.Apply();

        return gray;
    }

    private static Texture2D GreenChannel(Texture2D texture)
    {
        var gray = new Texture2D(
            texture.width,
            texture.height,
            texture.format,
            texture.mipmapCount > 0);

        for (int i = 0; i < texture.width; ++i)
        {
            for (int j = 0; j < texture.height; ++j)
            {
                var c = texture.GetPixel(i, j);
                c.r = c.g;
                c.b = c.g;
                c.a = 1.0f;
                gray.SetPixel(i, j, c);
            }
        }

        gray.Apply();

        return gray;
    }

    private static Texture2D BlueChannel(Texture2D texture)
    {
        var gray = new Texture2D(
            texture.width,
            texture.height,
            texture.format,
            texture.mipmapCount > 0);

        for (int i = 0; i < texture.width; ++i)
        {
            for (int j = 0; j < texture.height; ++j)
            {
                var c = texture.GetPixel(i, j);
                c.r = c.b;
                c.g = c.b;
                c.a = 1.0f;
                gray.SetPixel(i, j, c);
            }
        }

        gray.Apply();

        return gray;
    }

    private static Texture2D AlphaChannel(Texture2D texture)
    {
        var gray = new Texture2D(
            texture.width,
            texture.height,
            texture.format,
            texture.mipmapCount > 0);

        for (int i = 0; i < texture.width; ++i)
        {
            for (int j = 0; j < texture.height; ++j)
            {
                var c = texture.GetPixel(i, j);
                c.r = c.a;
                c.g = c.a;
                c.b = c.a;
                c.a = 1.0f;
                gray.SetPixel(i, j, c);
            }
        }

        gray.Apply();

        return gray;
    }

    private static Texture2D BuildReadable(Texture2D texture)
    {
        // Create a temporary RenderTexture of the same size as the texture
        var tmp = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(texture, tmp);

        // Backup the currently set RenderTexture
        var previous = RenderTexture.active;

        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;

        // Create a new readable Texture2D to copy the pixels to it
        var readableTex = new Texture2D(texture.width, texture.height);

        // Copy the pixels from the RenderTexture to the new Texture
        readableTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readableTex.Apply();

        // Reset the active RenderTexture
        RenderTexture.active = previous;

        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tmp);

        return readableTex;
    }

    private static Texture2D BuildPreview(MaterialProperty texProp, int channel)
    {
        var tex = (Texture2D)texProp.textureValue;
        if (tex == null)
        {
            return null;
        }

        var readableTex = BuildReadable(tex);
        switch (channel)
        {
        case 1:
            return RedChannel(readableTex);
        case 2:
            return GreenChannel(readableTex);
        case 3:
            return BlueChannel(readableTex);
        case 4:
            return AlphaChannel(readableTex);
        }

        return null;
    }
}