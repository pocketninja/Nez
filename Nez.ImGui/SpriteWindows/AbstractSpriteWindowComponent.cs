using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez.Sprites;
using Nez.Textures;
using Vector2 = System.Numerics.Vector2;

namespace Nez.ImGuiTools.SpriteWindows
{
	public class AbstractSpriteWindowComponent : RenderableComponent, IUpdatable
	{
		public bool RenderImGuiToTexture = true;
		
		public virtual float WindowWidth => 300;
		public virtual float WindowHeight => 200;

		public sealed override float Width => WindowWidth;
		public sealed override float Height => WindowHeight;

		protected virtual ImGuiWindowFlags WindowFlags =>
			ImGuiWindowFlags.NoCollapse
			| ImGuiWindowFlags.NoResize
			| ImGuiWindowFlags.NoTitleBar
			| ImGuiWindowFlags.NoMove
			| ImGuiWindowFlags.NoBringToFrontOnFocus;

		protected RenderTarget2D RenderTarget = null;
		private SpriteRenderer _spriteRenderer;

		public virtual bool DrawMouseCursor => false;

		/// <summary>
		/// lower renderLayers are in the front and higher are in the back, just like layerDepth but not clamped to 0-1. Note that this means
		/// higher renderLayers are sent to the Batcher first. An important fact when using the stencil buffer.
		/// </summary>
		/// <value>The render layer.</value>
		public new int RenderLayer
		{
			get => _renderLayer;
			set => SetRenderLayer(value);
		}

		/// <summary>
		/// lower renderLayers are in the front and higher are in the back, just like layerDepth but not clamped to 0-1. Note that this means
		/// higher renderLayers are sent to the Batcher first. An important fact when using the stencil buffer.
		/// </summary>
		/// <returns>The render layer.</returns>
		/// <param name="renderLayer">Render layer.</param>
		public new RenderableComponent SetRenderLayer(int renderLayer)
		{
			if (renderLayer != _renderLayer && _spriteRenderer != null)
			{
				_spriteRenderer.RenderLayer = renderLayer;
			}

			base.SetRenderLayer(renderLayer);

			return this;
		}

		public new RenderableComponent SetColor(Color color)
		{
			base.SetColor(color);
			_spriteRenderer?.SetColor(color);
			return this;
		}

		protected SpriteWindowRenderer ResolveWindowRenderer()
		{
			var renderer = Core.Scene.GetRenderer<SpriteWindowRenderer>();

			if (renderer == null)
			{
				System.Console.WriteLine(
					"!! SpriteWindowRenderer was not present on the scene. It has been added, though the scene should do this explicitly."
				);
				renderer = Core.Scene.AddRenderer(new SpriteWindowRenderer(-100));
			}

			return renderer;
		}

		public virtual void SetupStyle()
		{
			NezImGuiThemes.DarkTheme2();
		}

		public override void OnAddedToEntity()
		{
			base.OnAddedToEntity();

			RenderTarget = ResolveWindowRenderer().ResolveRenderTarget(this);

			var sprite = new Sprite(RenderTarget);
			_spriteRenderer = new SpriteRenderer(sprite);
			_spriteRenderer.SetColor(Color);
			_spriteRenderer.RenderLayer = RenderLayer;
			Entity.AddComponent(_spriteRenderer);
		}

		public virtual void RenderImGuiWindow()
		{
			SetWindowPositionAndSize();
			ImGui.Begin(Entity.Name, WindowFlags);
			RenderUi();
			ImGui.End();
		}

		protected virtual void SetWindowPositionAndSize()
		{
			ImGui.SetNextWindowSize(new Vector2(WindowWidth, WindowHeight));
			ImGui.SetNextWindowPos(new Vector2(
				0,
				0
			));
		}

		protected virtual void RenderUi()
		{
			ImGui.Text("This component did not declare any UI...");
			ImGui.Text("FPS: " + (int)(1f / Time.DeltaTime));
			ImGui.Text("S: " + Screen.Width + "x" + Screen.Height);
		}

		public override void Render(Batcher batcher, Camera camera)
		{
			// Noop...
		}

		public virtual void Update()
		{
		}
	}
}