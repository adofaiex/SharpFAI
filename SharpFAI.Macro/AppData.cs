using System.Text;

namespace SharpFAI.Macro
{
	public class AppData
	{
		public static AppData instance = new AppData();

		private readonly Dictionary<string, string> dataMap = new Dictionary<string, string>();

		private readonly string dataPath = AppDomain.CurrentDomain.BaseDirectory + "\\data.properties";

		private AppData()
		{
			if (!File.Exists(dataPath))
			{
				File.Create(dataPath).Close();
				return;
			}
			string[] array = File.ReadAllText(dataPath).Split("\r\n");
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2.Length == 2)
				{
					dataMap[array2[0]] = array2[1];
				}
			}
		}

		public void Write(string key, object val)
		{
			if (val != null)
			{
				dataMap[key] = val.ToString();
			}
			Save();
		}

		public string Read(string key, string def)
		{
			return dataMap.GetValueOrDefault(key, def);
		}

		public void Save()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, string> item in dataMap)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2);
				handler.AppendFormatted(item.Key);
				handler.AppendLiteral("=");
				handler.AppendFormatted(item.Value);
				stringBuilder2.AppendLine(ref handler);
			}
			File.WriteAllText(dataPath, stringBuilder.ToString(), Encoding.UTF8);
		}
	}
}
