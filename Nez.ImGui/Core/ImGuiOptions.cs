using ImGuiNET;
using System;
using System.Collections.Generic;
using Num = System.Numerics;


namespace Nez.ImGuiTools
{
	public class ImGuiOptions
	{
		internal bool _includeDefaultFont = true;
		internal List<Tuple<string, float, ushort[]>> _fonts = new List<Tuple<string, float, ushort[]>>();
		internal string _gameWindowTitle = "Game Window";
		internal Num.Vector2 _gameWindowFirstPosition = new Num.Vector2(345f, 25f);
		internal ImGuiWindowFlags _gameWindowFlags = 0;


		public ImGuiOptions AddFont(string path, float size, ushort[] glyphRange)
		{
			_fonts.Add(new Tuple<string, float, ushort[]>(path, size, glyphRange));
			return this;
		}

		public ImGuiOptions AddFont(string path, float size)
		{
			return AddFont(path, size, Array.Empty<ushort>());
		}

		public ImGuiOptions IncludeDefaultFont(bool include)
		{
			_includeDefaultFont = include;
			return this;
		}

		public ImGuiOptions SetGameWindowTitle(string title)
		{
			_gameWindowTitle = title;
			return this;
		}

		public ImGuiOptions SetGameWindowFirstPosition(float x, float y)
		{
			_gameWindowFirstPosition = new Num.Vector2(x, y);
			return this;
		}

		public ImGuiOptions SetGameWindowFlag(ImGuiWindowFlags flag)
		{
			_gameWindowFlags |= flag;
			return this;
		}
	}
}