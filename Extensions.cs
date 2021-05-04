using System;
using System.Numerics;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace SideHUDPlugin
{
	public static class Extensions
	{
		public static unsafe string GetString(this Utf8String utf8String) {
			var s = utf8String.BufUsed > int.MaxValue ? int.MaxValue : (int) utf8String.BufUsed;
			try {
				return s <= 1 ? string.Empty : Encoding.UTF8.GetString(utf8String.StringPtr, s - 1);
			} catch (Exception ex) {
				return $"<<{ex.Message}>>";
			}
		}
	}
}