using System.Collections.Generic;
using UnityEngine;

namespace Core.CoreModules
{
    public class ActionsPermissionsController : CoreModule
    {
        private bool _isInitialized;
        private CoreController _coreController;
        private readonly Dictionary<ActionId, HashSet<string>> _blockers = new();

        #region Unity Methods

        private void OnDestroy()
        {
            if (!_coreController.RemoveCoreModule(this))
            {
                Debug.LogWarning($"Не получилось корректно деинициализировать модуль по случаю его уничтожеения.");
            }

            _isInitialized = false;
        }

        #endregion

        #region Public API

        public bool CanPerform(ActionId action)
        {
            if (!action.IsValid)
            {
                return false;
            }
            return !_blockers.TryGetValue(action, out var blockers) || blockers.Count == 0;
        }

        public void BlockAction(ActionId action, string blockerId)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"{this} is not initialized, can't block action {action}");
                return;
            }

            if (!action.IsValid)
            {
                Debug.LogWarning($"Tried to block action using non-existing actionId {action}");
                return;
            }

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
            if (!_isInitialized)
            {
                Debug.LogWarning($"{this} is not initialized, can't unblock action {action}");
                return;
            }

            if (!action.IsValid)
            {
                Debug.LogWarning($"Tried to unblock action using non-existing actionId {action}");
                return;
            }

            if (!string.IsNullOrWhiteSpace(blockerId))
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
            else
            {
                Debug.LogWarning($"blockerId is null or whitespace, so can't unblock action {action}");
            }
        }

        #endregion


        #region Initialization API

        public override bool Initialize(CoreController core)
        {
            if (!core)
            {
                Debug.LogWarning("Tried to initialize ActionsPermissionsController with null core");
                return false;
            }

            _coreController = core;
            _isInitialized = true;
            return true;
        }

        public override void Deinitialize()
        {
            _coreController = null;
            _isInitialized = false;
        }

        #endregion
    }
}