using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Structs;
using Dalamud.Game.Command;
using Dalamud.Game.Internal.Gui.Structs;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using SideHUDPlugin.GameStructs;
using SideHUDPlugin.Interface;

namespace SideHUDPlugin
{
	public class Plugin : IDalamudPlugin
	{
		public string Name => "Side HUD";

		private DalamudPluginInterface _pluginInterface;
		private PluginConfiguration _pluginConfiguration;
		private HudWindow _hudWindowWindow;
		private ConfigurationWindow _configurationWindow;
		
		public readonly Dictionary<string, TextureWrap[]> Styles = new Dictionary<string, TextureWrap[]>();
		public readonly Dictionary<string, TextureWrap[]> UserStyles = new Dictionary<string, TextureWrap[]>();

		public string AssemblyLocation { get; set; } = Assembly.GetExecutingAssembly().Location;

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			_pluginInterface = pluginInterface;
			_pluginConfiguration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();

			_pluginConfiguration.Init(_pluginInterface);

			_hudWindowWindow = new HudWindow(_pluginInterface, _pluginConfiguration);
			_configurationWindow = new ConfigurationWindow(this, _pluginInterface, _pluginConfiguration);

			var items = new[]
			{
				"CleanCurves",
				"GlowArc",
				"RivetBar"
			};

			foreach (var item in items)
			{
				LoadStyle(item, $@"{Path.GetDirectoryName(AssemblyLocation)}\Styles", false);
			}

			if (!string.IsNullOrEmpty(_pluginConfiguration.UserStylePath))
			{
				if (!Directory.Exists(_pluginConfiguration.UserStylePath))
				{
					PluginLog.Error($"{_pluginConfiguration.UserStylePath} was not found.");
				}
				else
				{
					foreach (var item in Directory.GetDirectories(_pluginConfiguration.UserStylePath))
					{
						LoadStyle(item, _pluginConfiguration.UserStylePath, true);
					}
				}
			}

			if (_pluginConfiguration.IsUserStyle && !UserStyles.ContainsKey(_pluginConfiguration.SelectedStyle))
			{
				_pluginConfiguration.IsUserStyle = false;
				_pluginConfiguration.SelectedStyle = items[0];
				_pluginConfiguration.Save();
			}

			if (_pluginConfiguration.IsUserStyle)
			{
				_pluginConfiguration.BarImage = UserStyles[_pluginConfiguration.SelectedStyle][0];
				_pluginConfiguration.BarBackgroundImage =
					UserStyles[_pluginConfiguration.SelectedStyle][1];
				_pluginConfiguration.BarCastImage = UserStyles[_pluginConfiguration.SelectedStyle][2];
				_pluginConfiguration.BarCastBackgroundImage =
					UserStyles[_pluginConfiguration.SelectedStyle][3];
			}
			else
			{
				_pluginConfiguration.BarImage = Styles[_pluginConfiguration.SelectedStyle][0];
				_pluginConfiguration.BarBackgroundImage =
					Styles[_pluginConfiguration.SelectedStyle][1];
				_pluginConfiguration.BarCastImage = Styles[_pluginConfiguration.SelectedStyle][2];
				_pluginConfiguration.BarCastBackgroundImage =
					Styles[_pluginConfiguration.SelectedStyle][3];
			}

			_pluginInterface.CommandManager.AddHandler("/pside", new CommandInfo(PluginCommand)
			{
				HelpMessage = "Opens configuration window",
				ShowInHelp = true
			});

			_pluginInterface.UiBuilder.OnBuildUi += UiBuilderOnOnBuildUi;
			_pluginInterface.UiBuilder.OnOpenConfigUi += UiBuilderOnOnOpenConfigUi;
		}

		public void LoadStyle(string name, string path, bool isUserStyle)
		{
			var fileNames = new[]
			{
				"Bar.png",
				"BarBG.png",
				"Cast.png",
				"CastBG.png"
			};

			if (isUserStyle ? UserStyles.ContainsKey(name) : Styles.ContainsKey(name))
			{
				return;
			}

			var images = new TextureWrap[4];
			
			for (var i = 0; i != fileNames.Length; ++i)
			{
				var filePath = Path.Combine(path, $@"{name}\{fileNames[i]}");
				
				if (!File.Exists(filePath))
				{
					foreach (var image in images)
					{
						image?.Dispose();
					}
					
					PluginLog.Error($"{filePath} was not found.");
					return;
				}
				
				images[i] = _pluginInterface.UiBuilder.LoadImage(filePath);

				if (images[i] != null)
				{
					continue;
				}

				foreach (var image in images)
				{
					image?.Dispose();
				}
				
				PluginLog.Error($"Failed to load {filePath}.");
				return;
			}

			if (isUserStyle)
			{
				UserStyles[name] = images;
			}
			else
			{
				Styles[name] = images;
			}
		}

		public void ReloadUserStyles()
		{
			foreach (var image in UserStyles.SelectMany(style => style.Value))
			{
				image?.Dispose();
			}
			
			UserStyles.Clear();
			
			if (!string.IsNullOrEmpty(_pluginConfiguration.UserStylePath))
			{
				if (!Directory.Exists(_pluginConfiguration.UserStylePath))
				{
					PluginLog.Error($"{_pluginConfiguration.UserStylePath} was not found.");
				}
				else
				{
					foreach (var item in Directory.GetDirectories(_pluginConfiguration.UserStylePath))
					{
						LoadStyle(item, _pluginConfiguration.UserStylePath, true);
					}
				}
			}
		}

		private void PluginCommand(string command, string arguments)
		{
			_configurationWindow.IsVisible = !_configurationWindow.IsVisible;
		}
		
		private void UiBuilderOnOnBuildUi()
		{
			_configurationWindow.Draw();
			_hudWindowWindow.Draw();
		}
		
		private void UiBuilderOnOnOpenConfigUi(object sender, EventArgs e)
		{
			_configurationWindow.IsVisible = !_configurationWindow.IsVisible;
		}


		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_configurationWindow.IsVisible = false;
			_hudWindowWindow.IsVisible = false;
			
			foreach (var item in Styles.SelectMany(style => style.Value))
			{
				item?.Dispose();
			}
			
			foreach (var item in UserStyles.SelectMany(style => style.Value))
			{
				item?.Dispose();
			}
			
			_pluginInterface.CommandManager.RemoveHandler("/pside");
			_pluginInterface.UiBuilder.OnBuildUi -= UiBuilderOnOnBuildUi;
			_pluginInterface.UiBuilder.OnOpenConfigUi -= UiBuilderOnOnOpenConfigUi;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}