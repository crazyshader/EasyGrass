using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyGrass
{
    public class LayerAttribute : PropertyAttribute { }

    [Serializable]
    public class GrassDetailData
    {
        [SerializeField] public int BrushIndex;
        [SerializeField] public int CullDistance;
        [SerializeField] public float ShowDensity;
        [SerializeField] public Vector2 WidthScale;
        [SerializeField] public Vector2 HeightScale;
        [SerializeField] public float NoiseSpread;
        [SerializeField] public float HeightOffset;
        [SerializeField, Layer] public int DetailLayer;
        [SerializeField] public float DetailThreshold;
        [SerializeField] public bool CastShadows;
        [SerializeField] public bool ReceiveShadows;
        [SerializeField] public bool UseQuad;
        [SerializeField] public Mesh DetailMesh;
        [SerializeField] public Material DetailMaterial;
    }

    [CreateAssetMenu(menuName = "TerrainTextureExport/UnityTerrainData")]
    public class EasyGrassData : ScriptableObject
    {
        [SerializeField] public bool InstanceDraw;
        [SerializeField] public int GridSize;
        [SerializeField] public Vector3 TerrainPos;
        [SerializeField] public Vector3 TerrainSize;
        [SerializeField] public Bounds TerrainBounds;
        [SerializeField] public int HeightmapResolution;
        [SerializeField] public int DetailResolution;
        [SerializeField] public int DetailMaxDensity;
        [SerializeField] public string HeightmapPath;
        //[SerializeField] public Texture2D HeightMap;
        [SerializeField] public Texture2D NormalMap;
        [SerializeField] public List<Texture2D> DetailMapList = default;
        [SerializeField] public List<GrassDetailData> DetailDataList = default;
    }
}

