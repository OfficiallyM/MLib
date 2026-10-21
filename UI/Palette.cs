using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Named colours.
	/// </summary>
	internal static class Palette
	{
		// Neutrals.
		public static readonly Color Black = new Color(0f, 0f, 0f);
		public static readonly Color White = new Color(1f, 1f, 1f);
		public static readonly Color LightGrey = new Color(0.7f, 0.7f, 0.7f);
		public static readonly Color Grey = new Color(0.4f, 0.4f, 0.4f);
		public static readonly Color DarkGrey = new Color(0.15f, 0.15f, 0.15f);

		// Reds.
		public static readonly Color LightRed = new Color(1f, 0.4f, 0.38f);
		public static readonly Color Red = new Color(0.91f, 0.14f, 0.12f);
		public static readonly Color DarkRed = new Color(0.55f, 0.1f, 0.1f);
		public static readonly Color Crimson = new Color(0.76f, 0.08f, 0.24f);
		public static readonly Color Maroon = new Color(0.5f, 0.09f, 0.09f);

		// Oranges.
		public static readonly Color LightOrange = new Color(1f, 0.72f, 0.35f);
		public static readonly Color Orange = new Color(0.91f, 0.55f, 0.12f);
		public static readonly Color DarkOrange = new Color(0.7f, 0.4f, 0.05f);
		public static readonly Color Brown = new Color(0.4f, 0.26f, 0.13f);

		// Yellows.
		public static readonly Color LightYellow = new Color(1f, 0.96f, 0.55f);
		public static readonly Color Yellow = new Color(0.9f, 0.82f, 0.15f);
		public static readonly Color DarkYellow = new Color(0.65f, 0.58f, 0.05f);
		public static readonly Color Gold = new Color(0.78f, 0.62f, 0.1f);

		// Greens.
		public static readonly Color LightGreen = new Color(0.55f, 0.9f, 0.4f);
		public static readonly Color Green = new Color(0f, 0.69f, 0.008f);
		public static readonly Color DarkGreen = new Color(0f, 0.4f, 0.01f);
		public static readonly Color Olive = new Color(0.42f, 0.48f, 0.12f);
		public static readonly Color Lime = new Color(0.6f, 0.87f, 0.13f);
		public static readonly Color Emerald = new Color(0.06f, 0.47f, 0.29f);

		// Cyans and teals.
		public static readonly Color LightCyan = new Color(0.6f, 0.95f, 0.97f);
		public static readonly Color Cyan = new Color(0.1f, 0.8f, 0.85f);
		public static readonly Color DarkCyan = new Color(0.05f, 0.5f, 0.55f);
		public static readonly Color Teal = new Color(0.06f, 0.5f, 0.44f);

		// Blues.
		public static readonly Color LightBlue = new Color(0.55f, 0.75f, 1f);
		public static readonly Color Blue = new Color(0.161f, 0.463f, 0.859f);
		public static readonly Color DarkBlue = new Color(0.08f, 0.2f, 0.5f);
		public static readonly Color SkyBlue = new Color(0.27f, 0.65f, 0.87f);
		public static readonly Color Navy = new Color(0.06f, 0.13f, 0.35f);

		// Purples.
		public static readonly Color LightPurple = new Color(0.78f, 0.6f, 0.92f);
		public static readonly Color Purple = new Color(0.5f, 0.15f, 0.65f);
		public static readonly Color DarkPurple = new Color(0.3f, 0.08f, 0.4f);
		public static readonly Color Violet = new Color(0.6f, 0.4f, 0.85f);
		public static readonly Color Indigo = new Color(0.29f, 0.1f, 0.55f);

		// Pinks and magentas.
		public static readonly Color LightPink = new Color(1f, 0.78f, 0.87f);
		public static readonly Color Pink = new Color(0.9f, 0.5f, 0.65f);
		public static readonly Color DarkPink = new Color(0.7f, 0.25f, 0.4f);
		public static readonly Color HotPink = new Color(0.95f, 0.2f, 0.55f);
		public static readonly Color Magenta = new Color(0.85f, 0.1f, 0.6f);
		public static readonly Color Rose = new Color(0.85f, 0.35f, 0.45f);

		/// <summary>The same colour at 70% opacity.</summary>
		public static Color Translucent(Color c) => new Color(c.r, c.g, c.b, 0.7f);
	}
}
