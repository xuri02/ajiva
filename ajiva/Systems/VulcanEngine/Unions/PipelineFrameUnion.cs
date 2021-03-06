﻿using ajiva.Helpers;

namespace ajiva.Systems.VulcanEngine.Unions
{
    public class PipelineFrameUnion : DisposingLogger
    {
        public GraphicsPipelineUnion PipelineUnion { get; }
        public FrameBufferUnion FrameBuffer { get; }

        public PipelineFrameUnion(GraphicsPipelineUnion pipelineUnion, FrameBufferUnion frameBuffer)
        {
            PipelineUnion = pipelineUnion;
            FrameBuffer = frameBuffer;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            PipelineUnion.Dispose();
            FrameBuffer.Dispose();
        }
    };
}
