using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Colours derived from a base colour.
	/// </summary>
	internal sealed class ShadePalette
	{
		public Color Base;
		public Color Hover;
		public Color Deep;
		public Color Edge;
		public Color Text;
		public Color On;
		public Color OnMuted;

		public ShadePalette(Color basis)
		{
			Color on = Shade.TextOn(basis);

			Base = basis;

			// Interactive states.
			Hover = Shade.Lighten(basis, 0.10f);
			Deep = Shade.Darken(basis, 0.07f);
			Edge = Shade.Darken(basis, 0.15f);

			// Readable accent-coloured text on dark surfaces.
			Text = Shade.WithLightness(basis, 0.72f);

			// Text on the base colour.
			On = on;
			OnMuted = Color.Lerp(on, basis, 0.16f);
		}
	}
}
