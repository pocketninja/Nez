using ImGuiNET;
using Microsoft.Xna.Framework;
using Nez.Sprites;
using Nez.Textures;

namespace Nez.ImGuiTools.SpriteWindows
{
	public abstract class AbstractAtlasSpriteWindowComponent : RenderableComponent, IUpdatable
	{
		public virtual bool ShouldReceiveInput => true;

		public virtual float WindowWidth => 300;
		public virtual float WindowHeight => 200;

		public sealed override float Width => WindowWidth;
		public sealed override float Height => WindowHeight;

		public virtual int WindowPhysicsLayer => AtlasSpriteWindowRenderer.DefaultWindowPhysicsLayer;

		public virtual ImGuiWindowFlags WindowResizeMode => ImGuiWindowFlags.NoResize;

		public virtual ImGuiWindowFlags WindowFlags =>
			ImGuiWindowFlags.NoCollapse
			| WindowResizeMode
			| ImGuiWindowFlags.NoTitleBar
			| ImGuiWindowFlags.NoMove
			| ImGuiWindowFlags.NoBringToFrontOnFocus;

		public WindowAnchor Anchor = WindowAnchor.Center;

		public SpriteRenderer SpriteRenderer { get; protected set; }
		public Collider Collider { get; protected set; }

		public virtual bool DrawMouseCursor => true;

		public new int RenderLayer
		{
			get => _renderLayer;
			set => SetRenderLayer(value);
		}

		public new float LayerDepth
		{
			get => _layerDepth;
			set => SetLayerDepth(value);
		}

		/// <summary>
		/// lower renderLayers are in the front and higher are in the back, just like layerDepth but not clamped to 0-1. Note that this means
		/// higher renderLayers are sent to the Batcher first. An important fact when using the stencil buffer.
		/// </summary>
		/// <returns>The render layer.</returns>
		/// <param name="renderLayer">Render layer.</param>
		public new RenderableComponent SetRenderLayer(int renderLayer)
		{
			if (renderLayer != _renderLayer && SpriteRenderer != null)
			{
				SpriteRenderer.RenderLayer = renderLayer;
			}

			base.SetRenderLayer(renderLayer);

			return this;
		}

		public new RenderableComponent SetLayerDepth(float depth)
		{
			if (depth != _layerDepth && SpriteRenderer != null)
			{
				SpriteRenderer.LayerDepth = depth;
			}

			base.SetLayerDepth(depth);

			return this;
		}

		public new RenderableComponent SetColor(Color color)
		{
			base.SetColor(color);
			SpriteRenderer?.SetColor(color);
			return this;
		}

		public virtual void SetupStyle()
		{
			NezImGuiThemes.DarkTheme2();
		}

		public override void OnAddedToEntity()
		{
			base.OnAddedToEntity();

			AtlasSpriteWindowRenderer renderer =
				AtlasSpriteWindowRenderer.ResolveCurrentWindowRenderer(Entity.Scene, WindowPhysicsLayer);
			SpriteRenderer = renderer.MakeAllocatedSprite(this);
			Entity.AddComponent(SpriteRenderer);

			Collider = new BoxCollider(SpriteRenderer.Sprite.SourceRect.Width, SpriteRenderer.Sprite.SourceRect.Height);
			Collider.IsTrigger = true;
			// TODO: Should this be some other value to prevent any collisions?
			Collider.CollidesWithLayers = WindowPhysicsLayer;
			Collider.PhysicsLayer = WindowPhysicsLayer;
			Entity.AddComponent(Collider);
		}

		public override void OnRemovedFromEntity()
		{
			base.OnRemovedFromEntity();

			AtlasSpriteWindowRenderer renderer =
				AtlasSpriteWindowRenderer.ResolveCurrentWindowRenderer(Entity.Scene, WindowPhysicsLayer);
			renderer.DeallocateFromAtlas(this);
		}

		public override void Render(Batcher batcher, Camera camera)
		{
			// Noop...
		}

		public virtual void Update()
		{
		}

		public virtual void RenderImGuiWindow()
		{
			RenderUi();
		}

		protected virtual void RenderUi()
		{
			ImGui.Text("This AtlasSpriteWindowComponent did not declare any UI...");
			ImGui.Text("FPS: " + (int)(1f / Time.DeltaTime));
			ImGui.Text("S: " + Screen.Width + "x" + Screen.Height);
		}

		public void UpdateRects(Rectangle newRect)
		{
			// TODO: Check perf of this - this seems bad, but there's no way currently to resize a sprite.
			// Could potentially change the origin and leave it at that?
			SpriteRenderer.SetSprite(new Sprite(SpriteRenderer.Sprite.Texture2D, newRect));

			Vector2 origin;
			var halfSize = newRect.GetHalfSize();

			switch (Anchor)
			{
				case WindowAnchor.TopLeft:
					origin = Vector2.Zero;
					break;
				case WindowAnchor.TopCenter:
					origin = new Vector2(halfSize.X, 0);
					break;
				case WindowAnchor.TopRight:
					origin = new Vector2(newRect.Width, 0);
					break;
				case WindowAnchor.BottomLeft:
					origin = new Vector2(0, newRect.Height);
					break;
				case WindowAnchor.BottomCenter:
					origin = new Vector2(halfSize.X, newRect.Height);
					break;
				case WindowAnchor.BottomRight:
					origin = new Vector2(newRect.Width, newRect.Height);
					break;
				default:
					origin = halfSize;
					break;
			}

			SpriteRenderer.SetOrigin(origin);

			if (Collider is BoxCollider box)
			{
				box.SetSize(newRect.Width, newRect.Height);

				Vector2 offset;
				switch (Anchor)
				{
					case WindowAnchor.TopLeft:
						offset = new Vector2(halfSize.X, halfSize.Y);
						break;
					case WindowAnchor.TopCenter:
						offset = new Vector2(0, halfSize.Y);
						break;
					case WindowAnchor.TopRight:
						offset = new Vector2(-halfSize.X, halfSize.Y);
						break;
					case WindowAnchor.BottomLeft:
						offset = new Vector2(halfSize.X, -halfSize.Y);
						break;
					case WindowAnchor.BottomCenter:
						offset = new Vector2(0, -halfSize.Y);
						break;
					case WindowAnchor.BottomRight:
						offset = new Vector2(-halfSize.X, -halfSize.Y);
						break;
					default:
						offset = Vector2.Zero;
						break;
				}

				box.SetLocalOffset(offset);
			}
		}
	}
}