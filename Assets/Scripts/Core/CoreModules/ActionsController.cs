using UnityEngine;
using System.Collections.Generic;

namespace Core.CoreModules
{
    public class ActionsController : CoreModule
    {
        private bool _isInitialized;
        
        
        
        #region Public API
        private readonly Dictionary<ActionId, HashSet<string>> _actionOwners = new();
        public IReadOnlyCollection<ActionId> CurrentActions => _actionOwners.Keys;

        public void AddAction(ActionId actionId, string authorId)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Tried to add action using non-initialized {this}. ActionID: {actionId}," +
                                 $" AuthorID: {authorId}");
                return;
            }
            
            if (!actionId.IsValid)
            {
                Debug.LogWarning($"Tried to add action using non-existing actionId {actionId}");
                return;
            }
            
            if (!string.IsNullOrWhiteSpace(authorId))
            {
                if (!_actionOwners.TryGetValue(actionId, out var authors))
                {
                    authors = new HashSet<string>();
                    authors.Add(authorId);
                    _actionOwners.Add(actionId, authors);
                }
                else
                {
                    authors.Add(authorId);
                }
            }
            else
            {
                Debug.LogWarning($"Something tried to add action using non-existing authorId {authorId}");
            }
        }

        public void RemoveAction(ActionId actionId, string authorId)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Tried to remove action using non-initialized {this}. ActionID: {actionId}," +
                                 $" AuthorID: {authorId}");
                return;
            }

            if (!actionId.IsValid)
            {
                Debug.LogWarning($"Tried to remove action using non-existing actionId {actionId}");
                return;
            }
            
            if (!string.IsNullOrWhiteSpace(authorId))
            {
                if (!_actionOwners.TryGetValue(actionId, out var authors))
                {
                    Debug.LogWarning(
                        $"Tried to remove non-existing action: {actionId} from ActionsController. " +
                        $"AuthorID: {authorId}");
                    return;
                }

                if (!authors.Remove(authorId))
                {
                    Debug.LogWarning(
                        $"Tried to remove action from non-existing author: {actionId} from ActionsController. " +
                        $"AuthorID: {authorId}");
                    return;
                }

                if (authors.Count == 0)
                {
                    _actionOwners.Remove(actionId);
                }
            }
            else
            {
                Debug.LogWarning($"Something tried to remove action using non-existing authorId {authorId}");
            }
        }
        #endregion
        
        
        
        #region Initialization API

        public override bool Initialize(CoreController core)
        {
            _isInitialized = true;
            return true;
        }

        public override void Deinitialize()
        {
            _isInitialized = false;
        }
        #endregion
    }
}