using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            if (GetComponents())
            {
                // InitializeCoreModules();
            }
            else
            {
                // Debug.LogError("GetComponents failed. So CoreController cannot still work");
            }
        }

        private void Update()
        {
            CheckValidationUpdates();
        }

        private void OnValidate()
        {
            _validatedCoreModules.Clear();
            _nonValidatedCoreModules.Clear();
            foreach (var module in coreModules)
            {
                ValidateCoreModule(module);
            }

            coreModules.RemoveAll(module => module == null || _nonValidatedCoreModules.Contains(module));
            _nonValidatedCoreModules.Clear();
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

        #region Modules Validation & Initialization

        private HashSet<ICoreModule> _validatedCoreModules = new HashSet<ICoreModule>();
        private HashSet<ICoreModule> _validatedCoreModulesLastState = new HashSet<ICoreModule>();
        private HashSet<MonoBehaviour> _nonValidatedCoreModules = new HashSet<MonoBehaviour>();
        private HashSet<ICoreModule> _initializedCoreModules = new HashSet<ICoreModule>();


        public void AddCoreModule(MonoBehaviour module)
        {
            if (module == null || coreModules.Contains(module))
            {
                return;
            }

            //coreModule - это исходный список с черновыми вариантами модулей.
            //Модуль не нуждается в проверке при добавлении его в coreModules, тк отсеется позже в ValidateCoreModule.
            coreModules.Add(module);

            ValidateCoreModule(module);
            InitializeCoreModules();
        }

        public void RemoveCoreModule(ICoreModule module)
        {
            if (ReferenceEquals(module, null))
            {
                return;
            }

            if (!_validatedCoreModules.Remove(module))
            {
                Debug.LogWarning($"Failed to remove ICoreModule module {module} from _validatedCoreModules");
            }

            if (module is MonoBehaviour behaviour && behaviour != null)
            {
                _nonValidatedCoreModules.Remove(behaviour);
                if (!coreModules.Remove(behaviour))
                {
                    Debug.LogWarning($"Failed to remove MonoBehaviour module {module} from coreModules");
                }
            }

            InitializeCoreModules();
        }

        private void CheckValidationUpdates()
        {
            if (!_validatedCoreModules.SetEquals(_validatedCoreModulesLastState))
            {
                InitializeCoreModules();
            }
        }

        private void ValidateCoreModule(MonoBehaviour module)
        {
            if (module == null)
            {
                Debug.LogWarning("Module is null.");
                return;
            }

            if (module is not ICoreModule coreModule)
            {
                _nonValidatedCoreModules.Add(module);

                Debug.LogWarning(
                    $"Module {module} does not implement ICoreModule interface.");

                return;
            }

            _validatedCoreModules.Add(coreModule);
        }

        private void InitializeCoreModules()
        {
            var modulesToDeinitialize = _initializedCoreModules.Except(_validatedCoreModules).ToHashSet();
            //Deinitialization
            foreach (var module in modulesToDeinitialize)
            {
                module.Initialize(null);
                _initializedCoreModules.Remove(module);
            }

            //Initialization
            foreach (var module in _validatedCoreModules)
            {
                if (!_initializedCoreModules.Contains(module))
                {
                    module.Initialize(this);
                    _initializedCoreModules.Add(module);
                }
            }

            _validatedCoreModulesLastState = new HashSet<ICoreModule>(_validatedCoreModules);
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
    }
}