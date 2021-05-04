using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Dalamud.Plugin;
using ImGuiNET;
using ImGuiScene;

namespace SideHUDPlugin.Interface
{
	public class ConfigurationWindow
	{
		public bool IsVisible = false;
		private bool _showStyles = false;
		private bool _showUserPath = false;

		private byte[] pathBuffer = new byte[512];

		private readonly Plugin _plugin;
		private readonly DalamudPluginInterface _pluginInterface;
		private readonly PluginConfiguration _pluginConfiguration;

		public ConfigurationWindow(Plugin plugin, DalamudPluginInterface pluginInterface, PluginConfiguration pluginConfiguration)
		{
			_plugin = plugin;
			_pluginInterface = pluginInterface;
			_pluginConfiguration = pluginConfiguration;

			Array.Copy(pathBuffer, Encoding.UTF8.GetBytes(_pluginConfiguration.UserStylePath), _pluginConfiguration.UserStylePath.Length);
		}

		public void Draw()
		{
			if (!IsVisible)
			{
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);

			if (ImGui.Begin("Side HUD configuration", ref IsVisible, ImGuiWindowFlags.NoCollapse))
			{
				var changed = false;

				changed |= ImGui.Checkbox("Hide HUD", ref _pluginConfiguration.HideHud);
				changed |= ImGui.Checkbox("Show percentages", ref _pluginConfiguration.ShowPercentage);
				changed |= ImGui.Checkbox("Enable slidecast", ref _pluginConfiguration.ShowSlidecast);
				changed |= ImGui.Checkbox("Only show in combat", ref _pluginConfiguration.HideCombat);
				changed |= ImGui.Checkbox("Make cast times count up", ref _pluginConfiguration.CastTimeUp);
				changed |= ImGui.Checkbox("Flip HP and resource bars", ref _pluginConfiguration.FlipBars);
				changed |= ImGui.Checkbox("Display cast bar on left bar", ref _pluginConfiguration.FlipCastBar);
				changed |= ImGui.SliderFloat("Gap", ref _pluginConfiguration.BarGap, 0f, 300f, "%.2f",
					ImGuiSliderFlags.AlwaysClamp);
				changed |= ImGui.SliderFloat("Scale", ref _pluginConfiguration.Scale, 0.25f, 2f, "%.2f",
					ImGuiSliderFlags.AlwaysClamp);
				changed |= ImGui.SliderFloat2("Offset", ref _pluginConfiguration.Offset, -200f, 200f, "%.2f",
					ImGuiSliderFlags.AlwaysClamp);
				changed |= ImGui.SliderFloat("Slidecast time", ref _pluginConfiguration.SlidecastTime, 250f, 1000f, "%.0f ms",
					ImGuiSliderFlags.AlwaysClamp);
				changed |= ImGui.SliderFloat("Font Scale", ref _pluginConfiguration.FontScale, 0.25f, 2f, "%.2f",
					ImGuiSliderFlags.AlwaysClamp);
				changed |= ImGui.SliderFloat("Transparency", ref _pluginConfiguration.Transparency, 0f, 100f, "%.0f%%",
					ImGuiSliderFlags.AlwaysClamp);
				changed |= ImGui.ColorEdit3("Background Color", ref _pluginConfiguration.BgColor);
				changed |= ImGui.ColorEdit3("HP Color", ref _pluginConfiguration.HpColor);
				changed |= ImGui.ColorEdit3("MP Color", ref _pluginConfiguration.MpColor);
				changed |= ImGui.ColorEdit3("CP Color", ref _pluginConfiguration.CpColor);
				changed |= ImGui.ColorEdit3("GP Color", ref _pluginConfiguration.GpColor);
				changed |= ImGui.ColorEdit3("Cast Color", ref _pluginConfiguration.CastColor);
				changed |= ImGui.ColorEdit3("Shield Color", ref _pluginConfiguration.ShieldColor);
				changed |= ImGui.ColorEdit3("Slidecast Color", ref _pluginConfiguration.SlidecastColor);
				changed |= ImGui.ColorEdit3("Cast Interrupted Color", ref _pluginConfiguration.CastInterruptColor);
				changed |= ImGui.ColorEdit3("Text Outline Color", ref _pluginConfiguration.OutlineColor);

				if (ImGui.Button("Change style"))
				{
					_showStyles = !_showStyles;
				}

				ImGui.SameLine();

				if (ImGui.Button("Set user style path"))
				{
					_showUserPath = !_showUserPath;
				}

				ImGui.SameLine();

				if (ImGui.Button("Reload user styles"))
				{
					_showStyles = false;
					_showUserPath = false;
					_plugin.ReloadUserStyles();
				}

				if (changed)
				{
					_pluginConfiguration.Save();
				}

				ImGui.End();

				if (_showUserPath)
				{
					ImGui.SetNextWindowSize(new Vector2(300, 0), ImGuiCond.Always);
					if (ImGui.Begin("User style path", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
					{
						ImGui.InputText("##userStylePath", pathBuffer, 512);

						if (ImGui.Button("Save"))
						{
							_pluginConfiguration.UserStylePath = Encoding.UTF8.GetString(pathBuffer).Replace("\0", "");
							_pluginConfiguration.Save();
							_showUserPath = false;
						}

						ImGui.End();
					}
				}

				if (_showStyles)
				{
					ImGui.SetNextWindowSize(new Vector2(450, 400), ImGuiCond.Always);
					if (ImGui.Begin("Style selector", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
					{

						ImGui.Text("Built-in styles");
						ImGui.Separator();

						ImGui.Columns(3);

						void DrawPreview(KeyValuePair<string, TextureWrap[]> entry, bool isUserStyle)
						{
							var item = entry.Key;
							var images = entry.Value;

							if (ImGui.Selectable(isUserStyle ? Path.GetFileName(item) : item))
							{
								_pluginConfiguration.IsUserStyle = isUserStyle;
								_pluginConfiguration.SelectedStyle = item;
								_pluginConfiguration.Save();
								_showStyles = false;

								if (isUserStyle)
								{
									_pluginConfiguration.BarImage =
										_plugin.UserStyles[_pluginConfiguration.SelectedStyle][0];
									_pluginConfiguration.BarBackgroundImage =
										_plugin.UserStyles[_pluginConfiguration.SelectedStyle][1];
									_pluginConfiguration.BarCastImage =
										_plugin.UserStyles[_pluginConfiguration.SelectedStyle][2];
									_pluginConfiguration.BarCastBackgroundImage =
										_plugin.UserStyles[_pluginConfiguration.SelectedStyle][3];
								}
								else
								{
									_pluginConfiguration.BarImage =
										_plugin.Styles[_pluginConfiguration.SelectedStyle][0];
									_pluginConfiguration.BarBackgroundImage =
										_plugin.Styles[_pluginConfiguration.SelectedStyle][1];
									_pluginConfiguration.BarCastImage =
										_plugin.Styles[_pluginConfiguration.SelectedStyle][2];
									_pluginConfiguration.BarCastBackgroundImage =
										_plugin.Styles[_pluginConfiguration.SelectedStyle][3];
								}
							}

							// Limit width and height to a maximum of 100 or 300 respectively
							var scale = Math.Min(1f,
								Math.Min(150f / images[0].Width,
									300f / images[0].Height));

							var cursorPos = ImGui.GetCursorPos();

							ImGui.SetCursorPos(cursorPos);
							ImGui.Image(images[1].ImGuiHandle,
								new Vector2(images[1].Width * scale, images[1].Height * scale),
								Vector2.Zero, Vector2.One, _pluginConfiguration.BgColorAlpha);

							ImGui.SetCursorPos(new Vector2(cursorPos.X,
								cursorPos.Y + images[0].Height * 0.25f * scale));
							ImGui.Image(images[0].ImGuiHandle,
								new Vector2(images[0].Width * scale, images[0].Height * scale * 0.75f),
								new Vector2(0f, 0.25f), Vector2.One, _pluginConfiguration.MpColorAlpha);

							ImGui.SetCursorPos(cursorPos);
							ImGui.Image(images[3].ImGuiHandle,
								new Vector2(images[3].Width * scale, images[3].Height * scale), Vector2.Zero,
								Vector2.One, _pluginConfiguration.BgColorAlpha);

							ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + images[2].Height * 0.5f * scale));
							ImGui.Image(images[2].ImGuiHandle,
								new Vector2(images[2].Width * scale, images[2].Height * scale * 0.5f),
								new Vector2(0f, 0.5f), Vector2.One, _pluginConfiguration.CastColorAlpha);

							ImGui.NextColumn();
						}

						foreach (var entry in _plugin.Styles)
						{
							DrawPreview(entry, false);
						}

						ImGui.Separator();

						if (_plugin.UserStyles.Count > 0)
						{
							ImGui.Columns(1);
							ImGui.Text("User styles");
							ImGui.Separator();
							ImGui.Columns(3);

							foreach (var entry in _plugin.UserStyles)
							{
								DrawPreview(entry, true);
							}
						}

						ImGui.End();
					}
				}
			}
		}
	}
}