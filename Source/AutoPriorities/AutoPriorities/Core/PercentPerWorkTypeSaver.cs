using AutoPriorities.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Verse;

namespace AutoPriorities.Core
{
    public static class PercentPerWorkTypeSaver
    {
        private const string _filename = "ModAutoPrioritiesSave.xml";
        private const string _filenameBackup = "ModAutoPrioritiesBackupSave.xml";

        private static string _savePathFull;
        private static string _saveBackupPathFull;

        private static XmlSerializer _writer;

        static PercentPerWorkTypeSaver()
        {
            _savePathFull = UnityEngine.Application.persistentDataPath + _filename;
            _saveBackupPathFull = UnityEngine.Application.persistentDataPath + _filenameBackup;
            try
            {
                _writer = new XmlSerializer(typeof(State));
            }
            catch(Exception e)
            {
                ExceptionUtil.LogAllInnerExceptions(e);
            }
        }

        public static void SaveState(List<Tuple2<int, Dictionary<WorkTypeDef, float>>> state)
        {
            using(var stream = new FileStream(_savePathFull, FileMode.Create))
            {
                var list = new State()
                {
                    _intAndLists = new List<IntAndList>(state.Count)
                };
                foreach(var tuple in state)
                {
                    var listOfWorks = new List<WorkTypeAndFloat>(tuple._val2.Count);
                    foreach(var keyValue in tuple._val2)
                    {
                        listOfWorks.Add(new WorkTypeAndFloat()
                        {
                            _percent = keyValue.Value,
                            _workTypeDefName = keyValue.Key.defName,
                        });
                    }
                    list._intAndLists.Add(new IntAndList()
                    {
                        _list = listOfWorks,
                        _priority = tuple._val1,
                    });
                }

                _writer.Serialize(stream, list);
            }
        }

        public static List<Tuple2<int, Dictionary<WorkTypeDef, float>>> LoadState()
        {
            var output = new List<Tuple2<int, Dictionary<WorkTypeDef, float>>>();
            using(var stream = new FileStream(_savePathFull, FileMode.Open))
            {
                var des = (State)_writer.Deserialize(stream);

                foreach(var prior in des._intAndLists)
                {
                    var dict = new Dictionary<WorkTypeDef, float>();
                    foreach(var workType in prior._list)
                    {
                        dict.Add(StringToDef(workType._workTypeDefName), workType._percent);
                    }

                    output.Add(new Tuple2<int, Dictionary<WorkTypeDef, float>>(prior._priority, dict));
                }
            }
            return output;
        }

        public static WorkTypeDef StringToDef(string name)
        {
            foreach(var workType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
            {
                if(workType.defName == name)
                    return workType;
            }
            Log.Error($"name {name} not found");
            return null;
        }

        [XmlRoot]
        public class State
        {
            [XmlElement]
            public List<IntAndList> _intAndLists;
        }

        public class IntAndList
        {
            [XmlAttribute]
            public int _priority;
            [XmlElement]
            public List<WorkTypeAndFloat> _list;
        }

        public class WorkTypeAndFloat
        {
            [XmlAttribute]
            public string _workTypeDefName;
            [XmlAttribute]
            public float _percent;
        }
    }
}
