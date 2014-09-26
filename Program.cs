using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.DLCPackage.Manifest;
using RocksmithToolkitLib.Extensions;

namespace DLCRenamer
{
	class Program
	{
		private static string _artistSongSeparator = "_";
		private static string _spaceSeparator = "-";
		private static bool _useMetadataVersion;
		private static bool _useMetadataDd;
		private static bool _padVersion;
		private static bool _overrideCleanName;
		private static bool _includeSubfolders = true;
		private static readonly string[] InvalidStrings = {"\\", "/", "?", "*", ":", "\"", "<", ">", "|", "&"};

		static void Main()
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			GetOptions();

			var fileNames = GetFileList();
			ProcessFiles(fileNames);
			Console.WriteLine();
			Console.WriteLine(@"Done! Press any key to exit.");
			Console.ReadKey();
		}

		static void GetOptions()
		{
			if (!File.Exists("DLCRenamerOptions.txt")) return;

			var options = File.ReadAllLines("DLCRenamerOptions.txt");

			foreach (var option in options.Where(line => !line.StartsWith("#")))
			{
				if (option.Contains("Artist-Song-Separator:"))
					_artistSongSeparator = option.Replace("Artist-Song-Separator:", "").Trim().Replace("\"", "");
				if (option.Contains("Space-Character:"))
					_spaceSeparator = option.Replace("Space-Character:", "").Trim().Replace("\"", "");
				if (option.Contains("Use-Metadata-Version:") && option.ToLower().Contains("true"))
					_useMetadataVersion = true;
				if (option.Contains("Use-Metadata-DD:") && option.ToLower().Contains("true"))
					_useMetadataDd = true;
				if (option.Contains("Zero-Pad-Version:") && option.ToLower().Contains("true"))
					_padVersion = true;
				if (option.Contains("Override-Clean-Name:") && option.ToLower().Contains("true"))
					_overrideCleanName = true;
				if (option.Contains("Include-Subfolders:") && option.ToLower().Contains("false"))
					_includeSubfolders = false;
			}
		}

		private static void ProcessFiles(IEnumerable<string> fileNames)
		{
			var workingDirectory = Directory.GetCurrentDirectory();
			using (var writer = new StreamWriter("Songs-Renamed-" + DateTime.Now.Month + DateTime.Now.Day + DateTime.Now.Hour + DateTime.Now.Minute + ".txt"))
			{
				foreach (var filePathAndName in fileNames)
				{
					if (!filePathAndName.IsValidPSARC()) continue;
					if (filePathAndName.Contains("rs1compat")) continue;

					var filePath = Path.GetDirectoryName(filePathAndName) + "\\";

					var unpackedDir = Packer.Unpack(filePathAndName, workingDirectory, false, false, false);

					Attributes2014 attrs = null;
					var jsonFiles = Directory.GetFiles(unpackedDir, String.Format("*.json"), SearchOption.AllDirectories);
					if (jsonFiles.Length > 0 && !String.IsNullOrEmpty(jsonFiles[0]))
						attrs = Manifest2014<Attributes2014>.LoadFromFile(jsonFiles[0]).Entries.ToArray()[0].Value.ToArray()[0].Value;

					if (attrs == null) continue;

					var version = _useMetadataVersion ? GetVersionFromMetadata(unpackedDir) : GetVersionFromFileName(filePathAndName);

					var dynamicDifficulty = _useMetadataDd
						? GetDynamicDifficultyFromMetadata(attrs)
						: GetDynamicDifficultyFromFileName(filePathAndName);

					var newFileName = _overrideCleanName
						? GetFileNameSafeString(attrs.ArtistNameSort).Replace(" ", _spaceSeparator) +
						  _artistSongSeparator +
						  GetFileNameSafeString(attrs.SongNameSort).Replace(" ", _spaceSeparator) +
						  version +
						  dynamicDifficulty +
						  "_p.psarc"
						: attrs.ArtistNameSort.GetValidName(true, true).Replace(" ", _spaceSeparator) +
						  _artistSongSeparator +
						  attrs.SongNameSort.GetValidName(true, true).Replace(" ", _spaceSeparator) +
						  version +
						  dynamicDifficulty +
						  "_p.psarc";

					var artist = attrs.ArtistName;
					var song = attrs.SongName;

					writer.WriteLine("Old Filename: " + filePathAndName);
					writer.WriteLine("New Filename: " + filePath + newFileName);
					writer.WriteLine("      Artist: " + artist);
					writer.WriteLine(" Artist Sort: " + attrs.ArtistNameSort);
					writer.WriteLine("        Song: " + song);
					writer.WriteLine("   Song Sort: " + attrs.SongNameSort);
					writer.WriteLine("Meta Version: " + GetUnformattedVersionFromMetadata(unpackedDir));
					writer.WriteLine("     Meta DD: " + (attrs.MaxPhraseDifficulty > 0));
					writer.WriteLine("  DLC Author: " + GetAuthorFromMetadata(unpackedDir));
					writer.WriteLine();

					try
					{
						DeleteDirectory(unpackedDir);

						File.Move(filePathAndName, filePath + newFileName);
						Console.WriteLine(Path.GetFileName(filePathAndName) + @" -> " + newFileName);
					}
					catch (IOException ex)
					{
						if (ex.Message.Contains("Cannot create a file when that file already exists"))
						{
							newFileName = FileNameHelper.GetNextFileName(newFileName);
							try
							{
								File.Move(filePathAndName, filePath + newFileName);
							}
							catch (Exception exception)
							{
								LogErrorMessage(filePathAndName, newFileName, exception, writer);
							}
						}
						else
						{
							LogErrorMessage(filePathAndName, newFileName, ex, writer);
						}
					}
					catch (Exception ex)
					{
						LogErrorMessage(filePathAndName, newFileName, ex, writer);
					}
				}
			}
		}

