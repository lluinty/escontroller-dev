using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

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
            InitializeModules();
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
                
                if(blockers.Count == 0)
                {
                    _blockers.Remove(action);
                }
            }
            else
            {
                blockers.Add(blockerId);
            }
        }
        #endregion
        
        
        
        #region ActionController
        private readonly Dictionary<ActionId, HashSet<string>> _actionsAndAuthors = new();
        
        public IReadOnlyCollection<ActionId> CurrentActions => _actionsAndAuthors.Keys;

        public void AddAction (ActionId actionId, string authorId)
        {
            if (!_actionsAndAuthors.TryGetValue(actionId, out var authors))
            {
                _actionsAndAuthors.Add(actionId, new HashSet<string>());
                _actionsAndAuthors[actionId].Add(authorId);
            }
            else
            {
                _actionsAndAuthors[actionId].Add(authorId);
            }
        }

        public void RemoveAction(ActionId actionId, string authorId)
        {
            if (!_actionsAndAuthors.TryGetValue(actionId, out var authors))
            {
                Debug.LogWarning($"Tried to remove non-existing action: {actionId} from ActionsController. AuthorID: {authorId}");
                return;
            }

            if (!_actionsAndAuthors[actionId].Remove(authorId))
            {
                Debug.LogWarning($"Tried to remove action from non-existing author: {actionId} from ActionsController. AuthorID: {authorId}");
                return;
            }

            if(_actionsAndAuthors[actionId].Count == 0)
            {
                _actionsAndAuthors.Remove(actionId);
            }
        }
        #endregion

        private void GetComponents()
        {
            //Rigidbody
            if (rigidbody == null)
            {
                rigidbody = GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    Debug.LogError("Rigidbody component not found on CoreController. Please, add ir manually.");
                    enabled = false;
                    return;
                }
            }
        }

        private void InitializeModules()
        {
            foreach (var module in coreModules)
            {
                if (module != null)
                {
                    if (module is ICoreModule checkedModule)
                    {
                        checkedModule.Initialize(this);
                    }
                    else
                    {
                        Debug.LogError($"Module {module} does not implement ICoreModule interface.");
                    }
                }
            }
        }
    }
}
