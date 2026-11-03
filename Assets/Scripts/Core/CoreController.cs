using UnityEngine;
using System.Collections.Generic;

namespace Core
{
    public class CoreController : MonoBehaviour
    {
        //Core Modules
        [SerializeField] private List<MonoBehaviour> coreModules;

        public IReadOnlyList<MonoBehaviour> CoreModules => coreModules;

        //Rigidbody
        [SerializeField] private Rigidbody rigidbody;
        public Rigidbody CoreRigidbody => rigidbody;


        #region Unity Methods

        private void Awake()
        {
            GetComponents();
            if (GetComponents())
            {
                InitializeModules();
            }
            else
            {
                Debug.LogError("GetComponents failed. So CoreController cannot still work");
            }
        }

        #endregion


        #region ActionPermissionController

        private readonly Dictionary<ActionId, HashSet<string>> _blockers = new();

        public bool CanPerform(ActionId action)
        {
            return !_blockers.TryGetValue(action, out var blockers) || blockers.Count == 0;
        }

        public void BlockAction(ActionId action, string blockerId)
        {
            if (!_blockers.TryGetValue(action, out var blockers))
            {
                if (!string.IsNullOrWhiteSpace(blockerId))
                {
                    blockers = new HashSet<string>();
                    blockers.Add(blockerId);
                    _blockers.Add(action, blockers);
                }
                else
                {
                    Debug.LogWarning($"blockerId is null or whitespace, so can't block action {action}");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(blockerId))
                {
                    blockers.Add(blockerId);
                }
                else
                {
                    Debug.LogWarning($"blockerId is null or whitespace, so can't block action {action}");
                }
            }
        }

        public void UnblockAction(ActionId action, string blockerId)
        {
            if (_blockers.TryGetValue(action, out var blockers))
            {
                if (!blockers.Remove(blockerId))
                {
                    Debug.LogWarning($"Something tried to remove action using non-existing blockerId {blockerId}");
                }

                if (blockers.Count == 0)
                {
                    _blockers.Remove(action);
                }
            }
            else
            {
                Debug.LogWarning(
                    $"Something tried to unblock action, that was already unblocked. blockerId {blockerId}");
            }
        }

        #endregion


        #region ActionController

        private readonly Dictionary<ActionId, HashSet<string>> _actionOwners = new();

        public IReadOnlyCollection<ActionId> CurrentActions => _actionOwners.Keys;

        public void AddAction(ActionId actionId, string authorId)
        {
            if (!_actionOwners.TryGetValue(actionId, out var authors))
            {
                if (!string.IsNullOrWhiteSpace(authorId))
                {
                    authors = new HashSet<string>();
                    authors.Add(authorId);
                    _actionOwners.Add(actionId, authors);
                }
                else
                {
                    Debug.LogWarning($"Something tried to add action using non-existing authorId {authorId}");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(authorId))
                {
                    authors.Add(authorId);
                }
                else
                {
                    Debug.LogWarning($"Something tried to add action using non-existing authorId {authorId}");
                }
            }
        }

        public void RemoveAction(ActionId actionId, string authorId)
        {
            if (!_actionOwners.TryGetValue(actionId, out var authors))
            {
                Debug.LogWarning(
                    $"Tried to remove non-existing action: {actionId} from ActionsController. AuthorID: {authorId}");
                return;
            }

            if (!authors.Remove(authorId))
            {
                Debug.LogWarning(
                    $"Tried to remove action from non-existing author: {actionId} from ActionsController. AuthorID: {authorId}");
                return;
            }

            if (authors.Count == 0)
            {
                _actionOwners.Remove(actionId);
            }
        }

        #endregion

        private bool GetComponents()
        {
            //Rigidbody
            if (rigidbody == null)
            {
                rigidbody = GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    Debug.LogError("Rigidbody component not found on CoreController. Please, add ir manually.");
                    enabled = false;
                    return false;
                }
            }

            return true;
        }

        private void InitializeModules()
        {
            if (coreModules != null)
            {
                foreach (var module in coreModules)
                {
                    if (module is ICoreModule checkedModule)
                    {
                        checkedModule.Initialize(this);
                    }
                    else
                    {
                        Debug.LogError($"Module {module} does not implement ICoreModule interface.");
                        return;
                    }
                }
            }
            else
            {
                {
                    Debug.Log("CoreController started without any Core Modules");
                }
            }
        }
    }
}