using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Body and heading fonts. A mod can supply its own Font objects (e.g. loaded from an
	/// asset bundle), or ask for OS fonts by name. With no configuration it falls back to the
	/// first installed font from a cross-platform list.
	/// </summary>
	internal static class Fonts
	{
		/// <summary>Raised whenever a font changes so cached text styles can be rebuilt.</summary>
		public static event Action Changed;

		private static readonly string[] _fallbackNames =
		{
			"Segoe UI", "Inter", "Roboto", "Noto Sans", "DejaVu Sans",
			"Liberation Sans", "Helvetica Neue", "Arial",
		};

		private static Font _body;
		private static Font _heading;

		/// <summary>Font used for normal text.</summary>
		public static Font Body
		{
			get
			{
				if (_body == null)
					_body = FromOs(_fallbackNames);
				return _body;
			}
		}

		/// <summary>Font used for headings. Falls back to <see cref="Body"/> if none is set.</summary>
		public static Font Heading => _heading != null ? _heading : Body;

		/// <summary>Use fonts supplied by the mod.</summary>
		public static void Use(Font body, Font heading = null)
		{
			_body = body;
			_heading = heading;
			Changed?.Invoke();
		}

		/// <summary>
		/// Use OS fonts by name, in order of preference. Names that aren't installed are skipped
		/// and the built-in fallback list is appended, so this is safe on any platform.
		/// </summary>
		public static void UseOs(string[] bodyNames, string[] headingNames = null)
		{
			_body = FromOs(bodyNames);
			_heading = headingNames != null ? FromOs(headingNames) : null;
			Changed?.Invoke();
		}

		/// <summary>Go back to the default font selection.</summary>
		public static void Reset()
		{
			_body = null;
			_heading = null;
			Changed?.Invoke();
		}

		private static Font FromOs(string[] preferred)
		{
			HashSet<string> installed = new HashSet<string>(
				Font.GetOSInstalledFontNames() ?? new string[0],
				StringComparer.OrdinalIgnoreCase);

			string[] picks = (preferred ?? new string[0])
				.Concat(_fallbackNames)
				.Where(installed.Contains)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			// Nothing from our lists is installed, let Unity choose its own default.
			if (picks.Length == 0)
				picks = new[] { "Arial" };

			return Font.CreateDynamicFontFromOSFont(picks, 16);
		}
	}
}
