﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using ajiva.Engine;
using GlmSharp;
using SharpVk.Glfw;
using SharpVk.Khronos;
using Glfw3 = SharpVk.Glfw.Glfw3;
using Key = SharpVk.Glfw.Key;

namespace ajiva.EngineManagers
{
    public class PlatformWindow : IPlatformWindow, IEngineManager
    {
        private readonly IEngine engine;
        public event PlatformEventHandler OnFrame;
        public event KeyEventHandler OnKeyEvent;
        public event EventHandler OnResize;
        public event EventHandler<vec2> OnMouseMove;
        public Surface Surface { get; private set; }

        public vec2 PreviousMousePosition = vec2.Zero;

        public PlatformWindow(IEngine engine)
        {
            this.engine = engine;
        }

        private WindowHandle window;

        public void CreateSurface()
        {
            Surface = engine.Instance.CreateGlfw3Surface(window);
        }

        public void InitWindow(int surfaceWidth, int surfaceHeight)
        {
            Width = surfaceWidth;
            Height = surfaceHeight;

            Glfw3.Init();

            Glfw3.WindowHint(WindowAttribute.ClientApi, 0);
            window = Glfw3.CreateWindow(surfaceWidth, surfaceHeight, "First test", MonitorHandle.Zero, WindowHandle.Zero);
            Glfw3.SetWindowSizeCallback(window, (a, w, h) =>
            {
                Height = h;
                Width = w;

                OnResize.Invoke(this, EventArgs.Empty);
            });

            //glfw3.Glfw3.Public.SetWindowSizeLimits_0(window.RawHandle, surfaceWidth / 2, surfaceHeight / 2, 0, 0);
            Glfw3.SetKeyCallback(window, KeyCallback);
            Glfw3.SetCursorPosCallback(window, MouseCallback);
            UpdateCursor();
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public uint SurfaceWidth => (uint)Width;
        public uint SurfaceHeight => (uint)Height;

        private void MouseCallback(WindowHandle windowHandle, double xPosition, double yPosition)
        {
            if (!mouseMotion) return;

            var mousePos = new vec2((float)xPosition, (float)yPosition);

            if (mousePos == PreviousMousePosition)
                return;

            OnMouseMove.Invoke(this, -(PreviousMousePosition - mousePos));
            PreviousMousePosition = mousePos;
        }

        private bool mouseMotion = true;

        private void KeyCallback(WindowHandle windowHandle, Key key, int scancode, InputAction inputAction, Modifier modifiers)
        {
            if (key == Key.Escape) //todo dev only
            {
                Environment.Exit(0);
            }

            if (key == Key.Escape && inputAction == InputAction.Release)
            {
                mouseMotion = !mouseMotion;
                UpdateCursor();
                Console.WriteLine($"mouseMotion: {mouseMotion}");
            }
            OnKeyEvent.Invoke(this, key, scancode, inputAction, modifiers);
        }

        private void UpdateCursor()
        {
            Glfw3.SetInputMode(window, Glfw3Enum.GLFW_CURSOR, mouseMotion ? Glfw3Enum.GLFW_CURSOR_DISABLED : Glfw3Enum.GLFW_CURSOR_NORMAL);
        }

        public void MainLoop()
        {
            var delta = TimeSpan.Zero;
            var now = Stopwatch.GetTimestamp();
            while (!Glfw3.WindowShouldClose(window))
            {
                OnFrame.Invoke(this, delta);

                Glfw3.PollEvents();

                if (mouseMotion)
                {
                    Glfw3.SetCursorPosition(window, Width / 2f, Height / 2f);
                    PreviousMousePosition = new(Width / 2f, Height / 2f);
                }

                var end = Stopwatch.GetTimestamp();
                delta = new(end - now);

                now = end;
            }
        }

        public void CloseWindow()
        {
            Glfw3.DestroyWindow(window);

            Glfw3.Terminate();
        }

        public IEnumerable<string> GetRequiredInstanceExtensions()
        {
            return Glfw3.GetRequiredInstanceExtensions();
        }

        private void Dispose(bool disposing)
        {
            CloseWindow();
            if (disposing)
            {
                Surface.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PlatformWindow()
        {
            Dispose(false);
        }
    }
    public interface IPlatformWindow : IDisposable
    {
        public void InitWindow(int surfaceWidth, int surfaceHeight);
        public IEnumerable<string> GetRequiredInstanceExtensions();
        public void CreateSurface();
        public void MainLoop();
        public void CloseWindow();
        public event PlatformEventHandler OnFrame;
        public event KeyEventHandler OnKeyEvent;
        public event EventHandler OnResize;
        public event EventHandler<vec2> OnMouseMove;

        public Surface Surface { get; }
        uint SurfaceWidth { get; }
        uint SurfaceHeight { get; }
    }

    public delegate void KeyEventHandler(object? sender, Key key, int scancode, InputAction inputAction, Modifier modifiers);

    public delegate void PlatformEventHandler(object sender, TimeSpan delta);
}