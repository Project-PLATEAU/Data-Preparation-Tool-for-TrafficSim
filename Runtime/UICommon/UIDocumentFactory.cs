using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Runtime.UICommon
{
    /// <summary>
    /// 実行時にUIDocumentを生成します。
    /// </summary>
    public class UIDocumentFactory
    {
        private const string PanelSettingsName = "PanelSettings";
        private static readonly PanelSettings panelSettingsDefault = Resources.Load<PanelSettings>(PanelSettingsName);

        /// <summary>
        /// 新しいゲームオブジェクトを作り、そこにUIDocumentを付与し、
        /// Resourcesフォルダから<paramref name="uxmlName"/>を名前とするUXMLを読み込んで表示します。
        /// IUIDocumentInitializableを継承しているControllerはInitialize処理を行います
        /// ViewControllerのインスタンスを返します。
        /// </summary>
        public static T CreateWithUxmlName<T>(string uxmlName) where T : MonoBehaviour
        {
            var uiDocObj = new GameObject(uxmlName);
            var uiDocComponent = uiDocObj.AddComponent<UIDocument>();
            var panelSettings = panelSettingsDefault;
            if (panelSettings == null)
            {
                Debug.LogError("Panel Settings file is not found.");
            }
            uiDocComponent.panelSettings = panelSettings;
            var uiRoot = uiDocComponent.rootVisualElement;

            var visualTree = Resources.Load<VisualTreeAsset>(uxmlName);
            if (visualTree == null)
            {
                Debug.LogError("Failed to load UXML file.");
            }
            visualTree.CloneTree(uiRoot);

            T viewController = uiDocObj.AddComponent<T>();
            if(viewController is IUIDocumentInitializable)
                (viewController as IUIDocumentInitializable).Initialize(uiDocComponent);

            return viewController;
        }
    }

    public interface IUIDocumentInitializable: IDisposable
    {
        public void Initialize(UIDocument uiDocument);
    }

    public interface IVisualElementInitializable : IDisposable
    {
        public void Initialize(VisualElement rootElement);
    }
}

