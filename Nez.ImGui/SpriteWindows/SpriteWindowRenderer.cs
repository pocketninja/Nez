using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez.Textures;

namespace Nez.ImGuiTools.SpriteWindows
{
	public class SpriteWindowRenderer : Renderer
	{
		protected Dictionary<uint, RenderTarget2D> Targets = new Dictionary<uint, RenderTarget2D>();
		protected Dictionary<uint, IntPtr> Contexts = new Dictionary<uint, IntPtr>();
		protected unsafe ImFontAtlasPtr SharedFontAtlas;

		public SpriteWindowRenderer(int renderOrder) : base(renderOrder)
		{
		}

		public SpriteWindowRenderer(int renderOrder, Camera camera) : base(renderOrder, camera)
		{
		}

		public override void OnAddedToScene(Scene scene)
		{
			base.OnAddedToScene(scene);
			SpriteWindowSystem.Initialize(Core.Instance);
			// @TODO Maybe draw the mouse cursor ourselves? Old code at end of file... 
		}

		public override void Render(Scene scene)
		{
			var cam = Camera ?? scene.Camera;
			BeginRender(cam);

			var currentTargets = Core.GraphicsDevice.GetRenderTargets();

			for (var i = 0; i < scene.RenderableComponents.Count; i++)
			{
				var renderable = scene.RenderableComponents[i];
				if (renderable is AbstractSpriteWindowComponent window)
				{
					ImGui.SetCurrentContext(ResolveWindowContext(window));

					Core.GraphicsDevice.SetRenderTarget(ResolveRenderTarget(window));
					Core.GraphicsDevice.Clear(Color.Transparent);

					SpriteWindowSystem.BeforeLayout(Time.DeltaTime, window);
					window.RenderImGuiWindow();
					SpriteWindowSystem.AfterLayout();
				}
			}

			Core.GraphicsDevice.SetRenderTargets(currentTargets);

			EndRender();
		}

		public unsafe IntPtr ResolveWindowContext(AbstractSpriteWindowComponent window)
		{
			if (Contexts.TryGetValue(window.Entity.Id, out var context))
			{
				return context;
			}

			var newContext = ImGui.CreateContext();

			// Need to switch temporarily to configure its IO...
			var currentContext = ImGui.GetCurrentContext();
			ImGui.SetCurrentContext(newContext);
			SpriteWindowSystem.SetupStyle();

			var io = ImGui.GetIO();

			if (SharedFontAtlas.NativePtr == null)
			{
				SharedFontAtlas = io.Fonts;
				SharedFontAtlas.Build();
			}
			else
			{
				io.NativePtr->Fonts = SharedFontAtlas.NativePtr;
			}

			ImGui.SetCurrentContext(currentContext);

			Contexts[window.Entity.Id] = newContext;

			return newContext;
		}

		public RenderTarget2D ResolveRenderTarget(AbstractSpriteWindowComponent window)
		{
			if (Targets.TryGetValue(window.Entity.Id, out var target))
			{
				return target;
			}

			var newTarget = RenderTarget.Create(
				(int)window.WindowWidth,
				(int)window.WindowHeight
			);

			Targets[window.Entity.Id] = newTarget;

			return newTarget;
		}
	}
}