using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using SideHUDPlugin.GameStructs;

namespace SideHUDPlugin.Interface
{
	public class HudWindow
	{
		public bool IsVisible = true;
		private readonly DalamudPluginInterface _pluginInterface;
		private readonly PluginConfiguration _pluginConfiguration;

		public HudWindow(DalamudPluginInterface pluginInterface, PluginConfiguration pluginConfiguration)
		{
			_pluginInterface = pluginInterface;
			_pluginConfiguration = pluginConfiguration;
		}

		private static void DrawOutlineText(float x, float y, Vector4 color, Vector4 outlineColor, string text, int thickness)
		{
			var mat = new[] {new[] {1, 1}, new[] {1, -1}, new[] {-1, 1}, new[] {-1, -1}};

			var pos = new Vector2();

			while (thickness-- != 0)
			{
				for (var i = 0; i != mat.Length; ++i)
				{
					pos.X = x - mat[i][0];
					pos.Y = y - mat[i][1];
					ImGui.SetCursorPos(pos);
					ImGui.TextColored(outlineColor, text);
					mat[i][0] += mat[i][0] > 0 ? -1 : 1;
					mat[i][1] += mat[i][1] > 0 ? -1 : 1;
				}
			}

			pos.X = x;
			pos.Y = y;

			ImGui.SetCursorPos(pos);
			ImGui.TextColored(color, text);
		}

