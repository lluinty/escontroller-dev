namespace Core
{
    public interface ICoreModule
    {
        bool Initialize(CoreController core);
        void Deinitialize();
    }
}