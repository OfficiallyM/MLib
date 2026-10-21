using System;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// A layout scope that hands out rects. Widgets are methods on it, e.g. <c>ui.Button("OK")</c>.
	/// Scopes nest (<see cref="Row"/>, <see cref="Columns"/>, <see cref="Panel"/>, <see cref="Scroll"/>)
	/// and are IDisposable so they read naturally with <c>using</c>.
	///
	/// Widths: pass a positive number for an exact width, or one of
	/// <see cref="Fill"/> (all remaining space), <see cref="Auto"/> (fit the content) or
	/// <see cref="Default"/> (what suits the widget in the current layout).
	/// </summary>
	internal sealed partial class Ui : IDisposable
	{
		public const float Default = 0f;
		public const float Fill = -1f;
		public const float Auto = -2f;

		/// <summary>Gap between consecutive widgets.</summary>
		public static float Spacing = 8f;
		/// <summary>Height of buttons, fields and rows.</summary>
		public static float ControlHeight = 34f;
		/// <summary>Window content padding.</summary>
		public static float Padding = 14f;
		/// <summary>Default text size.</summary>
		public static int TextSize = 14;

		private enum Mode { Vertical, Horizontal, Columns }

		private static readonly Rect _everywhere = new Rect(-100000f, -100000f, 200000f, 200000f);

		private readonly Mode _mode;
		private readonly string _scope;
		private readonly Ui _root;
		private Rect _bounds;
		private Rect _clip;
		private float _cursor;
		private float[] _cells;
		private int _cell;
		private int _fieldIndex;
		private Action _onDispose;
		private bool _disposed;

		private Ui(Mode mode, Rect bounds, Rect clip, string scope, Ui parent)
		{
			_mode = mode;
			_bounds = bounds;
			_clip = clip;
			_scope = scope;
			_root = parent != null ? parent._root : this;
		}

		/// <summary>The area this scope lays widgets out in.</summary>
		public Rect Bounds => _bounds;

		/// <summary>
		/// Remaining space: height in a vertical scope, width in a row.
		/// </summary>
		public float Remaining => _mode == Mode.Vertical
			? Mathf.Max(0f, _bounds.height - _cursor)
			: AvailableWidth;

		/// <summary>Height consumed so far by a vertical scope (without the trailing gap).</summary>
		public float UsedHeight => _cursor > 0f ? _cursor - Spacing : 0f;

		private float AvailableWidth => _mode == Mode.Vertical
			? _bounds.width
			: Mathf.Max(0f, _bounds.width - _cursor);

		/// <summary>
		/// Lay widgets out inside an existing rect, for hosting in a tab or panel you already have.
		/// With <paramref name="clip"/> the content is clipped to the rect.
		/// </summary>
		/// <param name="scope">Prefix for text field control names. Use a distinct one per area.</param>
		public static Ui Area(Rect area, string scope = "ui", bool clip = false)
		{
			if (!clip)
				return new Ui(Mode.Vertical, area, _everywhere, scope, null);

			GUI.BeginGroup(area);
			Rect local = new Rect(0f, 0f, area.width, area.height);
			Ui ui = new Ui(Mode.Vertical, local, local, scope, null);
			ui._onDispose = GUI.EndGroup;
			return ui;
		}

		/// <summary>A horizontal row. Widgets inside are placed left to right.</summary>
		public Ui Row(float height = Default)
		{
			float h = height == Default ? ControlHeight : ResolveHeight(height);
			Rect r = Place(Fill, -1f, true, h);
			return new Ui(Mode.Horizontal, r, _clip, _scope, this);
		}

		/// <summary>
		/// A row split into cells sized by weight. <c>Columns(34, 1, 2)</c> gives a third and two
		/// thirds. Each widget added takes the next cell and its width argument is ignored.
		/// </summary>
		public Ui Columns(float height, params float[] weights)
		{
			if (weights == null || weights.Length == 0)
				weights = new[] { 1f };

			float h = height == Default ? ControlHeight : ResolveHeight(height);
			Rect r = Place(Fill, -1f, true, h);

			float total = Mathf.Max(0f, r.width - Spacing * (weights.Length - 1));
			float sum = 0f;
			for (int i = 0; i < weights.Length; i++)
				sum += weights[i];

			Ui ui = new Ui(Mode.Columns, r, _clip, _scope, this);
			ui._cells = new float[weights.Length];
			for (int i = 0; i < weights.Length; i++)
				ui._cells[i] = total * weights[i] / Mathf.Max(sum, 0.0001f);
			return ui;
		}

		/// <summary>
		/// A card with a background. Height must be given (or <see cref="Fill"/> for the rest of the space).
		/// </summary>
		public Ui Panel(float height = Fill, float padding = 12f)
		{
			Theme t = Theme.Current;
			float h = ResolveHeight(height);
			Rect r = Place(Fill, -1f, true, h);
			Draw.Box(r, t.Panel, 10f, t.Border);

			Rect inner = new Rect(r.x + padding, r.y + padding, Mathf.Max(0f, r.width - padding * 2f), Mathf.Max(0f, r.height - padding * 2f));
			return new Ui(Mode.Vertical, inner, Intersect(_clip, r), _scope, this);
		}

		/// <summary>Empty space. Vertical in a column, horizontal in a row.</summary>
		public void Space(float amount)
		{
			_cursor += amount;
		}

		/// <summary>A thin horizontal line.</summary>
		public void Divider()
		{
			Rect r = Place(Fill, -1f, true, 1f);
			Draw.Fill(r, Theme.Current.Divider);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			if (_onDispose != null)
				_onDispose();
		}

		private float ResolveHeight(float height)
		{
			return height < 0f ? Mathf.Max(0f, _bounds.height - _cursor) : height;
		}

		private float ResolveWidth(float width, float intrinsic, bool fillWhenVertical)
		{
			if (_mode == Mode.Columns && _cells != null && _cells.Length > 0)
				return _cells[Mathf.Min(_cell, _cells.Length - 1)];

			if (width == Default)
				width = _mode == Mode.Vertical && !fillWhenVertical ? intrinsic : (_mode == Mode.Vertical ? Fill : intrinsic);

			if (width == Auto)
				width = intrinsic;

			if (width < 0f)
				width = AvailableWidth;

			return Mathf.Max(0f, width);
		}

		private Rect PlaceResolved(float width, float height)
		{
			if (_mode == Mode.Vertical)
			{
				Rect v = new Rect(_bounds.x, _bounds.y + _cursor, Mathf.Min(width, _bounds.width), height);
				_cursor += height + Spacing;
				return v;
			}

			// Rows and columns: height comes from the row itself.
			Rect h = new Rect(_bounds.x + _cursor, _bounds.y, width, _bounds.height);
			_cursor += width + Spacing;
			if (_mode == Mode.Columns)
				_cell++;
			return h;
		}

		private Rect Place(float width, float intrinsic, bool fillWhenVertical, float height)
		{
			return PlaceResolved(ResolveWidth(width, intrinsic, fillWhenVertical), height);
		}

		private static Rect Intersect(Rect a, Rect b)
		{
			float x = Mathf.Max(a.xMin, b.xMin);
			float y = Mathf.Max(a.yMin, b.yMin);
			float xMax = Mathf.Min(a.xMax, b.xMax);
			float yMax = Mathf.Min(a.yMax, b.yMax);
			return new Rect(x, y, Mathf.Max(0f, xMax - x), Mathf.Max(0f, yMax - y));
		}

		/// <summary>True if the mouse is over the rect and inside this scope's visible area.</summary>
		private bool Hit(Rect r)
		{
			Vector2 m = Event.current.mousePosition;
			return _clip.Contains(m) && r.Contains(m);
		}

		/// <summary>
		/// Standard press-and-release button behaviour using a hot control, so a click only counts
		/// if the press started on the control and the release is still over it.
		/// Always claims a control id, even when disabled, to keep ids stable between events.
		/// </summary>
		private bool Interact(Rect r, bool enabled, out bool hover, out bool held)
		{
			int id = GUIUtility.GetControlID(FocusType.Passive);
			Event e = Event.current;
			bool inside = enabled && Hit(r);
			bool clicked = false;

			switch (e.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (inside && e.button == 0)
					{
						GUIUtility.hotControl = id;
						GUI.FocusControl(null);
						e.Use();
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;
						clicked = inside;
						e.Use();
					}
					break;
			}

			held = GUIUtility.hotControl == id && inside;
			hover = inside && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == id);
			return clicked;
		}

		private string NextFieldName()
		{
			return _root._scope + ":" + (_root._fieldIndex++);
		}
	}
}
