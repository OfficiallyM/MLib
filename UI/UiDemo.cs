using System;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// A window using every widget, plus a static panel and slide animation. Create one and call
	/// <see cref="OnGUI"/> from your mod's OnGUI. Handy as a smoke test in a new game
	/// and as a reference for how the API reads.
	/// </summary>
	internal sealed class UiDemo
	{
		private const string AnimationKey = "mlib-demo";

		private bool _open = false;
		private Rect _rect;
		private int _tab;
		private bool _toggleA = true;
		private bool _toggleB;
		private float _slider = 0.5f;
		private float _stepped = 40f;
		private float _number = 12.5f;
		private int _integer = 3;
		private string _text = string.Empty;
		private string _lastClick = "Nothing clicked yet";
		private readonly ScrollState _scroll = new ScrollState();
		private readonly ScrollState _listScroll = new ScrollState();

		private readonly WindowOptions _options;

		/// <summary>
		/// Runs when the window's X button is clicked. If not set, the window just closes itself.
		/// </summary>
		public Action OnClose;

		public UiDemo()
		{
			_options = new WindowOptions
			{
				AnimationKey = AnimationKey,
				SlideDirection = Animator.SlideDirection.Left,
				OnClose = HandleClose,
			};

			// Start in whichever state _open says, without sliding.
			Animator.SetVisible(AnimationKey, _open, animate: false);
		}

		private void HandleClose()
		{
			if (OnClose != null)
				OnClose();
			else
				Open = false;
		}

		/// <summary>Showing or hiding slides the demo window in or out.</summary>
		public bool Open
		{
			get { return _open; }
			set
			{
				if (_open == value) return;
				_open = value;
				Animator.SetVisible(AnimationKey, value);
			}
		}

		public void OnGUI()
		{
			DrawStaticPanel();

			// Once the slide-out has finished there is nothing left to draw.
			if (!_open && Animator.IsHidden(AnimationKey)) return;

			using (Ui ui = Ui.Window(ref _rect, "MLib demo", _options))
			{
				if (ui.Hidden) return;

				_tab = ui.Tabs(_tab, new[] { "Widgets", "List" });
				ui.Space(4f);

				if (_tab == 0)
					DrawWidgets(ui);
				else
					DrawList(ui);
			}
		}

		/// <summary>A fixed, header-less panel in the top right that reopens the demo window.</summary>
		private void DrawStaticPanel()
		{
			Vector2 screen = Ui.ScreenSize;
			Rect rect = new Rect(screen.x - 270f, 20f, 250f, 120f);

			using (Ui ui = Ui.StaticWindow(rect, "mlib-static"))
			{
				ui.Label("Static window", bold: true);
				ui.Hint("No header, never moves.");
				if (ui.Button(_open ? "Hide demo window" : "Show demo window", ButtonStyle.Secondary))
					Open = !_open;
			}
		}

		private void DrawWidgets(Ui ui)
		{
			using (Ui box = ui.Scroll(_scroll))
			{
				box.Header("Buttons");
				using (Ui cols = box.Columns(34f, 1f, 1f, 1f, 1f))
				{
					if (cols.Button("Primary")) _lastClick = "Primary";
					if (cols.Button("Secondary", ButtonStyle.Secondary)) _lastClick = "Secondary";
					if (cols.Button("Ghost", ButtonStyle.Ghost)) _lastClick = "Ghost";
					if (cols.Button("Delete", ButtonStyle.Danger, icon: "close")) _lastClick = "Danger";
				}
				box.Hint("Last click: " + _lastClick);
				box.Space(6f);

				box.Header("Toggles and sliders");
				_toggleA = box.Toggle("Pill toggle, on", _toggleA);
				_toggleB = box.Toggle("Pill toggle, off", _toggleB);
				box.Toggle("Disabled toggle", true, enabled: false);
				_slider = box.Slider(_slider, 0f, 1f, "0.00");
				_stepped = box.Slider(_stepped, 0f, 100f, "0", step: 5f);
				box.Space(6f);

				box.Header("Fields");
				using (Ui row = box.Row())
				{
					row.Label("Number");
					_number = row.NumberField(_number, width: 100f);
					row.Label("Integer");
					_integer = row.IntField(_integer, width: 80f, min: 0, max: 10);
				}
				_text = box.TextField(_text, "Type something...");
				box.Space(6f);

				box.Header("Badges and icons");
				using (Ui row = box.Row(26f))
				{
					row.Badge("Default");
					row.Badge("Good", Theme.Current.Good.Base);
					row.Badge("Warning", Theme.Current.Warning.Base);
					row.Badge("Info", Palette.SkyBlue);
					row.Badge("Teal", Palette.Teal);
				}
				using (Ui row = box.Row())
				{
					foreach (string icon in new[] { "gear", "star", "star_outline", "search", "undo", "plus", "minus", "check" })
						row.IconButton(icon, ButtonStyle.Secondary);
				}
				box.Divider();
				box.Hint("Hints are small, muted and wrap onto several lines when the window is narrow, " +
					"which is useful for explaining a setting without crowding the control itself.");
			}
		}

		private void DrawList(Ui ui)
		{
			using Ui panel = ui.Panel();
			using Ui list = panel.Scroll(_listScroll);
			for (int i = 1; i <= 40; i++)
			{
				using Ui row = list.Columns(32f, 6f, 2f, 1f);
				row.Label("List item " + i);
				row.Badge(i % 3 == 0 ? "Special" : "Normal", i % 3 == 0 ? Theme.Current.Warning.Base : Theme.Current.Info.Base);
				if (row.IconButton("star_outline"))
					_lastClick = "Starred item " + i;
			}
		}
	}
}