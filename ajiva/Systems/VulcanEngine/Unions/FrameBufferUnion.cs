﻿using System.Linq;
using ajiva.Helpers;
using ajiva.Models;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Unions
{
    public class FrameBufferUnion : DisposingLogger
    {
        public CommandPool CommandPool { get; }
        public Framebuffer[] FrameBuffers { get; }
        public CommandBuffer[] RenderBuffers { get; }

        public FrameBufferUnion(CommandPool commandPool, Framebuffer[] frameBuffers, CommandBuffer[] renderBuffers)
        {
            CommandPool = commandPool;
            FrameBuffers = frameBuffers;
            RenderBuffers = renderBuffers;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            CommandPool.FreeCommandBuffers(RenderBuffers);

            foreach (var frameBuffer in FrameBuffers)
            {
                frameBuffer.Dispose();
            }
        }

        public static FrameBufferUnion CreateFrameBufferUnion(SwapChainUnion swapChainRecord, GraphicsPipelineUnion graphicsPipelineUnion, Device device, bool useDepthImage, AImage depthImage, CommandPool commandPool)
        {
            Framebuffer MakeFrameBuffer(ImageView imageView)
            {
                ImageView?[] views = useDepthImage ? new[] {imageView, depthImage!.View} : new[] {imageView};

                return device.CreateFramebuffer(graphicsPipelineUnion.RenderPass,
                    views,
                    swapChainRecord.SwapChainExtent.Width,
                    swapChainRecord.SwapChainExtent.Height,
                    1);
            }

            Framebuffer[] frameBuffers = swapChainRecord.SwapChainImage!.Select(x => MakeFrameBuffer(x.View!)).ToArray();

            //commandPool.Reset(CommandPoolResetFlags.ReleaseResources); // not needed!, releases currently used Resources

            CommandBuffer[] renderBuffers = device.AllocateCommandBuffers(commandPool, CommandBufferLevel.Primary, (uint)frameBuffers!.Length);

            return new(commandPool, frameBuffers, renderBuffers);
        }
    }
}
