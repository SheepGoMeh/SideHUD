using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiScene;
using Newtonsoft.Json;

namespace SideHUDPlugin
{
	public class PluginConfiguration : IPluginConfiguration
	{
		public int Version { get; set; }

		[JsonIgnore] public Vector4 BgColorAlpha => new Vector4(BgColor, Transparency / 100f);
		[JsonIgnore] public Vector4 HpColorAlpha => new Vector4(HpColor, Transparency / 100f);
		[JsonIgnore] public Vector4 MpColorAlpha => new Vector4(MpColor, Transparency / 100f);
		[JsonIgnore] public Vector4 CpColorAlpha => new Vector4(CpColor, Transparency / 100f);
		[JsonIgnore] public Vector4 GpColorAlpha => new Vector4(GpColor, Transparency / 100f);
		[JsonIgnore] public Vector4 CastColorAlpha => new Vector4(CastColor, Transparency / 100f);
		[JsonIgnore] public Vector4 ShieldColorAlpha => new Vector4(ShieldColor, Transparency / 100f);
		[JsonIgnore] public Vector4 HpTextColorAlpha => new Vector4(HpTextColor, Transparency / 100f);
		[JsonIgnore] public Vector4 ResourceTextColorAlpha => new Vector4(ResourceTextColor, Transparency / 100f);
		[JsonIgnore] public Vector4 CastTextColorAlpha => new Vector4(CastTextColor, Transparency / 100f);
		[JsonIgnore] public Vector4 CastInterruptedTextColorAlpha => new Vector4(CastInterruptedTextColor, Transparency / 100f);
		[JsonIgnore] public Vector4 HpTextOutlineColorAlpha => new Vector4(HpTextOutlineColor, Transparency / 100f);
		[JsonIgnore] public Vector4 ResourceTextOutlineColorAlpha => new Vector4(ResourceTextOutlineColor, Transparency / 100f);
		[JsonIgnore] public Vector4 CastTextOutlineColorAlpha => new Vector4(CastTextOutlineColor, Transparency / 100f);
		[JsonIgnore] public Vector4 CastInterruptedTextOutlineColorAlpha => new Vector4(CastInterruptedTextOutlineColor, Transparency / 100f);
		[JsonIgnore] public Vector4 SlidecastColorAlpha => new Vector4(SlidecastColor, Transparency / 100f);
		[JsonIgnore] public Vector4 CastInterruptColorAlpha => new Vector4(CastInterruptColor, Transparency / 100f);

		public bool HideHud = false;
		public bool FlipBars = false;
		public bool HideCombat = false;
		public bool CastTimeUp = false;
		public bool FlipCastBar = false;
		public bool IsUserStyle = false;
		public bool ShowNumbers = true;
		public bool ShowSlidecast = false;
		public bool ShowPercentage = false;
		public float Scale = 1f;
		public float BarGap = 100f;
		public float FontScale = 1f;
		public float Transparency = 100f;
		public float SlidecastTime = 500f;
		public string SelectedStyle = "CleanCurves";
		public string UserStylePath = string.Empty;
		public Vector2 Offset = Vector2.Zero;

		public Vector3 BgColor = Vector3.Zero;
		public Vector3 HpColor = new Vector3(0.258f, 0.478f, 0.082f);
		public Vector3 MpColor = new Vector3(0.705f, 0.172f, 0.4f);
		public Vector3 CpColor = new Vector3(0.466f, 0.215f, 0.592f);
		public Vector3 GpColor = new Vector3(0.172f, 0.443f, 0.584f);
		public Vector3 CastColor = new Vector3(0.878f, 0.847f, 0.796f);
		public Vector3 ShieldColor = new Vector3(0.172f, 0.443f, 0.584f);
		public Vector3 HpTextColor = Vector3.One;
		public Vector3 ResourceTextColor = Vector3.One;
		public Vector3 CastTextColor = Vector3.One;
		public Vector3 CastInterruptedTextColor = Vector3.One;
		public Vector3 HpTextOutlineColor = new Vector3(0.615f, 0.513f, 0.356f);
		public Vector3 ResourceTextOutlineColor = new Vector3(0.615f, 0.513f, 0.356f);
		public Vector3 CastTextOutlineColor = new Vector3(0.615f, 0.513f, 0.356f);
		public Vector3 CastInterruptedTextOutlineColor = new Vector3(0.556f, 0.415f, 0.047f);
		public Vector3 SlidecastColor = new Vector3(0.215f, 0.980f, 0.180f);
		public Vector3 CastInterruptColor = new Vector3(0.215f, 0.980f, 0.180f);

		[JsonIgnore] private DalamudPluginInterface _pluginInterface;

		[JsonIgnore] public TextureWrap BarImage = null;
		[JsonIgnore] public TextureWrap BarBackgroundImage = null;
		[JsonIgnore] public TextureWrap BarCastImage = null;
		[JsonIgnore] public TextureWrap BarCastBackgroundImage = null;

		public void Init(DalamudPluginInterface pluginInterface)
		{
			_pluginInterface = pluginInterface;
		}

		public void Save()
		{
			_pluginInterface.SavePluginConfig(this);
		}
	}
}