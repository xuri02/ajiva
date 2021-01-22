﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Entitys;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.RenderEngine.Engine;
using ajiva.Systems.RenderEngine.EngineManagers;
using GlmSharp;
using SharpVk;
using SharpVk.Khronos;
using SharpVk.Multivendor;

// ReSharper disable once CheckNamespace
namespace ajiva.Systems.RenderEngine
{
    public class AjivaRenderEngine : IRenderEngine, IDisposable
    {
        public AjivaRenderEngine(Instance instance)
        {
            Instance = instance;
            DeviceComponent = new(this);
            SwapChainComponent = new(this);
            ImageComponent = new(this);
            Window = new(this);
            GraphicsComponent = new(this);
            ShaderComponent = new(this);
            AEntityComponent = new(this);
            SemaphoreComponent = new(this);
            TextureComponent = new(this);
        }

        //public static Instance? Instance { get; set; }
        /// <inheritdoc />
        public bool Runing { get; set; }
        /// <inheritdoc />
        public Instance? Instance { get; set; }
        /// <inheritdoc />
        public DeviceComponent DeviceComponent { get; }
        /// <inheritdoc />
        public SwapChainComponent SwapChainComponent { get; }
        /// <inheritdoc />
        public PlatformWindow Window { get; }
        /// <inheritdoc />
        public ImageComponent ImageComponent { get; }
        /// <inheritdoc />
        public GraphicsComponent GraphicsComponent { get; }
        /// <inheritdoc />
        public ShaderComponent ShaderComponent { get; }
        /// <inheritdoc />
        public AEntityComponent AEntityComponent { get; }
        /// <inheritdoc />
        public SemaphoreComponent SemaphoreComponent { get; }
        /// <inheritdoc />
        public TextureComponent TextureComponent { get; }

        /// <inheritdoc />
        public event PlatformEventHandler OnFrame;
        /// <inheritdoc />
        public event PlatformEventHandler OnUpdate;
        /// <inheritdoc />
        public event KeyEventHandler OnKeyEvent;
        /// <inheritdoc />
        public event EventHandler OnResize;
        /// <inheritdoc />
        public event EventHandler<vec2> OnMouseMove;
        /// <inheritdoc />
        public Cameras.Camera MainCamara
        {
            get => mainCamara;
            set
            {
                MainCamara?.Dispose();
                mainCamara = value;
            }
        }
        /// <inheritdoc />
        public object RenderLock { get; } = new();
        /// <inheritdoc />
        public object UpdateLock { get; } = new();

        #region Public

        #region Vars

        private static readonly DebugReportCallbackDelegate DebugReportDelegate = DebugReport;

        public long InitialTimestamp;

        #endregion

        private static Bool32 DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong @object, HostSize location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Console.WriteLine($"[{flags}] ({objectType}) {layerPrefix}:\n{message}");

            return false;
        }

  #endregion

        public static (Instance instance, DebugReportCallback debugReportCallback) CreateInstance(IEnumerable<string> enabledExtensionNames)
        {
            //if (Instance != null) return;

            List<string> enabledLayers = new();

            var props = Instance.EnumerateLayerProperties();

            void AddAvailableLayer(string layerName)
            {
                if (props.Any(x => x.LayerName == layerName))
                    enabledLayers.Add(layerName);
            }

            AddAvailableLayer("VK_LAYER_LUNARG_standard_validation");
            AddAvailableLayer("VK_LAYER_KHRONOS_validation");
            AddAvailableLayer("VK_LAYER_GOOGLE_unique_objects");
            //AddAvailableLayer("VK_LAYER_LUNARG_api_dump");
            AddAvailableLayer("VK_LAYER_LUNARG_core_validation");
            AddAvailableLayer("VK_LAYER_LUNARG_image");
            AddAvailableLayer("VK_LAYER_LUNARG_object_tracker");
            AddAvailableLayer("VK_LAYER_LUNARG_parameter_validation");
            AddAvailableLayer("VK_LAYER_LUNARG_swapchain");
            AddAvailableLayer("VK_LAYER_GOOGLE_threading");

            var instance = Instance.Create(
                enabledLayers.ToArray(),
                enabledExtensionNames.Append(ExtExtensions.DebugReport).ToArray(),
                applicationInfo: new ApplicationInfo
                {
                    ApplicationName = "ajiva",
                    ApplicationVersion = new(0, 0, 1),
                    EngineName = "ajiva-engine",
                    EngineVersion = new(0, 0, 1),
                    ApiVersion = new(1, 0, 0)
                });

            var debugReportCallback = instance.CreateDebugReportCallback(DebugReportDelegate, DebugReportFlags.Error | DebugReportFlags.Warning | DebugReportFlags.PerformanceWarning);

            //foreach (var extension in SharpVk.Instance.EnumerateExtensionProperties())
            //    Console.WriteLine($"Extension available: {extension.ExtensionName}");
            //
            //foreach (var layer in SharpVk.Instance.EnumerateLayerProperties())
            //    Console.WriteLine($"Layer available: {layer.LayerName}, {layer.Description}");

            return (instance, debugReportCallback);
        }

        private Cameras.Camera mainCamara;

        private void UpdateCamaraProjView()
        {
            ShaderComponent.ViewProj.UpdateExpresion(delegate(int index, ref UniformViewProj value)
            {
                if (index != 0) return;

                value.View = MainCamara.View;
                value.Proj = MainCamara.Projection;
                value.Proj[1, 1] *= -1;
            });
            ShaderComponent.ViewProj.Copy();
        }

        private void DrawFrame()
        {
            ATrace.Assert(SwapChainComponent.SwapChain != null, "SwapChainComponent.SwapChain != null");
            var nextImage = SwapChainComponent.SwapChain.AcquireNextImage(uint.MaxValue, SemaphoreComponent.ImageAvailable, null);

            var si = new SubmitInfo
            {
                CommandBuffers = new[]
                {
                    DeviceComponent.CommandBuffers![nextImage]
                },
                SignalSemaphores = new[]
                {
                    SemaphoreComponent.RenderFinished
                },
                WaitDestinationStageMask = new[]
                {
                    PipelineStageFlags.ColorAttachmentOutput
                },
                WaitSemaphores = new[]
                {
                    SemaphoreComponent.ImageAvailable
                }
            };
            DeviceComponent.GraphicsQueue!.Submit(si, null);
            var result = new Result[1];
            DeviceComponent.PresentQueue.Present(SemaphoreComponent.RenderFinished, SwapChainComponent.SwapChain, nextImage, result);
            si.SignalSemaphores = null!;
            si.WaitSemaphores = null!;
            si.WaitDestinationStageMask = null;
            si.CommandBuffers = null;
            result = null;
            si = default;
        }
    }
}
