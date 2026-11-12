using UnityEngine;

namespace Core.CoreModules
{
    public class CheckGround : CoreModule
    {
        #region Variables

        private bool _isInitialized;
        private CoreController _coreController;
        private Rigidbody _rigidbody;

        //CheckGround
        [SerializeField] private float groundCheckRadius;
        [SerializeField] private float groundCheckVerticalOffset;
        [SerializeField] private LayerMask groundCheckMask;
        public bool IsGrounded { get; private set; }

        #endregion


        #region Unity Methods

        private void FixedUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_rigidbody)
            {
                var origin = _rigidbody.position +
                             Vector3.up * (groundCheckRadius + groundCheckVerticalOffset);

                IsGrounded = Physics.CheckSphere(
                    origin,
                    groundCheckRadius,
                    groundCheckMask,
                    QueryTriggerInteraction.Ignore);
            }
            else
            {
                Debug.LogError
                ($"{this} был инициализирован ранее, но не может продолжать работу, " +
                 $"так как утерян доступ к Rigidbody");
                _coreController.RemoveCoreModule(this);
            }
        }

        private void OnDestroy()
        {
            if (!_coreController.RemoveCoreModule(this))
            {
                Debug.LogWarning(
                    $"Не получилось корректно деинициализировать модуль {this} по случаю его уничтожеения.");
            }

            _isInitialized = false;
        }

        #endregion

        #region Initialization API

        public override bool Initialize(CoreController core)
        {
            if (!CheckComponents())
            {
                Debug.LogError
                    ($"{this} не смог инициализироваться в следствии отсутвия необходимых локальных компонентов");
                return false;
            }

            if (!core)
            {
                Debug.LogError
                    ($"{this} не смог инициализироваться, так как не получил доступ к CoreController");
                return false;
            }

            _coreController = core;
            _rigidbody = core.CoreRigidbody;

            if (!_rigidbody)
            {
                Debug.LogError
                    ($"{this} не смог инициализироваться, так как не получил доступ к CoreRigidbody");
                return false;
            }

            _isInitialized = true;
            return true;
        }

        public override void Deinitialize()
        {
            _rigidbody = null;
            _coreController = null;
            _isInitialized = false;
        }

        #endregion

        private bool CheckComponents()
        {
            if (groundCheckMask == 0)
            {
                Debug.LogError
                    ($"GroundCheckMask не назачен на {this}, пожалуйста, добавтье его вручную.");
                return false;
            }

            return true;
        }
    }
}