		public unsafe void Draw()
		{
			if (!IsVisible || _pluginConfiguration.HideHud ||
			    (_pluginConfiguration.HideCombat && !_pluginInterface.ClientState.Condition[ConditionFlag.InCombat])) 
			{
				return;
			}

			var actor = _pluginInterface.ClientState.LocalPlayer;
			var parameterWidget =
				(AtkUnitBase*) _pluginInterface.Framework.Gui.GetUiObjectByName("_ParameterWidget", 1);
			var fadeMiddleWidget =
				(AtkUnitBase*) _pluginInterface.Framework.Gui.GetUiObjectByName("FadeMiddle", 1);

			// Display HUD only if parameter widget is visible and we're not in a fade event
			if (actor == null || parameterWidget == null || fadeMiddleWidget == null || !parameterWidget->IsVisible ||
			    fadeMiddleWidget->IsVisible)
			{
				return;
			}

			var viewportSize = ImGui.GetMainViewport().Size;
			ImGui.SetNextWindowPos(Vector2.Zero);
			ImGui.SetNextWindowSize(viewportSize);
			if (!ImGui.Begin("Side HUD",
				ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize |
				ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBringToFrontOnFocus))
			{
				return;
			}

			var hpScale = (float) actor.CurrentHp / actor.MaxHp;
			int resourceValue;
			float resourceScale;
			Vector4 resourceColor;

			if (actor.MaxCp > 0)
			{
				resourceValue = actor.CurrentCp;
				resourceColor = _pluginConfiguration.CpColorAlpha;
				resourceScale = (float) actor.CurrentCp / actor.MaxCp;
			}
			else if (actor.MaxCp > 0)
			{
				resourceValue = actor.CurrentGp;
				resourceColor = _pluginConfiguration.GpColorAlpha;
				resourceScale = (float) actor.CurrentGp / actor.MaxGp;
			}
			else
			{
				resourceValue = actor.CurrentMp;
				resourceColor = _pluginConfiguration.MpColorAlpha;
				resourceScale = (float) actor.CurrentMp / actor.MaxMp;
			}

			float leftBarScale;
			Vector4 leftBarColor;

			float rightBarScale;
			Vector4 rightBarColor;

			if (_pluginConfiguration.FlipBars)
			{
				leftBarScale = resourceScale;
				leftBarColor = resourceColor;
				rightBarScale = hpScale;
				rightBarColor = _pluginConfiguration.HpColorAlpha;
			}
			else
			{
				rightBarScale = resourceScale;
				rightBarColor = resourceColor;
				leftBarScale = hpScale;
				leftBarColor = _pluginConfiguration.HpColorAlpha;
			}

			ImGui.SetWindowFontScale(2.4f * _pluginConfiguration.FontScale * _pluginConfiguration.Scale);

			var cursorPos = new Vector2(viewportSize.X / 2f + _pluginConfiguration.Offset.X,
				viewportSize.Y / 2f + _pluginConfiguration.Offset.Y -
				(_pluginConfiguration.BarImage.Height / 2f + 100f) * _pluginConfiguration.Scale);
			var imageWidth = (_pluginConfiguration.BarImage.Width + _pluginConfiguration.BarGap) * _pluginConfiguration.Scale;

			// Left bar

			ImGui.SetCursorPos(new Vector2(cursorPos.X - imageWidth, cursorPos.Y));

			ImGui.Image(_pluginConfiguration.BarBackgroundImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarBackgroundImage.Width * _pluginConfiguration.Scale, _pluginConfiguration.BarBackgroundImage.Height * _pluginConfiguration.Scale),
				Vector2.One,
				Vector2.Zero, _pluginConfiguration.BgColorAlpha);
			ImGui.SetCursorPos(new Vector2(cursorPos.X - imageWidth,
				cursorPos.Y + _pluginConfiguration.BarImage.Height * _pluginConfiguration.Scale * (1f - leftBarScale)));
			ImGui.Image(_pluginConfiguration.BarImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarImage.Height * leftBarScale * _pluginConfiguration.Scale),
				new Vector2(1f, leftBarScale), Vector2.Zero, leftBarColor);
			ImGui.SetCursorPos(new Vector2(cursorPos.X - imageWidth, cursorPos.Y));
			ImGui.Image(_pluginConfiguration.BarCastBackgroundImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarCastBackgroundImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarCastBackgroundImage.Height * _pluginConfiguration.Scale),
				Vector2.One,
				Vector2.Zero, _pluginConfiguration.BgColorAlpha);

			// Right bar
			
			ImGui.SetCursorPos(new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
				cursorPos.Y));

			ImGui.Image(_pluginConfiguration.BarBackgroundImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarBackgroundImage.Width * _pluginConfiguration.Scale, _pluginConfiguration.BarBackgroundImage.Height * _pluginConfiguration.Scale),
				Vector2.Zero,
				Vector2.One, _pluginConfiguration.BgColorAlpha);

			ImGui.SetCursorPos(new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
				cursorPos.Y + _pluginConfiguration.BarImage.Height * _pluginConfiguration.Scale * (1f - rightBarScale)));
			ImGui.Image(_pluginConfiguration.BarImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarImage.Height * rightBarScale * _pluginConfiguration.Scale), new Vector2(0f, 1f - rightBarScale),
				Vector2.One, rightBarColor);
			ImGui.SetCursorPos(new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
				cursorPos.Y));
			ImGui.Image(_pluginConfiguration.BarCastBackgroundImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarCastBackgroundImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarCastBackgroundImage.Height * _pluginConfiguration.Scale),
				Vector2.Zero,
				Vector2.One, _pluginConfiguration.BgColorAlpha);

			var cursorY = ImGui.GetCursorPosY();
			var shieldScale = *(int*) (actor.Address + 0x1997) / 100f;

			if (_pluginConfiguration.FlipCastBar)
			{
				// Shield
				ImGui.SetCursorPos(new Vector2(
					cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale, cursorPos.Y));

				ImGui.Image(_pluginConfiguration.BarCastImage.ImGuiHandle,
					new Vector2(_pluginConfiguration.BarCastImage.Width * _pluginConfiguration.Scale,
						_pluginConfiguration.BarCastImage.Height * shieldScale * _pluginConfiguration.Scale),
					Vector2.Zero, new Vector2(1f, shieldScale),
					_pluginConfiguration.ShieldColorAlpha);
			}
			else
			{
				// Shield
				ImGui.SetCursorPos(new Vector2(cursorPos.X - imageWidth, cursorPos.Y));

				ImGui.Image(_pluginConfiguration.BarCastImage.ImGuiHandle,
					new Vector2(_pluginConfiguration.BarCastImage.Width * _pluginConfiguration.Scale,
						_pluginConfiguration.BarCastImage.Height * shieldScale * _pluginConfiguration.Scale),
					Vector2.One, new Vector2(0f, 1f + shieldScale),
					_pluginConfiguration.ShieldColorAlpha);
			}
			
			// Cast bar

			var castBar = (AddonCastBar*) _pluginInterface.Framework.Gui.GetUiObjectByName("_CastBar", 1);

			if (castBar != null && castBar->AtkUnitBase.UldManager.NodeList != null &&
			    castBar->AtkUnitBase.UldManager.NodeListCount > 11 && castBar->AtkUnitBase.IsVisible)
			{
				var castScale = castBar->CastPercent / 100;
				var castTime = ((_pluginConfiguration.CastTimeUp ? 0 : castBar->CastTime) -
				                castBar->CastTime * castScale) / 100;
				var slideCastScale = _pluginConfiguration.SlidecastTime / 10f / castBar->CastTime; 
				var castSign = _pluginConfiguration.CastTimeUp ? "" : "−";
				var castString = $"{castBar->CastName.GetString()}\n{castSign} {Math.Abs(castTime):F}";
				var castStringSize = ImGui.CalcTextSize(castString);

				var interrupted = false;

				for (var i = 0; i != castBar->AtkUnitBase.UldManager.NodeListCount; ++i)
				{
					var node = castBar->AtkUnitBase.UldManager.NodeList[i];
					// ReSharper disable once InvertIf
					if (node->NodeID == 2 && node->IsVisible) // Interrupted text node
					{
						interrupted = true;
						break;
					}
				}

				if (_pluginConfiguration.FlipCastBar)
				{
					DrawOutlineText(
						cursorPos.X - castStringSize.X / 2 - _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
						cursorPos.Y - castStringSize.Y, _pluginConfiguration.CastColorAlpha, _pluginConfiguration.OutlineColorAlpha, castString, 2);

					ImGui.SetCursorPos(new Vector2(cursorPos.X - imageWidth,
						cursorPos.Y + _pluginConfiguration.BarCastImage.Height * _pluginConfiguration.Scale * (1f - castScale)));

					ImGui.Image(_pluginConfiguration.BarCastImage.ImGuiHandle,
						new Vector2(_pluginConfiguration.BarCastImage.Width * _pluginConfiguration.Scale,
							_pluginConfiguration.BarCastImage.Height * castScale * _pluginConfiguration.Scale),
						new Vector2(1f, castScale), Vector2.Zero,
						interrupted
							? _pluginConfiguration.CastInterruptColorAlpha
							: _pluginConfiguration.CastColorAlpha);

					// Slidecast
					if (_pluginConfiguration.ShowSlidecast)
					{
						ImGui.SetCursorPos(new Vector2(cursorPos.X - imageWidth, cursorPos.Y));

						ImGui.Image(_pluginConfiguration.BarCastImage.ImGuiHandle,
							new Vector2(_pluginConfiguration.BarCastImage.Width * _pluginConfiguration.Scale,
								_pluginConfiguration.BarCastImage.Height * slideCastScale * _pluginConfiguration.Scale),
							Vector2.One, new Vector2(0f, 1f + slideCastScale),
							_pluginConfiguration.SlidecastColorAlpha);
					}
				}
				else
				{
					DrawOutlineText(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
						cursorPos.Y - castStringSize.Y, _pluginConfiguration.CastColorAlpha, _pluginConfiguration.OutlineColorAlpha, castString, 2);

					ImGui.SetCursorPos(new Vector2(
						cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
						cursorPos.Y + _pluginConfiguration.BarCastImage.Height * _pluginConfiguration.Scale * (1f - castScale)));

					ImGui.Image(_pluginConfiguration.BarCastImage.ImGuiHandle,
						new Vector2(_pluginConfiguration.BarCastImage.Width * _pluginConfiguration.Scale,
							_pluginConfiguration.BarCastImage.Height * castScale * _pluginConfiguration.Scale),
						new Vector2(0f, 1f - castScale), Vector2.One,
						interrupted
							? _pluginConfiguration.CastInterruptColorAlpha
							: _pluginConfiguration.CastColorAlpha);

					// Slidecast
					if (_pluginConfiguration.ShowSlidecast)
					{
						ImGui.SetCursorPos(new Vector2(
							cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale, cursorPos.Y));

						ImGui.Image(_pluginConfiguration.BarCastImage.ImGuiHandle,
							new Vector2(_pluginConfiguration.BarCastImage.Width * _pluginConfiguration.Scale,
								_pluginConfiguration.BarCastImage.Height * slideCastScale * _pluginConfiguration.Scale),
							Vector2.Zero, new Vector2(1f, slideCastScale),
							_pluginConfiguration.SlidecastColorAlpha);
					}
				}
			}

			ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorY));

			Vector2 hpTextPos;
			Vector2 resourceTextPos;

			if (_pluginConfiguration.FlipBars)
			{
				hpTextPos = new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
					ImGui.GetCursorPosY());
				resourceTextPos =
					new Vector2(
						cursorPos.X - ImGui.CalcTextSize(resourceValue.ToString()).X -
						_pluginConfiguration.BarGap * _pluginConfiguration.Scale, ImGui.GetCursorPosY());
			}
			else
			{
				hpTextPos = new Vector2(
					cursorPos.X - ImGui.CalcTextSize(actor.CurrentHp.ToString()).X -
					_pluginConfiguration.BarGap * _pluginConfiguration.Scale, ImGui.GetCursorPosY());
				resourceTextPos = new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
					ImGui.GetCursorPosY());
			}

			var hpPercent = hpScale * 100f;
			var resourcePercent = resourceScale * 100f;

			DrawOutlineText(hpTextPos.X, hpTextPos.Y, _pluginConfiguration.HpColorAlpha, _pluginConfiguration.OutlineColorAlpha,
				actor.CurrentHp + (_pluginConfiguration.ShowPercentage ? $"\n({hpPercent:F0}%%)" : ""), 2);

			DrawOutlineText(resourceTextPos.X, resourceTextPos.Y, resourceColor, _pluginConfiguration.OutlineColorAlpha,
				resourceValue + (_pluginConfiguration.ShowPercentage ? $"\n({resourcePercent:F0} %%)" : ""), 2);

			ImGui.End();
		}
	}
}