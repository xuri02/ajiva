﻿using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Engine;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class GraphicsLayout : RenderEngineComponent, IThreadSaveCreatable
    {
        private PipelineLayout? PipelineLayout { get; set; }
        private RenderPass? RenderPass { get; set; }
        private Pipeline? Pipeline { get; set; }

        private DescriptorPool? DescriptorPool { get; set; }
        private DescriptorSetLayout? DescriptorSetLayout { get; set; }
        private DescriptorSet? DescriptorSet { get; set; }
        

        
        private Framebuffer[]? FrameBuffers { get; set; }
        public CommandBuffer[]? CommandBuffers { get; private set; }
        public GraphicsLayout(IRenderEngine renderEngine) : base(renderEngine)
        {
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            PipelineLayout?.Dispose();
            RenderPass?.Dispose();
            Pipeline?.Dispose();
            DescriptorPool?.Dispose();
            DescriptorSetLayout?.Dispose();

            if (FrameBuffers != null)
                foreach (var frameBuffer in FrameBuffers)
                    frameBuffer.Dispose();
            FrameBuffers = null;

            RenderEngine.DeviceComponent.CommandPool?.FreeCommandBuffers(CommandBuffers);
            CommandBuffers = null;
        }

        private void CreateGraphicsPipeline()
        {
            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            PipelineLayout = RenderEngine.DeviceComponent.Device!.CreatePipelineLayout(DescriptorSetLayout, null);

            Pipeline = RenderEngine.DeviceComponent.Device.CreateGraphicsPipelines(null, new[]
            {
                new GraphicsPipelineCreateInfo
                {
                    Layout = PipelineLayout,
                    RenderPass = RenderPass,
                    Subpass = 0,
                    VertexInputState = new()
                    {
                        VertexBindingDescriptions = new[]
                        {
                            bindingDescription
                        },
                        VertexAttributeDescriptions = attributeDescriptions
                    },
                    InputAssemblyState = new()
                    {
                        PrimitiveRestartEnable = false,
                        Topology = PrimitiveTopology.TriangleList
                    },
                    ViewportState = new()
                    {
                        Viewports = new[]
                        {
                            new Viewport
                            {
                                X = 0f,
                                Y = 0f,
                                Width = RenderEngine.SwapChainComponent.SwapChainExtent!.Value.Width,
                                Height = RenderEngine.SwapChainComponent.SwapChainExtent!.Value.Height,
                                MaxDepth = 1,
                                MinDepth = 0,
                            }
                        },
                        Scissors = new[]
                        {
                            new Rect2D
                            {
                                Offset = Offset2D.Zero,
                                Extent = RenderEngine.SwapChainComponent.SwapChainExtent!.Value
                            }
                        }
                    },
                    RasterizationState = new()
                    {
                        DepthClampEnable = false,
                        RasterizerDiscardEnable = false,
                        PolygonMode = PolygonMode.Fill,
                        LineWidth = 1,
                        //CullMode = CullModeFlags.Back,
                        //FrontFace = FrontFace.CounterClockwise,
                        DepthBiasEnable = false
                    },
                    MultisampleState = new()
                    {
                        SampleShadingEnable = false,
                        RasterizationSamples = SampleCountFlags.SampleCount1,
                        MinSampleShading = 1
                    },
                    ColorBlendState = new()
                    {
                        Attachments = new[]
                        {
                            new PipelineColorBlendAttachmentState
                            {
                                ColorWriteMask = ColorComponentFlags.R
                                                 | ColorComponentFlags.G
                                                 | ColorComponentFlags.B
                                                 | ColorComponentFlags.A,
                                BlendEnable = false,
                                SourceColorBlendFactor = BlendFactor.One,
                                DestinationColorBlendFactor = BlendFactor.Zero,
                                ColorBlendOp = BlendOp.Add,
                                SourceAlphaBlendFactor = BlendFactor.One,
                                DestinationAlphaBlendFactor = BlendFactor.Zero,
                                AlphaBlendOp = BlendOp.Add
                            }
                        },
                        LogicOpEnable = false,
                        LogicOp = LogicOp.Copy,
                        BlendConstants = (0, 0, 0, 0)
                    },
                    Stages = new[]
                    {
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Vertex,
                            Module = RenderEngine.ShaderComponent.Main!.VertShader,
                            Name = "main"
                        },
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Fragment,
                            Module = RenderEngine.ShaderComponent.Main!.FragShader,
                            Name = "main"
                        }
                    },
                    DepthStencilState = new()
                    {
                        DepthTestEnable = true,
                        DepthWriteEnable = true,
                        DepthCompareOp = CompareOp.Less,
                        DepthBoundsTestEnable = false,
                        MinDepthBounds = 0,
                        MaxDepthBounds = 1,
                        StencilTestEnable = false,
                        Back = new(),
                        Flags = new(),
                    }
                }
            }).Single();
        }

        private void CreateRenderPass()
        {
            RenderPass = RenderEngine.DeviceComponent.Device!.CreateRenderPass(
                new AttachmentDescription[]
                {
                    new()
                    {
                        Format = RenderEngine.SwapChainComponent.SwapChainFormat!.Value,
                        Samples = SampleCountFlags.SampleCount1,
                        LoadOp = AttachmentLoadOp.Clear,
                        StoreOp = AttachmentStoreOp.Store,
                        StencilLoadOp = AttachmentLoadOp.DontCare,
                        StencilStoreOp = AttachmentStoreOp.DontCare,
                        InitialLayout = ImageLayout.Undefined,
                        FinalLayout = ImageLayout.PresentSource
                    },
                    new()
                    {
                        Format = RenderEngine.ImageComponent.FindDepthFormat(),
                        Samples = SampleCountFlags.SampleCount1,
                        LoadOp = AttachmentLoadOp.Clear,
                        StoreOp = AttachmentStoreOp.DontCare,
                        StencilLoadOp = AttachmentLoadOp.DontCare,
                        StencilStoreOp = AttachmentStoreOp.DontCare,
                        InitialLayout = ImageLayout.Undefined,
                        FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                    }
                },
                new SubpassDescription
                {
                    DepthStencilAttachment = new(1, ImageLayout.DepthStencilAttachmentOptimal),
                    PipelineBindPoint = PipelineBindPoint.Graphics,
                    ColorAttachments = new[]
                    {
                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                    }
                },
                new[]
                {
                    new SubpassDependency
                    {
                        SourceSubpass = Constants.SubpassExternal,
                        DestinationSubpass = 0,
                        SourceStageMask = PipelineStageFlags.BottomOfPipe,
                        SourceAccessMask = AccessFlags.MemoryRead,
                        DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                        DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead
                    },
                    new SubpassDependency
                    {
                        SourceSubpass = 0,
                        DestinationSubpass = Constants.SubpassExternal,
                        SourceStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                        SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead,
                        DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                        DestinationAccessMask = AccessFlags.MemoryRead
                    },
                });
        }

        private void CreateDescriptorSetLayout()
        {
            DescriptorSetLayout = RenderEngine.DeviceComponent.Device!.CreateDescriptorSetLayout(
                new DescriptorSetLayoutBinding[]
                {
                    new()
                    {
                        Binding = 0,
                        DescriptorType = DescriptorType.UniformBuffer,
                        StageFlags = ShaderStageFlags.Vertex,
                        DescriptorCount = 1
                    },
                    new()
                    {
                        Binding = 1,
                        DescriptorType = DescriptorType.UniformBufferDynamic,
                        StageFlags = ShaderStageFlags.Vertex,
                        DescriptorCount = 1
                    },
                    new()
                    {
                        Binding = 2,
                        DescriptorCount = TextureComponent.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                        StageFlags = ShaderStageFlags.Fragment,
                    }
                });
        }

        private void CreateDescriptorPool()
        {
            DescriptorPool = RenderEngine.DeviceComponent.Device!.CreateDescriptorPool(
                10000,
                new DescriptorPoolSize[]
                {
                    new()
                    {
                        DescriptorCount = 1,
                        Type = DescriptorType.UniformBuffer
                    },
                    new()
                    {
                        DescriptorCount = 1,
                        Type = DescriptorType.UniformBufferDynamic
                    },
                    new()
                    {
                        DescriptorCount = TextureComponent.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        Type = DescriptorType.CombinedImageSampler
                    }
                });
        }

        private void CreateDescriptorSet()
        {
            DescriptorSet = RenderEngine.DeviceComponent.Device!.AllocateDescriptorSets(DescriptorPool, DescriptorSetLayout).Single();

            RenderEngine.DeviceComponent.Device.UpdateDescriptorSets(
                new WriteDescriptorSet[]
                {
                    new()
                    {
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = RenderEngine.ShaderComponent.ViewProj.Uniform.Buffer,
                                Offset = 0,
                                Range = RenderEngine.ShaderComponent.ViewProj.Uniform.SizeOfT
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 0,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.UniformBuffer
                    },
                    new()
                    {
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = RenderEngine.ShaderComponent.UniformModels.Uniform.Buffer,
                                Offset = 0,
                                Range = RenderEngine.ShaderComponent.UniformModels.Uniform.SizeOfT
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 1,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.UniformBufferDynamic
                    },
                    new()
                    {
                        ImageInfo = RenderEngine.TextureComponent.TextureSamplerImageViews,
                        DescriptorCount = TextureComponent.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 2,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                    }
                }, null);
        }

        private void CreateCommandBuffers()
        {
            RenderEngine.DeviceComponent.EnsureCommandPoolsExists();

            RenderEngine.DeviceComponent.CommandPool!.Reset(CommandPoolResetFlags.ReleaseResources);

            CommandBuffers ??= RenderEngine.DeviceComponent.Device!.AllocateCommandBuffers(RenderEngine.DeviceComponent.CommandPool, CommandBufferLevel.Primary, (uint)FrameBuffers!.Length);

            for (var index = 0; index < FrameBuffers!.Length; index++)
            {
                var commandBuffer = CommandBuffers[index];

                commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);

                commandBuffer.BeginRenderPass(RenderPass,
                    FrameBuffers[index],
                    new(new(), RenderEngine.SwapChainComponent.SwapChainExtent!.Value),
                    new ClearValue[]
                    {
                        new ClearColorValue(.1f, .1f, .1f, 1), new ClearDepthStencilValue(1, 0)
                    },
                    SubpassContents.Inline);

                commandBuffer.BindPipeline(PipelineBindPoint.Graphics, Pipeline);

                foreach (var (renderAble, _) in RenderEngine.ComponentEntityMap.Where(x => x.Key.Render))
                {
                    ATrace.Assert(renderAble.Mesh != null, "renderAble.Mesh != null");
                    renderAble.Mesh.Bind(commandBuffer);

                    commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, PipelineLayout, 0, DescriptorSet, renderAble.Id * (uint)Unsafe.SizeOf<UniformModel>());

                    renderAble.Mesh.DrawIndexed(commandBuffer);
                }

                commandBuffer.EndRenderPass();

                commandBuffer.End();
            }
        }

        private void CreateFrameBuffers()
        {
            Framebuffer Create(ImageView imageView) => RenderEngine.DeviceComponent.Device!.CreateFramebuffer(RenderPass,
                new[]
                {
                    imageView, RenderEngine.ImageComponent.DepthImage!.View
                },
                RenderEngine.SwapChainComponent.SwapChainExtent!.Value.Width,
                RenderEngine.SwapChainComponent.SwapChainExtent!.Value.Height,
                1);

            FrameBuffers ??= RenderEngine.SwapChainComponent.SwapChainImage!.Select(x => Create(x.View!)).ToArray();
        }

        /// <inheritdoc />
        public bool Created { get; private set; }

        public void EnsureExists()
        {
            if (Created) return;

            RenderEngine.DeviceComponent.EnsureDevicesExist();
            RenderEngine.SwapChainComponent.EnsureSwapChainExists();
            RenderEngine.ShaderComponent.EnsureShaderModulesExists();
            RenderEngine.ShaderComponent.UniformModels.EnsureExists();
            RenderEngine.ShaderComponent.ViewProj.EnsureExists();
            RenderEngine.TextureComponent.EnsureDefaultImagesExists();

            CreateRenderPass();

            CreateDescriptorSetLayout();
            CreateGraphicsPipeline();

            CreateDescriptorPool();
            CreateDescriptorSet();

            CreateFrameBuffers();
            CreateCommandBuffers();

            Created = true;
        }
    }
}