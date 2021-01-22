﻿using ajiva.Systems.VulcanEngine.Engine;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class GraphicsComponent : RenderEngineComponent
    {
        public GraphicsLayout? Current { get; private set; }

        public GraphicsComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            Current = new(renderEngine);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            EnsureGraphicsLayoutDeletion();
        }

        public void EnsureGraphicsLayoutExists()
        {
            Current ??= new(RenderEngine);
        }

        public void EnsureGraphicsLayoutDeletion()
        {
            Current?.Dispose();
            Current = null;
        }

        public void RecreateCurrentGraphicsLayout()
        {
            EnsureGraphicsLayoutDeletion();
            EnsureGraphicsLayoutExists();
        }
    }
}
