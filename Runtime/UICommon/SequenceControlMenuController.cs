using System;
using TrafficSimulationTool.Runtime.Simulator;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Runtime.UICommon
{
    public class SequenceControlMenuController : IVisualElementInitializable
    {
        public enum MenuButtonType
        {
            PLAY,
            PAUSE,
        }

        private VisualElement _rootElement;

        private RadioButton _buttonPlay;
        private RadioButton _buttonPause;
        private Slider _timelineSlider;
        private Label _dateLabel;
        private Label _timeLabel;

        public event Action<MenuButtonType> ButtonPressed;

        public event Action<SliderStateEvent> SliderStateChanged;

        public void Initialize(VisualElement rootElement)
        {
            _rootElement = rootElement;

            _buttonPlay = _rootElement.Q<RadioButton>("PlayButton");
            _buttonPause = _rootElement.Q<RadioButton>("StopButton");
            _timelineSlider = _rootElement.Q<Slider>("TimeSlider");
            _dateLabel = _rootElement.Q<Label>("LabelDate");
            _timeLabel = _rootElement.Q<Label>("LabelTime");

            _buttonPlay.RegisterValueChangedCallback(OnPlayClick);
            _buttonPause.RegisterValueChangedCallback(OnPauseClick);

            _timelineSlider.RegisterCallback<PointerDownEvent>(OnSliderPointerDown);
            _timelineSlider.RegisterCallback<PointerLeaveEvent>(OnSliderPointerLeave);
            _timelineSlider.RegisterValueChangedCallback(OnSliderValueChange);

            _timeLabel.text = string.Empty;
            _dateLabel.text = string.Empty;

            var speedButton = _rootElement.Q<Button>("SpeedControllerButton");
            var speedOptions = _rootElement.Q<GroupBox>("SpeedOptions");
            var radioButtons = speedOptions.Query<RadioButton>().ToList();

            // ボタンのクリックでリスト表示/非表示を切り替え
            speedButton.clicked += () =>
            {
                speedOptions.style.display = speedOptions.style.display == DisplayStyle.Flex
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            };

            // ラジオボタンの選択でボタンのテキストを変更
            foreach (var radioButton in radioButtons)
            {
                radioButton.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) // 選択状態なら
                    {
                        speedButton.text = radioButton.text;
                        speedOptions.style.display = DisplayStyle.None; // リストを非表示

                        var timelineManager = GameObject.FindObjectOfType<TimelineManager>();

                        timelineManager.SetSpeed(float.Parse(radioButton.text) * SubComponents.PLAYBACK_SPEED);
                    }
                });
            }
        }

        public void Dispose()
        {
            _buttonPlay.UnregisterValueChangedCallback(OnPlayClick);
            _buttonPause.UnregisterValueChangedCallback(OnPauseClick);

            _timelineSlider.UnregisterCallback<PointerDownEvent>(OnSliderPointerDown);
            _timelineSlider.UnregisterCallback<PointerLeaveEvent>(OnSliderPointerLeave);
            _timelineSlider.UnregisterValueChangedCallback(OnSliderValueChange);
        }

        public void SetEnabled(bool enabled)
        {
            _rootElement.SetEnabled(enabled);
        }

        public void SetSliderValue(float value)
        {
            _timelineSlider.SetValueWithoutNotify(value);
        }

        public void SetDateTime(DateTime dateTime)
        {
            _timeLabel.text = dateTime.ToString("hh:mm:ss");
            _dateLabel.text = dateTime.ToString("yyyy/M/d");
        }

        private void OnPlayClick(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                ButtonPressed?.Invoke(MenuButtonType.PLAY);
        }

        private void OnPauseClick(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                ButtonPressed?.Invoke(MenuButtonType.PAUSE);
        }

        private void OnSliderPointerDown(PointerDownEvent evt)
        {
            SliderStateChanged?.Invoke(new SliderStateEvent(SliderStateEvent.StateType.POINTER_DOWN, _timelineSlider.value));
        }

        private void OnSliderPointerLeave(PointerLeaveEvent evt)
        {
            SliderStateChanged?.Invoke(new SliderStateEvent(SliderStateEvent.StateType.POINTER_LEAVE, _timelineSlider.value));
        }

        private void OnSliderValueChange(ChangeEvent<float> evt)
        {
            SliderStateChanged?.Invoke(new SliderStateEvent(SliderStateEvent.StateType.VALUE_CHANGE, evt.newValue));
        }
    }

    public class SliderStateEvent
    {
        public enum StateType
        {
            POINTER_DOWN,
            POINTER_LEAVE,
            VALUE_CHANGE
        }

        public SliderStateEvent(StateType state, float value)
        {
            this.State = state;
            this.Value = value;
        }

        public StateType State { get; private set; }
        public float Value { get; private set; }
    }
}