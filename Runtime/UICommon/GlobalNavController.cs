using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Runtime.UICommon
{
    public class GlobalNavController : IVisualElementInitializable
    {
        public enum MenuButtonType
        {
            LOAD_DATA,
            CAMERA_TYPE_2D,
            CAMERA_TYPE_3D,
            TOGGLE_VEHICLE_ON,
            TOGGLE_TRAFFIC_ON,
            TOGGLE_VEHICLE_OFF,
            TOGGLE_TRAFFIC_OFF,
        }

        private VisualElement _rootElement;
        private Button _loadDataButton;
        private RadioButton _camera2DButton;
        private RadioButton _camera3DButton;
        private Toggle _toggleVehicle;
        private Toggle _toggleTraffic;
        private DropdownField _dataListView;

        public event Action<MenuButtonType> ButtonPressed;
        public event Action<int> DatalistSelected;

        public void Initialize(VisualElement rootElement)
        {
            _rootElement = rootElement;
            _loadDataButton = _rootElement.Q<Button>("ButtonLoadVehicleTimeline");
            _camera2DButton = _rootElement.Q<RadioButton>("Tab_2d");
            _camera3DButton = _rootElement.Q<RadioButton>("Tab_3d");
            _toggleVehicle = _rootElement.Q<Toggle>("ToggleVehicle");
            _toggleTraffic = _rootElement.Q<Toggle>("ToggleTraffic");
            _dataListView = _rootElement.Q<DropdownField>("DropDownList");

            _camera3DButton.value = true;
            _toggleVehicle.value = true;
            _toggleTraffic.value = true;

            _loadDataButton.clicked += OnLoadDataClick;
            _camera2DButton.RegisterValueChangedCallback(OnCamera2DClick);
            _camera3DButton.RegisterValueChangedCallback(OnCamera3DClick);
            _toggleVehicle.RegisterValueChangedCallback(OnToggleVehicleClick);
            _toggleTraffic.RegisterValueChangedCallback(OnToggleTrafficClick);
            _dataListView.RegisterValueChangedCallback(OnListViewSelectionChanged);
        }

        public void Dispose()
        {
            _loadDataButton.clicked -= OnLoadDataClick;
            _camera2DButton.UnregisterValueChangedCallback(OnCamera2DClick);
            _camera3DButton.UnregisterValueChangedCallback(OnCamera3DClick);
            _toggleVehicle.UnregisterValueChangedCallback(OnToggleVehicleClick);
            _toggleTraffic.UnregisterValueChangedCallback(OnToggleTrafficClick);
            _dataListView.UnregisterValueChangedCallback(OnListViewSelectionChanged);
        }

        public void SetDataSet(List<string> names)
        {
            _dataListView.choices = names;
            if (names.Count > 0)
                _dataListView.SetValueWithoutNotify(_dataListView.choices[names.Count - 1]);
            else
                _dataListView.SetValueWithoutNotify("");
        }

        private void OnListViewSelectionChanged(ChangeEvent<string> str)
        {
            DatalistSelected?.Invoke(_dataListView.index);
        }

        //Buttons
        private void OnLoadDataClick()
        {
            ButtonPressed?.Invoke(MenuButtonType.LOAD_DATA);
        }

        private void OnCamera2DClick(ChangeEvent<bool> evt)
        {
            if(evt.newValue) 
                ButtonPressed?.Invoke(MenuButtonType.CAMERA_TYPE_2D);
        }
        private void OnCamera3DClick(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                ButtonPressed?.Invoke(MenuButtonType.CAMERA_TYPE_3D);
        }
        private void OnToggleVehicleClick(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                ButtonPressed?.Invoke(MenuButtonType.TOGGLE_VEHICLE_ON);
            else
                ButtonPressed?.Invoke(MenuButtonType.TOGGLE_VEHICLE_OFF);
        }
        private void OnToggleTrafficClick(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                ButtonPressed?.Invoke(MenuButtonType.TOGGLE_TRAFFIC_ON);
            else
                ButtonPressed?.Invoke(MenuButtonType.TOGGLE_TRAFFIC_OFF);
        }
    }
}
