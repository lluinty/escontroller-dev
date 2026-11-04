namespace Core
{
    public interface ICoreModule
    {
        void Initialize(CoreController core);
        void Deinitialize();
    }
}