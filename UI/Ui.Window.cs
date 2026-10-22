using System;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>Options for <see cref="Ui.Window"/> and <see cref="Ui.StaticWindow"/>.</summary>
	internal sealed class WindowOptions
	{
		/// <summary>Show the title bar. Without it there is no close button and nothing to drag by.</summary>
		public bool Header = true;

		/// <summary>Show a close button in the title bar (needs <see cref="Header"/>).</summary>
		public bool Closable = true;

		/// <summary>Allow dragging by the title bar (needs <see cref="Header"/>). Ignored by static windows.</summary>
		public bool Movable = true;

		/// <summary>Show the resize grip. Ignored by static windows.</summary>
		public bool Resizable = true;

		/// <summary>Draw the window body, border and shadow. Turn off for a transparent HUD.</summary>
		public bool Background = true;

		public Vector2 MinSize = new Vector2(360f, 240f);

		/// <summary>Size used (and the window centred) when the rect passed in is empty.</summary>
		public Vector2 DefaultSize = new Vector2(900f, 600f);

		/// <summary>
		/// Set to slide the window in and out with <see cref="Animator"/>, using this key.
		/// Leave null for no animation.
		/// </summary>
		public string AnimationKey;

		/// <summary>The screen edge the window slides to and from.</summary>
		public Animator.SlideDirection SlideDirection = Animator.SlideDirection.Left;

		public float SlideSpeed = 1.5f;

		/// <summary>
		/// Called when the window close button is clicked.
		/// </summary>
		public Action OnClose;

		/// <summary>No header, dragging or resizing. Use for HUD panels and fixed menus.</summary>
		public static WindowOptions Static()
		{
			return new WindowOptions { Header = false, Closable = false, Movable = false, Resizable = false };
		}
	}

	internal sealed partial class Ui
	{
		private const float HeaderHeight = 48f;

		private static Vector2 _grab;
		private static readonly WindowOptions _defaultWindow = new WindowOptions();
		private static readonly WindowOptions _defaultStatic = WindowOptions.Static();

		/// <summary>Resolution the UI is designed for. Windows scale with screen height relative to this.</summary>
		public static float BaseHeight = 1080f;

		/// <summary>Extra multiplier on top of automatic scaling, e.g. from a user setting.</summary>
		public static float UserScale = 1f;

		/// <summary>Current UI scale factor.</summary>
		public static float Scale => Mathf.Clamp(Screen.height / BaseHeight, 0.6f, 3f) * UserScale;

		/// <summary>The screen size in UI units, for positioning windows (e.g. anchoring to a corner).</summary>
		public static Vector2 ScreenSize
		{
			get
			{
				float s = Scale;
				return new Vector2(Screen.width / s, Screen.height / s);
			}
		}

		/// <summary>True if the user clicked the window's close button this frame.</summary>
		public bool CloseRequested { get; private set; }

		/// <summary>True if the mouse is over the window when it was opened this frame.</summary>
		public bool MouseOver { get; private set; }

		/// <summary>
		/// True while an animated window is fully off-screen. Widgets added to it are still laid out
		/// but cannot be seen or clicked, so you can skip the contents.
		/// </summary>
		public bool Hidden { get; private set; }

		/// <summary>
		/// A window the user can move and resize. <paramref name="rect"/> is in scaled UI units and
		/// is updated as the window is moved or resized, so keep it in a field.
		/// A default-sized, centred window is created if the rect is empty.
		/// <code>
		/// using (var ui = Ui.Window(ref _rect, "My mod"))
		/// {
		///     if (ui.CloseRequested) _open = false;
		///     ui.Header("Hello");
		/// }
		/// </code>
		/// Call this from OnGUI on every event, not only on Repaint.
		/// </summary>
		public static Ui Window(ref Rect rect, string title, WindowOptions options = null)
		{
			return Begin(ref rect, title, options ?? _defaultWindow, false);
		}

		/// <summary>
		/// A window fixed at an exact rect. It never moves or resizes, and by default has no header.
		/// Positions are in scaled UI units; see <see cref="ScreenSize"/> for anchoring to screen edges.
		/// <paramref name="title"/> is shown if the header is on, and otherwise only names the window
		/// so that text fields in different windows don't clash.
		/// </summary>
		public static Ui StaticWindow(Rect rect, string title, WindowOptions options = null)
		{
			return Begin(ref rect, title, options ?? _defaultStatic, true);
		}

		private static Rect CloseRectOf(Rect r) => new Rect(r.xMax - 40f, r.y + 8f, 32f, 32f);

		private static Rect GripRectOf(Rect r) => new Rect(r.xMax - 22f, r.yMax - 22f, 22f, 22f);

		private static Ui Begin(ref Rect rect, string title, WindowOptions o, bool fixedRect)
		{
			Theme t = Theme.Current;
			float scale = Scale;
			Matrix4x4 previousMatrix = GUI.matrix;
			GUI.matrix = previousMatrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));

			Vector2 screen = new Vector2(Screen.width / scale, Screen.height / scale);
			Vector2 min = o.MinSize;

			bool header = o.Header;
			bool closable = header && o.Closable;
			bool movable = header && o.Movable && !fixedRect;
			bool resizable = o.Resizable && !fixedRect;

			if (!fixedRect)
			{
				Vector2 size = o.DefaultSize;
				if (rect.width <= 0f || rect.height <= 0f)
					rect = new Rect((screen.x - size.x) * 0.5f, (screen.y - size.y) * 0.5f, size.x, size.y);

				rect.width = Mathf.Clamp(rect.width, min.x, Mathf.Max(min.x, screen.x));
				rect.height = Mathf.Clamp(rect.height, min.y, Mathf.Max(min.y, screen.y));
				rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, screen.x - rect.width));
				rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, screen.y - rect.height));
			}

			// 'rect' is where the window rests; 'drawn' is where it is right now while sliding.
			Rect drawn = rect;
			bool idle = true;
			bool hidden = false;
			if (!string.IsNullOrEmpty(o.AnimationKey))
			{
				drawn = Animator.Slide(o.AnimationKey, rect, o.SlideDirection, screen, o.SlideSpeed);
				idle = Animator.IsIdle(o.AnimationKey);
				hidden = Animator.IsHidden(o.AnimationKey);
			}

			// No dragging or resizing while it is sliding.
			movable &= idle;
			resizable &= idle;

			Event e = Event.current;
			Ui ui = new Ui(Mode.Vertical, drawn, _everywhere, title ?? "window", null);

			// Control ids are claimed in a fixed order: close, move, resize.
			bool closeHover = false, closeHeld = false, closeClicked = false;
			if (closable)
				closeClicked = ui.Interact(CloseRectOf(drawn), true, out closeHover, out closeHeld);

			if (closeClicked && o.OnClose != null)
				o.OnClose();

			int moveId = GUIUtility.GetControlID(FocusType.Passive);
			int sizeId = GUIUtility.GetControlID(FocusType.Passive);

			if (movable)
			{
				Rect headerRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);

				switch (e.GetTypeForControl(moveId))
				{
					case EventType.MouseDown:
						if (e.button == 0 && headerRect.Contains(e.mousePosition))
						{
							GUIUtility.hotControl = moveId;
							GUI.FocusControl(null);
							_grab = e.mousePosition - rect.position;
							e.Use();
						}
						break;

					case EventType.MouseDrag:
						if (GUIUtility.hotControl == moveId)
						{
							rect.position = e.mousePosition - _grab;
							e.Use();
						}
						break;

					case EventType.MouseUp:
						if (GUIUtility.hotControl == moveId)
						{
							GUIUtility.hotControl = 0;
							e.Use();
						}
						break;
				}
			}

			if (resizable)
			{
				Rect grip = GripRectOf(rect);

				switch (e.GetTypeForControl(sizeId))
				{
					case EventType.MouseDown:
						if (e.button == 0 && grip.Contains(e.mousePosition))
						{
							GUIUtility.hotControl = sizeId;
							GUI.FocusControl(null);
							_grab = new Vector2(rect.xMax, rect.yMax) - e.mousePosition;
							e.Use();
						}
						break;

					case EventType.MouseDrag:
						if (GUIUtility.hotControl == sizeId)
						{
							Vector2 corner = e.mousePosition + _grab;
							rect.width = Mathf.Clamp(corner.x - rect.x, min.x, Mathf.Max(min.x, screen.x - rect.x));
							rect.height = Mathf.Clamp(corner.y - rect.y, min.y, Mathf.Max(min.y, screen.y - rect.y));
							e.Use();
						}
						break;

					case EventType.MouseUp:
						if (GUIUtility.hotControl == sizeId)
						{
							GUIUtility.hotControl = 0;
							e.Use();
						}
						break;
				}
			}

			// At rest the window is exactly where its rect says, which may have just changed.
			if (idle && !hidden)
				drawn = rect;

			if (e.type == EventType.Repaint && !hidden)
			{
				if (o.Background)
				{
					Draw.Glow(drawn, new Color(0f, 0f, 0f, 0.30f), 14f, 2, 2f);
					Draw.Box(drawn, t.Window, 12f, t.WindowBorder);
				}

				if (header)
				{
					Draw.Box(new Rect(drawn.x + 1f, drawn.y + 1f, drawn.width - 2f, HeaderHeight - 1f), t.Bar, new Vector4(11f, 11f, 0f, 0f));
					Draw.Fill(new Rect(drawn.x + 1f, drawn.y + HeaderHeight - 1f, drawn.width - 2f, 1f), t.Divider);

					Draw.Label(new Rect(drawn.x + 18f, drawn.y, Mathf.Max(0f, drawn.width - 80f), HeaderHeight),
						title ?? string.Empty, t.Text, 17, TextAnchor.MiddleLeft, true, true);
				}

				if (closable)
				{
					Rect closeRect = CloseRectOf(drawn);
					if (closeHover || closeHeld)
						Draw.Box(closeRect, closeHeld ? t.Selected : t.Hover, 8f);
					Draw.Icon(new Rect(closeRect.center.x - 8f, closeRect.center.y - 8f, 16f, 16f), "close",
						closeHover ? t.Text : t.TextSoft);
				}

				if (resizable)
				{
					Rect grip = GripRectOf(drawn);
					Draw.Icon(new Rect(grip.x + 2f, grip.y + 2f, 14f, 14f), "grip", t.TextMuted);
				}
			}

			// Content area, clipped to the window body.
			float top = header ? HeaderHeight : 0f;
			Rect content = new Rect(
				drawn.x + Padding,
				drawn.y + top + Padding,
				Mathf.Max(0f, drawn.width - Padding * 2f),
				Mathf.Max(0f, drawn.height - top - Padding * 2f));

			GUI.BeginGroup(content);
			Rect local = new Rect(0f, 0f, content.width, content.height);
			ui._bounds = local;
			ui._clip = local;
			ui._origin = content.position;
			ui.CloseRequested = closeClicked;
			ui.MouseOver = !hidden && drawn.Contains(e.mousePosition);
			ui.Hidden = hidden;

			// A ref parameter can't be captured by the lambda below, so copy what it needs.
			Rect windowRect = drawn;
			ui._onDispose = () =>
			{
				GUI.EndGroup();

				// Overlays go on top of everything in the window, still inside the scaled matrix.
				FlushDropdown();

				// Stop clicks and scrolling that nothing claimed from reaching other IMGUI below.
				Event current = Event.current;
				if (!hidden && windowRect.Contains(current.mousePosition) &&
					(current.type == EventType.MouseDown || current.type == EventType.MouseUp || current.type == EventType.ScrollWheel))
					current.Use();

				GUI.matrix = previousMatrix;
			};

			return ui;
		}
	}
}