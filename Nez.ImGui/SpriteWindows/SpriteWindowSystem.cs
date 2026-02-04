using System;
using System.Numerics;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;

namespace Nez.ImGuiTools.SpriteWindows
{
	public class SpriteWindowSystem
	{
		public static ImGuiRenderer Renderer;

		private static int _scrollWheelValue;

		private static VirtualButton _gamepadDpadUp = new VirtualButton();
		private static VirtualButton _gamepadDpadDown = new VirtualButton();
		private static VirtualButton _gamepadDpadRight = new VirtualButton();
		private static VirtualButton _gamepadDpadLeft = new VirtualButton();
		private static VirtualButton _gamepadFaceUp = new VirtualButton();
		private static VirtualButton _gamepadFaceDown = new VirtualButton();
		private static VirtualButton _gamepadFaceRight = new VirtualButton();
		private static VirtualButton _gamepadFaceLeft = new VirtualButton();

		public static void Initialize(Core instance)
		{
			if (Renderer != null)
				return;

			Renderer = new ImGuiRenderer(instance);
			var imGuiOptions = new ImGuiOptions();
			Renderer.RebuildFontAtlas(imGuiOptions);

			_gamepadDpadUp.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.DPadUp));
			_gamepadDpadDown.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.DPadDown));
			_gamepadDpadRight.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.DPadRight));
			_gamepadDpadLeft.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.DPadLeft));
			_gamepadFaceUp.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.Y));
			_gamepadFaceDown.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));
			_gamepadFaceRight.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.B));
			_gamepadFaceLeft.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));

			var io = ImGui.GetIO();

			io.BackendFlags |= ImGuiBackendFlags.HasGamepad;

			io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
			io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

			SetupStyle();
		}

		public static void SetupStyle()
		{
			var style = ImGui.GetStyle();
			NezImGuiThemes.DarkTheme2();
			// NezImGuiThemes.DefaultLightTheme();

			// style.ChildRounding = 0;
			// style.FrameRounding = 0;
			// style.GrabRounding = 0;
			// style.PopupRounding = 0;
			// style.ScrollbarRounding = 0;
			// style.TabRounding = 0;
			// style.WindowRounding = 0;
			//
			// style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.13f, 0.12f, 0.12f, .80f);
		}

		public static void BeforeLayout(float deltaTime, AbstractSpriteWindowComponent window)
		{
			ImGui.GetIO().DeltaTime = deltaTime;
			UpdateInput(window);
			ImGui.NewFrame();
		}

		private static void UpdateInput(AbstractSpriteWindowComponent window)
		{
			var io = ImGui.GetIO();

			// io.DisplaySize = new Vector2(
			//     window.WindowWidth,
			//     window.WindowHeight
			// );


			io.DisplaySize = new Vector2(
				Core.GraphicsDevice.PresentationParameters.BackBufferWidth,
				Core.GraphicsDevice.PresentationParameters.BackBufferHeight
			);
			io.DisplayFramebufferScale = new Vector2(1f, 1f);

			// Noop the input if the window isn't focused...
			// if (!window.WindowIsFocused)
			// {
			// 	return;
			// }


			var mouse = Nez.Input.CurrentMouseState;
			var keyboard = Nez.Input.CurrentKeyboardState;

			foreach (Keys key in Enum.GetValues(typeof(Keys)))
			{
				var isDown = keyboard.IsKeyDown(key);
				var translatedKey = TranslateKey(key);

				io.AddKeyEvent(translatedKey, isDown);
			}


			if (_gamepadDpadUp.IsPressed || _gamepadDpadUp.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadDpadUp, _gamepadDpadUp.IsPressed);
			}

			if (_gamepadDpadDown.IsPressed || _gamepadDpadDown.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadDpadDown, _gamepadDpadDown.IsPressed);
			}

			if (_gamepadDpadRight.IsPressed || _gamepadDpadRight.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadDpadRight, _gamepadDpadRight.IsPressed);
			}

			if (_gamepadDpadLeft.IsPressed || _gamepadDpadLeft.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadDpadLeft, _gamepadDpadLeft.IsPressed);
			}

			if (_gamepadFaceUp.IsPressed || _gamepadFaceUp.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadFaceUp, _gamepadFaceUp.IsPressed);
			}

			if (_gamepadFaceDown.IsPressed || _gamepadFaceDown.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadFaceDown, _gamepadFaceDown.IsPressed);
			}

			if (_gamepadFaceRight.IsPressed || _gamepadFaceRight.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadFaceRight, _gamepadFaceRight.IsPressed);
			}

			if (_gamepadFaceLeft.IsPressed || _gamepadFaceLeft.IsReleased)
			{
				io.AddKeyEvent(ImGuiKey.GamepadFaceLeft, _gamepadFaceLeft.IsPressed);
			}

			io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
			io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
			io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
			io.KeySuper = keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);

			var position = new Microsoft.Xna.Framework.Vector2(mouse.X, mouse.Y);

			// Scale to scene design size
			if (Core.Scene != null)
			{
				var scale = new Microsoft.Xna.Framework.Vector2(
					Core.Scene.SceneRenderTarget.Width /
					(float)Core.GraphicsDevice.PresentationParameters.BackBufferWidth,
					Core.Scene.SceneRenderTarget.Height /
					(float)Core.GraphicsDevice.PresentationParameters.BackBufferHeight
				);

				position = Nez.Input.RawMousePosition.ToVector2() * scale;
			}

			// Adjust for the entity window position/rotation and size
			var localPosition = Microsoft.Xna.Framework.Vector2.Transform(
				position - window.Entity.Position,
				Microsoft.Xna.Framework.Matrix.CreateRotationZ(-window.Entity.Rotation)
			);
			localPosition /= window.Entity.Scale;
			localPosition += new Microsoft.Xna.Framework.Vector2(window.WindowWidth, window.WindowHeight) / 2f;

			// io.AddMousePosEvent(position.X, position.Y);
			io.AddMousePosEvent(localPosition.X, localPosition.Y);
			io.MouseDrawCursor = false; // DrawImGuiMouseCursor;

			io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed;
			io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed;
			io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed;

			var scrollDelta = mouse.ScrollWheelValue - _scrollWheelValue;
			io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
			_scrollWheelValue = mouse.ScrollWheelValue;
		}

		public static ImGuiKey TranslateKey(Keys key)
		{
			switch (key)
			{
				// case Keys.ModNone: return ImGuiKey.ModNone;
				case Keys.None: return ImGuiKey.None;
				// case Keys.NamedKey_COUNT: return ImGuiKey.NamedKey_COUNT;
				// case Keys.NamedKey_BEGIN: return ImGuiKey.NamedKey_BEGIN;
				case Keys.Tab: return ImGuiKey.Tab;
				case Keys.Left: return ImGuiKey.LeftArrow;
				case Keys.Right: return ImGuiKey.RightArrow;
				case Keys.Up: return ImGuiKey.UpArrow;
				case Keys.Down: return ImGuiKey.DownArrow;
				case Keys.PageUp: return ImGuiKey.PageUp;
				case Keys.PageDown: return ImGuiKey.PageDown;
				case Keys.Home: return ImGuiKey.Home;
				case Keys.End: return ImGuiKey.End;
				case Keys.Insert: return ImGuiKey.Insert;
				case Keys.Delete: return ImGuiKey.Delete;
				case Keys.Back: return ImGuiKey.Backspace;
				case Keys.Space: return ImGuiKey.Space;
				case Keys.Enter: return ImGuiKey.Enter;
				case Keys.Escape: return ImGuiKey.Escape;
				case Keys.LeftControl: return ImGuiKey.LeftCtrl;
				case Keys.LeftShift: return ImGuiKey.LeftShift;
				case Keys.LeftAlt: return ImGuiKey.LeftAlt;
				case Keys.LeftWindows: return ImGuiKey.LeftSuper;
				case Keys.RightControl: return ImGuiKey.RightCtrl;
				case Keys.RightShift: return ImGuiKey.RightShift;
				case Keys.RightAlt: return ImGuiKey.RightAlt;
				case Keys.RightWindows: return ImGuiKey.RightSuper;
				// case Keys.Menu: return ImGuiKey.Menu;
				case Keys.D0: return ImGuiKey._0;
				case Keys.D1: return ImGuiKey._1;
				case Keys.D2: return ImGuiKey._2;
				case Keys.D3: return ImGuiKey._3;
				case Keys.D4: return ImGuiKey._4;
				case Keys.D5: return ImGuiKey._5;
				case Keys.D6: return ImGuiKey._6;
				case Keys.D7: return ImGuiKey._7;
				case Keys.D8: return ImGuiKey._8;
				case Keys.D9: return ImGuiKey._9;
				case Keys.A: return ImGuiKey.A;
				case Keys.B: return ImGuiKey.B;
				case Keys.C: return ImGuiKey.C;
				case Keys.D: return ImGuiKey.D;
				case Keys.E: return ImGuiKey.E;
				case Keys.F: return ImGuiKey.F;
				case Keys.G: return ImGuiKey.G;
				case Keys.H: return ImGuiKey.H;
				case Keys.I: return ImGuiKey.I;
				case Keys.J: return ImGuiKey.J;
				case Keys.K: return ImGuiKey.K;
				case Keys.L: return ImGuiKey.L;
				case Keys.M: return ImGuiKey.M;
				case Keys.N: return ImGuiKey.N;
				case Keys.O: return ImGuiKey.O;
				case Keys.P: return ImGuiKey.P;
				case Keys.Q: return ImGuiKey.Q;
				case Keys.R: return ImGuiKey.R;
				case Keys.S: return ImGuiKey.S;
				case Keys.T: return ImGuiKey.T;
				case Keys.U: return ImGuiKey.U;
				case Keys.V: return ImGuiKey.V;
				case Keys.W: return ImGuiKey.W;
				case Keys.X: return ImGuiKey.X;
				case Keys.Y: return ImGuiKey.Y;
				case Keys.Z: return ImGuiKey.Z;
				case Keys.F1: return ImGuiKey.F1;
				case Keys.F2: return ImGuiKey.F2;
				case Keys.F3: return ImGuiKey.F3;
				case Keys.F4: return ImGuiKey.F4;
				case Keys.F5: return ImGuiKey.F5;
				case Keys.F6: return ImGuiKey.F6;
				case Keys.F7: return ImGuiKey.F7;
				case Keys.F8: return ImGuiKey.F8;
				case Keys.F9: return ImGuiKey.F9;
				case Keys.F10: return ImGuiKey.F10;
				case Keys.F11: return ImGuiKey.F11;
				case Keys.F12: return ImGuiKey.F12;
				case Keys.F13: return ImGuiKey.F13;
				case Keys.F14: return ImGuiKey.F14;
				case Keys.F15: return ImGuiKey.F15;
				case Keys.F16: return ImGuiKey.F16;
				case Keys.F17: return ImGuiKey.F17;
				case Keys.F18: return ImGuiKey.F18;
				case Keys.F19: return ImGuiKey.F19;
				case Keys.F20: return ImGuiKey.F20;
				case Keys.F21: return ImGuiKey.F21;
				case Keys.F22: return ImGuiKey.F22;
				case Keys.F23: return ImGuiKey.F23;
				case Keys.F24: return ImGuiKey.F24;
				case Keys.OemTilde: return ImGuiKey.GraveAccent;
				case Keys.OemQuotes: return ImGuiKey.Apostrophe;
				case Keys.OemComma: return ImGuiKey.Comma;
				case Keys.OemMinus: return ImGuiKey.Minus;
				case Keys.OemPeriod: return ImGuiKey.Period;
				case Keys.OemQuestion: return ImGuiKey.Slash; //OemBackslash?
				case Keys.OemSemicolon: return ImGuiKey.Semicolon;
				case Keys.OemPlus: return ImGuiKey.Equal;
				case Keys.OemOpenBrackets: return ImGuiKey.LeftBracket;
				case Keys.OemBackslash: return ImGuiKey.Backslash;
				case Keys.OemCloseBrackets: return ImGuiKey.RightBracket;
				case Keys.CapsLock: return ImGuiKey.CapsLock;
				case Keys.Scroll: return ImGuiKey.ScrollLock;
				case Keys.NumLock: return ImGuiKey.NumLock;
				case Keys.PrintScreen: return ImGuiKey.PrintScreen;
				case Keys.Pause: return ImGuiKey.Pause;
				case Keys.NumPad0: return ImGuiKey.Keypad0;
				case Keys.NumPad1: return ImGuiKey.Keypad1;
				case Keys.NumPad2: return ImGuiKey.Keypad2;
				case Keys.NumPad3: return ImGuiKey.Keypad3;
				case Keys.NumPad4: return ImGuiKey.Keypad4;
				case Keys.NumPad5: return ImGuiKey.Keypad5;
				case Keys.NumPad6: return ImGuiKey.Keypad6;
				case Keys.NumPad7: return ImGuiKey.Keypad7;
				case Keys.NumPad8: return ImGuiKey.Keypad8;
				case Keys.NumPad9: return ImGuiKey.Keypad9;
				case Keys.Multiply: return ImGuiKey.KeypadMultiply;
				case Keys.Subtract: return ImGuiKey.KeypadSubtract;
				case Keys.Add: return ImGuiKey.KeypadAdd;
				// case Keys.NumPadEnter: return ImGuiKey.KeypadEnter; ??
				// case Keys.NumPadEqual: return ImGuiKey.KeypadEqual; ??
				case Keys.BrowserBack: return ImGuiKey.AppBack;
				case Keys.BrowserForward: return ImGuiKey.AppForward;
				// case Keys.GamepadStart: return ImGuiKey.GamepadStart;
				// case Keys.GamepadBack: return ImGuiKey.GamepadBack;
				// case Keys.GamepadFaceLeft: return ImGuiKey.GamepadFaceLeft;
				// case Keys.GamepadFaceRight: return ImGuiKey.GamepadFaceRight;
				// case Keys.GamepadFaceUp: return ImGuiKey.GamepadFaceUp;
				// case Keys.GamepadFaceDown: return ImGuiKey.GamepadFaceDown;
				// case Keys.GamepadDpadLeft: return ImGuiKey.GamepadDpadLeft;
				// case Keys.GamepadDpadRight: return ImGuiKey.GamepadDpadRight;
				// case Keys.GamepadDpadUp: return ImGuiKey.GamepadDpadUp;
				// case Keys.GamepadDpadDown: return ImGuiKey.GamepadDpadDown;
				// case Keys.GamepadL1: return ImGuiKey.GamepadL1;
				// case Keys.GamepadR1: return ImGuiKey.GamepadR1;
				// case Keys.GamepadL2: return ImGuiKey.GamepadL2;
				// case Keys.GamepadR2: return ImGuiKey.GamepadR2;
				// case Keys.GamepadL3: return ImGuiKey.GamepadL3;
				// case Keys.GamepadR3: return ImGuiKey.GamepadR3;
				// case Keys.GamepadLStickLeft: return ImGuiKey.GamepadLStickLeft;
				// case Keys.GamepadLStickRight: return ImGuiKey.GamepadLStickRight;
				// case Keys.GamepadLStickUp: return ImGuiKey.GamepadLStickUp;
				// case Keys.GamepadLStickDown: return ImGuiKey.GamepadLStickDown;
				// case Keys.GamepadRStickLeft: return ImGuiKey.GamepadRStickLeft;
				// case Keys.GamepadRStickRight: return ImGuiKey.GamepadRStickRight;
				// case Keys.GamepadRStickUp: return ImGuiKey.GamepadRStickUp;
				// case Keys.GamepadRStickDown: return ImGuiKey.GamepadRStickDown;
				// case Keys.MouseLeft: return ImGuiKey.MouseLeft;
				// case Keys.MouseRight: return ImGuiKey.MouseRight;
				// case Keys.MouseMiddle: return ImGuiKey.MouseMiddle;
				// case Keys.MouseX1: return ImGuiKey.MouseX1;
				// case Keys.MouseX2: return ImGuiKey.MouseX2;
				// case Keys.MouseWheelX: return ImGuiKey.MouseWheelX;
				// case Keys.MouseWheelY: return ImGuiKey.MouseWheelY;
				// case Keys.ReservedForModCtrl: return ImGuiKey.ReservedForModCtrl;
				// case Keys.ReservedForModShift: return ImGuiKey.ReservedForModShift;
				// case Keys.ReservedForModAlt: return ImGuiKey.ReservedForModAlt;
				// case Keys.ReservedForModSuper: return ImGuiKey.ReservedForModSuper;
				// case Keys.NamedKey_END: return ImGuiKey.NamedKey_END;
				// case Keys.ModCtrl: return ImGuiKey.ModCtrl;
				// case Keys.ModShift: return ImGuiKey.ModShift;
				// case Keys.ModAlt: return ImGuiKey.ModAlt;
				// case Keys.ModSuper: return ImGuiKey.ModSuper;
				// case Keys.ModMask: return ImGuiKey.ModMask; // 0x0000F000				

				default: return ImGuiKey.None;
			}
		}

		public static void AfterLayout()
		{
			Renderer.AfterLayout();
		}
	}
}