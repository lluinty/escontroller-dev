using UnityEngine;

namespace Core.CoreModules
{
    public class CheckGround : MonoBehaviour, ICoreModule

    {
        //CheckGround
        [SerializeField] private float groundCheckRadius;
        [SerializeField] private float groundCheckVerticalOffset;
        [SerializeField] private LayerMask groundCheckMask;
        private Rigidbody _rigidbody;
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            if (groundCheckMask == 0)
            {
                Debug.LogError("GroundCheckMask not found on CoreController. Please, add it manually.");
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody)
            {
                var origin = _rigidbody.position + (Vector3.up * groundCheckRadius) + (Vector3.up * + groundCheckVerticalOffset);

                IsGrounded = Physics.CheckSphere
                (
                    origin,
                    groundCheckRadius,
                    groundCheckMask,
                    QueryTriggerInteraction.Ignore
                );
            }
            else
            {
                Debug.LogWarning("Rigidbody not assigned on CheckGround CoreModule.");
            }
        }

        public void Initialize(CoreController core)
        {
            _rigidbody = core.CoreRigidbody;
        }
    }
}
