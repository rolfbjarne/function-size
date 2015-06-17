using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace functionsize
{
	class MainClass
	{
		class Data {
			public string Function;
			public ulong Size;
		}
		
		public static void Process (string filename)
		{
			var list = new List<Data> ();
			var last_match = new List<Data> ();
			ulong total = 0;

			using (var reader = new StreamReader (filename)) {
				string l;
				while ((l = reader.ReadLine ()) != null) {
					var s = l.IndexOf (' ');
					var d = new Data () {
						Function = l.Substring (s + 1),
						Size = ulong.Parse (l.Substring (0, s), System.Globalization.NumberStyles.HexNumber, null),
						};
					total += d.Size;
					list.Add (d);
				}
			}

			Console.WriteLine ("Found {0} functions, with a total of {1} bytes", list.Count, total);
			Console.WriteLine ("Write any regex to search for functions using that text (or ':q' to quit):");
			Console.Write ("$ ");
			string cmd;
			while ((cmd = Console.ReadLine ()) != null) {
				switch (cmd) {
				case ":quit":
				case ":q":
					return;
				case ":show-size":
					last_match.Sort ((a, b) => ((IComparable) a.Size).CompareTo (b.Size));
					foreach (var d in last_match)
						Console.WriteLine ("{0} {1}", d.Size, d.Function);
					break;
				case ":show":
					last_match.Sort ((a, b) => string.Compare (a.Function, b.Function));
					foreach (var d in last_match)
						Console.WriteLine ("{0} {1}", d.Size, d.Function);
					break;
				case "":
					break;
				default:
					var match = new Regex (cmd);
					var counter = 0;
					var agg_size = 0ul;
					last_match.Clear ();
					foreach (var d in list) {
						if (match.IsMatch (d.Function)) {
							counter++;
							agg_size += d.Size;
							last_match.Add (d);
						}
					}
					Console.WriteLine ("Found {0} functions for the expression {1}, adding up to {2} bytes ({3}% of the total number of functions and {4}% of the total size)",
						counter, cmd, agg_size, 100 * counter / (double) list.Count, 100 * agg_size / (double) total);
					break;
				}
				Console.Write ("$ ");
			}
		}

		public static void Main (string[] args)
		{
			Process ("/tmp/log");
		}

		public static void Main2 (string[] args)
		{
			using (var p = new Process ()) {
				p.StartInfo.FileName = "/usr/bin/dwarfdump";
				p.StartInfo.Arguments = "--all \"" + args [0] + "\"";
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.Start ();
				string line;
				using (var fs = new StreamWriter ("/tmp/log", false)) {
					while ((line = p.StandardOutput.ReadLine ()) != null) {
						if (line.Length < 39) {
	//						Console.Error.WriteLine ("Could not process line: {0}", line);
							continue;
						}
						var s_offset = line.Substring (2, 8);
						var s_start = line.Substring (15, 8);
						var s_end = line.Substring (28, 8);
						ulong offset, start, end;

						if (!ulong.TryParse (s_offset, System.Globalization.NumberStyles.HexNumber, null, out offset)) {
	//						Console.Error.WriteLine ("Could not process line (1): {0}", line);
							continue;
						}

						if (!ulong.TryParse (s_start, System.Globalization.NumberStyles.HexNumber, null, out start)) {
	//						Console.Error.WriteLine ("Could not process line (1): {0}", line);
							continue;
						}

						if (!ulong.TryParse (s_end, System.Globalization.NumberStyles.HexNumber, null, out end)) {
	//						Console.Error.WriteLine ("Could not process line (1): {0}", line);
							continue;
						}

						var fname = line.Substring (38);
						Console.WriteLine ("{0} {1}", end - start, fname);
						fs.WriteLine ("{0} {1}", end - start, fname);
					}
				}
			}
		}
	}
}
