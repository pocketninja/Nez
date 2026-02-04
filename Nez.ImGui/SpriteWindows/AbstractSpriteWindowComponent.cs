using System.Numerics;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using Nez.Sprites;
using Nez.Textures;

namespace Nez.ImGuiTools.SpriteWindows
{
	public class AbstractSpriteWindowComponent : RenderableComponent, IUpdatable
	{
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

		public override void OnAddedToEntity()
		{
			base.OnAddedToEntity();

			RenderTarget = ResolveWindowRenderer().ResolveRenderTarget(this);

			var sprite = new Sprite(RenderTarget);
			var spriteRenderer = new SpriteRenderer(sprite);
			Entity.AddComponent(spriteRenderer);
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