using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using BepInEx;
using Newtonsoft.Json;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Every run that ever finished on this machine, kept in one JSON file next to the mod's
///     config.
///     A run is an hour, and until now an hour left nothing behind but a results screen that
///     was closed and gone. The point of a history is the comparison: whether this run was
///     better than the last one is a question the results screen cannot answer on its own.
///     Loaded once and held in memory - the file is a few kilobytes after a hundred runs, and
///     reading it on every open would put disk IO inside a GUI pass.
/// </summary>
public class MatchHistoryService
{
	private const string _fileName = "AuthorTimeHunting.history.json";

	private const int _maxRecords = 200;

	private List<RunRecord> _records;

	private static string Path => System.IO.Path.Combine(Paths.ConfigPath, _fileName);

	public IReadOnlyList<RunRecord> Records => _records ??= Load();

	public void Add(RunRecord record)
	{
		if (record == null)
		{
			return;
		}

		List<RunRecord> records = _records ??= Load();

		records.Insert(0, record);

		if (records.Count > _maxRecords)
		{
			records.RemoveRange(_maxRecords, records.Count - _maxRecords);
		}

		Save(records);
	}

	private static List<RunRecord> Load()
	{
		try
		{
			if (!File.Exists(Path))
			{
				return [];
			}

			List<RunRecord> records = JsonConvert.DeserializeObject<List<RunRecord>>(File.ReadAllText(Path));

			return records?.Where(record => record != null).OrderByDescending(record => record.EndedAt).ToList() ??
			       [];
		}
		catch (Exception e)
		{
			Logger.LogError($"MatchHistoryService: Could not read {Path}: {e.Message}");
			return [];
		}
	}

	private static void Save(List<RunRecord> records)
	{
		try
		{
			File.WriteAllText(Path, JsonConvert.SerializeObject(records, Formatting.Indented));
		}
		catch (Exception e)
		{
			Logger.LogError($"MatchHistoryService: Could not write {Path}: {e.Message}");
		}
	}
}
