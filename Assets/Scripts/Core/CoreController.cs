using UnityEngine;
using System.Collections.Generic;

namespace Core
{
    public class CoreController : MonoBehaviour
    {
        //Rigidbody
        [SerializeField] private Rigidbody rigidbody;
        public Rigidbody CoreRigidbody => rigidbody;
        
        //CheckGround
        [SerializeField] private float groundCheckRadius;
        [SerializeField] private float groundCheckVerticalOffset;
        private LayerMask _groundCheckMask;
        private bool _lastIsGroundedState;
        public bool IsGrounded { get; private set; }
        
        
        
        #region Unity Methods
        private void Awake() => Initialize();

        private void Update()
        {
            CheckGround();
        }
        #endregion
        
        
        
        #region CheckGround
        private void CheckGround()
        {
            var origin = CoreRigidbody.position + (Vector3.up * groundCheckRadius) + (Vector3.up * + groundCheckVerticalOffset);

            IsGrounded = Physics.CheckSphere
            (
                origin,
                groundCheckRadius,
                _groundCheckMask,
                QueryTriggerInteraction.Ignore
            );
        }
        #endregion
        
        
        
        #region ActionPermissionController
        private readonly Dictionary<ActionId, HashSet<string>> _blockers = new();

        public bool CanPerform(ActionId action)
        {
            return !_blockers.TryGetValue(action, out var blockers) || blockers.Count == 0;
        }

        public void SetCanPerform (ActionId action, string blockerId, bool canPerform)
        {
            if (!_blockers.TryGetValue(action, out var blockers))
            {
                blockers = new HashSet<string>();
                _blockers.Add(action, blockers);
            }

            if (canPerform)
            {
                blockers.Remove(blockerId);
            }
            else
            {
                blockers.Add(blockerId);
            }
        }
        #endregion

        private void Initialize()
        {
            if (rigidbody == null)
            {
                rigidbody = GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    Debug.LogError("Rigidbody component not found on CoreController. Please, add ir manually.");
                }
            }
            
            _groundCheckMask = ~LayerMask.GetMask("Player");
        }
    }
}
