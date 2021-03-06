﻿using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;

namespace ajiva.Factories
{
    public class RectFactory : EntityFactoryBase<Rect>
    {
        /// <inheritdoc />
        public override Rect Create(AjivaEcs system, uint id)
        {
            var rect = new Rect();
            system.AttachComponentToEntity<ARenderAble2D>(rect);
            //system.AttachComponentToEntity<ATexture>(cube);
            return rect;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            
        }
    }
}
