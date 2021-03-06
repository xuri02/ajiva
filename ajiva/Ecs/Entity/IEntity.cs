﻿using System;
using ajiva.Ecs.Component;
using ajiva.Helpers;

namespace ajiva.Ecs.Entity
{
    public interface IEntity : IDisposable
    {
        uint Id { get; init; }

        bool TryGetComponent<T>(out T? value) where T : class, IComponent;
        T? GetComponent<T>() where T : class, IComponent;
        bool HasComponent<T>() where T : class, IComponent;
        void Update(UpdateInfo delta);
        void AddComponent<T>(T component) where T : class, IComponent;
        void RemoveComponent<T>() where T : class, IComponent;
    }
}
