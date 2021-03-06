﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Helpers;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(ImageSystem))]
    public class TextureSystem : ComponentSystemBase<ATexture>, IInit
    {
        // ReSharper disable once InconsistentNaming
        public const int MAX_TEXTURE_SAMPLERS_IN_SHADER = 128;

        public TextureSystem()
        {
            INextId<ATexture>.MaxId = MAX_TEXTURE_SAMPLERS_IN_SHADER;
            TextureSamplerImageViews = new DescriptorImageInfo[MAX_TEXTURE_SAMPLERS_IN_SHADER];
            Textures = new();
        }

        public ATexture? Default { get; private set; }
        private List<ATexture> Textures { get; }
        public DescriptorImageInfo[] TextureSamplerImageViews { get; }

        public void AddAndMapTextureToDescriptor(ATexture texture)
        {
            MapTextureToDescriptor(texture);
            Textures.Add(texture);
        }

        public void MapTextureToDescriptor(ATexture texture)
        {
            if (MAX_TEXTURE_SAMPLERS_IN_SHADER <= texture.TextureId) throw new ArgumentException($"{nameof(texture.TextureId)} is more then {nameof(MAX_TEXTURE_SAMPLERS_IN_SHADER)}", nameof(IBindCtx));

            TextureSamplerImageViews[texture.TextureId] = texture.DescriptorImageInfo;
        }

        /// <inheritdoc />
        public override ATexture CreateComponent(IEntity entity)
        {
            return new ATexture();
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterInit(this);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            for (var i = 0; i < MAX_TEXTURE_SAMPLERS_IN_SHADER; i++)
            {
                TextureSamplerImageViews[i] = default;
            }
            foreach (var texture in Textures)
            {
                texture.Dispose();
            }
        }

        public void EnsureDefaultImagesExists(AjivaEcs ecs)
        {
            if (Default != null) return;

            Default = ATexture.FromFile(ecs, "logo.png");
            Textures.Add(Default);

            for (var i = 0; i < MAX_TEXTURE_SAMPLERS_IN_SHADER; i++)
            {
                TextureSamplerImageViews[i] = Default.DescriptorImageInfo;
            }

            /* todo move int hot load
             AddAndMapTextureToDescriptor(new(1)
            {
                Image = CreateTextureImageFromFile("logo2.png"),
                Sampler = CreateTextureSampler()
            });*/
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            EnsureDefaultImagesExists(ecs);
        }
    }
}
