using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SideHUDPlugin.GameStructs;
using SideHUDPlugin.ImGuiExtension;

namespace SideHUDPlugin.Interface
{
	public class HudWindow
	{
		public bool IsVisible = true;
		private readonly PluginConfiguration _pluginConfiguration;

		public HudWindow(PluginConfiguration pluginConfiguration)
		{
			_pluginConfiguration = pluginConfiguration;
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

		private void DrawTargetInfo(BattleChara actor, Vector2 cursorPos, float castScale, float offsetX, string actionName,
			string interruptedText, bool drawCast, bool castInterrupted, bool castInterruptible, bool isRight)
		{
			var hpScale = (float) actor.CurrentHp / actor.MaxHp * 100f;
			DrawBar(cursorPos, offsetX, hpScale / 100f,
				_pluginConfiguration.HpColorAlpha, isRight);
			
			var actorName = $"{actor.Name}\n({hpScale:F2})%";

			var actorNameSize = TextExtension.CalcTextSize(actorName,
				_pluginConfiguration.FontScale * _pluginConfiguration.Scale);

			TextExtension.DrawOutlineText(
				new Vector2(isRight ? cursorPos.X + _pluginConfiguration.BarImage.Width * _pluginConfiguration.Scale + offsetX + actorNameSize.Y : cursorPos.X - offsetX - actorNameSize.Y,
					cursorPos.Y + _pluginConfiguration.BarBackgroundImage.Height / 2f * _pluginConfiguration.Scale -
					actorNameSize.X / 2f),
				_pluginConfiguration.CastTextColorAlpha, _pluginConfiguration.CastTextOutlineColorAlpha,
				actorName, _pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2, true, isRight);

			if (!drawCast)
			{
				return;
			}

			var castStringSize =
				TextExtension.CalcTextSize(actionName, _pluginConfiguration.FontScale * _pluginConfiguration.Scale);

			TextExtension.DrawOutlineText(
				new Vector2(isRight ? cursorPos.X + _pluginConfiguration.BarImage.Width * _pluginConfiguration.Scale + offsetX + actorNameSize.Y + castStringSize.Y: cursorPos.X - offsetX - actorNameSize.Y - castStringSize.Y,
					cursorPos.Y + _pluginConfiguration.BarBackgroundImage.Height / 2f * _pluginConfiguration.Scale -
					castStringSize.X / 2f),
				_pluginConfiguration.CastTextColorAlpha, _pluginConfiguration.CastTextOutlineColorAlpha,
				actionName, _pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2, true, isRight);

			if (castInterrupted)
			{
				var castInterruptedStringSize = TextExtension.CalcTextSize(interruptedText,
					_pluginConfiguration.FontScale * _pluginConfiguration.Scale);

				TextExtension.DrawOutlineText(
					new Vector2(isRight ? cursorPos.X + _pluginConfiguration.BarImage.Width * _pluginConfiguration.Scale + offsetX + actorNameSize.Y + castStringSize.Y + castInterruptedStringSize.Y : cursorPos.X - offsetX - actorNameSize.Y - castStringSize.Y - castInterruptedStringSize.Y,
						cursorPos.Y + _pluginConfiguration.BarBackgroundImage.Height / 2f * _pluginConfiguration.Scale -
						castInterruptedStringSize.X / 2f),
					_pluginConfiguration.CastTextColorAlpha, _pluginConfiguration.CastTextOutlineColorAlpha,
					interruptedText, _pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2, true, isRight);
			}

			DrawCastBar(cursorPos, offsetX, castScale,
				castInterrupted ? _pluginConfiguration.CastInterruptColorAlpha :
				castInterruptible ? _pluginConfiguration.CastInterruptColorAlpha : _pluginConfiguration.CastColorAlpha,
				isRight, true);
		}
		
		public unsafe void Draw()
		{
			if (!IsVisible || _pluginConfiguration.HideHud ||
			    (_pluginConfiguration.HideCombat && !Plugin.Condition[ConditionFlag.InCombat]))
			{
				return;
			}

			var actor = Plugin.ClientState.LocalPlayer;
			var parameterWidget =
				(AtkUnitBase*) Plugin.GameGui.GetAddonByName("_ParameterWidget", 1);
			var fadeMiddleWidget =
				(AtkUnitBase*) Plugin.GameGui.GetAddonByName("FadeMiddle", 1);

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
			uint resourceValue;
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

			//ImGui.SetWindowFontScale(2.4f * _pluginConfiguration.FontScale * _pluginConfiguration.Scale);

			var cursorPos = new Vector2(viewportSize.X / 2f + _pluginConfiguration.Offset.X,
				viewportSize.Y / 2f + _pluginConfiguration.Offset.Y -
				(_pluginConfiguration.BarImage.Height / 2f + 100f) * _pluginConfiguration.Scale);
			var imageWidth = (_pluginConfiguration.BarImage.Width + _pluginConfiguration.BarGap) *
			                 _pluginConfiguration.Scale;

			// ImGui.PushFont(_pluginConfiguration.NumberFont);
			// TextExtension.DrawText($"Font Size: {ImGui.GetFont().FontSize} Font Size Scale: {ImGui.GetFont().FontSize * _pluginConfiguration.FontScale * _pluginConfiguration.Scale}", .33f, cursorPos, _pluginConfiguration.HpTextColorAlpha, true, true);
			// ImGui.PopFont();

			// Left bar
			DrawBar(cursorPos, imageWidth, leftBarScale, leftBarColor, false);

			// Right bar
			DrawBar(cursorPos, _pluginConfiguration.BarGap * _pluginConfiguration.Scale, rightBarScale, rightBarColor,
				true);
			
			if (Plugin.TargetManager.Target is BattleChara { ObjectKind: ObjectKind.Player or ObjectKind.BattleNpc } targetActor)
			{
				var drawCast = false;
				var castScale = 0f;
				var actionName = string.Empty;
				var castInterrupted = false;
				var castInterruptible = false;
				var interruptedText = string.Empty;

				var targetInfoWidget =
					(AtkUnitBase*) Plugin.GameGui.GetAddonByName("_TargetInfo", 1);
				var targetInfoCastBarWidget =
					(AtkUnitBase*) Plugin.GameGui.GetAddonByName("_TargetInfoCastBar", 1);

				if (targetInfoWidget != null && targetInfoWidget->IsVisible && targetInfoWidget->UldManager.NodeList != null)
				{
					for (var i = 0; i != targetInfoWidget->UldManager.NodeListCount; ++i)
					{
						var node = targetInfoWidget->UldManager.NodeList[i];

						switch (node->NodeID)
						{
							case 11: // Interrupted text
								castInterrupted = node->IsVisible;
								interruptedText = ((AtkTextNode*) node)->NodeText.GetString();
								break;
							case 12: // Action name
								drawCast = node->IsVisible;
								actionName = ((AtkTextNode*) node)->NodeText.GetString();
								break;
							case 13: // Action cast image (Used for scale)
								drawCast = node->IsVisible;
								castScale = node->ScaleX;
								break;
							case 14: // Interruptible cast image
								castInterruptible = node->IsVisible;
								break;
						}
					}
				}
				
				if (targetInfoCastBarWidget != null && targetInfoCastBarWidget->IsVisible && targetInfoCastBarWidget->UldManager.NodeList != null)
				{
					for (var i = 0; i != targetInfoCastBarWidget->UldManager.NodeListCount; ++i)
					{
						var node = targetInfoCastBarWidget->UldManager.NodeList[i];

						switch (node->NodeID)
						{
							case 3: // Interrupted text
								castInterrupted = node->IsVisible;
								interruptedText = ((AtkTextNode*) node)->NodeText.GetString();
								break;
							case 4: // Action name
								drawCast = node->IsVisible;
								actionName = ((AtkTextNode*) node)->NodeText.GetString();
								break;
							case 5: // Action cast image (Used for scale)
								drawCast = node->IsVisible;
								castScale = node->ScaleX;
								break;
							case 6: // Interruptible cast image
								castInterruptible = node->IsVisible;
								break;
						}
					}
				}

				DrawTargetInfo(targetActor, cursorPos, castScale, imageWidth + 50f, actionName, interruptedText,
					drawCast, castInterrupted, castInterruptible, false);
			}
			
			if (Plugin.TargetManager.FocusTarget is BattleChara { ObjectKind: ObjectKind.Player or ObjectKind.BattleNpc } focusTargetActor)
			{
				var drawCast = false;
				var castScale = 0f;
				var actionName = string.Empty;
				var castInterrupted = false;
				var castInterruptible = false;
				var interruptedText = string.Empty;

				var focusTargetInfoWidget =
					(AtkUnitBase*) Plugin.GameGui.GetAddonByName("_FocusTargetInfo", 1);

				if (focusTargetInfoWidget != null && focusTargetInfoWidget->IsVisible && focusTargetInfoWidget->UldManager.NodeList != null)
				{
					for (var i = 0; i != focusTargetInfoWidget->UldManager.NodeListCount; ++i)
					{
						var node = focusTargetInfoWidget->UldManager.NodeList[i];

						switch (node->NodeID)
						{
							case 4: // Interrupted text
								castInterrupted = node->IsVisible;
								interruptedText = ((AtkTextNode*) node)->NodeText.GetString();
								break;
							case 5: // Action name
								drawCast = node->IsVisible;
								actionName = ((AtkTextNode*) node)->NodeText.GetString();
								break;
							case 6: // Action cast image (Used for scale)
								drawCast = node->IsVisible;
								castScale = node->ScaleX;
								break;
							case 7: // Interruptible cast image
								castInterruptible = node->IsVisible;
								break;
						}
					}
				}

				DrawTargetInfo(focusTargetActor, cursorPos, castScale, _pluginConfiguration.BarGap * _pluginConfiguration.Scale + 50f, actionName, interruptedText,
					drawCast, castInterrupted, castInterruptible, true);
			}

			var cursorY = ImGui.GetCursorPosY();
			var shieldScale = Math.Min(*(int*) (actor.Address + 0x1997), 100) / 100f;

			// Shield
			DrawCastBar(cursorPos,
				_pluginConfiguration.FlipCastBar
					? _pluginConfiguration.BarGap * _pluginConfiguration.Scale
					: imageWidth, shieldScale, _pluginConfiguration.ShieldColorAlpha, _pluginConfiguration.FlipCastBar,
				false);
			
			ImGui.PushFont(_pluginConfiguration.NumberFont);

			// Cast bar

			var castBar = (AddonCastBar*) Plugin.GameGui.GetAddonByName("_CastBar", 1);

			if (castBar != null && castBar->AtkUnitBase.UldManager.NodeList != null &&
			    castBar->AtkUnitBase.UldManager.NodeListCount > 11 && castBar->AtkUnitBase.IsVisible)
			{
				var castScale = castBar->CastPercent / 100;
				var castTime = ((_pluginConfiguration.CastTimeUp ? 0 : castBar->CastTime) -
				                castBar->CastTime * castScale) / 100;
				var slideCastScale = _pluginConfiguration.SlidecastTime / 10f / castBar->CastTime;
				var castSign = _pluginConfiguration.CastTimeUp ? "" : "-";
				var castString = $"{castBar->CastName.GetString()}\n{castSign} {Math.Abs(castTime):F}";
				var castStringSize = TextExtension.CalcTextSize(castString, _pluginConfiguration.FontScale * _pluginConfiguration.Scale);

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
					TextExtension.DrawOutlineText(
						new Vector2(
							cursorPos.X - castStringSize.X / 2 -
							_pluginConfiguration.BarGap * _pluginConfiguration.Scale, cursorPos.Y - castStringSize.Y),
						_pluginConfiguration.CastTextColorAlpha, _pluginConfiguration.CastTextOutlineColorAlpha,
						castString, _pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);

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
					TextExtension.DrawOutlineText(
						new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
							cursorPos.Y - castStringSize.Y), _pluginConfiguration.CastTextColorAlpha,
						_pluginConfiguration.CastTextOutlineColorAlpha, castString, _pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);

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
				Vector2 hpPercentTextPos;
				Vector2 resourcePercentTextPos;

				var hpPercent = hpScale * 100f;
				var resourcePercent = resourceScale * 100f;
				
				var hpText = $"{actor.CurrentHp}";
				var resourceText = $"{resourceValue}";
				var hpPercentText = $"({hpPercent:F0}%)";
				var resourcePercentText = $"({resourcePercent:F0}%)";

				var hpTextSize =
					TextExtension.CalcTextSize(hpText, _pluginConfiguration.FontScale * _pluginConfiguration.Scale);
				var hpPercentTextSize =
					TextExtension.CalcTextSize(hpPercentText, _pluginConfiguration.FontScale * _pluginConfiguration.Scale);
				var resourceTextSize =
					TextExtension.CalcTextSize(resourceText, _pluginConfiguration.FontScale * _pluginConfiguration.Scale);
				var resourcePercentTextSize =
					TextExtension.CalcTextSize(resourcePercentText, _pluginConfiguration.FontScale * _pluginConfiguration.Scale);

				if (_pluginConfiguration.FlipBars)
				{
					hpTextPos = new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
						ImGui.GetCursorPosY());
					hpPercentTextPos =
						new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
							ImGui.GetCursorPosY() + hpTextSize.Y);
					resourceTextPos =
						new Vector2(
							cursorPos.X - resourceTextSize.X - _pluginConfiguration.BarGap * _pluginConfiguration.Scale,
							ImGui.GetCursorPosY());
					resourcePercentTextPos =
						new Vector2(
							cursorPos.X - resourcePercentTextSize.X -
							_pluginConfiguration.BarGap * _pluginConfiguration.Scale,
							ImGui.GetCursorPosY() + resourceTextSize.Y);
				}
				else
				{
					hpTextPos = new Vector2(
						cursorPos.X - hpTextSize.X - _pluginConfiguration.BarGap * _pluginConfiguration.Scale, ImGui.GetCursorPosY());
					hpPercentTextPos = new Vector2(
						cursorPos.X - hpPercentTextSize.X - _pluginConfiguration.BarGap * _pluginConfiguration.Scale, ImGui.GetCursorPosY() + hpTextSize.Y);
					resourceTextPos =
						new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale, ImGui.GetCursorPosY());
					resourcePercentTextPos =
						new Vector2(cursorPos.X + _pluginConfiguration.BarGap * _pluginConfiguration.Scale, ImGui.GetCursorPosY() + resourceTextSize.Y);
				}

