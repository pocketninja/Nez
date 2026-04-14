using System;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez.Sprites;
using Nez.Textures;
using NVector2 = System.Numerics.Vector2;

namespace Nez.ImGuiTools.SpriteWindows
{
	public class AtlasSpriteWindowRenderer : Renderer
	{
		public const int DefaultWindowPhysicsLayer = 100_000;

		public int WindowPhysicsLayer = DefaultWindowPhysicsLayer;

		public AtlasSpriteWindowSystem AtlasSystem { get; protected set; }
		public RenderTarget2D Atlas;

		public int AtlasWidth = 4096;
		public int AtlasHeight = 4096;

		/// <summary>
		/// Width and height of the tiles.
		/// </summary>
		public int TileSize = 500;

		protected AbstractAtlasSpriteWindowComponent[] Slots;
		protected NVector2[] RenderedSlotWindowSize;

		protected Collider[] MouseOverWindowColliders = new Collider[3];
		protected AbstractAtlasSpriteWindowComponent[] MouseOverWindows = new AbstractAtlasSpriteWindowComponent[3];

		protected ImGuiOptions Options;


		public AtlasSpriteWindowRenderer(ImGuiOptions options, int renderOrder = 0) : base(renderOrder)
		{
			Options = options;
		}


		public AtlasSpriteWindowRenderer(int renderOrder = 0) : base(renderOrder)
		{
		}

		public AtlasSpriteWindowRenderer(int renderOrder, Camera camera) : base(renderOrder, camera)
		{
		}

		public static AtlasSpriteWindowRenderer ResolveCurrentWindowRenderer(
			Scene scene = null,
			int physicsLayer = DefaultWindowPhysicsLayer
		)
		{
			scene = scene ?? Core.Scene;
			var renderers = scene._renderers;

			AtlasSpriteWindowRenderer renderer = null;

			for (int i = 0; i < renderers.Length; i++)
			{
				if (renderers[i] is AtlasSpriteWindowRenderer atlasRenderer)
				{
					if (atlasRenderer.WindowPhysicsLayer != physicsLayer)
					{
						continue;
					}

					renderer = atlasRenderer;
					break;
				}
			}

			if (renderer == null)
			{
				System.Console.WriteLine(
					$"!! AtlasSpriteWindowRenderer(physicsLayer:{physicsLayer}) was not present on the scene. It has been added, though the scene should do this explicitly."
				);
				renderer = Core.Scene.AddRenderer(new AtlasSpriteWindowRenderer()
				{
					WindowPhysicsLayer = physicsLayer
				});
			}

			return renderer;
		}

		public override void OnAddedToScene(Scene scene)
		{
			base.OnAddedToScene(scene);
			AtlasSystem = new AtlasSpriteWindowSystem(Core.Instance);

			//TODO: Provide a way to pass in ImGuiOptions...
			AtlasSystem.Initialize(Options ?? new ImGuiOptions());

			SetupTheme();

			InitializeTexture();
		}

		protected virtual void SetupTheme()
		{
			NezImGuiThemes.DarkHighContrastTheme();
		}

		protected void InitializeTexture()
		{
			Atlas?.Dispose();

			System.Console.WriteLine(
				$"Initialising new ImGui Atlas: {AtlasWidth}x{AtlasHeight}, tile size: {TileSize}");

			Atlas = RenderTarget.Create(AtlasWidth, AtlasHeight);

			var tileCount = Math.Floor((float)AtlasWidth / TileSize) * Math.Floor((float)AtlasHeight / TileSize);

			var currentSlots = Slots;
			var size = (int)tileCount;

			Slots = new AbstractAtlasSpriteWindowComponent[size];

			System.Console.WriteLine($"...atlas has {tileCount} slots");

			if (currentSlots != null)
			{
				if (currentSlots.Length > Slots.Length)
				{
					System.Console.WriteLine($"Slots list may be pruned - it was longer than the new atlas allows");
				}

				var limit = Math.Min(currentSlots.Length, Slots.Length);

				for (int i = 0; i < limit; i++)
				{
					Slots[i] = currentSlots[i];
				}
			}

			// Rebuild the window size cache
			RenderedSlotWindowSize = new NVector2[size];
			for (int i = 0; i < size; i++)
			{
				RenderedSlotWindowSize[i] = NVector2.Zero;
			}
		}

