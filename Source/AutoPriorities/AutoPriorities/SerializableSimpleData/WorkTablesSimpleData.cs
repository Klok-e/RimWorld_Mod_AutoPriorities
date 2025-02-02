using System.Collections.Generic;
using AutoPriorities.Core;

namespace AutoPriorities.SerializableSimpleData
{
    public class WorkTablesSimpleData
    {
        public JobCount jobCount;
        public Priority priority;
        public List<WorkTypesSimpleData>? workTypes;
    }
}
