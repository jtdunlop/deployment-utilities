using System.Collections.Generic;
using NDesk.Options;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace DBSoft.Config.Transform
{
	static class Program
	{
		static readonly Hashtable Params = new Hashtable();
		static bool _verbose;
		static string _workingDirectory = ".";
		static bool _recursive;

		static void Main(string[] args)
		{
			if (!args.Any())
			{
				ShowOptions();
				return;
			}
			var options = new OptionSet ()
				.Add("?", f => ShowOptions())
				.Add("v", f => _verbose = true)
				.Add("recursive", f => _recursive = true)
				.Add("workingDirectory|dir=", SetWorkingDirectory)
				.Add ("setParam=|value=", SaveParam);
			options.Parse(args);

			if (_verbose)
			{
				foreach (var arg in args)
				{
					Console.WriteLine(arg);
				}
				foreach (DictionaryEntry param in Params)
				{
					Console.WriteLine("{0}={1}", param.Key, param.Value);
				}
			}

			var doc = new XmlDocument();
			doc.Load(_workingDirectory + "\\Parameters.Xml");
			const string select = "/parameters/parameter";
			var nodes = doc.SelectNodes(select);
			if (nodes == null) return;

			var xmlNodeList = nodes.OfType<XmlLinkedNode>().ToList();
			ValidateArguments(xmlNodeList, Params);
			foreach (var parameter in xmlNodeList.Where(parameter => parameter.Attributes != null))
			{
				ProcessParameter(parameter);
			}
		}

		private static void ValidateArguments(IEnumerable<XmlLinkedNode> xmlNodeList, IEnumerable @params)
		{
			foreach (var msg in @params.OfType<DictionaryEntry>()
				.Where(param => !xmlNodeList.Any(f => f.Attributes != null && f.Attributes["name"].InnerText == param.Key as string))
				.Select(param => string.Format("Source does not support parameter called '{1}'. Must be one of ({0}).",
				string.Join(", ", xmlNodeList.Select(f => f.Attributes != null ? f.Attributes["name"].InnerText : null).ToList()), param.Key)))
			{
				throw new Exception(msg);
			}
		}

		private static void SetWorkingDirectory(string workingDirectory)
		{
			_workingDirectory = workingDirectory;
		}

		private static void SaveParam(string s)
		{
			var split = s.Split(',');
			if (split.Count() != 2) return;
			var name = split[0].Split('=');
			var value = split[1].Split('=');
			if ( name.Count() == 2 && value.Count() == 2)
			{
				Params.Add(name[1].Trim('\''), value[1].Trim('\''));
			}
		}

		private static void ShowOptions()
		{
			Console.WriteLine("ClickOnce Application Config File Transformation Command Line Utility");
			Console.WriteLine("ClickOnce.Transform [args ...]");
			Console.WriteLine("-setParam:<params>             Sets a parameter.");
			Console.WriteLine("-recursive                     Apply parameters recursively.");
			Console.WriteLine("-verbose                       Enables more verbose output.");
			Console.WriteLine("-workingDirectory              Select the working directory.");
			Console.WriteLine("-?                             Displays options.");
		}

		private static void ProcessParameter(XmlNode parameter)
		{
			if (parameter.Attributes == null) return;
			var attr = parameter.Attributes["defaultValue"] == null ? "" : parameter.Attributes["defaultValue"].InnerText;
			var value = Params[parameter.Attributes["name"].InnerText] as string ?? attr;
			foreach (var entry in parameter.OfType<XmlLinkedNode>())
			{
				ApplyEntry(entry, value);
			}
		}

		private static void ApplyEntry(XmlNode entry, string value)
		{
			if (entry.Attributes == null || entry.Attributes["kind"].InnerText != "XmlFile") return;
			var expr = entry.Attributes["scope"].InnerText;
			if (_recursive)
			{
				RecursiveApply(_workingDirectory, expr, entry, value);
			}
			else
			{
				Apply(_workingDirectory, expr, entry, value);
			}
		}

		private static void Apply(string dir, string expr, XmlNode entry, string value)
		{
			if (_verbose)
			{
				Console.WriteLine("Looking in {0}", dir);
			}
			var reg = new Regex(expr);
			var files = Directory.GetFiles(dir, "*").Where(path => reg.IsMatch(path));
			foreach (var file in files)
			{
				if (_verbose)
				{
					Console.WriteLine("Found {0}", file);
				}
				var config = new XmlDocument();
				config.Load(file);
				if (entry.Attributes != null)
				{
					var match = entry.Attributes["match"].InnerText;
					var nodes = config.SelectNodes(match);
					if (nodes != null)
					{
						foreach (var node in nodes.OfType<XmlNode>())
						{
							node.Value = value;
							if (_verbose)
							{
								Console.WriteLine("Applying {0} to {1} in {2}", value, match, file);
							}
						}
					}
				}
				config.Save(file);
			}
		}

		private static void RecursiveApply(string start, string expr, XmlNode entry, string value)
		{
			Apply(start, expr, entry, value);
			foreach (var dir in Directory.GetDirectories(start))
			{
				RecursiveApply(dir, expr, entry, value);
			}
		}
	}
}
