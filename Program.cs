using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.DLCPackage.Manifest;
using RocksmithToolkitLib.Extensions;

namespace DLCRenamer
{
	class Program
	{
		private static string _artistSongSeparator = "_";
		private static string _spaceSeparator = "-";
		private static bool _useMetadataVersion = false;
		private static bool _useMetadataDd = false;

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
				if (option.Contains("Use-Metadata-Version:") && option.Contains("true"))
					_useMetadataVersion = true;
				if (option.Contains("Use-Metadata-DD:") && option.Contains("true"))
					_useMetadataDd = true;
			}
		}

		private static void ProcessFiles(IEnumerable<string> fileNames)
		{
			var dir = Directory.GetCurrentDirectory();
			using (var writer = new StreamWriter("Songs.txt"))
			{
				foreach (var fileName in fileNames)
				{
					if (!fileName.IsValidPSARC()) continue;
					if (fileName.Contains("rs1compat")) continue;

					var unpackedDir = Packer.Unpack(fileName, dir, false, false, false);

					Attributes2014 attrs = null;
					var jsonFiles = Directory.GetFiles(unpackedDir, String.Format("*.json"), SearchOption.AllDirectories);
					if (jsonFiles.Length > 0 && !String.IsNullOrEmpty(jsonFiles[0]))
						attrs = Manifest2014<Attributes2014>.LoadFromFile(jsonFiles[0]).Entries.ToArray()[0].Value.ToArray()[0].Value;

					if (attrs == null) continue;

					var version = _useMetadataVersion ? GetVersionFromMetadata(unpackedDir) : GetVersionFromFileName(fileName);

					var dynamicDifficulty = _useMetadataDd
						? GetDynamicDifficultyFromMetadata(attrs)
						: GetDynamicDifficultyFromFileName(fileName);

					var newFileName = attrs.ArtistNameSort.GetValidName(true, true).Replace(" ", _spaceSeparator) + 
											_artistSongSeparator +
											attrs.SongNameSort.GetValidName(true, true).Replace(" ", _spaceSeparator) + 
											version + 
											dynamicDifficulty + 
											"_p.psarc";

					var artist = attrs.ArtistName;
					var song = attrs.SongName;

					DeleteDirectory(unpackedDir);

					try
					{
						File.Move(fileName, newFileName);
						Console.WriteLine(fileName + @": " + newFileName);
					}
					catch (IOException ex)
					{
						if (!ex.Message.Contains("Cannot create a file when that file already exists")) continue;

						newFileName = FileNameHelper.GetNextFileName(newFileName);
						try
						{
							File.Move(fileName, newFileName);
						}
						catch (Exception exception)
						{
							Console.WriteLine(fileName + @": " + newFileName);
							Console.WriteLine(exception.Message);
							Console.WriteLine(exception.InnerException);
							Console.ReadLine();
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(fileName + @": " + newFileName);
						Console.WriteLine(ex.Message);
						Console.WriteLine(ex.InnerException);
						Console.ReadLine();
					}

					writer.WriteLine("Old Filename: " + fileName);
					writer.WriteLine("New Filename: " + newFileName);
					writer.WriteLine("      Artist: " + artist);
					writer.WriteLine("        Song: " + song);
				}
			}
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

			return _artistSongSeparator + "v" + version;
		}

		private static string GetVersionFromFileName(string fileName)
		{
			var version = "1";

			var regex = new Regex(@"(_v+[0-9])([_.][^A-z])?");
			var match = regex.Match(fileName);
			if (match.Success)
			{
				version = match.Value.Replace("_v", "");
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
			var fileNames = Directory.GetFiles(dir, "*.psarc").Select(Path.GetFileName);
			return fileNames;
		}

		//from: http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
		public static void DeleteDirectory(string path)
		{
			foreach (var directory in Directory.GetDirectories(path))
			{
				DeleteDirectory(directory);
			}

			try
			{
				Directory.Delete(path, true);
			}
			catch (IOException)
			{
				Directory.Delete(path, true);
			}
			catch (UnauthorizedAccessException)
			{
				Directory.Delete(path, true);
			}
		}

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
