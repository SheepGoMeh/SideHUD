using System;
using System.Numerics;
using ImGuiNET;

namespace SideHUDPlugin.ImGuiExtension
{
	public static class TextExtension
	{
		private struct FontGlyph
		{
#pragma warning disable 649
			public uint Codepoint;
			public float AdvanceX;
			public float X0, Y0, X1, Y1;
			public float U0, V0, U1, V1;
#pragma warning restore 649
		}

		public static unsafe void DrawText(string text, float scale, Vector2 position, Vector4 color, bool isVertical, bool isFlipped)
		{
			var uColor = ImGui.ColorConvertFloat4ToU32(color);
			var font = ImGui.GetFont();
			var textSize = CalcTextSize(text, scale);
			Vector2 textPos;

			if (isVertical)
			{
				textPos = isFlipped ? position : new Vector2(position.X, position.Y + textSize.X);
			}
			else
			{
				textPos = isFlipped ? position + textSize : position;
			}
			
			var currentTextPos = textPos;
			var charsUsed = 0;

			var drawList = ImGui.GetWindowDrawList();
			drawList.PrimReserve(6 * text.Length, 4 * text.Length);

			foreach (var c in text)
			{
				switch (c)
				{
					case '\n':
					{
						if (isVertical)
						{
							currentTextPos = isFlipped
								? new Vector2(currentTextPos.X - font.FontSize * scale, textPos.Y)
								: new Vector2(currentTextPos.X + font.FontSize * scale, textPos.Y);
						}
						else
						{
							currentTextPos = isFlipped
								? new Vector2(textPos.X, currentTextPos.Y - font.FontSize * scale)
								: new Vector2(textPos.X, currentTextPos.Y + font.FontSize * scale);
						}
					
						continue;
					}
					case '\r':
						continue;
				}

				var glyph = (FontGlyph*) font.FindGlyph(c).NativePtr;
				
				if (glyph == null)
				{
					continue;
				}

				if (isVertical)
				{
					// Vertical text

					if (isFlipped)
					{
						drawList.PrimQuadUV(
							currentTextPos + new Vector2(-glyph->Y1, glyph->X1) * scale, currentTextPos + new Vector2(-glyph->Y1, glyph->X0) * scale,
							currentTextPos + new Vector2(-glyph->Y0, glyph->X0) * scale, currentTextPos + new Vector2(-glyph->Y0, glyph->X1) * scale,
							new Vector2(glyph->U1, glyph->V1), new Vector2(glyph->U0, glyph->V1),
							new Vector2(glyph->U0, glyph->V0), new Vector2(glyph->U1, glyph->V0),
							uColor
						);
					}
					else
					{
						drawList.PrimQuadUV(
							currentTextPos + new Vector2(glyph->Y0, -glyph->X0) * scale, currentTextPos + new Vector2(glyph->Y0, -glyph->X1) * scale,
							currentTextPos + new Vector2(glyph->Y1, -glyph->X1) * scale, currentTextPos + new Vector2(glyph->Y1, -glyph->X0) * scale,
							new Vector2(glyph->U0, glyph->V0), new Vector2(glyph->U1, glyph->V0),
							new Vector2(glyph->U1, glyph->V1), new Vector2(glyph->U0, glyph->V1),
							uColor
						);
					}
				}
				else
				{
					if (isFlipped) // Upside-down text
					{
						drawList.PrimQuadUV(
							currentTextPos + new Vector2(-glyph->X1, -glyph->Y1) * scale, currentTextPos + new Vector2(-glyph->X0, -glyph->Y1) * scale,
							currentTextPos + new Vector2(-glyph->X0, -glyph->Y0) * scale, currentTextPos + new Vector2(-glyph->X1, -glyph->Y0) * scale,
							new Vector2(glyph->U1, glyph->V1), new Vector2(glyph->U0, glyph->V1),
							new Vector2(glyph->U0, glyph->V0), new Vector2(glyph->U1, glyph->V0),
							uColor
						);
					}
					else // Normal text
					{
						drawList.PrimQuadUV(
							currentTextPos + new Vector2(glyph->X0, glyph->Y0) * scale, currentTextPos + new Vector2(glyph->X1, glyph->Y0) * scale,
							currentTextPos + new Vector2(glyph->X1, glyph->Y1) * scale, currentTextPos + new Vector2(glyph->X0, glyph->Y1) * scale,
							new Vector2(glyph->U0, glyph->V0), new Vector2(glyph->U1, glyph->V0),
							new Vector2(glyph->U1, glyph->V1), new Vector2(glyph->U0, glyph->V1),
							uColor
						);
					}
				}

				if (isVertical)
				{
					if (isFlipped)
					{
						currentTextPos.Y += glyph->AdvanceX * scale;
					}
					else
					{
						currentTextPos.Y -= glyph->AdvanceX * scale;
					}
				}
				else
				{
					if (isFlipped)
					{
						currentTextPos.X -= glyph->AdvanceX * scale;
					}
					else
					{
						currentTextPos.X += glyph->AdvanceX * scale;
					}
				}

				charsUsed++;
			}
			
			drawList.PrimUnreserve(6 * (text.Length - charsUsed), 4 * (text.Length - charsUsed)); // Return unused primitves
		}
		
		public static void DrawOutlineText(Vector2 position, Vector4 color, Vector4 outlineColor, string text,
			float scale = 1f, uint thickness = 1, bool isVertical = false, bool isFlipped = false)
		{
			var mat = new[] {new[] {1, 1}, new[] {1, -1}, new[] {-1, 1}, new[] {-1, -1}};

			var pos = new Vector2();

			while (thickness-- != 0)
			{
				for (var i = 0; i != mat.Length; ++i)
				{
					pos.X = position.X - mat[i][0];
					pos.Y = position.Y - mat[i][1];
					DrawText(text, scale, pos, outlineColor, isVertical, isFlipped);
					mat[i][0] += mat[i][0] > 0 ? -1 : 1;
					mat[i][1] += mat[i][1] > 0 ? -1 : 1;
				}
			}

			pos = position;

			DrawText(text, scale, pos, color, isVertical, isFlipped);
		}

		public static unsafe Vector2 CalcTextSize(string text, float scale = 1f)
		{
			var font = ImGui.GetFont();
			
			var ret = new Vector2(0f, font.FontSize * scale);

			foreach (var c in text)
			{
				switch (c)
				{
					case '\n':
						ret.Y += font.FontSize * scale;
						continue;
					case '\r':
						continue;
				}

				var glyph = (FontGlyph*) font.FindGlyph(c).NativePtr;
				
				if (glyph == null)
				{
					continue;
				}

				ret.X += glyph->AdvanceX * scale;
			}

			return ret;
		}
	}
}