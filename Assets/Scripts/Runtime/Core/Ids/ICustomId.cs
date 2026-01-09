namespace Survivor.Runtime.Core
{
    public interface ICustomId<T> where T : unmanaged
    {
        public bool IsValid();
        
        public T GetNext();

        public int ToInt();
        
        public T SetFromInt(int idValue);

        public T SetInvalid();
    }
}