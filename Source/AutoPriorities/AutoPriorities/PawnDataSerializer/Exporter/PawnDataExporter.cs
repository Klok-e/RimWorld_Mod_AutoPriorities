using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using Verse;
using FileMode = Mono.Posix.FileMode;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    internal class PawnDataExporter : IPawnDataExporter
    {
        private const string Extension = ".xml";
        private const string NodeName = "Priorities";
        private readonly ILogger _logger;
        private readonly string _saveDirectoryPath;
        private readonly PawnsData _pawnsData;
        private readonly IPawnDataStringSerializer _pawnDataStringSerializer;

        public PawnDataExporter(ILogger logger,
            string saveDirectoryPath,
            PawnsData pawnsData,
            IPawnDataStringSerializer pawnDataStringSerializer)
        {
            _logger = logger;
            _saveDirectoryPath = saveDirectoryPath;
            _pawnsData = pawnsData;
            _pawnDataStringSerializer = pawnDataStringSerializer;
        }

        public void ExportCurrentPawnData(string name)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null) return;

            var stream = new MemoryStream();

            new XmlSerializer(typeof(PercentTableSaver.Ser)).Serialize(stream,
                PercentTableSaver.Ser.Serialized((_pawnsData.WorkTables, _pawnsData.ExcludedPawns)));

            stream.Position = 0;
            mapData.pawnsDataXml = stream.ToArray();

            try
            {
                try
                {
                    Scribe.saver.InitSaving(FullPath(name), NodeName);
                }
                catch (Exception ex)
                {
                    GenUI.ErrorDialog("ProblemSavingFile".Translate(ex.ToString()));
                    throw;
                }

                ScribeMetaHeaderUtility.WriteMetaHeader();
                Scribe_Deep.Look(ref mapData, "Blueprint");
            }
            catch (Exception ex2)
            {
                Log.Error("Exception while saving blueprint: " + ex2);
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
        }

        public void ImportPawnData(string name)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null) return;

#if DEBUG
            _logger.Info("Attempting to load from: " + name);
#endif

            try
            {
                Scribe.loader.InitLoading(FullPath(name));
                ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.Map, true);
                Scribe.EnterNode(NodeName);
                mapData.ExposeData();
                Scribe.ExitNode();
            }
            catch (Exception e)
            {
                Log.Error("Exception while loading priorities: " + e);
            }
            finally
            {
                // done loading
                Scribe.loader.FinalizeLoading();
                Scribe.mode = LoadSaveMode.Inactive;
            }

            if (mapData.pawnsDataXml == null)
            {
                _logger.Warn("Saved priorities are null somehow");
                return;
            }

            var save = _pawnDataStringSerializer.Deserialize(mapData.pawnsDataXml);
            if (save == null) return;
            _pawnsData.SetData(save);
        }

        public IEnumerable<string> ListSaves()
        {
            return Directory.EnumerateFiles(_saveDirectoryPath);
        }

        public void DeleteSave(string name)
        {
            File.Delete(FullPath(name));
        }

        private string FullPath(string name)
        {
            var nameWithExt = Path.ChangeExtension(name, Extension);
            return Path.Combine(_saveDirectoryPath, nameWithExt);
        }
    }
}
