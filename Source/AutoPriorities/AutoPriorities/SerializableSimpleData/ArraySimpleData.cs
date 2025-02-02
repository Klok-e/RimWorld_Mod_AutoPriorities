using System.Collections.Generic;

namespace AutoPriorities.SerializableSimpleData
{
    public class ArraySimpleData<T>
    {
        public List<T>? array;

        public ArraySimpleData()
        {
        }

        public ArraySimpleData(List<T> array)
        {
            this.array = array;
        }
    }
}
