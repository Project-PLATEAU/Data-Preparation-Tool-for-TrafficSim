using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// ユーザーの操作によってカメラを動かします。
    /// </summary>
    public class CameraManager : MonoBehaviour, InputActions.ICameraMoveActions, IDisposable
    {
        private Camera targetCamera;
        private CameraMoveData cameraMoveSpeedData;
        private Vector2 horizontalMoveByKeyboard;
        private float verticalMoveByKeyboard;
        private Vector2 parallelMoveByMouse;
        private Vector2 rotateByMouse;
        private float zoomMoveByMouse;
        private InputActions.CameraMoveActions input;
        private bool isParallelMoveByMouse;
        private bool isRotateByMouse;
        private GameObject cameraParent;
        private RaycastHit hit;

        /// <summary>
        /// キーボードでの移動を有効にするかどうかです
        /// </summary>
        public static bool IsKeyboardActive = true;

        /// <summary>
        /// マウスでの移動を有効にするかどうか
        /// </summary>
        public static bool IsMouseActive = true;

        public void Initialize(Camera camera)
        {
            this.targetCamera = camera;

            //カメラ回転用オブジェクト準備
            cameraParent = new GameObject("CameraParent");
            cameraParent.transform.position = targetCamera.transform.position;
            cameraParent.transform.rotation = targetCamera.transform.rotation;
            targetCamera.transform.parent = cameraParent.transform;
            targetCamera.transform.localPosition = new Vector3(0, 0, 0);
            targetCamera.transform.localRotation = Quaternion.identity;
            cameraMoveSpeedData = Resources.Load<CameraMoveData>("CameraMoveSpeedData");

            //default value
            cameraMoveSpeedData.heightLimitY = 0;

            // ユーザーの操作を受け取る準備
            input = new InputActions.CameraMoveActions(new InputActions());
            input.SetCallbacks(this);
            input.Enable();
        }

        public void Dispose()
        {
            input.Disable();
            input.RemoveCallbacks(this);
        }

        public void SetEnable(bool bEnabled)
        {
            if (bEnabled)
                input.Enable();
            else
                input.Disable();
        }

        public void OnEnable()
        {
            //input.Enable();
        }

        public void OnDisable()
        {
            input.Disable();
        }

        public enum CameraViewType
        {
            TYPE_2D,
            TYPE_3D,
        }

        private class PostionAndRotation
        {
            public Vector3 Position { get; private set; }
            public Quaternion Rotation { get; private set; }

            public PostionAndRotation(Transform transform)
            {
                Position = transform.position;
                Rotation = transform.rotation;
            }
        }

        private PostionAndRotation previousCameraTransform;

        public CameraViewType CurrentCameraViewType { get; private set; } = CameraViewType.TYPE_3D;

        public void SetCameraViewType(CameraViewType type)
        {
            CurrentCameraViewType = type;
            if (type == CameraViewType.TYPE_2D)
            {
                previousCameraTransform = new PostionAndRotation(targetCamera.transform);

                var current = targetCamera.transform.position;
                Vector3 hitPoint = current;
                Ray ray = new Ray(targetCamera.transform.position, targetCamera.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, targetCamera.farClipPlane))
                    hitPoint = hit.point;
                targetCamera.transform.position = new Vector3(hitPoint.x, targetCamera.farClipPlane - 10f, hitPoint.z);

                targetCamera.orthographic = true;
                targetCamera.orthographicSize = 50;
                targetCamera.transform.rotation = Quaternion.AngleAxis(90f, Vector3.right);
            }
            else if (type == CameraViewType.TYPE_3D)
            {
                Vector3 hitPoint = Vector3.zero;
                Ray ray = new Ray(targetCamera.transform.position, targetCamera.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, targetCamera.farClipPlane))
                    hitPoint = hit.point;

                targetCamera.orthographic = false;
                if (previousCameraTransform != null)
                {
                    targetCamera.transform.SetPositionAndRotation(previousCameraTransform.Position, previousCameraTransform.Rotation);
                    //targetCamera.transform.position = previousCameraTransform.Position;
                    if (hitPoint != Vector3.zero)
                        targetCamera.transform.LookAt(hitPoint);
                }

                previousCameraTransform = null;
            }
        }

        /// <summary>
        /// InputActionsからカメラWASD移動のキーボード操作を受け取り、カメラをWASD移動します。
        /// </summary>
        public void OnHorizontalMoveCameraByKeyboard(InputAction.CallbackContext context)
        {
            if (!IsKeyboardActive) return;
            if (context.performed)
            {
                var delta = context.ReadValue<Vector2>();
                horizontalMoveByKeyboard = delta;
            }
            else if (context.canceled)
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
            if (!IsKeyboardActive) return;
            if (context.performed)
            {
                var delta = context.ReadValue<float>();
                verticalMoveByKeyboard = delta;
            }
            else if (context.canceled)
            {
                verticalMoveByKeyboard = 0f;
            }
        }

        private Transform hitItem; //クリックアイテム記憶

        /// <summary>
        /// InputActionsからマウスの左クリックドラッグを受け取り、カメラを平行移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnParallelMoveCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive) return;
            if (context.started)
            {
                isParallelMoveByMouse = true;

                //Click検知(Press)
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
                {
                    hitItem = hit.transform;
                }
            }
            else if (context.canceled)
            {
                isParallelMoveByMouse = false;
                parallelMoveByMouse = Vector2.zero;

                //Click検知(Release)
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
                {
                    if (hitItem == hit.transform)
                    {
                        if (hitItem?.gameObject?.TryGetComponent<TrafficFocusArea>(out var comp) ?? false)
                        {
                            //Debug.Log($"hit: {comp.gameObject.name}");
                            comp.OnClicked();
                        }
                        else if (hitItem?.gameObject?.TryGetComponent<TrafficFocusGroup>(out var comp2) ?? false)
                        {
                            //Debug.Log($"hit: {comp.gameObject.name}");
                            comp2.OnClicked();
                        }
                    }
                }
                hitItem = null;
            }
        }

        /// <summary>
        /// InputActionsからのマウスのスクロールを受け取り、カメラを前後移動します。
        /// </summary>
        /// <param name="context"></param>
        public void OnZoomMoveCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive) return;

            if (context.performed)
            {
                var delta = context.ReadValue<float>();
                zoomMoveByMouse = delta;
            }
            else if (context.canceled)
            {
                zoomMoveByMouse = 0f;
            }
        }

        public void OnRotateCameraByMouse(InputAction.CallbackContext context)
        {
            if (!IsMouseActive) return;
            if (context.started)
            {
                if (Camera.main == null)
                {
                    Debug.LogError("カメラが必要です");
                    return;
                }
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    //Debug.Log(hit.point);
                    //isRotateByMouse = true;
                }
                else
                {
                    hit.point = Camera.main.transform.position;
                }

                isRotateByMouse = true;
            }
            else if (context.canceled)
            {
                isRotateByMouse = false;
                rotateByMouse = Vector2.zero;
            }
        }

        public void Update()
        {
            if (cameraParent == null)
                return;

            var deltaTime = Time.deltaTime;
            var trans = cameraParent.transform;
            parallelMoveByMouse = Mouse.current.delta.ReadValue();
            rotateByMouse = Mouse.current.delta.ReadValue();

            MoveCameraHorizontal(cameraMoveSpeedData.horizontalMoveSpeed * deltaTime * horizontalMoveByKeyboard, trans);
            MoveCameraVertical(cameraMoveSpeedData.verticalMoveSpeed * deltaTime * verticalMoveByKeyboard, trans);
            MoveCameraParallel(cameraMoveSpeedData.parallelMoveSpeed * deltaTime * parallelMoveByMouse, trans);
            MoveCameraZoom(cameraMoveSpeedData.zoomMoveSpeed * deltaTime * zoomMoveByMouse, trans);
            RotateCamera(cameraMoveSpeedData.rotateSpeed * deltaTime * rotateByMouse, trans);
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
            var rot = targetCamera.transform.eulerAngles;
            dir = Quaternion.Euler(new Vector3(0.0f, rot.y, rot.z)) * dir;
            cameraTrans.position -= dir;
        }

        /// <summary>
        /// カメラ垂直移動
        /// </summary>
        private void MoveCameraVertical(float moveDelta, Transform cameraTrans)
        {
            if (CurrentCameraViewType == CameraViewType.TYPE_3D)
            {
                var dir = new Vector3(0.0f, moveDelta, 0.0f);
                cameraTrans.position += dir;
            }
            else if (CurrentCameraViewType == CameraViewType.TYPE_2D)
            {
                ZoomCamera2D(-moveDelta);
            }
        }

        /// <summary>
        /// カメラ平行移動
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        private void MoveCameraParallel(Vector2 moveDelta, Transform cameraTrans)
        {
            if (!isParallelMoveByMouse) return;
            var dir = new Vector3(-moveDelta.x, 0.0f, -moveDelta.y);
            var rotY = targetCamera.transform.eulerAngles.y;
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
            if (CurrentCameraViewType == CameraViewType.TYPE_3D)
            {
                var dir = new Vector3(0.0f, 0.0f, moveDelta);
                var rot = targetCamera.transform.eulerAngles;
                dir = Quaternion.Euler(new Vector3(rot.x, rot.y, rot.z)) * dir;
                //Debug.Log((cameraTrans.position - camera.transform.position).magnitude);
                //Debug.Log((cameraTrans.position - camera.transform.position).magnitude > cameraMoveSpeedData.zoomLimit);
                cameraTrans.position += dir;
                if (moveDelta < 0.0f || (cameraTrans.position - targetCamera.transform.position).magnitude > cameraMoveSpeedData.zoomLimit)
                {
                    targetCamera.transform.position += dir;
                    //return;
                }
                //if (moveDelta > 0.0f)
                //    cameraTrans.position += dir;
            }
            else if (CurrentCameraViewType == CameraViewType.TYPE_2D)
            {
                ZoomCamera2D(moveDelta);
            }
        }

        //2DカメラでのZoom
        private void ZoomCamera2D(float delta)
        {
            var size = targetCamera.orthographicSize - delta * 0.1f;
            if (size > 0f)
                targetCamera.orthographicSize = size;
        }

        /// <summary>
        /// カメラ回転
        /// </summary>
        /// <param name="moveDelta"></param>
        /// <param name="cameraTrans"></param>
        private void RotateCamera(Vector2 moveDelta, Transform cameraTrans)
        {
            if (!isRotateByMouse) return;

            //中心を固定した回転
            //camera.transform.RotateAround(cameraTrans.position, Vector3.up, moveDelta.x);
            //camera.transform.RotateAround(cameraTrans.position, camera.transform.right, -moveDelta.y);

            cameraTrans.RotateAround(hit.point, Vector3.up, moveDelta.x);
            //cameraTrans.RotateAround(hit.point, camera.transform.right, -moveDelta.y);

            float pitch = targetCamera.transform.eulerAngles.x;
            //Debug.Log("OriginPitch: "+ pitch);
            pitch = (pitch > 180) ? pitch - 360 : pitch;
            float newPitch = Mathf.Clamp(pitch - moveDelta.y, 0, 85);
            float pitchDelta = pitch - newPitch;
            //Debug.Log("NowPitch " + pitch + " NewPitch" + newPitch + "PitchDelta"+pitchDelta);

            //3Dカメラの場合
            if (CurrentCameraViewType == CameraViewType.TYPE_3D)
                cameraTrans.RotateAround(hit.point, targetCamera.transform.right, -pitchDelta);
        }
    }
}