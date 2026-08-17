using Newtonsoft.Json.Linq;

namespace SharpFAI.Macro;

public class MacroThread
{
	private readonly List<int> _keys;

	private List<(double, bool)> _noteTimes;

	private volatile bool _isRunning;

	private Thread? _workerThread;

	private int _keyIndex;

	private double _offset;

	private readonly Lock _syncLock = new Lock();

	private readonly List<double> intervals;

	private readonly int _startIndex;

	private readonly double[] angleData;

	private readonly bool[] autoFloors;

	private readonly bool _useNewTiming;

	public double Offset
	{
		get
		{
			using (_syncLock.EnterScope())
			{
				return _offset;
			}
		}
		set
		{
			using (_syncLock.EnterScope())
			{
				_offset = value;
			}
		}
	}

	public MacroThread(List<int> keys, List<(double, bool)> noteTimes, bool cmd = false, int startIndex = 0)
		: this(keys, noteTimes, cmd, startIndex, useNewTiming: false)
	{
	}

	public MacroThread(List<int> keys, List<(double, bool)> noteTimes, bool cmd, int startIndex, bool useNewTiming)
	{
		_keys = keys ?? throw new ArgumentNullException("keys");
		if (noteTimes == null)
		{
			throw new ArgumentNullException("noteTimes");
		}

		_useNewTiming = useNewTiming;
		_noteTimes = (useNewTiming ? noteTimes.Skip(1) : noteTimes).ToList();
		intervals = _noteTimes.Skip(1).Select((time, index) => (_useNewTiming ? _noteTimes[index].Item2 : time.Item2) ? (time.Item1 - _noteTimes[index].Item1) : Math.Min(time.Item1 - _noteTimes[index].Item1, 200.0)).ToList();
		intervals.Add(10.0);
		_startIndex = startIndex;
		angleData = MainWindow.Instance.currentLevel.angles.ToArray();
		autoFloors = BuildAutoFloors(MainWindow.Instance.currentLevel, angleData.Length + 1);
	}

	/// <summary>
	/// 按 AutoPlayTiles 事件构建每块的 auto 状态（持续状态，与游戏 scnGame.ApplyEventsToFloors 一致）
	/// </summary>
	private static bool[] BuildAutoFloors(Serialization.Level level, int floorCount)
	{
		var eventsByFloor = new Dictionary<int, List<JToken>>();
		foreach (JToken action in level.actions)
		{
			if ((string)action["eventType"] != "AutoPlayTiles")
			{
				continue;
			}

			int floor = action["floor"].Value<int>();
			if (floor < 0 || floor >= floorCount)
			{
				continue;
			}

			if (!eventsByFloor.TryGetValue(floor, out var list))
			{
				list = [];
				eventsByFloor[floor] = list;
			}

			list.Add(action["enabled"]);
		}

		var result = new bool[floorCount];
		bool auto = false;
		for (int i = 0; i < floorCount; i++)
		{
			if (eventsByFloor.TryGetValue(i, out var tokens))
			{
				foreach (JToken token in tokens)
				{
					auto = ToBool(token);
				}
			}

			result[i] = auto;
		}

		return result;
	}

	private static bool ToBool(JToken token)
	{
		if (token.Type == JTokenType.Boolean)
		{
			return token.Value<bool>();
		}

		string s = token.ToString();
		return string.Equals(s, "Enabled", StringComparison.OrdinalIgnoreCase)
		       || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)
		       || s == "1";
	}

	public void Start()
	{
		if (!_isRunning)
		{
			_isRunning = true;
			_workerThread = new Thread(WorkerLoop)
			{
				Name = "MacroThread",
				Priority = ThreadPriority.Highest
			};
			_workerThread.Start();
		}
	}

	public void ChangeNoteTimes(List<(double, bool)> noteTimes)
	{
		if (_isRunning)
		{
			Stop();
		}
		_noteTimes = noteTimes;
	}

	public void Stop()
	{
		_isRunning = false;
		Thread workerThread = _workerThread;
		if (workerThread != null && workerThread.IsAlive)
		{
			if (!_workerThread.Join(TimeSpan.FromSeconds(1L)))
			{
				_workerThread.Interrupt();
			}
			_workerThread = null;
		}
	}

	private void WorkerLoop()
	{
		try
		{
			_keyIndex = 0;
			int i = _startIndex + 1;
			double currentTime = GetCurrentTime();
			while (_isRunning && i < _noteTimes.Count)
			{
				for (double num = GetCurrentTime() - currentTime + _noteTimes[_startIndex].Item1; i < _noteTimes.Count && _noteTimes[i].Item1 + Offset <= num; i++)
				{
					bool targetIsAuto = i + 1 < autoFloors.Length && autoFloors[i + 1];
					if (!angleData[i].Equals(999.0) && !targetIsAuto)
					{
						TriggerKeyPress(ref _keyIndex, i);
					}
				}
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
		_isRunning = false;
	}

	private double GetCurrentTime()
	{
		return HighPrecisionTimer.GetTimeInNanoseconds() / 1000000.0;
	}

	private void TriggerKeyPress(ref int keyIndex, int currentIndex)
	{
		if (keyIndex > _keys.Count - 1)
		{
			keyIndex = 0;
		}
		Native.PressKey(_keys[keyIndex], intervals[currentIndex] - 5.0);
		keyIndex++;
	}
}