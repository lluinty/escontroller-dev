using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Core
{
    public class CoreController : MonoBehaviour
    {
        //Core Modules
        [SerializeField] private List<MonoBehaviour> coreModules = new();
        private ReadOnlyCollection<MonoBehaviour> _coreModulesReadOnly;

        public IReadOnlyList<MonoBehaviour> CoreModules =>
            _coreModulesReadOnly ??= coreModules.AsReadOnly();

        //Rigidbody
        [SerializeField] private Rigidbody rigidbody;
        public Rigidbody CoreRigidbody => rigidbody;


        #region Unity Methods

        private void Awake()
        {
            if (GetComponents())
            {
                InitializeCoreModules();
            }
            else
            {
                Debug.LogError("GetComponents failed. So CoreController cannot still work");
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
        
        

        #region Modules Validation & Initialization

        private HashSet<ICoreModule> _validatedCoreModules = new HashSet<ICoreModule>();
        private HashSet<ICoreModule> _validatedCoreModulesLastState = new HashSet<ICoreModule>();
        private HashSet<MonoBehaviour> _nonValidatedCoreModules = new HashSet<MonoBehaviour>();
        private HashSet<ICoreModule> _initializedCoreModules = new HashSet<ICoreModule>();


        public void AddCoreModule(MonoBehaviour module)
        {
            if (module == null || coreModules.Contains(module) || module is not ICoreModule)
            {
                return;
            }

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
                _initializedCoreModules.Remove(module);
                module.Deinitialize();
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