using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Runtime.UICommon
{
    public class LoadDataMenuController : IVisualElementInitializable
    {
        public enum MenuButtonType
        {
            LOAD_VEHICLE_TIMELINE,
            LOAD_ROAD_INDICATOR,
            LOAD_DATA_DIALOG_CLOSE,
        }

        private VisualElement _rootElement;
        private Button _loadVehicleTimelineButton;
        private Button _loadRoadIndicatorButton;
        private Button _buttonOK;
        private Button _buttonCancel;
        private TextField _textVehicle;
        private TextField _textRoadIndicator;

        public event Action<MenuButtonType> ButtonPressed;

        public void Initialize(VisualElement rootElement)
        {
            _rootElement = rootElement;
            _loadVehicleTimelineButton = _rootElement.Q<Button>("SelectButtonVehicle");
            _loadRoadIndicatorButton = _rootElement.Q<Button>("SelectButtonRoadIndicator");
            _buttonOK = _rootElement.Q<Button>("ButtonOK");
            _buttonCancel = _rootElement.Q<Button>("ButtonCancel");
            _textVehicle = _rootElement.Q<TextField>("TextFieldVehicle");
            _textRoadIndicator = _rootElement.Q<TextField>("TextFieldRoadIndicator");

            _loadVehicleTimelineButton.clicked += OnLoadVehicleTimelineClick;
            _loadRoadIndicatorButton.clicked += OnLoadRoadIndicatorClick;
            _buttonOK.clicked += OnOkButtonClick;
            _buttonCancel.clicked += OnCancelButtonClick;

            SetVisibility(false);
        }

        public void Dispose()
        {
            _loadVehicleTimelineButton.clicked -= OnLoadVehicleTimelineClick;
            _loadRoadIndicatorButton.clicked -= OnLoadRoadIndicatorClick;
            _buttonOK.clicked -= OnOkButtonClick;
            _buttonCancel.clicked -= OnCancelButtonClick;
        }

        public void ClearSelections()
        {
            SetVehicleText(string.Empty);
            SetRoadIndicatorText(string.Empty);
        }

        public void SetVisibility(bool visible)
        {
            ClearSelections();
            _rootElement.visible = visible;
        }

        public void SetEnabled(bool enabled)
        {
            _rootElement.SetEnabled(enabled);
        }


        public void SetVehicleText(string txt)
        {
            _textVehicle.value = txt;
        }

        public void SetRoadIndicatorText(string txt)
        {
            _textRoadIndicator.value = txt;
        }

        private void OnLoadVehicleTimelineClick() {
            ButtonPressed?.Invoke(MenuButtonType.LOAD_VEHICLE_TIMELINE);
        }

        private void OnLoadRoadIndicatorClick() {
            ButtonPressed?.Invoke(MenuButtonType.LOAD_ROAD_INDICATOR);
        }

        private void OnOkButtonClick()
        {
            ButtonPressed?.Invoke(MenuButtonType.LOAD_DATA_DIALOG_CLOSE);
        }

        private void OnCancelButtonClick()
        {
            SetVisibility(false ); 
        }
    }
}
