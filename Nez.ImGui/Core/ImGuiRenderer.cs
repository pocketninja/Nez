using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Nez.ImGuiTools
{
	/// <summary>
	/// ImGui renderer for use with XNA-likes (FNA & MonoGame)
	/// </summary>
	public class ImGuiRenderer
	{
		public ImFontPtr DefaultFontPtr { get; private set; }

		// Graphics
		BasicEffect _effect;
		RasterizerState _rasterizerState;

		readonly VertexDeclaration _vertexDeclaration;
		readonly int _vertexDeclarationSize;

		byte[] _vertexData;
		VertexBuffer _vertexBuffer;
		int _vertexBufferSize;

		byte[] _indexData;
		IndexBuffer _indexBuffer;
		int _indexBufferSize;

		// Textures
		Dictionary<IntPtr, Texture2D> _loadedTextures = new Dictionary<IntPtr, Texture2D>();

		int _textureId;
		IntPtr? _fontTextureId;

		// Input
		int _scrollWheelValue;


		List<int> _keys = new List<int>();


		public ImGuiRenderer(Game game)
		{
			unsafe
			{
				_vertexDeclarationSize = sizeof(ImDrawVert);
			}

			_vertexDeclaration = new VertexDeclaration(
				_vertexDeclarationSize,

				// Position
				new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),

				// UV
				new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),

				// Color
				new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
			);

			ImGui.SetCurrentContext(ImGui.CreateContext());

			_rasterizerState = new RasterizerState()
			{
				CullMode = CullMode.None,
				DepthBias = 0,
				FillMode = FillMode.Solid,
				MultiSampleAntiAlias = false,
				ScissorTestEnable = true,
				SlopeScaleDepthBias = 0
			};

			SetupInput();
		}


		#region ImGuiRenderer

		/// <summary>
		/// Creates a texture and loads the font data from ImGui. Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
		/// </summary>
		public unsafe void RebuildFontAtlas(ImGuiOptions options)
		{
			// Get font texture from ImGui
			var io = ImGui.GetIO();

			if (options._includeDefaultFont)
				DefaultFontPtr = io.Fonts.AddFontDefault();

			foreach (var font in options._fonts)
				io.Fonts.AddFontFromFileTTF(font.Item1, font.Item2);

			io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

			// Copy the data to a managed array
			var pixels = new byte[width * height * bytesPerPixel];
			Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);

			// Create and register the texture as an XNA texture
			var tex2d = new Texture2D(Core.GraphicsDevice, width, height, false, SurfaceFormat.Color);
			tex2d.SetData(pixels);

			// Should a texture already have been built previously, unbind it first so it can be deallocated
			if (_fontTextureId.HasValue)
				UnbindTexture(_fontTextureId.Value);

			// Bind the new texture to an ImGui-friendly id
			_fontTextureId = BindTexture(tex2d);

			// Let ImGui know where to find the texture
			io.Fonts.SetTexID(_fontTextureId.Value);
			io.Fonts.ClearTexData(); // Clears CPU side texture data
		}

		/// <summary>
		/// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
		/// </summary>
		public IntPtr BindTexture(Texture2D texture)
		{
			var id = new IntPtr(_textureId++);
			_loadedTextures.Add(id, texture);
			return id;
		}

		/// <summary>
		/// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
		/// </summary>
		public void UnbindTexture(IntPtr textureId)
		{
			_loadedTextures.Remove(textureId);
		}

		/// <summary>
		/// Sets up ImGui for a new frame, should be called at frame start
		/// </summary>
		public void BeforeLayout(float deltaTime)
		{
			ImGui.GetIO().DeltaTime = deltaTime;
			UpdateInput();
			ImGui.NewFrame();
		}

		/// <summary>
		/// Asks ImGui for the generated geometry data and sends it to the graphics pipeline, should be called after the UI is drawn using ImGui.** calls
		/// </summary>
		public void AfterLayout()
		{
			ImGui.Render();
			unsafe
			{
				RenderDrawData(ImGui.GetDrawData());
			}
		}

		#endregion


		#region Setup & Update

