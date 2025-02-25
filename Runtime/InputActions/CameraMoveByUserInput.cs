using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// ユーザーの操作によってカメラを動かします。
    /// </summary>
    public class CameraMoveByUserInput : InputActions.ICameraMoveActions, ISubComponent
    {
	    private readonly CinemachineVirtualCamera camera;
        CameraMoveData cameraMoveSpeedData;
        private Vector2 horizontalMoveByKeyboard;
        private float verticalMoveByKeyboard;
        private Vector2 parallelMoveByMouse;
        private Vector2 rotateByMouse;
        private float zoomMoveByMouse;
        private InputActions.CameraMoveActions input;
        private bool isParallelMoveByMouse;
        private bool isRotateByMouse;
        private GameObject cameraParent;
        private RaycastHit rotateHit, parallelHit;
        private float translationFactor = 1f;

        private static bool isKeyboardActive = true;
        private static bool isMouseActive = true;

        public static bool IsCameraMoveActive { get; set; } = true;

        /// <summary>
        /// キーボードでの移動を有効にするかどうか
        /// </summary>
        public static bool IsKeyboardActive {
            get => isKeyboardActive && IsCameraMoveActive;
            set
            {
                isKeyboardActive = value;
            }
        }

        /// <summary>
        /// マウスでの移動を有効にするかどうか
        /// </summary>
        public static bool IsMouseActive {
            get => isMouseActive && IsCameraMoveActive;
            set {
                isMouseActive = value;
            }
        }

	    public CameraMoveByUserInput(CinemachineVirtualCamera camera)
	    {
		    this.camera = camera;
        }
	    
	    public void OnEnable()
	    {
		    // ユーザーの操作を受け取る準備
		    input = new InputActions.CameraMoveActions(new InputActions());
		    input.SetCallbacks(this);
		    input.Enable();
	    }

	    public void OnDisable()
	    {
		    input.Disable();
	    }
	    
		/// <summary>
		/// InputActionsからカメラWASD移動のキーボード操作を受け取り、カメラをWASD移動します。
		/// </summary>
        public void OnHorizontalMoveCameraByKeyboard(InputAction.CallbackContext context)
        {
            if (!IsKeyboardActive)
            {
                horizontalMoveByKeyboard = Vector2.zero;
                return;
            }
			if (context.performed)
			{
				var delta = context.ReadValue<Vector2>();
				horizontalMoveByKeyboard = delta;
			}else if (context.canceled)
			{
				horizontalMoveByKeyboard = Vector2.zero;
			}
		}

        /// <summary>
        /// InputActionsからカメラ上下移動のキーボード操作を受け取り、カメラを上下移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnVerticalMoveCameraByKeyboard(InputAction.CallbackContext context)
        {
            if (!IsKeyboardActive)
            {
                verticalMoveByKeyboard = 0f;
                return;
            }
            if(context.performed)
            {
                var delta = context.ReadValue<float>();
                verticalMoveByKeyboard = delta;
            }else if (context.canceled)
            {
                verticalMoveByKeyboard = 0f;
            }
        }

        /// <summary>
        /// InputActionsからマウスの左クリックドラッグを受け取り、カメラを平行移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnParallelMoveCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive)
            {
                isParallelMoveByMouse = false;
                parallelMoveByMouse = Vector2.zero;
                return;
            }
            if (context.started)
            {
                isParallelMoveByMouse = true;
                if (Camera.main == null)
                {
                    Debug.LogError("カメラが必要です");
                    return;
                }
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out parallelHit))
                {
                    translationFactor = parallelHit.distance * cameraMoveSpeedData.parallelMoveSpeed;
                }
                else
                {
                    translationFactor = 1f;
                }

            }
            else if (context.canceled)
            {
                isParallelMoveByMouse = false;
                parallelMoveByMouse = Vector2.zero;
            }
        }

        /// <summary>
        /// InputActionsからのマウスのスクロールを受け取り、カメラを前後移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnZoomMoveCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive)
            {
                zoomMoveByMouse = 0f;
                return;
            }
            if (context.performed)
            {
                var delta = context.ReadValue<float>();
                zoomMoveByMouse = delta;
            }else if (context.canceled)
            {
                zoomMoveByMouse= 0f;
            }
        }

        /// <summary>
        /// InputActionsからのマウスのスクロールを受け取り、カメラを注視点を元に回転します。
        /// </summary>
        /// <param name="context"></param>
        public void OnRotateCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive)
            {
                isRotateByMouse = false;
                rotateByMouse = Vector2.zero;
            }
            if (context.started)
            {
                if(Camera.main == null)
                {
                    Debug.LogError("カメラが必要です");
                    return;
                }
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out rotateHit))
                {
                    isRotateByMouse = true;
                }
            }
            else if (context.canceled)
            {
                isRotateByMouse = false;
                rotateByMouse = Vector2.zero;
            }
        }

        public void Start()
        {
            //カメラ回転用オブジェクト準備
            cameraParent = new GameObject("CameraParent");
            camera.transform.SetParent(cameraParent.transform);
            camera.transform.position = new Vector3(0, 0, 0);
            cameraMoveSpeedData = Resources.Load<CameraMoveData>("CameraMoveSpeedData");
            cameraParent.transform.SetPositionAndRotation(new Vector3(0,215,0), Quaternion.Euler(new Vector3(45,0,0)));
        }

		public void Update(float deltaTime)
		{
            var trans = cameraParent.transform;
            parallelMoveByMouse = Mouse.current.delta.ReadValue();
            rotateByMouse = Mouse.current.delta.ReadValue();

            MoveCameraHorizontal(cameraMoveSpeedData.horizontalMoveSpeed * deltaTime * horizontalMoveByKeyboard, trans);
            MoveCameraVertical(cameraMoveSpeedData.verticalMoveSpeed * deltaTime * verticalMoveByKeyboard, trans);
            MoveCameraParallel(parallelMoveByMouse, trans);
            MoveCameraZoom(cameraMoveSpeedData.zoomMoveSpeed * zoomMoveByMouse, trans);
            RotateCamera(cameraMoveSpeedData.rotateSpeed * rotateByMouse, trans);
            if (cameraParent.transform.position.y < cameraMoveSpeedData.heightLimitY)
            {
                cameraParent.transform.position = new Vector3(cameraParent.transform.position.x, cameraMoveSpeedData.heightLimitY, cameraParent.transform.position.z);
            }
        }
		
		/// <summary>
		/// カメラ水平移動
		/// </summary>
		private void MoveCameraHorizontal(Vector2 moveDelta, Transform cameraTrans)
		{
			var dir = new Vector3(moveDelta.x, 0.0f, moveDelta.y);
            var rot = camera.transform.eulerAngles;
            dir = Quaternion.Euler(new Vector3(0.0f, rot.y, rot.z)) * dir;
			cameraTrans.position -= dir;
		}

		/// <summary>
		/// カメラ垂直移動
		/// </summary>
		private void MoveCameraVertical(float moveDelta, Transform cameraTrans)
		{
            var dir = new Vector3(0.0f, moveDelta, 0.0f);
            cameraTrans.position += dir;
		}

        /// <summary>
        /// カメラ平行移動
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        private void MoveCameraParallel(Vector2 moveDelta, Transform cameraTrans)
        {
            if (!isParallelMoveByMouse || !IsCameraMoveActive) return;

            

            var dir = new Vector3(-moveDelta.x,  0.0f, - moveDelta.y) * translationFactor;
            var rotY = camera.transform.eulerAngles.y;
            dir = Quaternion.Euler(new Vector3(0, rotY, 0)) * dir;
            cameraTrans.position += dir;
        }

        /// <summary>
        /// カメラズーム
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        /// <param name="timeDelta"></param>
        private void MoveCameraZoom(float moveDelta, Transform cameraTrans)
        {
            var dir = new Vector3(0.0f, 0.0f, moveDelta);
            var rot = camera.transform.eulerAngles;
            dir = Quaternion.Euler(new Vector3(rot.x, rot.y, rot.z)) * dir;
            cameraTrans.position += dir;
            if(moveDelta < 0.0f || (cameraTrans.position - camera.transform.position).magnitude > cameraMoveSpeedData.zoomLimit)
            {
                camera.transform.position += dir;
            }
        }


        /// <summary>
        /// カメラ回転
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        private void RotateCamera(Vector2 moveDelta, Transform cameraTrans)
        {
            if (!isRotateByMouse) return;

            cameraTrans.RotateAround(rotateHit.point, Vector3.up, moveDelta.x);

            float pitch = camera.transform.eulerAngles.x;
            pitch = (pitch > 180) ? pitch  -360 : pitch;
            float newPitch = Mathf.Clamp(pitch - moveDelta.y, 0, 85);
            float pitchDelta = pitch - newPitch;
            cameraTrans.RotateAround(rotateHit.point, camera.transform.right, -pitchDelta);
        }
    }
}