				switch (_pluginConfiguration.ShowNumbers)
				{
					case true when _pluginConfiguration.ShowPercentage:
						TextExtension.DrawOutlineText(hpTextPos, _pluginConfiguration.HpTextColorAlpha,
							_pluginConfiguration.HpTextOutlineColorAlpha, hpText,
							_pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						TextExtension.DrawOutlineText(hpPercentTextPos, _pluginConfiguration.HpTextColorAlpha,
							_pluginConfiguration.HpTextOutlineColorAlpha, hpPercentText,
							_pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						TextExtension.DrawOutlineText(resourceTextPos, _pluginConfiguration.ResourceTextColorAlpha,
							_pluginConfiguration.ResourceTextOutlineColorAlpha, resourceText,
							_pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						TextExtension.DrawOutlineText(resourcePercentTextPos,
							_pluginConfiguration.ResourceTextColorAlpha,
							_pluginConfiguration.ResourceTextOutlineColorAlpha, resourcePercentText,
							_pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						break;
					case true:
						TextExtension.DrawOutlineText(hpTextPos, _pluginConfiguration.HpTextColorAlpha,
							_pluginConfiguration.HpTextOutlineColorAlpha, hpText,
							_pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						TextExtension.DrawOutlineText(resourceTextPos, _pluginConfiguration.ResourceTextColorAlpha,
							_pluginConfiguration.ResourceTextOutlineColorAlpha, resourceText,
							_pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						break;
					default:
						TextExtension.DrawOutlineText(
							new Vector2(hpPercentTextPos.X, hpPercentTextPos.Y - hpTextSize.Y),
							_pluginConfiguration.HpTextColorAlpha, _pluginConfiguration.HpTextOutlineColorAlpha,
							hpPercentText, _pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						TextExtension.DrawOutlineText(
							new Vector2(resourcePercentTextPos.X, resourcePercentTextPos.Y - resourceTextSize.Y),
							_pluginConfiguration.ResourceTextColorAlpha,
							_pluginConfiguration.ResourceTextOutlineColorAlpha, resourcePercentText,
							_pluginConfiguration.FontScale * _pluginConfiguration.Scale, 2);
						break;
				}

				ImGui.PopFont();
			}

			ImGui.End();
		}
	}
}