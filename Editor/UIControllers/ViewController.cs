using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// メインメニュータブのコントローラの基底クラス
    /// </summary>
    public class ViewController : VisualElement
    {
        /// <summary>
        /// 紐づけるビュー
        /// </summary>
        protected VisualElement rootElement;

        /// <summary>
        /// 親ウィンドウ
        /// </summary>
        protected TrafficSimulationToolWindow parentWindow;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="element"></param>
        /// <param name="parent"></param>
        public ViewController(VisualElement element, TrafficSimulationToolWindow parent)
        {
            rootElement = element;

            parentWindow = parent;

            Initialize();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// タブ切り替えによりビューがアクティブになった際の処理
        /// </summary>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// タブ切り替えによりビューが非アクティブになった際の処理
        /// </summary>
        public virtual void OnDisable()
        {
        }
    }
}