#if FNA
		delegate string GetClipboardTextDelegate();
		delegate void SetClipboardTextDelegate(IntPtr userData, string txt);

		static void SetClipboardText(IntPtr userData, string txt) => SDL2.SDL.SDL_SetClipboardText(txt);

		static string GetClipboardText() => SDL2.SDL.SDL_GetClipboardText();
#endif

		/// <summary>
		/// Maps ImGui keys to XNA keys. We use this later on to tell ImGui what keys were pressed
		/// </summary>
		void SetupInput()
		{
			var io = ImGui.GetIO();

#if FNA
		// forward clipboard methods to SDL
		io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate<SetClipboardTextDelegate>(SetClipboardText);
		io.GetClipboardTextFn =
Marshal.GetFunctionPointerForDelegate<GetClipboardTextDelegate>(SDL2.SDL.SDL_GetClipboardText);
        TextInputEXT.TextInput += c =>
		{
			if (c == '\t')
				return;
			ImGui.GetIO().AddInputCharacter(c);
		};
#else
		Core.Instance.Window.TextInput += (s, a) =>
		{
			if (a.Character == '\t')
				return;

			io.AddInputCharacter(a.Character);
		};
#endif
		}

		/// <summary>
		/// Updates the <see cref="Effect" /> to the current matrices and texture
		/// </summary>
		Effect UpdateEffect(Texture2D texture)
		{
			_effect = _effect ?? new BasicEffect(Core.GraphicsDevice);

			var io = ImGui.GetIO();

			_effect.World = Matrix.Identity;
			_effect.View = Matrix.Identity;
			_effect.Projection = Matrix.CreateOrthographicOffCenter(0, io.DisplaySize.X, io.DisplaySize.Y, 0, -1f, 1f);
			_effect.TextureEnabled = true;
			_effect.Texture = texture;
			_effect.VertexColorEnabled = true;

			return _effect;
		}

		/// <summary>
		/// Sends XNA input state to ImGui
		/// </summary>
		void UpdateInput()
		{
			var io = ImGui.GetIO();

			var mouse = Input.CurrentMouseState;
			var keyboard = Input.CurrentKeyboardState;

			foreach (Keys key in Enum.GetValues(typeof(Keys)))
			{
				var isDown = keyboard.IsKeyDown(key);
				var translatedKey = TranslateKey(key);
            
				// TODO: This needs more work, specifically with regard to inputs for text fields/etc...
				io.AddKeyEvent(translatedKey, isDown);
			}

			io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
			io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
			io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
			io.KeySuper = keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);

			io.DisplaySize = new System.Numerics.Vector2(Core.GraphicsDevice.PresentationParameters.BackBufferWidth,
				Core.GraphicsDevice.PresentationParameters.BackBufferHeight);
			io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);

			io.MousePos = new System.Numerics.Vector2(mouse.X, mouse.Y);

			io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed;
			io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed;
			io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed;

			var scrollDelta = mouse.ScrollWheelValue - _scrollWheelValue;
			io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
			_scrollWheelValue = mouse.ScrollWheelValue;
		}

		ImGuiKey TranslateKey(Keys key)
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
				// case Keys._0: return ImGuiKey._0;
				// case Keys._1: return ImGuiKey._1;
				// case Keys._2: return ImGuiKey._2;
				// case Keys._3: return ImGuiKey._3;
				// case Keys._4: return ImGuiKey._4;
				// case Keys._5: return ImGuiKey._5;
				// case Keys._6: return ImGuiKey._6;
				// case Keys._7: return ImGuiKey._7;
				// case Keys._8: return ImGuiKey._8;
				// case Keys._9: return ImGuiKey._9;
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
				// case Keys.GraveAccent: return ImGuiKey.GraveAccent;
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

		#endregion


		#region Internals

		/// <summary>
		/// Gets the geometry as set up by ImGui and sends it to the graphics device
		/// </summary>
		void RenderDrawData(ImDrawDataPtr drawData)
		{
			// Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
			var lastViewport = Core.GraphicsDevice.Viewport;
			var lastScissorBox = Core.GraphicsDevice.ScissorRectangle;

			Core.GraphicsDevice.BlendFactor = Color.White;
			Core.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
			Core.GraphicsDevice.RasterizerState = _rasterizerState;
			Core.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

			// Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
			drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

			// Setup projection
			Core.GraphicsDevice.Viewport = new Viewport(0, 0,
				Core.GraphicsDevice.PresentationParameters.BackBufferWidth,
				Core.GraphicsDevice.PresentationParameters.BackBufferHeight);

			UpdateBuffers(drawData);
			RenderCommandLists(drawData);

			// Restore modified state
			Core.GraphicsDevice.Viewport = lastViewport;
			Core.GraphicsDevice.ScissorRectangle = lastScissorBox;
		}

		unsafe void UpdateBuffers(ImDrawDataPtr drawData)
		{
			if (drawData.TotalVtxCount == 0)
			{
				return;
			}

			// Expand buffers if we need more room
			if (drawData.TotalVtxCount > _vertexBufferSize)
			{
				_vertexBuffer?.Dispose();

				_vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
				_vertexBuffer = new VertexBuffer(Core.GraphicsDevice, _vertexDeclaration, _vertexBufferSize,
					BufferUsage.None);
				_vertexData = new byte[_vertexBufferSize * _vertexDeclarationSize];
			}

			if (drawData.TotalIdxCount > _indexBufferSize)
			{
				_indexBuffer?.Dispose();

				_indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
				_indexBuffer = new IndexBuffer(Core.GraphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize,
					BufferUsage.None);
				_indexData = new byte[_indexBufferSize * sizeof(ushort)];
			}

			// Copy ImGui's vertices and indices to a set of managed byte arrays
			int vtxOffset = 0;
			int idxOffset = 0;

			for (var n = 0; n < drawData.CmdListsCount; n++)
			{
				var cmdList = drawData.CmdLists[n];

				fixed (void* vtxDstPtr = &_vertexData[vtxOffset * _vertexDeclarationSize])
				fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
				{
					Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length,
						cmdList.VtxBuffer.Size * _vertexDeclarationSize);
					Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length,
						cmdList.IdxBuffer.Size * sizeof(ushort));
				}

				vtxOffset += cmdList.VtxBuffer.Size;
				idxOffset += cmdList.IdxBuffer.Size;
			}

			// Copy the managed byte arrays to the gpu vertex- and index buffers
			_vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * _vertexDeclarationSize);
			_indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
		}

		unsafe void RenderCommandLists(ImDrawDataPtr drawData)
		{
			Core.GraphicsDevice.SetVertexBuffer(_vertexBuffer);
			Core.GraphicsDevice.Indices = _indexBuffer;

			int vtxOffset = 0;
			int idxOffset = 0;

			for (int n = 0; n < drawData.CmdListsCount; n++)
			{
				var cmdList = drawData.CmdLists[n];
				for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
				{
					var drawCmd = cmdList.CmdBuffer[cmdi];
					if (!_loadedTextures.ContainsKey(drawCmd.TextureId))
					{
						throw new InvalidOperationException(
							$"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
					}

					Core.GraphicsDevice.ScissorRectangle = new Rectangle(
						(int)drawCmd.ClipRect.X,
						(int)drawCmd.ClipRect.Y,
						(int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
						(int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
					);

					var effect = UpdateEffect(_loadedTextures[drawCmd.TextureId]);
					foreach (var pass in effect.CurrentTechnique.Passes)
					{
						pass.Apply();

#pragma warning disable CS0618 // FNA does not expose an alternative method.
						Core.GraphicsDevice.DrawIndexedPrimitives(
							primitiveType: PrimitiveType.TriangleList,
							baseVertex: vtxOffset,
							minVertexIndex: 0,
							numVertices: cmdList.VtxBuffer.Size,
							startIndex: idxOffset,
							primitiveCount: (int)drawCmd.ElemCount / 3
						);
#pragma warning restore CS0618
					}

					idxOffset += (int)drawCmd.ElemCount;
				}

				vtxOffset += cmdList.VtxBuffer.Size;
			}
		}

		#endregion
	}
}