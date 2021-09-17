using System.Diagnostics;
using System.Xml.Serialization;

namespace Ax.Fw.Windows
{
    public class Schtasks
    {
		public bool GetTaskInfo(string _taskName, out TaskInfo _taskInfo)
		{
			using (Process schtasks = Process.Start(new ProcessStartInfo() { 
				FileName = "schtasks.exe", 
				Arguments = $"/query /xml /tn {_taskName}", 
				RedirectStandardError = true, 
				RedirectStandardOutput = true, 
				UseShellExecute = false }))
			{
				schtasks.WaitForExit();
				if (schtasks.ExitCode != 0)
				{
					_taskInfo = null;
					return false;
				}

				XmlSerializer serializer = new XmlSerializer(typeof(TaskInfo));
				_taskInfo = (TaskInfo)serializer.Deserialize(schtasks.StandardOutput);
				return true;
			}
		}

		public bool CreateTaskFromXML(string _taskName, string _xmlFilePath)
        {
			using (Process schtasks = Process.Start(new ProcessStartInfo() { 
				FileName = "schtasks.exe", 
				Arguments = $"/create /f /xml \"{_xmlFilePath}\" /tn {_taskName}", 
				RedirectStandardError = true, 
				RedirectStandardOutput = true, 
				UseShellExecute = false }))
			{
				schtasks.WaitForExit();
				return schtasks.ExitCode == 0;
			}
		}

		public bool DeleteTask(string _taskName)
		{
			using (Process schtasks = Process.Start(new ProcessStartInfo() { 
				FileName = "schtasks.exe", 
				Arguments = $"/delete /f /tn {_taskName}", 
				RedirectStandardError = true, 
				RedirectStandardOutput = true,
				UseShellExecute = false }))
			{
				schtasks.WaitForExit();
				return schtasks.ExitCode == 0;
			}
		}

    }

	[XmlRoot(ElementName = "RegistrationInfo", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class RegistrationInfo
	{
		[XmlElement(ElementName = "Date", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string Date { get; set; }

		[XmlElement(ElementName = "Author", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string Author { get; set; }

		[XmlElement(ElementName = "URI", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string URI { get; set; }
	}

	[XmlRoot(ElementName = "Principal", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class Principal
	{
		[XmlElement(ElementName = "UserId", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string UserId { get; set; }

		[XmlElement(ElementName = "LogonType", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string LogonType { get; set; }

		[XmlElement(ElementName = "RunLevel", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string RunLevel { get; set; }

		[XmlAttribute(AttributeName = "id")]
		public string Id { get; set; }
	}

	[XmlRoot(ElementName = "Principals", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class Principals
	{
		[XmlElement(ElementName = "Principal", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public Principal Principal { get; set; }
	}

	[XmlRoot(ElementName = "IdleSettings", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class IdleSettings
	{
		[XmlElement(ElementName = "StopOnIdleEnd", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string StopOnIdleEnd { get; set; }

		[XmlElement(ElementName = "RestartOnIdle", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string RestartOnIdle { get; set; }
	}

	[XmlRoot(ElementName = "Settings", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class Settings
	{
		[XmlElement(ElementName = "DisallowStartIfOnBatteries", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string DisallowStartIfOnBatteries { get; set; }

		[XmlElement(ElementName = "StopIfGoingOnBatteries", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string StopIfGoingOnBatteries { get; set; }

		[XmlElement(ElementName = "ExecutionTimeLimit", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string ExecutionTimeLimit { get; set; }

		[XmlElement(ElementName = "MultipleInstancesPolicy", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string MultipleInstancesPolicy { get; set; }

		[XmlElement(ElementName = "Priority", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string Priority { get; set; }

		[XmlElement(ElementName = "IdleSettings", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public IdleSettings IdleSettings { get; set; }
	}

	[XmlRoot(ElementName = "LogonTrigger", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class LogonTrigger
	{
		[XmlElement(ElementName = "UserId", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string UserId { get; set; }
	}

	[XmlRoot(ElementName = "Triggers", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class Triggers
	{
		[XmlElement(ElementName = "LogonTrigger", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public LogonTrigger LogonTrigger { get; set; }
	}

	[XmlRoot(ElementName = "Exec", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class Exec
	{
		[XmlElement(ElementName = "Command", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string Command { get; set; }

		[XmlElement(ElementName = "Arguments", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public string Arguments { get; set; }
	}

	[XmlRoot(ElementName = "Actions", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class Actions
	{
		[XmlElement(ElementName = "Exec", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public Exec Exec { get; set; }

		[XmlAttribute(AttributeName = "Context")]
		public string Context { get; set; }
	}

	[XmlRoot(ElementName = "Task", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
	public class TaskInfo
	{
		[XmlElement(ElementName = "RegistrationInfo", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public RegistrationInfo RegistrationInfo { get; set; }

		[XmlElement(ElementName = "Principals", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public Principals Principals { get; set; }

		[XmlElement(ElementName = "Settings", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public Settings Settings { get; set; }

		[XmlElement(ElementName = "Triggers", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public Triggers Triggers { get; set; }

		[XmlElement(ElementName = "Actions", Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task")]
		public Actions Actions { get; set; }

		[XmlAttribute(AttributeName = "version")]
		public string Version { get; set; }

		[XmlAttribute(AttributeName = "xmlns")]
		public string Xmlns { get; set; }
	}

}
