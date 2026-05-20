using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleRtsCamera.Scripts
{
    public class SimpleRtsCamera : MonoBehaviour
    {
        [Header("RTS Camera Settings")]
        [Header("Move")]
        [SerializeField] private float _moveSpeed = 200;
        [SerializeField] private float _edgeThreshold = 5;
        [SerializeField] private float _rightMouseSpeedMultiplier = 10;

        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 1000;

        [Header("Rotate")]
        [SerializeField] private float _rotateSpeed = 100f; 
        [SerializeField] private float _rotateFallback = 1000f;
        
        [Tooltip("Скорость вертикального наклона камеры (колесиком мыши)")]
        [SerializeField] private float _verticalRotateSpeed = 150f;
        [Tooltip("Минимальный угол наклона (чтобы не смотреть слишком снизу)")]
        [SerializeField] private float _minVerticalAngle = 20f;
        [Tooltip("Максимальный угол наклона (чтобы не перевернуться)")]
        [SerializeField] private float _maxVerticalAngle = 85f;

        [Header("Collision & Ground")]
        [Tooltip("Минимальная высота над уровнем земли")]
        [SerializeField] private float _minHeightAboveGround = 5f;
        [Tooltip("Максимальная высота (потолок)")]
        [SerializeField] private float _maxHeightAboveGround = 120f;
        [Tooltip("Слой, на котором находятся ваши 4 террейна")]
        [SerializeField] private LayerMask _groundLayer;

        private PlayerInput _playerInput;
        private Camera _camera;
        private Vector2 _moveInput;
        private Vector2 _mousePositionInput;
        private float _rightMouseInput;
        private Vector2 _initialMousePosition;
        private float _scrollMouseInput;
        private float _middleMouseInput;
        private Vector3 _rotationPivot; // Запоминает точку, вокруг которой крутимся
        private bool _isRotating;       // Флаг, чтобы понимать, начали мы вращение или нет

        private InputAction _rotateAction;
        private float _currentPitch;

        private void Awake()
        {
            _playerInput = FindAnyObjectByType<PlayerInput>();
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = GetComponentInChildren<Camera>();

            _rotateAction = _playerInput.actions["CameraRotate"];
            
            _currentPitch = transform.eulerAngles.x;
        }

        private void OnEnable()
        {
            _playerInput.actions["CameraMove"].performed += MoveHandler;
            _playerInput.actions["CameraMove"].canceled += MoveHandler;
            _playerInput.actions["MousePosition"].performed += MousePositionHandler;
            _playerInput.actions["MousePosition"].canceled += MousePositionHandler;
            _playerInput.actions["RightMouse"].started += InitialMousePositionHandler;
            _playerInput.actions["RightMouse"].performed += RightMouseHandler;
            _playerInput.actions["RightMouse"].canceled += RightMouseHandler;
            _playerInput.actions["ScrollMouse"].performed += ScrollMouseHandler;
            _playerInput.actions["ScrollMouse"].canceled += ScrollMouseHandler;
            _playerInput.actions["MiddleMouse"].started += InitialMousePositionHandler;
            _playerInput.actions["MiddleMouse"].performed += MiddleMouseHandler;
            _playerInput.actions["MiddleMouse"].canceled += MiddleMouseHandler;
        }

        private void LateUpdate()
        {
            MoveCamera();
            MoveCameraWithCursor();
            MoveCameraWithRightMouse();
            TiltCameraWithMiddleMouse(); 
            ZoomCamera();
            RotateCamera();

            ApplyHeightConstraints();
        }

        private void OnDisable()
        {
            if (!_playerInput) return;
            _playerInput.actions["CameraMove"].performed -= MoveHandler;
            _playerInput.actions["CameraMove"].canceled -= MoveHandler;
            _playerInput.actions["MousePosition"].performed -= MousePositionHandler;
            _playerInput.actions["MousePosition"].canceled -= MousePositionHandler;
            _playerInput.actions["RightMouse"].started -= InitialMousePositionHandler;
            _playerInput.actions["RightMouse"].performed -= RightMouseHandler;
            _playerInput.actions["RightMouse"].canceled -= RightMouseHandler;
            _playerInput.actions["ScrollMouse"].performed -= ScrollMouseHandler;
            _playerInput.actions["ScrollMouse"].canceled -= ScrollMouseHandler;
            _playerInput.actions["MiddleMouse"].started -= InitialMousePositionHandler;
            _playerInput.actions["MiddleMouse"].performed -= MiddleMouseHandler;
            _playerInput.actions["MiddleMouse"].canceled -= MiddleMouseHandler;
        }

        private void MoveHandler(InputAction.CallbackContext context) => _moveInput = context.ReadValue<Vector2>();
        private void MousePositionHandler(InputAction.CallbackContext context) => _mousePositionInput = context.ReadValue<Vector2>();
        private void InitialMousePositionHandler(InputAction.CallbackContext context) => _initialMousePosition = _mousePositionInput;
        private void RightMouseHandler(InputAction.CallbackContext context) => _rightMouseInput = context.ReadValue<float>();
        private void ScrollMouseHandler(InputAction.CallbackContext context) => _scrollMouseInput = context.ReadValue<float>();
        private void MiddleMouseHandler(InputAction.CallbackContext context) => _middleMouseInput = context.ReadValue<float>();

        private Vector3 GetCameraBasedDirection(float xInput, float yInput)
        {
            Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
            Vector3 right = transform.right; right.y = 0; right.Normalize();
            return (forward * yInput + right * xInput);
        }

        private void MoveCamera()
        {
            if (_moveInput == Vector2.zero) return;
            transform.position += GetCameraBasedDirection(_moveInput.x, _moveInput.y).normalized * (_moveSpeed * Time.deltaTime);
        }

        private void MoveCameraWithCursor()
        {
            float x = 0, y = 0;
            if (_mousePositionInput.x < _edgeThreshold) x = -1;
            else if (_mousePositionInput.x > Screen.width - _edgeThreshold) x = 1;
            if (_mousePositionInput.y < _edgeThreshold) y = -1;
            else if (_mousePositionInput.y > Screen.height - _edgeThreshold) y = 1;

            if (x != 0 || y != 0)
                transform.position += GetCameraBasedDirection(x, y).normalized * (_moveSpeed * Time.deltaTime);
        }

        private void MoveCameraWithRightMouse()
        {
            if (Mathf.Approximately(_rightMouseInput, 0)) return;
            var mouseDelta = _mousePositionInput - _initialMousePosition;
            transform.position += GetCameraBasedDirection(mouseDelta.x / Screen.width, mouseDelta.y / Screen.height) * (_moveSpeed * _rightMouseSpeedMultiplier * Time.deltaTime);
        }

        private void TiltCameraWithMiddleMouse()
        {
            if (Mathf.Approximately(_middleMouseInput, 0)) return;

            float mouseDeltaY = _mousePositionInput.y - _initialMousePosition.y;
            float tiltAmount = (mouseDeltaY / Screen.height) * _verticalRotateSpeed * Time.deltaTime;
            _currentPitch -= tiltAmount;
            _currentPitch = Mathf.Clamp(_currentPitch, _minVerticalAngle, _maxVerticalAngle);

            Vector3 currentAngles = transform.eulerAngles;
            currentAngles.x = _currentPitch;
            transform.eulerAngles = currentAngles;
        }

        private void ZoomCamera()
        {
            if (Mathf.Approximately(_scrollMouseInput, 0)) return;
            Ray ray = _camera.ScreenPointToRay(_mousePositionInput);
            Vector3 zoomDir = Physics.Raycast(ray, out RaycastHit hit) ? (hit.point - transform.position).normalized : transform.forward;
            transform.position += zoomDir * (_scrollMouseInput * _zoomSpeed * Time.deltaTime);
        }

       private void RotateCamera()
        {
            float rotateValue = -_rotateAction.ReadValue<float>();

            // Если кнопка вращения нажата
            if (!Mathf.Approximately(rotateValue, 0))
            {
                // Если мы только что начали вращение (флаг еще false)
                if (!_isRotating)
                {
                    _rotationPivot = GetCameraLookAtPoint(); // Запоминаем точку ОДИН раз
                    _isRotating = true;                      // Поднимаем флаг
                }

                // Вращаемся вокруг зафиксированной точки
                transform.RotateAround(_rotationPivot, Vector3.up, rotateValue * _rotateSpeed * Time.deltaTime);
            }
            else
            {
                // Кнопка отпущена — сбрасываем флаг, чтобы при следующем нажатии найти новую точку
                _isRotating = false;
            }
        }

        private Vector3 GetCameraLookAtPoint()
        {
            var ray = new Ray(transform.position, transform.forward);
            
            // 1. Попадание прямо в террейн (юнита)
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayer)) 
                return hit.point;
            
            // ПРОБЛЕМА 2: Защита от "улетания" камеры при потере террейна из вида.
            // Теперь мы вычисляем реальную высоту земли точно под камерой, а не используем нулевую высоту.
            float groundHeight = 0f;
            if (Physics.Raycast(new Vector3(transform.position.x, 1000f, transform.position.z), Vector3.down, out RaycastHit heightHit, 1500f, _groundLayer))
            {
                groundHeight = heightHit.point.y;
            }

            // Создаем виртуальную плоскость ровно на высоте террейна и крутимся вокруг неё
            var groundPlane = new Plane(Vector3.up, new Vector3(0, groundHeight, 0));
            if (groundPlane.Raycast(ray, out float enter)) 
                return ray.GetPoint(enter);
            
            return transform.position + transform.forward * _rotateFallback;
        }

        private void ApplyHeightConstraints()
        {
            Vector3 currentPos = transform.position;
            RaycastHit hit;

            Vector3 rayStart = new Vector3(currentPos.x, 1000f, currentPos.z);
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 1500f, _groundLayer))
            {
                float groundHeight = hit.point.y;
                float clampedY = Mathf.Clamp(currentPos.y, groundHeight + _minHeightAboveGround, groundHeight + _maxHeightAboveGround);
                transform.position = new Vector3(currentPos.x, clampedY, currentPos.z);
            }
        }
    }
}