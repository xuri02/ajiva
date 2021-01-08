﻿using System;
using System.Collections.Generic;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.EngineManagers
{
    public class ImageManager : IEngineManager
    {
        private readonly IEngine engine;

        public ManagedImage DepthImage { get; set; }

        public List<ManagedImage> Images { get; set; }

        public ImageManager(IEngine engine)
        {
            this.engine = engine;
        }

        public ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags)
        {
            engine.Dependent(engine.DeviceManager.Device, nameof(engine.DeviceManager.Device));
            return engine.DeviceManager.Device.CreateImageView(image, ImageViewType.ImageView2d, format, ComponentMapping.Identity, new()
            {
                AspectMask = aspectFlags,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            });
        }

        public void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, out Image image, out DeviceMemory imageMemory)
        {
            var device = engine.DeviceManager.Device;

            image = device.CreateImage(ImageType.Image2d, format, new Extent3D(width, height, 1), 1, 1, SampleCountFlags.SampleCount1, tiling, usage, SharingMode.Exclusive, ArrayProxy<uint>.Null, ImageLayout.Undefined);

            var memRequirements = device.GetImageMemoryRequirements2(new()
            {
                Image = image
            });

            imageMemory = device.AllocateMemory(memRequirements.MemoryRequirements.Size, engine.DeviceManager.FindMemoryType(memRequirements.MemoryRequirements.MemoryTypeBits, properties), new()
            {
                Image = image,
            });

            device.BindImageMemory2(new BindImageMemoryInfo
            {
                Image = image,
                Memory = imageMemory,
                MemoryOffset = 0,
            });
        }

        public Format FindDepthFormat()
        {
            return FindSupportedFormat(new[]
                {
                    Format.D32SFloat, Format.D32SFloatS8UInt, Format.D24UNormS8UInt
                },
                ImageTiling.Optimal,
                FormatFeatureFlags.DepthStencilAttachment
                //VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT
            );
        }

        private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (var format in candidates)
            {
                var props = engine.DeviceManager.PhysicalDevice.GetFormatProperties(format);

                switch (tiling)
                {
                    case ImageTiling.Linear when (props.LinearTilingFeatures & features) == features:
                        return format;
                    case ImageTiling.Optimal when (props.OptimalTilingFeatures & features) == features:
                        return format;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tiling), tiling, "failed to find supported format!");
                }
            }

            throw new ArgumentOutOfRangeException(nameof(candidates), candidates, "failed to find supported format!");
        }

        public void CreateDepthResources()
        {
            DepthImage = CreateManagedImage(FindDepthFormat(), ImageAspectFlags.Depth);

            /*
            var depthFormat =;

            ImageManager.CreateImage(engine.SwapChainManager.swapChainExtent.Width, engine.SwapChainManager.swapChainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachment, MemoryPropertyFlags.DeviceLocal, out depthImage, out depthImageMemory);
            depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.Depth);

            TransitionImageLayout(depthImage, depthFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
        */
        }

        private ManagedImage CreateManagedImage(Format format, ImageAspectFlags aspectFlags)
        {
            var managed = new ManagedImage();

            CreateImage(engine.SwapChainManager.SwapChainExtent.Width, engine.SwapChainManager.SwapChainExtent.Height, format, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachment, MemoryPropertyFlags.DeviceLocal, out managed.Image, out managed.Memory);
            managed.View = CreateImageView(managed.Image, format, aspectFlags);

            TransitionImageLayout(managed.Image, format, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
            return managed;
        }

        #region imageHelp

        bool HasStencilComponent(Format format)
        {
            return format == Format.D32SFloatS8UInt || format == Format.D24UNormS8UInt;
        }

        public void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
        {
            engine.DeviceManager.SingleTimeCommand(command =>
            {
                command.CopyBufferToImage(buffer, image, ImageLayout.TransferDestinationOptimal, new BufferImageCopy()
                {
                    BufferOffset = 0,
                    BufferRowLength = 0,
                    BufferImageHeight = 0,
                    ImageOffset = new(),
                    ImageExtent = new(width, height, 0),
                    ImageSubresource = new()
                    {
                        AspectMask = ImageAspectFlags.Color,
                        MipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                });
            });
            var commandBuffer = engine.DeviceManager.BeginSingleTimeCommands();

            commandBuffer.CopyBufferToImage(buffer, image, ImageLayout.TransferDestinationOptimal, new BufferImageCopy()
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageOffset = new(),
                ImageExtent = new(width, height, 0),
                ImageSubresource = new()
                {
                    AspectMask = ImageAspectFlags.Color,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }
            });

            engine.DeviceManager.EndSingleTimeCommands(commandBuffer);
        }

        public void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
        {
            ImageSubresourceRange subresourceRange = new()
            {
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            };

            if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                subresourceRange.AspectMask = ImageAspectFlags.Depth;

                if (HasStencilComponent(format)) subresourceRange.AspectMask |= ImageAspectFlags.Stencil;
            }
            else
            {
                subresourceRange.AspectMask = ImageAspectFlags.Color;
            }

            var barrier = new ImageMemoryBarrier
            {
                OldLayout = oldLayout,
                NewLayout = newLayout,
                Image = image,
                SubresourceRange = subresourceRange,
            };

            //barrier.SourceQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
            //barrier.DestinationQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;

            PipelineStageFlags sourceStage;
            PipelineStageFlags destinationStage;

            switch (oldLayout)
            {
                case ImageLayout.Undefined when newLayout == ImageLayout.TransferDestinationOptimal:
                    barrier.SourceAccessMask = 0;
                    barrier.DestinationAccessMask = AccessFlags.TransferWrite;

                    sourceStage = PipelineStageFlags.TopOfPipe;
                    destinationStage = PipelineStageFlags.Transfer;
                    break;
                case ImageLayout.TransferDestinationOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                    barrier.SourceAccessMask = AccessFlags.TransferWrite;
                    barrier.DestinationAccessMask = AccessFlags.ShaderRead;

                    sourceStage = PipelineStageFlags.Transfer;
                    destinationStage = PipelineStageFlags.FragmentShader;
                    break;
                case ImageLayout.Undefined when newLayout == ImageLayout.DepthStencilAttachmentOptimal:
                    barrier.SourceAccessMask = 0;
                    barrier.DestinationAccessMask = AccessFlags.DepthStencilAttachmentRead | AccessFlags.DepthStencilAttachmentWrite;

                    sourceStage = PipelineStageFlags.TopOfPipe;
                    destinationStage = PipelineStageFlags.EarlyFragmentTests;
                    break;
                default:
                    throw new ArgumentException("unsupported layout transition!");
            }

            engine.DeviceManager.SingleTimeCommand(command => command.PipelineBarrier(sourceStage, destinationStage, ArrayProxy<MemoryBarrier>.Null, ArrayProxy<BufferMemoryBarrier>.Null, barrier));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DepthImage.Dispose();
            foreach (var managedImage in Images)
            {
                managedImage.Dispose();
            }
        }

  #endregion
    }
}