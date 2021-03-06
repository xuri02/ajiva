﻿using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Entities;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using GlmSharp;
using SharpVk.Glfw;

namespace ajiva.Systems.VulcanEngine
{
    [Dependent(typeof(WindowSystem))]
    public class Ajiva3dSystem : ComponentSystemBase<ARenderAble3D>, IInit, IUpdate
    {
        public Cameras.Camera MainCamara
        {
            get => mainCamara;
            set
            {
                mainCamara?.Dispose();
                mainCamara = value;
            }
        }

        private Cameras.Camera? mainCamara;

        private void UpdateCamaraProjView(ShaderSystem shaderSystem)
        {
            var updateCtr = false;
            shaderSystem.ShaderUnions[PipelineName.PipeLine3d].ViewProj.UpdateExpresion(delegate(int index, ref UniformViewProj value)
            {
                if (index != 0) return;

                if (value.View != mainCamara!.View)
                {
                    updateCtr = true;
                    value.View = MainCamara.View;
                }
                if (value.Proj == MainCamara.Projection) return;

                updateCtr = true;
                value.Proj = MainCamara.Projection;
                value.Proj[1, 1] *= -1;
            });
            if (updateCtr)
                shaderSystem.ShaderUnions[PipelineName.PipeLine3d].ViewProj.Copy();
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            ShaderSystem shaderSystem = Ecs.GetSystem<ShaderSystem>();

            var updated = new List<uint>();
            foreach (var (renderAble, entity) in ComponentEntityMap)
            {
                var update = false;
                Transform3d? transform = null!;
                //ATexture? texture = null!;
                if (entity.TryGetComponent(out transform) && transform!.Dirty)
                {
                    shaderSystem.ShaderUnions[PipelineName.PipeLine3d].UniformModels.Staging.Value[renderAble!.Id].Model = transform.ModelMat; // texture?.TextureId ?? 0
                    update = true;
                    transform!.Dirty = false;
                }
                if (renderAble.Dirty /*entity.TryGetComponent(out texture) && texture!.Dirty ||*/)
                {
                    shaderSystem.ShaderUnions[PipelineName.PipeLine3d].UniformModels.Staging.Value[renderAble!.Id].TextureSamplerId = renderAble!.Id; // texture?.TextureId ?? 0
                    update = true;
                    renderAble.Dirty = false;
                }

                if (update)
                    updated.Add(renderAble.Id);
            }

            if (updated.Any())
                lock (MainLock)
                    shaderSystem.ShaderUnions[PipelineName.PipeLine3d].UniformModels.CopyRegions(updated);

            lock (MainLock)
                UpdateCamaraProjView(shaderSystem);
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            var window = Ecs.GetSystem<WindowSystem>();

            window.OnResize += (_, eventArgs) =>
            {
                lock (MainLock)
                {
                    foreach (var entity in ComponentEntityMap)
                    {
                        entity.Key.Dirty = true;
                    }

                    mainCamara?.UpdatePerspective(mainCamara.Fov, window.Width, window.Height);
                }
            };

            window.OnKeyEvent += delegate(object? _, Key key, int scancode, InputAction action, Modifier modifier)
            {
                var down = action != InputAction.Release;

                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (key)
                {
                    case Key.W:
                        MainCamara.Keys.up = down;
                        break;
                    case Key.D:
                        MainCamara.Keys.right = down;
                        break;
                    case Key.S:
                        MainCamara.Keys.down = down;
                        break;
                    case Key.A:
                        MainCamara.Keys.left = down;
                        break;
                }
            };

            window.OnMouseMove += delegate(object? _, vec2 vec2)
            {
                MainCamara.OnMouseMoved(vec2.x, vec2.y);
            };
        }

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterInit(this);
            Ecs.RegisterUpdate(this);
        }

        /// <inheritdoc />
        public override ARenderAble3D CreateComponent(IEntity entity)
        {
            var rnd = new ARenderAble3D();
            ComponentEntityMap.Add(rnd, entity);
            return rnd;
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        private object MainLock { get; } = new();

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            lock (MainLock)
            {
                Ecs.GetSystem<DeviceSystem>().WaitIdle();
                mainCamara?.Dispose();
            }
            base.ReleaseUnmanagedResources();
        }
    }
}
