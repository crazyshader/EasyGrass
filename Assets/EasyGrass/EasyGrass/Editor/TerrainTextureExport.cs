using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using EasyGrass;

public class TerrainTextureExport : EditorWindow
{
    private enum ExportType
    {
        Default,
        DiffuseAndNormal,
        IndexAndControl,
        Audio,
        Detail,
        Normal,
    }

    private enum TextureArrayType
    {
        Diffuse,
        Normal,
    }

    private class IndexAndControl
    {
        public int Index;
        public float TexIndex;
        public float TexBlend;
    }

    private Terrain _targetTerrain;
    private DefaultAsset _savePath;
    private ExportType _exportType;
    private uint _audioLayerCount = 2;
    private EasyGrassData _easyGrassData;

    [MenuItem("Tools/Terrain/TextureExport")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TerrainTextureExport));
    }

    private void OnCreateTextureArray(List<Texture2D> textureList, string path, TextureArrayType textureType)
    {
        Texture2D texture = textureList[0];
        Texture2DArray textureArray = new Texture2DArray(texture.width, texture.height, textureList.Count, texture.format, texture.mipmapCount > 1)
        {
            
            anisoLevel = texture.anisoLevel,
            filterMode = texture.filterMode,
            wrapMode = texture.wrapMode
        };

        for (int i = 0; i < textureList.Count; i++)
            for (int m = 0; m < texture.mipmapCount; m++)
                Graphics.CopyTexture(textureList[i], 0, m, textureArray, i, m);

        var existing = AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(textureArray, existing);
        }
        else
        {
            AssetDatabase.CreateAsset(textureArray, path);
        }

        AssetDatabase.ImportAsset(path);
        AssetDatabase.Refresh();
    }

    private string GetSelectedPath()
    {
        string path = "Assets";
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                break;
            }
        }
        return path;
    }

    private string GetSavePath()
    {
        string path = "Assets";
        if (_savePath != null)
        {
            path = AssetDatabase.GetAssetPath(_savePath);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }
        }
        else
        {
            Debug.LogError("Please select save path.");
        }

        return path;
    }

    private void ExportDiffuseAndNormalMap()
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        TerrainLayer[] terrainLayerArray = terrainData.terrainLayers;

        List<Texture2D> diffuselist = new List<Texture2D>();
        List<Texture2D> normalMaplist = new List<Texture2D>();
        for (int i = 0; i < terrainLayerArray.Length; i++)
        {
            if (terrainLayerArray[i].diffuseTexture != null)
            {
                diffuselist.Add(terrainLayerArray[i].diffuseTexture);
            }
            if (terrainLayerArray[i].normalMapTexture)
            {
                normalMaplist.Add(terrainLayerArray[i].normalMapTexture);
            }
        }

        if (diffuselist.Count <= 0)
        {
            Debug.LogError("The number of terrain splat must be greater than 0.");
            return;
        }
        string path = GetSavePath();
        string relativePath = path.Replace(Application.dataPath, "");
        OnCreateTextureArray(diffuselist, relativePath + "/TerrainTexture_Diffuse.asset", TextureArrayType.Diffuse);

        if (diffuselist.Count != normalMaplist.Count)
        {
            Debug.LogError("The number of diffuse and the number of normal are inconsistent.");
        }
        else
        {
            OnCreateTextureArray(normalMaplist, relativePath + "/TerrainTexture_Normal.asset", TextureArrayType.Normal);
        }
    }

    private void ExportAudioMap()
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        TerrainLayer[] terrainLayArray = terrainData.terrainLayers;
        Texture2D[] alphaMapArray = terrainData.alphamapTextures;
        int textureNum = terrainLayArray.Length;
        float maxTexCount = textureNum - 1;
        int witdh = alphaMapArray[0].width;
        int height = alphaMapArray[0].height;

        Texture2D audioTex = new Texture2D(witdh, height, TextureFormat.RGBA32, false, true);

        Color curColor = Color.clear;
        Color audioColor = Color.clear;
        float index = 0.0f;
        int controlMapNumber = 0;
        int controlChannelNum = 0;
        int colorChannel = 0;
        float totalWeight = 0;
        List<IndexAndControl> indexAndblendList = new List<IndexAndControl>(8);
        for (int i = 0; i < 8; i++)
        {
            indexAndblendList.Add(new IndexAndControl());
        }

        for (int j = 0; j < witdh; j++)
        {
            for (int k = 0; k < height; k++)
            {
                for (int i = 0; i < textureNum; i++)
                {
                    controlMapNumber = (i % 16) / 4;
                    controlChannelNum = i % 4;
                    curColor = alphaMapArray[controlMapNumber].GetPixel(j, k);
                    index = (float)i / maxTexCount;
                    switch ((TextureChannel)controlChannelNum)
                    {
                        case TextureChannel.R:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.r;
                            break;
                        case TextureChannel.G:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.g;
                            break;
                        case TextureChannel.B:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.b;
                            break;
                        case TextureChannel.A:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.a;
                            break;
                    }
                }

                var datas = indexAndblendList.OrderByDescending(item => item.TexBlend).Take((int)_audioLayerCount);
                //datas = datas.OrderByDescending(item => item.TexIndex);

                totalWeight = datas.Sum(item => item.TexBlend);
                totalWeight = totalWeight > 0.0f ? totalWeight : 1.0f;
                colorChannel = 0;
                audioColor = Color.clear;

                foreach (var indexAndblend in datas)
                {
                    if (colorChannel < _audioLayerCount)
                    {
                        switch ((TextureChannel)colorChannel)
                        {
                            case TextureChannel.R:
                                audioColor.r = indexAndblend.TexIndex;
                                audioColor.b = indexAndblend.TexBlend / totalWeight;
                                break;
                            case TextureChannel.G:
                                audioColor.g = indexAndblend.TexIndex;
                                audioColor.a = indexAndblend.TexBlend / totalWeight;
                                break;
                        }
                        colorChannel++;
                    }
                }

                audioTex.SetPixel(j, k, audioColor);
            }
        }

        string assets = "Assets/";
        string absolutePath = Application.dataPath;
        string path = GetSavePath();
        if (path.Length > assets.Length)
        {
            absolutePath = Path.Combine(Application.dataPath, path.Substring(assets.Length));
        }

        audioTex.Apply();
        byte[] audioBytes = audioTex.EncodeToPNG();
        string audioTexturePath = Path.Combine(absolutePath, "TerrainTexture_Audio.png");
        File.WriteAllBytes(audioTexturePath, audioBytes);
        AssetDatabase.ImportAsset(audioTexturePath);

        AssetDatabase.Refresh();
    }

    private void ExportIndexAndControlMap()
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        TerrainLayer[] terrainLayArray = terrainData.terrainLayers;
        Texture2D[] alphaMapArray = terrainData.alphamapTextures;
        int textureNum = terrainLayArray.Length;
        float maxTexCount = textureNum - 1;
        int witdh = alphaMapArray[0].width;
        int height = alphaMapArray[0].height;

        Texture2D indexTex = new Texture2D(witdh, height, TextureFormat.RGBA32, false, true);
        Texture2D blendTex = new Texture2D(witdh, height, TextureFormat.RGBA32, false, true);

        Color curColor = Color.clear;
        Color indexColor = Color.clear;
        Color blendColor = Color.clear;
        float index = 0.0f;
        int controlMapNumber = 0;
        int controlChannelNum = 0;
        int colorChannel = 0;
        List<IndexAndControl> indexAndblendList = new List<IndexAndControl>(8);
        for (int i = 0; i < 8; i++)
        {
            indexAndblendList.Add(new IndexAndControl());
        }

        for (int j = 0; j < witdh; j++)
        {
            for (int k = 0; k < height; k++)
            {
                for (int i = 0; i < textureNum; i++)
                {
                    controlMapNumber = (i % 16) / 4;
                    controlChannelNum = i % 4;
                    curColor = alphaMapArray[controlMapNumber].GetPixel(j, k);
                    index = (float)i / maxTexCount;

                    switch ((TextureChannel)controlChannelNum)
                    {
                        case TextureChannel.R:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.r;
                            break;
                        case TextureChannel.G:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.g;
                            break;
                        case TextureChannel.B:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.b;
                            break;
                        case TextureChannel.A:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.a;
                            break;
                    }
                }

                var datas = indexAndblendList.OrderByDescending(item => item.TexBlend).Take(4);
                //datas = datas.OrderByDescending(item => item.TexIndex);

                colorChannel = 0;
                indexColor = Color.clear;
                blendColor = Color.clear;

                foreach (var indexAndblend in datas)
                {
                    if (colorChannel < 4)
                    {
                        switch ((TextureChannel)colorChannel)
                        {
                            case TextureChannel.R:
                                indexColor.r = indexAndblend.TexIndex;
                                blendColor.r = indexAndblend.TexBlend;
                                break;
                            case TextureChannel.G:
                                indexColor.g = indexAndblend.TexIndex;
                                blendColor.g = indexAndblend.TexBlend;
                                break;
                            case TextureChannel.B:
                                indexColor.b = indexAndblend.TexIndex;
                                blendColor.b = indexAndblend.TexBlend;
                                break;
                            case TextureChannel.A:
                                indexColor.a = indexAndblend.TexIndex;
                                blendColor.a = indexAndblend.TexBlend;
                                break;
                        }
                        colorChannel++;
                    }
                }

                indexTex.SetPixel(j, k, indexColor);
                blendTex.SetPixel(j, k, blendColor);
            }
        }

        string assets = "Assets/";
        string absolutePath = Application.dataPath;
        string path = GetSavePath();
        if (path.Length > assets.Length)
        {
            absolutePath = Path.Combine(Application.dataPath, path.Substring(assets.Length));
        }

        indexTex.Apply();
        byte[] bytes = indexTex.EncodeToPNG();
        string indexTexturePath = Path.Combine(absolutePath, "TerrainTexture_Index.png");
        File.WriteAllBytes(indexTexturePath, bytes);
        AssetDatabase.ImportAsset(indexTexturePath);

        blendTex.Apply();
        byte[] blendBytes = blendTex.EncodeToPNG();
        string controlTexturePath = Path.Combine(absolutePath, "TerrainTexture_Control.png");
        File.WriteAllBytes(controlTexturePath, blendBytes);
        AssetDatabase.ImportAsset(controlTexturePath);

        AssetDatabase.Refresh();
    }

    private void ExportDetailMap()
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        int detailLayerNum = terrainData.detailPrototypes.Length;
        if (detailLayerNum <= 0)
        {
            Debug.LogError($"'{_targetTerrain.name}' detail layer count is zero.");
            return;
        }

        int detailTextureNum = detailLayerNum / 4 + 1;
        Color[] densityColor = new Color[detailTextureNum];
        Texture2D[] detailTextureArray = new Texture2D[detailTextureNum];
        for (int i = 0; i < detailTextureNum; i++)
        {
            densityColor[i] = Color.clear;
            detailTextureArray[i] = new Texture2D(terrainData.detailWidth, terrainData.detailHeight, TextureFormat.RGBA32, false, true);
        }

        int totalMaxDensity = 0;
        int[] layerMaxDensityList = new int[detailLayerNum];
        int detailMapNumber = 0;
        int detailChannelNum = 0;
        int[][,] detailMapData = new int[detailLayerNum][,];
        for (int i = 0; i < detailLayerNum; i++)
        {
            int layerMaxDensity = 0;
            detailMapData[i] = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, i);
            for (int j = 0; j < detailMapData[0].GetLength(0); j++)
            {
                for (int k = 0; k < detailMapData[0].GetLength(1); k++)
                {
                    var density = detailMapData[i][j, k];
                    layerMaxDensity = Mathf.Max(layerMaxDensity, density);
                    totalMaxDensity = Mathf.Max(totalMaxDensity, density);
                }
            }
            layerMaxDensityList[i] = layerMaxDensity;
        }

        for (int j = 0; j < detailMapData[0].GetLength(0); j++)
        {
            for (int k = 0; k < detailMapData[0].GetLength(1); k++)
            {
                for (int i = 0; i < detailLayerNum; i++)
                {
                    detailMapNumber = (i % 16) / 4;
                    detailChannelNum = i % 4;
                    var density = detailMapData[i][j, k];

                    switch ((TextureChannel)detailChannelNum)
                    {
                        case TextureChannel.R:
                            densityColor[detailMapNumber].r = (float)density / (float)totalMaxDensity;
                            break;
                        case TextureChannel.G:
                            densityColor[detailMapNumber].g = (float)density / (float)totalMaxDensity;
                            break;
                        case TextureChannel.B:
                            densityColor[detailMapNumber].b = (float)density / (float)totalMaxDensity;
                            break;
                        case TextureChannel.A:
                            densityColor[detailMapNumber].a = (float)density / (float)totalMaxDensity;
                            break;
                    }
                }

                for (int l = 0; l < detailTextureNum; l++)
                {
                    detailTextureArray[l].SetPixel(k, j, densityColor[l]);
                }
            }
        }

        string assets = "Assets/";
        string absolutePath = Application.dataPath;
        string path = GetSavePath();
        if (path.Length > assets.Length)
        {
            absolutePath = Path.Combine(Application.dataPath, path.Substring(assets.Length));
        }

        for (int i = 0; i < detailTextureNum; i++)
        {
            detailTextureArray[i].Apply();
            byte[] bytes = detailTextureArray[i].EncodeToPNG();
            string detailTexturePath = Path.Combine(absolutePath, $"TerrainDetailTexture_{i}.png");
            File.WriteAllBytes(detailTexturePath, bytes);
        }

        if (_easyGrassData.DetailMapList == null)
        {
            _easyGrassData.DetailMapList = new List<Texture2D>();
        }
        else
        {
            _easyGrassData.DetailMapList.Clear();
        }
        for (int i = 0; i < detailTextureNum; i++)
        {
            var savePath = Path.Combine(path, $"TerrainDetailTexture_{i}.png");
            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
            _easyGrassData.DetailMapList.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(savePath));
        }

        if (_easyGrassData.DetailDataList == null)
        {
            _easyGrassData.DetailDataList = new List<GrassDetailData>();
        }
        else
        {
            _easyGrassData.DetailDataList.Clear();
        }
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            if (layerMaxDensityList[i] < 0.001f) continue;
            var detailPrototype = terrainData.detailPrototypes[i];
            GrassDetailData detailData = new GrassDetailData
            {
                BrushIndex = i,
                CullDistance = 100,
                ShowDensity = 1,
                DetailMesh = null,
                DetailMaterial = null,
                DetailThreshold = 0,
                DetailLayer = 0,
                CastShadows = false,
                ReceiveShadows = false,
                WidthScale = new Vector2(detailPrototype.minWidth, detailPrototype.maxWidth),
                HeightScale = new Vector2(detailPrototype.minHeight, detailPrototype.maxHeight),
                NoiseSpread = detailPrototype.noiseSpread
            };
            _easyGrassData.DetailDataList.Add(detailData);
        }
        _easyGrassData.InstanceDraw = true;
        _easyGrassData.GridSize = 32;
        _easyGrassData.TerrainPos = _targetTerrain.transform.position;
        _easyGrassData.TerrainSize = terrainData.size;
        _easyGrassData.TerrainBounds = terrainData.bounds;
        _easyGrassData.HeightmapResolution = terrainData.heightmapResolution;
        _easyGrassData.DetailResolution = terrainData.detailResolution;
        _easyGrassData.DetailMaxDensity = totalMaxDensity;
        Debug.Log($"Terrain size:{_easyGrassData.TerrainSize}");
        Debug.Log($"Terrain Heightmap Resolutio:{_easyGrassData.HeightmapResolution}");
        Debug.Log($"Terrain Detail Resolution:{_easyGrassData.DetailResolution}");
        Debug.Log($"Terrain detail max density:{_easyGrassData.DetailMaxDensity}");
        Debug.Log($"Terrain detail save path:{absolutePath}");

        AssetDatabase.Refresh();
        EditorUtility.SetDirty(_easyGrassData);
        AssetDatabase.SaveAssets();
        EditorUtility.UnloadUnusedAssetsImmediate();
    }

    private void CheckTerrainData()
    {
        if (_easyGrassData != null)
            return;

        try
        {
            string path = GetSavePath();
            path = Path.Combine(path, "EasyGrassData.asset");
            _easyGrassData = AssetDatabase.LoadAssetAtPath<EasyGrassData>(path);
            if (_easyGrassData == null)
            {
                _easyGrassData = ScriptableObject.CreateInstance<EasyGrassData>();
                AssetDatabase.CreateAsset(_easyGrassData, path);
                AssetDatabase.SaveAssets();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);  
        }
    }

    private void ExportNormalMap()
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        Texture2D normalTex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.RGB24, false, true);
        Color[] normalData = new Color[normalTex.width * normalTex.height];
        for (int x = 0; x < normalTex.width; x++)
        {
            for (int y = 0; y < normalTex.height; y++)
            {
                float xCoord = x / (float)normalTex.width;
                float yCoord = y / (float)normalTex.width;
                Vector3 normal = terrainData.GetInterpolatedNormal(xCoord, yCoord);

                int index = x + y * normalTex.width;
                normalData[index].r = (normal.x + 1) * 0.5f;
                normalData[index].g = (normal.y + 1) * 0.5f;
                normalData[index].b = (normal.z + 1) * 0.5f;
            }
        }
        normalTex.SetPixels(normalData);
        normalTex.Apply();

        string assets = "Assets/";
        string absolutePath = Application.dataPath;
        string path = GetSavePath();
        if (path.Length > assets.Length)
        {
            absolutePath = Path.Combine(Application.dataPath, path.Substring(assets.Length));
        }

        byte[] bytes = normalTex.EncodeToPNG();
        string normalTexturePath = Path.Combine(absolutePath, $"TerrainNormalTexture.png");
        File.WriteAllBytes(normalTexturePath, bytes);
        var savePath = Path.Combine(path, $"TerrainNormalTexture.png");
        AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);

        /*
        var heightmapTexture = terrainData.heightmapTexture;
        Texture2D heightmapTex = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.R16, false, true);
        RenderTexture.active = heightmapTexture;
        heightmapTex.ReadPixels(new Rect(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution), 0, 0);
        RenderTexture.active = null;
        bytes = heightmapTex.EncodeToPNG();
        string heightTexturePath = Path.Combine(absolutePath, $"TerrainHeightTexture.png");
        File.WriteAllBytes(heightTexturePath, bytes);
        savePath = Path.Combine(path, $"TerrainHeightTexture.png");
        AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
        */

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        EditorUtility.UnloadUnusedAssetsImmediate();
    }

    private void OnExportTerrainTexture()
    {
        CheckTerrainData();
        if (_targetTerrain == null)
        {
            Debug.LogError("Please select one terrain.");
            return;
        }

        if (_exportType == ExportType.DiffuseAndNormal)
        {
            ExportDiffuseAndNormalMap();
        }
        else if (_exportType == ExportType.IndexAndControl)
        {
            ExportIndexAndControlMap();
        }
        else if (_exportType == ExportType.Audio)
        {
            ExportAudioMap();
        }
        else if (_exportType == ExportType.Detail)
        {
            ExportDetailMap();
        }
        else if (_exportType == ExportType.Normal)
        {
            ExportNormalMap();
        }
        else
        {
            ExportDiffuseAndNormalMap();
            ExportIndexAndControlMap();
            ExportAudioMap();
            ExportDetailMap();
            ExportNormalMap();
        }

        EditorUtility.UnloadUnusedAssetsImmediate();
        Debug.Log("The terrain export finished.");
    }

    private void OnGUI()
    {
        _targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", _targetTerrain, typeof(Terrain), true);
        _savePath = (DefaultAsset)EditorGUILayout.ObjectField("Save Path", _savePath, typeof(DefaultAsset), false);
        _easyGrassData = (EasyGrassData)EditorGUILayout.ObjectField("Terrain Data", _easyGrassData, typeof(EasyGrassData), false);
        _exportType = (ExportType)EditorGUILayout.EnumPopup("Export Texture Type", _exportType);
        if (_exportType != ExportType.DiffuseAndNormal && _exportType != ExportType.IndexAndControl 
            && _exportType != ExportType.Detail && _exportType != ExportType.Normal)
        {
            _audioLayerCount = (uint)EditorGUILayout.IntField("Audio Layer Count", (int)_audioLayerCount);
        }
        GUILayout.Space(10);

        if (GUILayout.Button("Export Terrain Texture"))
        {
            OnExportTerrainTexture();
        }
    }
}