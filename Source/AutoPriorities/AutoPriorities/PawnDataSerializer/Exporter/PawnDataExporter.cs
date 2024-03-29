using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using Verse;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    internal class PawnDataExporter : IPawnDataExporter
    {
        private const string Extension = ".xml";
        private const string NodeName = "Priorities";
        private readonly ILogger _logger;
        private readonly IPawnDataStringSerializer _pawnDataStringSerializer;
        private readonly PawnsData _pawnsData;
        private readonly string _saveDirectoryPath;
        private List<SavedPawnDataReference> _savesCached = new();

        public PawnDataExporter(ILogger logger,
            string saveDirectoryPath,
            PawnsData pawnsData,
            IPawnDataStringSerializer pawnDataStringSerializer)
        {
            _logger = logger;
            _saveDirectoryPath = saveDirectoryPath;
            _pawnsData = pawnsData;
            _pawnDataStringSerializer = pawnDataStringSerializer;

            RecacheSaves();
        }

        private void RecacheSaves()
        {
            _savesCached = Directory.EnumerateFiles(_saveDirectoryPath)
                .Select(x => new SavedPawnDataReference(this, Path.GetFileNameWithoutExtension(x)))
                .ToList();
        }

        private string FullPath(string name)
        {
            var nameWithExt = Path.ChangeExtension(name, Extension);
            return Path.Combine(_saveDirectoryPath, nameWithExt);
        }

        #region IPawnDataExporter Members

        public void ExportCurrentPawnData(SavedPawnDataReference name)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null)
                throw new Exception("Current map null, couldn't export");

            var stream = new MemoryStream();

            new XmlSerializer(typeof(PercentTableSaver.Ser)).Serialize(
                stream,
                PercentTableSaver.Ser.Serialized((_pawnsData.WorkTables, _pawnsData.ExcludedPawns)));

            stream.Position = 0;
            mapData.pawnsDataXml = stream.ToArray();

            try
            {
                try
                {
                    Scribe.saver.InitSaving(FullPath(name.RenamableLabel), NodeName);
                }
                catch (Exception ex)
                {
                    GenUI.ErrorDialog("ProblemSavingFile".Translate(ex.ToString()));
                    throw;
                }

                ScribeMetaHeaderUtility.WriteMetaHeader();
                Scribe_Deep.Look(ref mapData, NodeName);
            }
            catch (Exception ex2)
            {
                Log.Error("Exception while saving table: " + ex2);
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
                RecacheSaves();
            }
        }

        public void ImportPawnData(string name)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null)
                return;

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
            if (save == null)
                return;

#if DEBUG
            _logger.Info("Loading successful. Setting loaded data.");
#endif
            _pawnsData.SetData(save);
        }

        public void RenameFile(string name, string newName)
        {
            var path = FullPath(name);
            if (!File.Exists(path))
                _logger.Warn("Tried to rename a nonexistent file.");
            File.Move(path, FullPath(newName));
            RecacheSaves();
        }

        public IEnumerable<SavedPawnDataReference> ListSaves()
        {
            return _savesCached;
        }

        public SavedPawnDataReference GetNextSavedPawnDataReference()
        {
            var number = _savesCached.Select(
                    savedPawnDataReference => Regex.Match(savedPawnDataReference.FileName, @"^([\w]+?)([\d]*)$"))
                .Where(match => match.Success)
                .Select(
                    match =>
                    {
                        var s = match.Groups[2]
                            .ToString();
#if DEBUG
                        _logger.Info($"Group 2: {s}");
#endif
                        return int.TryParse(s, out var num) ? num : 0;
                    })
                .DefaultIfEmpty(0)
                .Max() + 1;
            return new SavedPawnDataReference(this, $"{SavedPawnDataReference.StartingName}{number}");
        }

        public void DeleteSave(string name)
        {
            var path = FullPath(name);
            if (!File.Exists(path))
                _logger.Warn("Tried to delete a nonexistent file.");
            File.Delete(path);
            RecacheSaves();
        }

        #endregion
    }
}