		private static void LogErrorMessage(string fileName, string newFileName, Exception exception, TextWriter writer)
		{
			writer.WriteLine("Error encountered!");
			writer.WriteLine(fileName + @" -> " + newFileName);
			writer.WriteLine(exception.Message);
			writer.WriteLine(exception.InnerException);

			Console.WriteLine(@"Error encountered!");
			Console.WriteLine(Path.GetFileName(fileName) + @" -> " + newFileName);
			Console.WriteLine(exception.Message);
			Console.WriteLine(exception.InnerException);
			Console.WriteLine();
			Console.WriteLine(@"Press any key to continue.");
			Console.ReadKey();
		}

		private static string GetFileNameSafeString(string value)
		{
			foreach (var invalidChar in InvalidStrings)
			{
				if (value.Contains(invalidChar))
				{
					value = value.Replace(invalidChar, "");
				}
			}

			return value;
		}

		private static string GetAuthorFromMetadata(string unpackedDir)
		{
			var author = string.Empty;

			if (!File.Exists(unpackedDir + "\\toolkit.version"))
				return author;

			var lines = File.ReadAllLines(unpackedDir + "\\toolkit.version");
			foreach (var line in lines.Where(line => line.Contains("Package Author")))
			{
				author = line.Replace("Package Author:", "").Trim();
			}

			return author;
		}

		private static string GetUnformattedVersionFromMetadata(string unpackedDir)
		{
			var version = string.Empty;

			if (!File.Exists(unpackedDir + "\\toolkit.version"))
				return version;

			var lines = File.ReadAllLines(unpackedDir + "\\toolkit.version");
			foreach (var line in lines.Where(line => line.Contains("Package Version")))
			{
				version = line.Replace("Package Version:", "").Trim();
			}

			return version;
		}

		private static string GetVersionFromMetadata(string unpackedDir)
		{
			var version = "1";

			if (!File.Exists(unpackedDir + "\\toolkit.version")) 
				return _artistSongSeparator + "v" + version;

			var lines = File.ReadAllLines(unpackedDir + "\\toolkit.version");
			foreach (var line in lines.Where(line => line.Contains("Package Version")))
			{
				version = line.Replace("Package Version:", "").Trim().Replace(".", "_");
			}

			return GetFormattedVersionString(version);
		}