		public override void Render(Scene scene)
		{
			if (Slots.Length == 0)
			{
				return;
			}

			var currentContext = ImGui.GetCurrentContext();
			ImGui.SetCurrentContext(AtlasSystem.Context);

			var currentTargets = Core.GraphicsDevice.GetRenderTargets();

			// In ImGuiRenderer.RenderDrawData(), it grabs the size of the back buffer explicitly, rather than the 
			// current render target. Ideally it'd use the current target in some way, but we don't want to mess with
			// that too much right now, so we'll flip it out for our render, and restore it.
			var originalPresentationWidth = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
			var originalPresentationHeight = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;

			Core.GraphicsDevice.PresentationParameters.BackBufferWidth = AtlasWidth;
			Core.GraphicsDevice.PresentationParameters.BackBufferHeight = AtlasHeight;

			Core.GraphicsDevice.SetRenderTarget(Atlas);
			Core.GraphicsDevice.Clear(Color.Transparent);

			BeforeLayout();

			bool deliveredInput = false;

			MouseState mouse = Nez.Input.CurrentMouseState;
			Point mouseWindowPosition = mouse.Position;
			Vector2 mouseWorldPosition = Core.Scene.Camera.ScreenToWorldPoint(mouseWindowPosition);


			for (int i = 0; i < MouseOverWindowColliders.Length; i++)
			{
				MouseOverWindowColliders[i] = null;
				MouseOverWindows[i] = null;
			}

			var hit = Physics.OverlapCircleAll(
				mouseWorldPosition, 2f,
				MouseOverWindowColliders,
				WindowPhysicsLayer
			);

			int mouseOverWindows = 0;

			if (hit > 0)
			{
				for (int i = 0; i < hit; i++)
				{
					var candidate = MouseOverWindowColliders[i]
						.Entity
						.GetComponent<AbstractAtlasSpriteWindowComponent>();

					MouseOverWindows[i] = candidate;

					if (candidate != null)
					{
						mouseOverWindows++;
					}
				}

				MouseOverWindows = MouseOverWindows.OrderBy(window => window?.RenderLayer ?? 999999999).ToArray();
			}

			for (int slotIndex = 0; slotIndex < Slots.Length; slotIndex++)
			{
				var window = Slots[slotIndex];

				if (window == null)
				{
					continue;
				}

				var slotRect = GetSlotSourceRect(slotIndex);

				// The window had no rect, there might be too many windows!
				if (slotRect.IsEmpty)
				{
					continue;
				}

				if (mouseOverWindows > 0 && window.ShouldReceiveInput && !deliveredInput)
				{
					for (int x = 0; x < MouseOverWindows.Length; x++)
					{
						if (MouseOverWindows[x] == window)
						{
							deliveredInput = true;
							ImGui.SetNextWindowFocus();
							AtlasSystem.SendMouseInput(window, slotRect);
							break;
						}

						// For now, only take the top-most window (by render layer)... Maybe in the future we can handle multiple, but 
						// with the way the atlas works that probably doesn't make any sense.
						break;
					}
				}

				// AtlasSystem.SendKeyboardInput(window);
				// AtlasSystem.SendGamepadInput(window);

				ImGui.SetNextWindowPos(new NVector2(slotRect.X, slotRect.Y));

				// Make sure windows are constrained to the atlas tile size.
				ImGui.SetNextWindowSizeConstraints(
					NVector2.Zero,
					new NVector2(TileSize, TileSize)
				);

				// If the window is not auto-sizing, set the size explicitly.
				if (!window.WindowFlags.HasFlag(ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.SetNextWindowSize(new NVector2(
						window.WindowWidth,
						window.WindowHeight
					));
				}

				ImGui.Begin(window.Entity.Name, window.WindowFlags);

				window.RenderImGuiWindow();

				var windowSize = ImGui.GetWindowSize();
				var lastWindowSize = RenderedSlotWindowSize[slotIndex];

				if (lastWindowSize.X != windowSize.X || lastWindowSize.Y != windowSize.Y)
				{
					RenderedSlotWindowSize[slotIndex] = windowSize;

					var rect = GetSlotSourceRect(slotIndex);
					rect.Width = (int)windowSize.X;
					rect.Height = (int)windowSize.Y;

					// TODO: Check perf of this - this seems bad, but there's no way currently to resize a sprite.
					// Could potentially change the origin and leave it at that?
					window.SpriteRenderer.SetSprite(new Sprite(Atlas, rect));

					if (window.Collider is BoxCollider box)
					{
						box.SetSize(rect.Width, rect.Height);
					}
				}

				ImGui.End();
			}

			AfterLayout();

			ImGui.SetCurrentContext(currentContext);

			Core.GraphicsDevice.PresentationParameters.BackBufferWidth = originalPresentationWidth;
			Core.GraphicsDevice.PresentationParameters.BackBufferHeight = originalPresentationHeight;

			Core.GraphicsDevice.SetRenderTargets(currentTargets);
		}

		protected int AvailableSlotIndex()
		{
			for (int i = 0; i < Slots.Length; i++)
			{
				if (Slots[i] == null)
				{
					return i;
				}
			}

			return -1;
		}

		protected Rectangle GetSlotSourceRect(int slotIndex)
		{
			if (slotIndex < 0 || slotIndex >= Slots.Length)
			{
				return Rectangle.Empty;
			}

			var tilesPerRow = AtlasWidth / TileSize;

			var x = slotIndex % tilesPerRow;
			var y = slotIndex / tilesPerRow;

			return new Rectangle(
				x * TileSize,
				y * TileSize,
				TileSize,
				TileSize
			);
		}

		public SpriteRenderer MakeAllocatedSprite(AbstractAtlasSpriteWindowComponent component)
		{
			var entity = component.Entity;

			var slotIndex = AvailableSlotIndex();

			if (slotIndex < 0)
			{
				System.Console.WriteLine($"Could not find an available atlas slot for window {entity.Id}");
			}
			else
			{
				Slots[slotIndex] = component;
			}

			// The rect is the slot's rect in the atlas - we need to update its width to the window so
			// we don't overdraw.
			var rect = GetSlotSourceRect(slotIndex);

			// If a valid rect was returned, there was a viable slot! Otherwise leave it empty.
			if (!rect.IsEmpty)
			{
				rect.Width = Math.Min(rect.Width, (int)component.WindowWidth);
				rect.Height = Math.Min(rect.Height, (int)component.WindowHeight);
			}

			var sprite = new Sprite(Atlas, rect);

			var spriteRenderer = new SpriteRenderer(sprite);
			spriteRenderer.SetColor(component.Color);
			spriteRenderer.RenderLayer = component.RenderLayer;
			spriteRenderer.LayerDepth = component.LayerDepth;

			return spriteRenderer;
		}

		public void DeallocateFromAtlas(AbstractAtlasSpriteWindowComponent component)
		{
			for (int i = 0; i < Slots.Length; i++)
			{
				if (Slots[i] == component)
				{
					RenderedSlotWindowSize[i] = NVector2.Zero;
					Slots[i] = null;
					return;
				}
			}

			System.Console.WriteLine(
				$"Tried to deallocate an atlas slot for window {component.Entity.Id}, but it was not in a slot!"
			);
		}

		protected virtual void BeforeLayout()
		{
			var io = ImGui.GetIO();
			ImGui.GetIO().DeltaTime = Time.DeltaTime;

			io.DisplaySize = new System.Numerics.Vector2(AtlasWidth, AtlasHeight);
			io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);

			ImGui.NewFrame();
		}

		protected virtual void AfterLayout()
		{
			AtlasSystem.Renderer.AfterLayout();
		}
	}
}