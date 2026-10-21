using System;
using System.Globalization;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Semantic colour slots. Widgets ask for "a raised surface" or "muted text" rather than a
	/// specific colour, so swapping <see cref="Current"/> restyles everything.
	/// </summary>
	internal sealed class Theme
	{
		public string Name;

		// Surfaces, roughly darkest to lightest.

		/// <summary>
		/// Wells: text fields, recessed areas.
		/// </summary>
		public Color Inset;
		/// <summary>
		/// Toggle, slider and scrollbar tracks.
		/// </summary>
		public Color Track;
		/// <summary>
		/// Main window body.
		/// </summary>
		public Color Window;
		/// <summary>
		/// Sidebars and side panels.
		/// </summary>
		public Color Panel;
		/// <summary>
		/// Headers and footers.
		/// </summary>
		public Color Bar;
		/// <summary>
		/// Buttons and keys.
		/// </summary>
		public Color Raised;
		/// <summary>
		/// Generic hover fill.
		/// </summary>
		public Color Hover;
		/// <summary>
		/// Selected row or tab fill.
		/// </summary>
		public Color Selected;

		// Lines.
		public Color WindowBorder;
		public Color Divider;
		public Color Border;
		public Color BorderHover;
		public Color SelectedBorder;

		// Text.
		public Color Text;
		public Color TextSoft;
		public Color TextMuted;

		// Accent family.
		public ShadePalette Accent;

		// Status.
		public ShadePalette Good;
		public ShadePalette Warning;
		public ShadePalette Bad;
		public ShadePalette Info;

		// Depth.
		public Color Shadow;
		public Color Highlight;

		private static Theme _current;

		/// <summary>The theme widgets draw with. Assign a new one to restyle the UI.</summary>
		public static Theme Current
		{
			get { return _current ??= Dark(); }
			set { if (value != null) _current = value; }
		}

		/// <summary>A copy to tweak into a variant: <c>var t = Theme.Dark().Clone("Mine"); t.Accent = ...</c></summary>
		public Theme Clone(string name)
		{
			Theme copy = (Theme)MemberwiseClone();
			copy.Name = name;
			return copy;
		}

		/// <summary>Default dark preset.</summary>
		public static Theme Dark()
		{
			return new Theme
			{
				Name = "Dark",

				Inset = Hex("0c0d12"),
				Track = Hex("0f1015"),
				Window = Hex("14161d"),
				Panel = Hex("171921"),
				Bar = Hex("1a1c24"),
				Raised = Hex("242733"),
				Hover = Hex("262a36"),
				Selected = Hex("2b2745"),

				WindowBorder = Hex("2b2e3a"),
				Divider = Hex("262935"),
				Border = Hex("363a49"),
				BorderHover = Hex("5c6277"),
				SelectedBorder = Hex("5b4bb3"),

				Text = Hex("eceef4"),
				TextSoft = Hex("c5c9d6"),
				TextMuted = Hex("8a90a3"),

				Accent = new ShadePalette(Hex("500084")),

				Good = new ShadePalette(Hex("3fb96b")),
				Warning = new ShadePalette(Hex("e5a93a")),
				Bad = new ShadePalette(Hex("e5575a")),
				Info = new ShadePalette(Hex("4da3ff")),

				Shadow = Hex("05060a"),
				Highlight = new Color(1f, 1f, 1f, 0.07f),
			};
		}

		/// <summary>
		/// Parse "rgb", "rrggbb" or "rrggbbaa", with an optional leading '#'.
		/// </summary>
		public static Color Hex(string hex)
		{
			if (string.IsNullOrEmpty(hex))
				throw new ArgumentException("Colour string is empty.", nameof(hex));

			string h = hex.TrimStart('#');
			if (h.Length == 3)
				h = string.Concat(h[0], h[0], h[1], h[1], h[2], h[2]);
			if (h.Length != 6 && h.Length != 8)
				throw new FormatException($"Invalid colour '{hex}', expected rgb, rrggbb or rrggbbaa.");

			uint v = uint.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			float a = 1f;
			if (h.Length == 8)
			{
				a = (v & 0xFF) / 255f;
				v >>= 8;
			}

			return new Color(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, a);
		}

		/// <summary>Blend a surface toward a hue.</summary>
		public static Color Tint(Color surface, Color hue, float amount) => Color.Lerp(surface, hue, amount);

		/// <summary>Same colour with a different alpha.</summary>
		public static Color WithAlpha(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);
	}
}
