using System;
using System.Collections.Generic;
using UnityEngine;
using Ubongo.Application.Bootstrap;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode.Shared
{
    public sealed class DependencyFixtureBuilder
    {
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();

        public static DependencyFixtureBuilder CreateBaseline()
        {
            var builder = new DependencyFixtureBuilder();
            builder.Add<GameManager>("GameManager_Test");
            builder.Add<RoundManager>("RoundManager_Test");
            builder.Add<GemSystem>("GemSystem_Test");
            builder.Add<DifficultySystem>("DifficultySystem_Test");
            builder.Add<TiebreakerManager>("TiebreakerManager_Test");
            builder.Add<InputManager>("InputManager_Test");
            builder.Add<LevelGenerator>("LevelGenerator_Test");
            builder.Add<UIManager>("UIManager_Test");
            builder.Add<GameBoard>("GameBoard_Test");
            return builder;
        }

        public DependencyFixtureBuilder Remove<T>() where T : Component
        {
            Type key = typeof(T);
            if (!components.TryGetValue(key, out Component component))
            {
                return this;
            }

            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component.gameObject);
            }

            components.Remove(key);
            return this;
        }

        private void Add<T>(string objectName) where T : Component
        {
            GameObject gameObject = new GameObject(objectName);
            components[typeof(T)] = gameObject.AddComponent<T>();
        }
    }
}
