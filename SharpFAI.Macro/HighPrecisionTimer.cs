using System.Runtime.InteropServices;

namespace SharpFAI.Macro
{
	public static class HighPrecisionTimer
	{
		private const double SEC_2_TICK = 10000000.0;

		private const double TICK_2_SEC = 1E-07;

		private const long UNIX_EPOCH_TICKS = 621355968000000000L;

		private static readonly long qpcFrequency;

		private static readonly double qpcToSeconds;

		private static readonly double qpcToMilliseconds;

		private static readonly double qpcToMicroseconds;

		private static readonly double qpcToNanoseconds;

		private static readonly long baseQpcCount;

		private static readonly long baseFileTime;

		private static readonly DateTime baseDateTime;

		static HighPrecisionTimer()
		{
			if (!QueryPerformanceFrequency(out qpcFrequency))
			{
				throw new InvalidOperationException("硬件不支持高性能计数器");
			}
			qpcToSeconds = 1.0 / (double)qpcFrequency;
			qpcToMilliseconds = 1000.0 / (double)qpcFrequency;
			qpcToMicroseconds = 1000000.0 / (double)qpcFrequency;
			qpcToNanoseconds = 1000000000.0 / (double)qpcFrequency;
			long num = 0L;
			long num2 = 0L;
			for (int i = 0; i < 10; i++)
			{
				QueryPerformanceCounter(out var count);
				GetSystemTimePreciseAsFileTime(out var fileTime);
				num += count;
				num2 += fileTime;
				if (i < 9)
				{
					Thread.SpinWait(100);
				}
			}
			baseQpcCount = num / 10;
			baseFileTime = num2 / 10;
			baseDateTime = DateTime.FromFileTimeUtc(baseFileTime);
		}

		[DllImport("Kernel32.dll")]
		private static extern void GetSystemTimePreciseAsFileTime(out long fileTime);

		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(out long frequency);

		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceCounter(out long count);

		public static long GetQPCCount()
		{
			QueryPerformanceCounter(out var count);
			return count;
		}

		public static long GetFileTime()
		{
			GetSystemTimePreciseAsFileTime(out var fileTime);
			return fileTime;
		}

		public static double GetTimeInSeconds()
		{
			QueryPerformanceCounter(out var count);
			return (double)(count - baseQpcCount) * qpcToSeconds;
		}

		public static double GetTimeInMilliseconds()
		{
			QueryPerformanceCounter(out var count);
			return (double)(count - baseQpcCount) * qpcToMilliseconds;
		}

		public static double GetTimeInMicroseconds()
		{
			QueryPerformanceCounter(out var count);
			return (double)(count - baseQpcCount) * qpcToMicroseconds;
		}

		public static double GetTimeInNanoseconds()
		{
			QueryPerformanceCounter(out var count);
			return (double)(count - baseQpcCount) * qpcToNanoseconds;
		}

		public static DateTime GetPreciseUtcNow()
		{
			GetSystemTimePreciseAsFileTime(out var fileTime);
			return DateTime.FromFileTimeUtc(fileTime);
		}

		public static DateTime GetPreciseLocalNow()
		{
			return GetPreciseUtcNow().ToLocalTime();
		}

		public static double GetElapsedSeconds()
		{
			return GetTimeInSeconds();
		}

		public static double GetElapsedMilliseconds()
		{
			return GetTimeInMilliseconds();
		}

		public static double GetElapsedMicroseconds()
		{
			return GetTimeInMicroseconds();
		}

		public static double GetElapsedNanoseconds()
		{
			return GetTimeInNanoseconds();
		}

		public static long GetTicks()
		{
			GetSystemTimePreciseAsFileTime(out var fileTime);
			return fileTime;
		}

		public static long GetQPCTicks()
		{
			QueryPerformanceCounter(out var count);
			return (long)((double)(count - baseQpcCount) * qpcToNanoseconds / 100.0);
		}

		public static double GetUnixTimestamp()
		{
			GetSystemTimePreciseAsFileTime(out var fileTime);
			return (double)(fileTime - 621355968000000000L) / 10000000.0;
		}

		public static long GetUnixTimestampMs()
		{
			GetSystemTimePreciseAsFileTime(out var fileTime);
			return (fileTime - 621355968000000000L) / 10000;
		}

		public static long GetTimestamp()
		{
			QueryPerformanceCounter(out var count);
			return count;
		}

		public static double DifferenceInSeconds(long start, long end)
		{
			return (double)(end - start) * qpcToSeconds;
		}

		public static double DifferenceInMilliseconds(long start, long end)
		{
			return (double)(end - start) * qpcToMilliseconds;
		}

		public static double DifferenceInMicroseconds(long start, long end)
		{
			return (double)(end - start) * qpcToMicroseconds;
		}

		public static double DifferenceInNanoseconds(long start, long end)
		{
			return (double)(end - start) * qpcToNanoseconds;
		}

		public static void SleepMicroseconds(int microseconds)
		{
			if (microseconds <= 0)
			{
				return;
			}
			long num = GetTimestamp() + (long)((double)microseconds / qpcToMicroseconds);
			while (GetTimestamp() < num)
			{
				if (microseconds > 5000)
				{
					Thread.Sleep(1);
				}
				else
				{
					Thread.SpinWait(10);
				}
			}
		}

		public static void SleepMilliseconds(int milliseconds)
		{
			SleepMicroseconds(milliseconds * 1000);
		}

		public static string GetTimerInfo()
		{
			return $"高精度计时器信息:\nQPC频率: {qpcFrequency:N0} Hz\nQPC精度: {qpcToNanoseconds:F2} ns\n基准时间: {baseDateTime:yyyy-MM-dd HH:mm:ss.fffffff} (UTC)\n基准QPC: {baseQpcCount:N0}\n基准FileTime: {baseFileTime:N0}";
		}

		public static string GetAllCurrentTimes()
		{
			double timeInSeconds = GetTimeInSeconds();
			double timeInMilliseconds = GetTimeInMilliseconds();
			double timeInMicroseconds = GetTimeInMicroseconds();
			double timeInNanoseconds = GetTimeInNanoseconds();
			DateTime preciseUtcNow = GetPreciseUtcNow();
			DateTime dateTime = preciseUtcNow.ToLocalTime();
			long fileTime = GetFileTime();
			long qPCTicks = GetQPCTicks();
			double unixTimestamp = GetUnixTimestamp();

			return $"当前时间:\n运行时间: {timeInSeconds:F9} 秒\n         {timeInMilliseconds:F6} 毫秒\n         {timeInMicroseconds:F3} 微秒\n         {timeInNanoseconds:F1} 纳秒\nUTC时间: {preciseUtcNow:yyyy-MM-dd HH:mm:ss.fffffff}\n本地时间: {dateTime:yyyy-MM-dd HH:mm:ss.fffffff}\nFileTime: {fileTime:N0}\nQPC Ticks: {qPCTicks:N0}\nUnix时间戳: {unixTimestamp:F6} 秒";
		}
	}
}
