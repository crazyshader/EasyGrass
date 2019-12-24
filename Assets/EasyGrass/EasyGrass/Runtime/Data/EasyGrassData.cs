using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Grass.Runtime
{
    public class LayerAttribute : PropertyAttribute { }

    [Serializable]
    public class GrassDetailData
    {
        [SerializeField] public int BrushIndex;
        //[SerializeField] public float ShowDistance;
        [SerializeField] public float ShowDensity;
        [SerializeField] public float HeightOffset;
        [SerializeField, Layer] public int DetailLayer;
        [SerializeField] public float DetailThreshold;
        [SerializeField] public bool CastShadows;
        [SerializeField] public bool ReceiveShadows;
        [SerializeField] public Vector2 WidthScale;
        [SerializeField] public Vector2 HeightScale;
        [SerializeField] public bool UseQuad;
        [SerializeField] public Mesh DetailMesh;
        [SerializeField] public Material DetailMaterial;
    }

    [CreateAssetMenu(menuName = "EasyGrass/EasyGrassData")]
    public class EasyGrassData : ScriptableObject
    {
        [SerializeField] public bool InstanceDraw;
        [SerializeField] public int CullDistance;
        [SerializeField] public float DetailDensity;
        [SerializeField] public int MaxCountPerPatch;
        [SerializeField] public int HeightmapResolution;
        [SerializeField] public int DetailResolution;
        [SerializeField] public string HeightDataPath;
        [SerializeField] public string DetailDataPath;
        [SerializeField] public Vector2Int CellSize;
        [SerializeField] public Vector3 TerrainPos;
        [SerializeField] public Vector3 TerrainSize;
        //[SerializeField] public Texture2D NormalMap;
        [SerializeField] public List<GrassDetailData> DetailDataList = default;

        private void OnValidate()
        {
            if (DetailDataList != null)
            {
                foreach (var detail in DetailDataList)
                {
                    if (detail.DetailMaterial != null)
                    {
                        detail.DetailMaterial.enableInstancing = InstanceDraw;
                    }
                }
            }
        }
    }
}