		private static string GetVersionFromFileName(string fileName)
		{
			var version = "1";

			var regex = new Regex(@"([vV]+[0-9])([_.-][0-9])?");
			var match = regex.Match(fileName);
			if (match.Success)
			{
				version = match.Value.ToLower().Replace("v", "");
			}

			return GetFormattedVersionString(version);
		}

		private static string GetFormattedVersionString(string version)
		{
			if (_padVersion && version.Length == 1)
			{
				version = version + "_0";
			}

			return _artistSongSeparator + "v" + version;
		}

		private static string GetDynamicDifficultyFromFileName(string fileName)
		{
			return fileName.Contains("_DD") ? _artistSongSeparator + "DD" : string.Empty;
		}

		private static string GetDynamicDifficultyFromMetadata(Attributes2014 attr)
		{
			return attr.MaxPhraseDifficulty > 0 ? _artistSongSeparator + "DD" : string.Empty;
		}

		private static IEnumerable<string> GetFileList()
		{
			var dir = Directory.GetCurrentDirectory();
			var fileNames = _includeSubfolders
				? Directory.GetFiles(dir, "*_p.psarc", SearchOption.AllDirectories)
				: Directory.GetFiles(dir, "*_p.psarc");
			return fileNames;
		}

		//from: http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
		private static void DeleteDirectory(string destinationDir)
		{
			const int magicDust = 10;
			for (var gnomes = 1; gnomes <= magicDust; gnomes++)
			{
				try
				{
					Directory.Delete(destinationDir, true);
				}
				catch (DirectoryNotFoundException)
				{
					return;  // good!
				}
				catch (IOException)
				{ 
					// System.IO.IOException: The directory is not empty
					//System.Diagnostics.Debug.WriteLine("Gnomes prevent deletion of {0}! Applying magic dust, attempt #{1}.", destinationDir, gnomes);
					Thread.Sleep(50);
					continue;
				}
				return;
			}
		}

		//from: http://stackoverflow.com/questions/189549/embedding-dlls-in-a-compiled-executable
		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");

			dllName = dllName.Replace(".", "_");

			if (dllName.EndsWith("_resources")) return null;

			var rm = new System.Resources.ResourceManager(typeof(Program).Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());

			var bytes = (byte[])rm.GetObject(dllName);

			return Assembly.Load(bytes);
		}
	}

	//from: http://social.msdn.microsoft.com/Forums/vstudio/en-US/feb18912-d1ae-46e0-b8aa-b739f5d2a86d/file-rename-algorithm
	public static class FileNameHelper
	{
		public static string GetNextFileName(string baseFileName)
		{
			var result = baseFileName;
			var filePath = Path.GetDirectoryName(baseFileName);
			var extensionPart = Path.GetExtension(baseFileName);

			while (File.Exists(result))
			{
				var fileNamePart = Path.GetFileNameWithoutExtension(result);
				var actuals = GetBaseAndCount(fileNamePart);
				var currentCount = actuals.Item2;
				fileNamePart = actuals.Item1;

				if (filePath != null)
					result = string.Format("{0} ({1}){2}",
						Path.Combine(filePath, fileNamePart),
						++currentCount, extensionPart);
			}

			return Path.GetFileName(result);
		}

		private static Tuple<string, int> GetBaseAndCount(string fileNamePart)
		{
			var currentCount = 1;
			var baseName = fileNamePart;

			// if string is non-null and non empty and last char is closing parenthesis
			if (!string.IsNullOrEmpty(fileNamePart) && fileNamePart[fileNamePart.Length - 1] == ')')
			{
				var lastOpeningParenthesis = fileNamePart.LastIndexOf('(');

				if (lastOpeningParenthesis >= 0)
				{
					// if found opening and closing parenthesis, pull out the number, if parses successfully
					var numberLength = fileNamePart.Length - lastOpeningParenthesis - 2;
					if (int.TryParse(fileNamePart.Substring(lastOpeningParenthesis + 1, numberLength),
									 out currentCount))
					{
						baseName = fileNamePart.Substring(0, lastOpeningParenthesis);
					}
				}
			}

			return new Tuple<string, int>(baseName, currentCount);
		}
	}
}
