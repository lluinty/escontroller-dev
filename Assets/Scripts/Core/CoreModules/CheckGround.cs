using System.Linq;
using UnityEngine;

namespace Core.CoreModules
{
    public class CheckGround : MonoBehaviour, ICoreModule
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
                Debug.LogError($"{this} haven't access to Rigidbody.");
                RequestToRemoveMe();
            }
        }
        #endregion
        
        #region Initialization API
        public void Initialize(CoreController core)
        {
            _coreController = core;
            
            if (!_coreController)
            {
                Debug.LogError($"{this} haven't access to CoreController.");
                Deinitialize();
                return;
            }
            
            _rigidbody = core.CoreRigidbody;
            
            if (!CheckComponents())
            {
                RequestToRemoveMe();
                return;
            }

            _isInitialized = true;
        }
        
        public void Deinitialize()
        {
            _rigidbody = null;
            _coreController = null;
            _isInitialized = false;
        }
        #endregion
        
        
        
        #region Private Methods
        private void RequestToRemoveMe()
        {
            if (!_coreController)
            {
                Debug.LogWarning(
                    $" Cannot remove {this} from CoreModules, because CoreController reference is missing.");
                Deinitialize();
                return;
            }

            _coreController.RemoveCoreModule(this);
        }

        private bool CheckComponents()
        {
            if (_rigidbody == null)
            {
                Debug.LogError($"{this} haven't access to CoreRigidbody.");
                return false;
            }

            if (groundCheckMask == 0)
            {
                Debug.LogError($"GroundCheckMask not found on {this}");
                return false;
            }

            return true;
        }
        #endregion
    }
}