namespace Omni.Core.Interfaces
{
    public interface IObjectPooling<T>
    {
        T Rent();
        void Return(T item);
    }
}