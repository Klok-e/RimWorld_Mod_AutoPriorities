using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AutoPriorities.APLogger;
using AutoPriorities.Core;
using Verse;

namespace AutoPriorities.PawnDataSerializer.Exporter
{
    public class PawnDataExporter
    {
        public const string NodeName = "Priorities";
        private readonly ILogger _logger;
        private readonly IPawnDataStringSerializer _pawnDataStringSerializer;
        private readonly PawnsData _pawnsData;
        private readonly SaveDataHandler _saveDataHandler;
        private readonly string _saveDirectoryPath;
        private readonly SaveFilePather _saveFilePather;
        private List<IPawnDataImportable> _savesCached = new();

        public PawnDataExporter(ILogger logger,
            string saveDirectoryPath,
            PawnsData pawnsData,
            SaveFilePather saveFilePather,
            IPawnDataStringSerializer pawnDataStringSerializer,
            SaveDataHandler saveDataHandler)
        {
            _logger = logger;
            _saveDirectoryPath = saveDirectoryPath;
            _pawnsData = pawnsData;
            _saveFilePather = saveFilePather;
            _pawnDataStringSerializer = pawnDataStringSerializer;
            _saveDataHandler = saveDataHandler;

            RecacheSaves();
        }

        private void RecacheSaves()
        {
            _savesCached = Directory.EnumerateFiles(_saveDirectoryPath)
                .Select<string, IPawnDataImportable>(
                    x => new PawnDataImportableReference(
                        Path.GetFileNameWithoutExtension(x),
                        this))
                .Prepend(new PawnDataPreset(_logger, _pawnDataStringSerializer, _pawnsData))
                .ToList();
        }

        private void ExportCurrentPawnData(string name)
        {
            var mapData = MapSpecificData.GetForCurrentMap();
            if (mapData == null)
                throw new Exception("Current map null, couldn't export");

            _saveDataHandler.SaveData(_pawnsData.GetSaveDataRequest(), mapData);

            try
            {
                try
                {
                    Scribe.saver.InitSaving(_saveFilePather.FullPath(name), NodeName);
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

        #region IPawnDataExporter Members

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
                Scribe.loader.InitLoading(_saveFilePather.FullPath(name));
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

            if (mapData.PawnsDataXml == null)
            {
                _logger.Warn("Saved priorities are null somehow");
                return;
            }

#if DEBUG
            _logger.Info("Loading successful. Setting loaded data.");
#endif
            var savedData = _saveDataHandler.GetSavedData(mapData);
            if (savedData == null)
                return;

            _pawnsData.SetData(savedData);
        }

        public void RenameFile(string name, string newName)
        {
            var path = _saveFilePather.FullPath(name);
            if (!File.Exists(path))
                _logger.Warn("Tried to rename a nonexistent file.");

            File.Move(path, _saveFilePather.FullPath(newName));
            RecacheSaves();
        }

        public IEnumerable<IPawnDataImportable> ListImportableSaves()
        {
            return _savesCached;
        }

        public IEnumerable<IPawnDataImportable> ListDeletableSaves()
        {
            return _savesCached.Where(x => !PawnDataPreset.PresetNames.Contains(x.FileName));
        }

        public SavedPawnDataRenameableReference ExportCurrentData()
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

            var savedDataName = $"{SavedPawnDataRenameableReference.StartingName}{number}";

            ExportCurrentPawnData(savedDataName);

            return new SavedPawnDataRenameableReference(
                this,
                savedDataName);
        }

        public void DeleteSave(string name)
        {
            var path = _saveFilePather.FullPath(name);
            if (!File.Exists(path))
                _logger.Warn("Tried to delete a nonexistent file.");
            File.Delete(path);
            RecacheSaves();
        }

        #endregion
    }
}
