using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.DLCPackage.Manifest;
using RocksmithToolkitLib.Extensions;

namespace DLCRenamer
{
	class Program
	{
		static void Main(string[] args)
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			var fileNames = GetFileList();
			ProcessFiles(fileNames);
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

		private static void ProcessFiles(IEnumerable<string> fileNames)
		{
			var dir = Directory.GetCurrentDirectory();
			using (var writer = new StreamWriter("Songs.txt"))
			{
				foreach (var fileName in fileNames)
				{
					if (!fileName.IsValidPSARC()) continue;

					var fileEnd = "_p.psarc";
					var versionIndex = fileName.LastIndexOf("_v", StringComparison.Ordinal);
					if (versionIndex > 0)
						fileEnd = fileName.Substring(versionIndex);

					var unpackedDir = Packer.Unpack(fileName, dir, false, false, false);

					Attributes2014 att = null;
					var jsonFiles = Directory.GetFiles(unpackedDir, String.Format("*.json"), SearchOption.AllDirectories);
					if (jsonFiles.Length > 0 && !String.IsNullOrEmpty(jsonFiles[0]))
						att = Manifest2014<Attributes2014>.LoadFromFile(jsonFiles[0]).Entries.ToArray()[0].Value.ToArray()[0].Value;

					DeleteDirectory(unpackedDir);

					if (att == null) continue;

					var newFileName = att.ArtistNameSort.GetValidName(true, true).Replace(" ", "-") + "_" +
					                  att.SongNameSort.GetValidName(true, true).Replace(" ", "-") + fileEnd;

					var artist = att.ArtistName.GetValidName(true, true).Replace(" ", "-");
					var song = att.SongName.GetValidName(true, true).Replace(" ", "-");

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

		private static IEnumerable<string> GetFileList()
		{
			var dir = Directory.GetCurrentDirectory();
			var fileNames = Directory.GetFiles(dir, "*.psarc").Select(Path.GetFileName);
			return fileNames;
		}

		public static void DeleteDirectory(string targetDir)
		{
			var files = Directory.GetFiles(targetDir);
			var dirs = Directory.GetDirectories(targetDir);

			foreach (var file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}

			foreach (var dir in dirs)
			{
				DeleteDirectory(dir);
			}

			Directory.Delete(targetDir, false);
		}
	}

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
