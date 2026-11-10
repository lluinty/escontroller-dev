using UnityEngine;
using System.Collections.Generic;

namespace Core
{
    public class CoreController : MonoBehaviour
    {
        //Core Modules
        [SerializeField] private List<CoreModule> coreModules = new List<CoreModule>();
        private List<CoreModule> _initializedCoreModules = new List<CoreModule>();
        public IReadOnlyCollection<CoreModule> CoreModules => coreModules;
        public IReadOnlyCollection<CoreModule> InitializedCoreModules => _initializedCoreModules;


        private bool _isInitializing;

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
                Debug.LogError
                    ("Не удалось получить необходимые компоненты. Попробуйте добавить их вручную");
            }
        }

        private void OnValidate()
        {
            if(_initializedCoreModules != coreModules) InitializeCoreModules();
        }
        #endregion


        #region Modules Initialization

        private void InitializeCoreModules()
        {
            if (_isInitializing)
            {
                return;
            }

            try
            {
                _isInitializing = true;
                var modulesToRemove = new List<CoreModule>();
                var initializedModulesToRemove = new List<CoreModule>();

                //Инициализация модулей.
                foreach (var module in coreModules)
                {
                    if (module != null)
                    {
                        if (!_initializedCoreModules.Contains(module))
                        {
                            if (module.Initialize(this))
                            {
                                _initializedCoreModules.Add(module);
                            }
                            else
                            {
                                Debug.LogWarning($"{module} вернул false при попытке инициализации.");
                                module.Deinitialize();
                                modulesToRemove.Add(module);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{module} имеет значение null, поэтому не может быть инициализирован.");
                        modulesToRemove.Add(module);
                    }
                }

                //Деинициализация и удаление уже неиспользуемых модулей. 
                foreach (var module in _initializedCoreModules)
                {
                    if (module != null)
                    {
                        if (!coreModules.Contains(module))
                        {
                            Debug.Log
                            ($"{module} находиться в _initializedCoreModules, но не найден в coreModules" +
                             $", следовательно будет удалён.");
                            module.Deinitialize();
                            initializedModulesToRemove.Add(module);
                        }
                    }
                    else
                    {
                        Debug.LogWarning
                            ($"Модуль внутри _initializedCoreModules имеет значение null, поэтому будет удалён.");
                        initializedModulesToRemove.Add(module);
                    }
                }

                //Удаление модулей нуждающихся в удалении после инициализации.
                while (_modulesToRemoveAfterInitialization.Count > 0)
                {
                    var modulesLocal = _modulesToRemoveAfterInitialization;
                    _modulesToRemoveAfterInitialization = new List<CoreModule>();
                    foreach (var module in modulesLocal)
                    {
                        if (!module) continue;
                        
                        coreModules.Remove(module);
                        if(_initializedCoreModules.Remove(module))
                        {
                            module.Deinitialize();
                        }
                    }
                }


                _modulesToRemoveAfterInitialization.Clear();
                coreModules.RemoveAll(modulesToRemove.Contains);
                _initializedCoreModules.RemoveAll(initializedModulesToRemove.Contains);
            }

            finally
            {
                _isInitializing = false;
            }
        }

        public void AddCoreModule(CoreModule module)
        {
            if (_isInitializing)
            {
                Debug.LogWarning
                    ($"{module} не может быть добавлен, так как происходит иницализация модулей.");
                return;
            }

            if (module && !coreModules.Contains(module))
            {
                coreModules.Add(module);
                InitializeCoreModules();
            }
            else
            {
                Debug.LogWarning
                    ($"{module} не может быть добавлен, тк имеет значение null или уже существует");
                return;
            }
        }

        private List<CoreModule> _modulesToRemoveAfterInitialization = new List<CoreModule>();

        public bool RemoveCoreModule(CoreModule module)
        {
            if (!module || !coreModules.Contains(module))
            {
                Debug.LogWarning
                    ($"{module} не может быть удалён, так как не найден в списке coreModules или имеет значение null");
                return false;
            }

            if (_isInitializing)
            {
                if (!_modulesToRemoveAfterInitialization.Contains(module))
                {
                    _modulesToRemoveAfterInitialization.Add(module);
                    Debug.Log
                    ($"{module} не может быть удалён сейчас, " +
                     $"так как происходит иницализация модулей. Модуль будет удалён после инициализации.");
                }
                return true;
            }

            coreModules.Remove(module);
            InitializeCoreModules();
            return true;
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