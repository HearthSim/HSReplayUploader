using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HSReplayUploader.LogReader.EventArgs;

namespace HSReplayUploader.LogReader
{
	internal class LogWatcher
	{
		private readonly LogReaderInfo _info;
		private readonly int _readDelay;
		private long _offset;
		private bool _running;
		private DateTime _startingPoint;
		private bool _logFileExists;
		private bool _stop;
		private Thread _thread;
		public delegate void NewLineEventHandler(object sender, LogLineEventArgs args);
		public delegate void IgnoredLineEventHandler(object sender, RawLogLineEventArgs args);
		public delegate void LogFoundEventHandler(object sender, LogFoundEventArgs args);
		public event IgnoredLineEventHandler OnIgnoredLine;
		public event NewLineEventHandler OnNewLine;
		public event LogFoundEventHandler OnLogFound;

		public LogWatcher(LogReaderInfo info, int readDelay = 100)
		{
			_info = info;
			_readDelay = readDelay;
		}

		public void Start(DateTime startingPoint)
		{
			if(_running)
				return;
			MoveOrDeleteLogFile();
			_startingPoint = startingPoint;
			_stop = false;
			_offset = 0;
			_logFileExists = false;
			_thread = new Thread(ReadLogFile) { IsBackground = true };
			_thread.Start();
		}

		private void MoveOrDeleteLogFile()
		{
			if(File.Exists(_info.FilePath))
			{
				try
				{
					//check if we can move it
					File.Move(_info.FilePath, _info.FilePath);
					var old = _info.FilePath.Replace(".log", "_old.log");
					if(File.Exists(old))
					{
						try
						{
							File.Delete(old);
						}
						catch
						{
						}
					}
					File.Move(_info.FilePath, old);
				}
				catch
				{
					try
					{
						File.Delete(_info.FilePath);
					}
					catch
					{
					}
				}
			}
		}

		public async Task Stop()
		{
			_stop = true;
			while(_running || _thread == null || _thread.ThreadState == ThreadState.Unstarted)
				await Task.Delay(50);
			await Task.Factory.StartNew(() => _thread?.Join());
		}

		private void ReadLogFile()
		{
			_running = true;
			_offset = FindInitialOffset();
			while(!_stop)
			{
				var fileInfo = new FileInfo(_info.FilePath);
				if(fileInfo.Exists)
				{
					if(!_logFileExists)
					{
						_logFileExists = true;
						OnLogFound?.Invoke(this, new LogFoundEventArgs(_info.Name));
					}
					using(var fs = new FileStream(_info.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						fs.Seek(_offset, SeekOrigin.Begin);
						if(fs.Length == _offset)
						{
							Thread.Sleep(_readDelay);
							continue;
						}
						var lines = new List<LogLineItem>();
						using(var sr = new StreamReader(fs))
						{
							string line;
							while(!sr.EndOfStream && (line = sr.ReadLine()) != null)
							{
								if(line.StartsWith("D "))
								{
									var next = sr.Peek();
									if(!sr.EndOfStream && !(next == 'D' || next == 'W'))
										break;
									var logLine = new LogLineItem(_info.Name, line);
									if((!_info.HasFilters || (_info.StartsWithFilters?.Any(x => logLine.LineContent.StartsWith(x)) ?? false)
										|| (_info.ContainsFilters?.Any(x => logLine.LineContent.Contains(x)) ?? false))
										&& logLine.Time >= _startingPoint)
										lines.Add(logLine);
								}
								else
									OnIgnoredLine?.Invoke(this, new RawLogLineEventArgs(line));
								_offset += Encoding.UTF8.GetByteCount(line + Environment.NewLine);
							}
						}
						OnNewLine?.Invoke(this, new LogLineEventArgs(lines));
					}
				}
				Thread.Sleep(_readDelay);
			}
			_running = false;
		}

		private long FindInitialOffset()
		{
			var fileInfo = new FileInfo(_info.FilePath);
			if(fileInfo.Exists)
			{
				using(var fs = new FileStream(_info.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using(var sr = new StreamReader(fs, Encoding.ASCII))
				{
					var offset = 0;
					while(offset < fs.Length)
					{
						var sizeDiff = 4096 - Math.Min(fs.Length - offset, 4096);
						offset += 4096;
						var buffer = new char[4096];
						fs.Seek(Math.Max(fs.Length - offset, 0), SeekOrigin.Begin);
						sr.ReadBlock(buffer, 0, 4096);
						var skip = 0;
						for(var i = 0; i < 4096; i++)
						{
							skip++;
							if(buffer[i] == '\n')
								break;
						}
						offset -= skip;
						var lines = (new string(buffer.Skip(skip).ToArray())).Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToArray();
						for(int i = lines.Length - 1; i > 0; i--)
						{
							if(string.IsNullOrWhiteSpace(lines[i].Trim('\0')))
								continue;
							var logLine = new LogLineItem(_info.Name, lines[i]);
							if(logLine.Time < _startingPoint)
							{
								var negativeOffset = lines.Take(i + 1).Sum(x => Encoding.UTF8.GetByteCount(x + Environment.NewLine));
								return Math.Max(fs.Length - offset + negativeOffset + sizeDiff, 0);
							}
						}
					}
				}
			}
			return 0;
		}

		public DateTime FindEntryPoint(params string[] str)
		{
			var fileInfo = new FileInfo(_info.FilePath);
			if(fileInfo.Exists)
			{
				var targets = str.Select(x => new string(x.Reverse().ToArray())).ToList();
				using(var fs = new FileStream(_info.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using(var sr = new StreamReader(fs, Encoding.ASCII))
				{
					var offset = 0;
					while(offset < fs.Length)
					{
						offset += 4096;
						var buffer = new char[4096];
						fs.Seek(Math.Max(fs.Length - offset, 0), SeekOrigin.Begin);
						sr.ReadBlock(buffer, 0, 4096);
						var skip = 0;
						for(var i = 0; i < 4096; i++)
						{
							skip++;
							if(buffer[i] == '\n')
								break;
						}
						if(skip >= 4096)
							continue;
						offset -= skip;
						var reverse = new string(buffer.Skip(skip).Reverse().ToArray());
						var targetOffsets = targets.Select(x => reverse.IndexOf(x, StringComparison.Ordinal)).Where(x => x > -1).ToList();
						var targetOffset = targetOffsets.Any() ? targetOffsets.Min() : -1;
						if(targetOffset != -1)
						{
							var line = new string(reverse.Substring(targetOffset).TakeWhile(c => c != '\n').Reverse().ToArray());
							return new LogLineItem("", line).Time;
						}
					}
				}
			}
			return DateTime.MinValue;
		}
	}
}
