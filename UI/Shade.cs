using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Colour stepping in HSL space. "Lighten by 0.08" moves lightness 8 percentage points while
	/// keeping hue and saturation, which is how the theme derives hover, pressed and border colours
	/// from a handful of core colours.
	/// </summary>
	internal static class Shade
	{
		public static void ToHsl(Color c, out float h, out float s, out float l)
		{
			float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
			float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
			float d = max - min;
			l = (max + min) * 0.5f;

			if (d < 1e-5f)
			{
				h = 0f;
				s = 0f;
				return;
			}

			s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

			if (max == c.r)
				h = (c.g - c.b) / d + (c.g < c.b ? 6f : 0f);
			else if (max == c.g)
				h = (c.b - c.r) / d + 2f;
			else
				h = (c.r - c.g) / d + 4f;

			h /= 6f;
		}

		public static Color FromHsl(float h, float s, float l, float a = 1f)
		{
			if (s < 1e-5f)
				return new Color(l, l, l, a);

			float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
			float p = 2f * l - q;
			return new Color(
				HueToChannel(p, q, h + 1f / 3f),
				HueToChannel(p, q, h),
				HueToChannel(p, q, h - 1f / 3f),
				a);
		}

		private static float HueToChannel(float p, float q, float t)
		{
			if (t < 0f) t += 1f;
			if (t > 1f) t -= 1f;
			if (t < 1f / 6f) return p + (q - p) * 6f * t;
			if (t < 0.5f) return q;
			if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
			return p;
		}

		/// <summary>Move lightness up (positive) or down (negative) by a fraction of the full range.</summary>
		public static Color Lighten(Color c, float amount)
		{
			float h, s, l;
			ToHsl(c, out h, out s, out l);
			return FromHsl(h, s, Mathf.Clamp01(l + amount), c.a);
		}

		public static Color Darken(Color c, float amount) => Lighten(c, -amount);

		/// <summary>Same hue and saturation at an exact lightness.</summary>
		public static Color WithLightness(Color c, float lightness)
		{
			float h, s, l;
			ToHsl(c, out h, out s, out l);
			return FromHsl(h, s, Mathf.Clamp01(lightness), c.a);
		}

		/// <summary>Add to (or subtract from) saturation.</summary>
		public static Color Saturate(Color c, float amount)
		{
			float h, s, l;
			ToHsl(c, out h, out s, out l);
			return FromHsl(h, Mathf.Clamp01(s + amount), l, c.a);
		}

		/// <summary>
		/// Black or white, whichever reads better on the given background.
		/// Uses perceived luminance (the eye favours green) on 0-1 channels.
		/// </summary>
		public static Color TextOn(Color background)
		{
			float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
			return luminance > 0.5f ? Color.black : Color.white;
		}
	}
}
