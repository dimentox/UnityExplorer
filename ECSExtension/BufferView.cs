using ECSExtension.Util;

namespace ECSExtension
{
    public class BufferView<T> where T : unmanaged
    {
        public readonly ModDynamicBuffer<T> buffer;

        public BufferView(ModDynamicBuffer<T> buffer)
        {
            this.buffer = buffer;
        }
    }
}