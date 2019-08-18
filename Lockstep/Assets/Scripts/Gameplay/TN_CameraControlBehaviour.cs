using UnityEngine;
using System.Collections;
using System;

namespace TN_InterviewTest
{
    public class TN_CameraControlBehaviour : MonoBehaviour
    {
        [Header("Speed Control")]
        [SerializeField]
        private float _cameraMovementSpeed = 30f;
        private Camera _mainCamera;
        private Vector3 _cameraPosition;

        [Header("Zoom Control")]
        [SerializeField]
        [Range(-10f, 0f)]
        private float _maxZ = -5f;

        [SerializeField]
        [Range(1f, 100f)]
        private float _zoomScaler = 20f;

        [Header("Rotation")]

        [SerializeField]
        [Range(1f, 100f)]
        private float _rotateAmount = 30f;

        [SerializeField] private Collider _landMesh;

        private float _zoomAmount;
        private float _currentZoom;
        private bool _zooming = false;
        private float _baseScrollingAmount = 1f;

        private float _maxZoom = 60f;
        private float _minZoom = 10f;

        private float _currentRotation;

        private float _smoothTime = .3f;
        private float _yVelocity = .0f;

        private bool _canRotate = true;

        //Set in awake to be whatever the camera's Z value currently is in the editor
        private float _minZ;

        private Plane _plane;

        // Use this for initialization
        void Awake()
        {
            _mainCamera = Camera.main;
            _cameraPosition = _mainCamera.transform.position;
            _minZ = _cameraPosition.z;
            _currentZoom = Camera.main.fieldOfView;

            _mainCamera.depthTextureMode = DepthTextureMode.Depth;
        }

        public void SetCameraPosition()
        {

        }

        private void OnEnable()
        {
            _currentRotation = this.transform.localEulerAngles.y;
        }

        private void OnDisable()
        {

        }

        private void Update()
        {
            UpdateCameraZoom();

            Event_OnKeyDown();

            MoveCamera();
        }

        private void Event_OnKeyDown()
        {
            //Calculate new camera position
            _cameraPosition = TransformCameraPosition(_cameraPosition, transform.forward, transform.right, -transform.right, _cameraMovementSpeed, Time.deltaTime);

            //Calculate rotation
            TransformRotation();
        }

        private void Event_OnMouseScrolled(float scroll)
        {
            //Calculate scrolling
            TransformCameraScroll(_mainCamera.fieldOfView, scroll, _minZ, _maxZ);
        }

        public void MoveCamera()
        {
            //Always move to this position
            _mainCamera.transform.position = _cameraPosition;
        }

        private RaycastHit Raycast()
        {
            RaycastHit hitInfo;
            if (!Physics.Raycast(_mainCamera.ScreenPointToRay(Input.mousePosition), out hitInfo))
                return hitInfo;

            return hitInfo;
        }

        private void UpdateCameraZoom()
        {
            _mainCamera.fieldOfView = ContinueZooming();
        }

        public Vector3 TransformCameraPosition(Vector3 cameraPosition, Vector3 forward, Vector3 right, Vector3 left, float cameraMovementSpeed, float deltaTime)
        {
            if (Input.GetKey(KeyCode.A))
            {
                cameraPosition.x += (left.x * cameraMovementSpeed) * deltaTime;
                cameraPosition.z += (left.z * cameraMovementSpeed) * deltaTime;
            }
            if (Input.GetKey(KeyCode.D))
            {
                cameraPosition.x += (right.x * cameraMovementSpeed) * deltaTime;
                cameraPosition.z += (right.z * cameraMovementSpeed) * deltaTime;
            }
            if (Input.GetKey(KeyCode.W))
            {
                cameraPosition.x += (forward.x * cameraMovementSpeed) * deltaTime;
                cameraPosition.z += (forward.z * cameraMovementSpeed) * deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                cameraPosition.x -= (forward.x * cameraMovementSpeed) * deltaTime;
                cameraPosition.z -= (forward.z * cameraMovementSpeed) * deltaTime;
            }
            return cameraPosition;
        }

        public void TransformCameraScroll(float currentZoom, float scrollAmount, float minZ, float maxZ)
        {
            float amountToZoom = 0f;

            if (scrollAmount != 0)
            {
                _zooming = true;
                _currentZoom = currentZoom;

                //Positive scroll
                if (scrollAmount > 0f)
                    amountToZoom = _baseScrollingAmount * _zoomScaler;

                //Negtive scroll
                if (scrollAmount < 0f)
                    amountToZoom = -_baseScrollingAmount * _zoomScaler;

                _zoomAmount = _currentZoom + amountToZoom;
                _zoomAmount = Mathf.Clamp(_zoomAmount, _minZoom, _maxZoom);
            }
        }

        public void TransformRotation()
        {
            float rotX = transform.localEulerAngles.x;
            float rotZ = transform.localEulerAngles.z;

            if (Input.GetKey(KeyCode.Q) && _canRotate)
            {
                _currentRotation -= _rotateAmount * Time.deltaTime;
                transform.localEulerAngles = new Vector3(rotX, _currentRotation, rotZ);
            }
            if (Input.GetKey(KeyCode.E) && _canRotate)
            {
                _currentRotation += _rotateAmount * Time.deltaTime;
                transform.localEulerAngles = new Vector3(rotX, _currentRotation, rotZ);
            }
        }

        public float ContinueZooming()
        {
            if (!_zooming) return _currentZoom;

            if (!Mathf.Approximately(_currentZoom, _zoomAmount))
            {
                _currentZoom = Mathf.SmoothDamp(_currentZoom, _zoomAmount, ref _yVelocity, _smoothTime);
                return _currentZoom;
            }
            else
            {
                _zooming = false;
                return _currentZoom;
            }
        }
    }
    
}