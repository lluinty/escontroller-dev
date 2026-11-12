using System;
using UnityEngine;

namespace Core
{
    public abstract class CoreModule : MonoBehaviour, ICoreModule

    {
        public abstract bool Initialize(CoreController core);
        public abstract void Deinitialize();
    }
}