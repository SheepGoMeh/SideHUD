using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState;
using Dalamud.Interface;
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

		private static void DrawOutlineText(float x, float y, Vector4 color, Vector4 outlineColor, string text,
			int thickness)
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

		private void DrawBar(Vector2 cursorPos, float offsetX, float scale, Vector4 color, bool isRight)
		{
			var offset = isRight ? offsetX : -offsetX;
			ImGui.SetCursorPos(new Vector2(cursorPos.X + offset, cursorPos.Y));

			ImGui.Image(_pluginConfiguration.BarBackgroundImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarBackgroundImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarBackgroundImage.Height * _pluginConfiguration.Scale),
				isRight ? Vector2.Zero : Vector2.One, isRight ? Vector2.One : Vector2.Zero,
				_pluginConfiguration.BgColorAlpha);
			ImGui.SetCursorPos(new Vector2(cursorPos.X + offset,
				cursorPos.Y + _pluginConfiguration.BarImage.Height * _pluginConfiguration.Scale * (1f - scale)));
			ImGui.Image(_pluginConfiguration.BarImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarImage.Height * scale * _pluginConfiguration.Scale),
				isRight ? new Vector2(0f, 1f - scale) : new Vector2(1f, scale),
				isRight ? Vector2.One : Vector2.Zero, color);
			ImGui.SetCursorPos(new Vector2(cursorPos.X + offset, cursorPos.Y));
			ImGui.Image(_pluginConfiguration.BarCastBackgroundImage.ImGuiHandle,
				new Vector2(_pluginConfiguration.BarCastBackgroundImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarCastBackgroundImage.Height * _pluginConfiguration.Scale),
				isRight ? Vector2.Zero : Vector2.One, isRight ? Vector2.One : Vector2.Zero,
				_pluginConfiguration.BgColorAlpha);
		}

		private void DrawCastBar(Vector2 cursorPos, float offsetX, float scale, Vector4 color, bool isRight,
			bool isYFlipped)
		{
			var offset = isRight ? offsetX : -offsetX;
			ImGui.SetCursorPos(new Vector2(cursorPos.X + offset,
				isYFlipped
					? cursorPos.Y + _pluginConfiguration.BarCastImage.Height * _pluginConfiguration.Scale *
					(1f - scale)
					: cursorPos.Y));
			ImGui.Image(_pluginConfiguration.BarCastImage.ImGuiHandle, new Vector2(
					_pluginConfiguration.BarCastImage.Width * _pluginConfiguration.Scale,
					_pluginConfiguration.BarCastImage.Height * scale * _pluginConfiguration.Scale),
				isRight ? isYFlipped ? new Vector2(0f, 1f + scale) : Vector2.Zero
				: isYFlipped ? new Vector2(1f, scale) : Vector2.One,
				isRight ? isYFlipped ? Vector2.One : new Vector2(1f, scale)
				: isYFlipped ? Vector2.Zero : new Vector2(0f, 1f + scale), color);
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
			ImGuiHelpers.ForceNextWindowMainViewport();
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
			var imageWidth = (_pluginConfiguration.BarImage.Width + _pluginConfiguration.BarGap) *
			                 _pluginConfiguration.Scale;

			// Left bar
			DrawBar(cursorPos, imageWidth, leftBarScale, leftBarColor, false);

			// Right bar
			DrawBar(cursorPos, _pluginConfiguration.BarGap * _pluginConfiguration.Scale, rightBarScale, rightBarColor,
				true);


			var cursorY = ImGui.GetCursorPosY();
			var shieldScale = *(int*) (actor.Address + 0x1997) / 100f;

			// Shield
			DrawCastBar(cursorPos,
				_pluginConfiguration.FlipCastBar
					? _pluginConfiguration.BarGap * _pluginConfiguration.Scale
					: imageWidth, shieldScale, _pluginConfiguration.ShieldColorAlpha, _pluginConfiguration.FlipCastBar,
				false);

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
						cursorPos.Y - castStringSize.Y, _pluginConfiguration.CastColorAlpha,
						_pluginConfiguration.OutlineColorAlpha, castString, 2);

					DrawCastBar(cursorPos, imageWidth, castScale,
						interrupted
							? _pluginConfiguration.CastInterruptColorAlpha
							: _pluginConfiguration.CastColorAlpha, false, true);

					// Slidecast
					if (_pluginConfiguration.ShowSlidecast)
					{
						DrawCastBar(cursorPos, imageWidth, slideCastScale, _pluginConfiguration.SlidecastColorAlpha,
							false, false);
					}
				}
				else
				{
					DrawOutlineText(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
						cursorPos.Y - castStringSize.Y, _pluginConfiguration.CastColorAlpha,
						_pluginConfiguration.OutlineColorAlpha, castString, 2);

					DrawCastBar(cursorPos, _pluginConfiguration.BarGap * _pluginConfiguration.Scale, castScale,
						interrupted
							? _pluginConfiguration.CastInterruptColorAlpha
							: _pluginConfiguration.CastColorAlpha, true, true);

					// Slidecast
					if (_pluginConfiguration.ShowSlidecast)
					{
						DrawCastBar(cursorPos, _pluginConfiguration.BarGap * _pluginConfiguration.Scale, slideCastScale,
							_pluginConfiguration.SlidecastColorAlpha, true, false);
					}
				}
			}

			if (_pluginConfiguration.ShowPercentage || _pluginConfiguration.ShowNumbers)
			{
				ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorY));

				Vector2 hpTextPos;
				Vector2 resourceTextPos;

				var hpText = String.Empty;
				var resourceText = string.Empty;

				var hpPercent = hpScale * 100f;
				var resourcePercent = resourceScale * 100f;

				switch (_pluginConfiguration.ShowNumbers)
				{
					case true when _pluginConfiguration.ShowPercentage:
						hpText = $"{actor.CurrentHp}\n({hpPercent:F0}%%)";
						resourceText = $"{resourceValue}\n({resourcePercent:F0}%%)";
						break;
					case true:
						hpText = $"{actor.CurrentHp}";
						resourceText = $"{resourceValue}";
						break;
					default:
						hpText = $"({hpPercent:F0}%%)";
						resourceText = $"({resourcePercent:F0}%%)";
						break;
				}

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
					resourceTextPos = new Vector2(
						cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
						ImGui.GetCursorPosY());
				}

				DrawOutlineText(hpTextPos.X, hpTextPos.Y, _pluginConfiguration.HpColorAlpha,
					_pluginConfiguration.OutlineColorAlpha, hpText, 2);

				DrawOutlineText(resourceTextPos.X, resourceTextPos.Y, resourceColor,
					_pluginConfiguration.OutlineColorAlpha, resourceText, 2);
			}

			ImGui.End();
		}
	}
}