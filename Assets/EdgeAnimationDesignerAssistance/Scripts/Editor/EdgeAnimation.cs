using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EdgeAnimationDesignerAssistance {
	public class EdgeAnimation {
		public struct IntVector2 {
			public int x;
			public int y;

			public IntVector2(int x, int y) {
				this.x = x;
				this.y = y;
			}

			public int this[int index] {
				get{
					switch (index){
					case 0: return x;
					case 1: return y;
					default: throw new System.IndexOutOfRangeException();
					}
				}
				set{
					switch (index){
					case 0: x = value; break;
					case 1: y = value; break;
					default: throw new System.IndexOutOfRangeException();
					}
				}
			}

			public static IntVector2 zero {get{
				return new IntVector2(0, 0);
			}}

			public override bool Equals(object other) {
				if (other is IntVector2) {
					var p = (IntVector2)other;
					return (x == p.x) && (y == p.y);
				}
				return false;
			}

			public override int GetHashCode() {
				return x ^ (y << 17) ^ (int)((uint)y >> 15);
			}

			public void Set(int new_x, int new_y) {
				x = new_x;
				y = new_y;
			}

			public override string ToString() {
				var sb = new StringBuilder(28);
				sb.Append('(');
				sb.Append(x);
				sb.Append(", ");
				sb.Append(y);
				sb.Append(')');
				return sb.ToString();
			}
		}

		public struct Frame {
			public string filename;
			public string winTitle;
			public IntVector2 src;
			public int srcX {get{return src.x;} set{src.x = value;}}
			public int srcY {get{return src.y;} set{src.y = value;}}
			public int layerNum;
			public IntVector2 size;
			public int width {get{return size.x;} set{size.x = value;}}
			public int height {get{return size.y;} set{size.y = value;}}
			public IntVector2 dest;
			public int destX {get{return dest.x;} set{dest.x = value;}}
			public int destY {get{return dest.y;} set{dest.y = value;}}
			public int delay;
			public int layerAdd;
			public bool ckeyEnable;
			public int ckeyNum;
		}

		public struct Pattern {
			public string name;
			public List<Frame> frames;
		}

		public int animeVersion;
		public List<Pattern> patterns;

		public static EdgeAnimation FromFile(string path) {
			EdgeAnimation result = null;
			using (var sr = new StreamReader(path, Encoding.GetEncoding("shift_jis"))) {
				result = new EdgeAnimation();
				var keyValueRegex = new Regex(@"\s*(\w+)=(.*)");

				while (!sr.EndOfStream) {
					var line = sr.ReadLine();
					var match = keyValueRegex.Match(line);
					while (match.Success) {
						FromFileSetParameter(result, match.Groups[1].Value, match.Groups[2].Value);
						match = match.NextMatch();
					}
				}
			}
			return result;
		}

		private static void FromFileSetParameter(EdgeAnimation src, string key, string value) {
			switch (key) {
			case "ANIME_VERSION":
				src.animeVersion = int.Parse(value);
				break;
			case "PATTERN_NAME":
				if (src.patterns == null) {
					src.patterns = new List<Pattern>();
				}
				src.patterns.Add(new Pattern(){name = value});
				break;
			case "FRAME_NUMBER":
				{
					var pattern = src.patterns[src.patterns.Count - 1];
					if (pattern.frames == null) {
						pattern.frames = new List<Frame>();
					}
					pattern.frames.Add(new Frame());
					src.patterns[src.patterns.Count - 1] = pattern;
				}
				break;
			default:
				{
					var pattern = src.patterns[src.patterns.Count - 1];
					var frame = pattern.frames[pattern.frames.Count - 1];
					switch (key) {
					case "FILENAME":	frame.filename = value;	break;
					case "WIN_TITLE":	frame.winTitle = value;	break;
					case "SRC_X":		frame.srcX = int.Parse(value);	break;
					case "SRC_Y":		frame.srcY = int.Parse(value);	break;
					case "LAYER_NUM":	frame.layerNum = int.Parse(value);	break;
					case "WIDTH":		frame.width = int.Parse(value);	break;
					case "HEIGHT":		frame.height = int.Parse(value);	break;
					case "DEST_X":		frame.destX = int.Parse(value);	break;
					case "DEST_Y":		frame.destY = int.Parse(value);	break;
					case "DELAY":		frame.delay = int.Parse(value);	break;
					case "LAYER_ADD":	frame.layerAdd = int.Parse(value);	break;
					case "CKEY_ENABLE":	frame.ckeyEnable = int.Parse(value) != 0;	break;
					case "CKEY_NUM":	frame.ckeyNum = int.Parse(value);	break;
					}
					pattern.frames[pattern.frames.Count - 1] = frame;
					src.patterns[src.patterns.Count - 1] = pattern;
				}
				break;
			}
		}

		public void Save(string path) {
			using (var sw = new StreamWriter(path, false, Encoding.GetEncoding("shift_jis"))) {
				sw.WriteLine(";EDGE用アニメーションデータ");
				sw.Write("ANIME_VERSION=");
				sw.WriteLine(animeVersion);
				sw.WriteLine();
				foreach (var pattern in patterns) {
					sw.WriteLine();
					sw.WriteLine(";--------------------------");
					sw.Write("PATTERN_NAME=");
					sw.WriteLine(pattern.name);
					for (int i = 0, iMax = pattern.frames.Count; i < iMax; ++i) {
						var frame = pattern.frames[i];

						sw.WriteLine();
						sw.Write("\tFRAME_NUMBER=");
						sw.WriteLine(i + 1);
						sw.Write("\t\tFILENAME=");
						sw.WriteLine(frame.filename);
						sw.Write("\t\tWIN_TITLE=");
						sw.WriteLine(frame.winTitle);
						sw.Write("\t\tSRC_X=");
						sw.WriteLine(frame.srcX);
						sw.Write("\t\tSRC_Y=");
						sw.WriteLine(frame.srcY);
						sw.Write("\t\tLAYER_NUM=");
						sw.WriteLine(frame.layerNum);
						sw.Write("\t\tWIDTH=");
						sw.WriteLine(frame.width);
						sw.Write("\t\tHEIGHT=");
						sw.WriteLine(frame.height);
						sw.Write("\t\tDEST_X=");
						sw.WriteLine(frame.destX);
						sw.Write("\t\tDEST_Y=");
						sw.WriteLine(frame.destY);
						sw.Write("\t\tDELAY=");
						sw.WriteLine(frame.delay);
						sw.Write("\t\tLAYER_ADD=");
						sw.WriteLine(frame.layerAdd);
						sw.Write("\t\tCKEY_ENABLE=");
						sw.WriteLine(((frame.ckeyEnable)? 1: 0));
						sw.Write("\t\tCKEY_NUM=");
						sw.WriteLine(frame.ckeyNum);
					}
					sw.WriteLine();
				}
			}
		}
	}
}
