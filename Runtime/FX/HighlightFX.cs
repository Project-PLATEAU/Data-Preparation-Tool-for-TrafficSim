using UnityEngine;
using System;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

namespace TrafficSimulationTool.Runtime.FX
{
    /// <summary>
    /// Vehicle, Trafficの色変更用
    /// </summary>
    public class HighlightFX : IDisposable
    {
        public static readonly string FX_LAYER_CAR = "CarSelection";
        public static readonly string FX_LAYER_TRAFFIC = "TrafficSelection";
        public static readonly string DEFAULT_LAYER = "Default";

        private readonly string FX_ASSET_PATH = "FX";

        private GameObject fxGameObject;

        public HighlightFX()
        {
            var asset = FX_ASSET_PATH;
            var fxPrefab = Resources.Load<GameObject>(asset);
            fxGameObject = GameObject.Instantiate(fxPrefab);
            fxGameObject.name = "FX";

            var comps = fxGameObject.GetComponents<CustomPassVolume>();

            var outline = comps[0].customPasses[0] as Outline;
            outline.outlineLayer.value = LayerMask.GetMask(FX_LAYER_CAR);

            var drawrenderers = comps[1].customPasses[0] as DrawRenderersCustomPass;
            drawrenderers.layerMask.value = LayerMask.GetMask(FX_LAYER_TRAFFIC);
        }

        public void Dispose()
        {
            if (fxGameObject != null)
            {
                GameObject.DestroyImmediate(fxGameObject);
            }
        }
    }
}